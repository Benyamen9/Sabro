#!/usr/bin/env node
// Imports candidate Lexicon entries from the SEDRA IV API (Beth Mardutho: The
// Syriac Institute — https://sedra.bethmardutho.org/api, Apache-2.0 licensed,
// public, unauthenticated, CORS-open) and creates them as Draft entries in
// Sabro through the validated admin API (the same write path the backoffice
// uses). Attribution is rendered sitewide in the footer (site.attribution).
//
// SEDRA has no Dutch/German/Swedish glosses, so imported drafts always carry
// only en/fr meanings (whichever SEDRA actually has — many entries have
// neither) — nl/de/sv (and any vocalization/root correction) are left for
// the Owner to complete in the backoffice. A Draft entry can never be
// published, played, or served to clients until all five required
// languages are present and the Owner explicitly publishes it (see
// LexiconEntry.Publish) — that gate IS the "not verified" marker for a bulk
// import; no separate schema field is used. Vocalization choices
// (qushoyo/rukkokho) are per-word lexical judgment calls, not derived here —
// SEDRA's own "western" spelling is taken as-is, not reprocessed.
//
// For each entry (a SEDRA lexeme id, Syriac consonantal text to resolve via
// SEDRA's word search, or every id in a --range) it:
//   1. Resolves a SEDRA lexeme id (direct, or via GET /api/word/{text},
//      preferring the match with isLexicalForm === true; logs and skips on
//      ambiguity so the Owner can disambiguate with an explicit lexeme id).
//   2. Fetches GET /api/lexeme/{id} for glosses, category, and the root ref.
//   3. Fetches GET /api/root/{id} (if present) and ensures it exists in Sabro
//      via POST /api/v1/lexicon-roots { form } (idempotent on form; a shared
//      in-flight cache dedupes concurrent workers racing the same root).
//   4. Creates the entry as Draft via POST /api/v1/admin/lexicon with
//      syriacUnvocalized, syriacVocalized (SEDRA's western spelling),
//      grammaticalCategory (mapped from SEDRA's CategoryType), rootId, and
//      whichever of en/fr meanings SEDRA has (may be neither — Draft allows
//      an empty meanings array).
//
// Never publishes and never sets playableInMeltho — that stays a deliberate
// Owner action once the entry is reviewed and completed.
//
// Idempotent: skips any Syriac form that already exists as a Sabro entry, so
// interrupting and re-running (e.g. after a crash on hour 6 of a full-lexicon
// import) is always safe — it picks up where it left off.
//
// Designed to run unattended for many hours against production for a full
// (--range 1-38812-scale) import:
//   - Self-throttles all Sabro API calls to stay under the server's rate
//     limiter (100 req/min per caller identity), independent of --concurrency.
//   - Retries 429/5xx/network errors with exponential backoff, honoring a
//     Retry-After header when present.
//   - Refreshes the Sabro access token on demand (a single static token
//     would expire long before a multi-hour run finishes) when client-
//     credential env vars are supplied; falls back to a fixed token
//     otherwise (fine for short curated-list runs).
//   - Processes entries with bounded concurrency on the SEDRA-fetch side
//     (Sabro calls are still serialized through the shared rate limiter, so
//     concurrency shortens wall-clock time without exceeding the API's
//     budget) and prints a periodic progress summary instead of one line
//     per entry, so a long run's log stays readable.
//
// Usage:
//   SABRO_ADMIN_TOKEN=<jwt> node scripts/import-sedra.mjs [--dry-run] [--file <path> | --range <start>-<end>] [--api <url>] [--delay <ms>] [--concurrency <n>] [--sabro-rate <perMin>]
//
// Env / flags:
//   SABRO_ADMIN_TOKEN         a Logto access token with api:v1:admin (+read/write)
//                             scope. Fine for short runs; for a multi-hour run
//                             prefer the client-credential env vars below so the
//                             script can refresh the token itself.
//   SABRO_ADMIN_CLIENT_ID     M2M client id — enables automatic token refresh.
//   SABRO_ADMIN_CLIENT_SECRET M2M client secret.
//   SABRO_TOKEN_URL           OIDC token endpoint (e.g. https://auth.sabro.be/oidc/token).
//   SABRO_API_RESOURCE        the API resource identifier (audience) to request.
//   SABRO_API_URL             Sabro API base URL (default http://localhost:5082); --api overrides
//   --file <path>             input list path (JSON {entries:[...]})
//   --range <start>-<end>     generate every SEDRA lexeme id in the (inclusive) range
//                             instead of reading a file — for a full-lexicon import
//   --dry-run                 resolve + fetch from SEDRA and print the planned drafts; no Sabro API calls
//   --delay <ms>              pause between SEDRA calls within one entry, courtesy rate-limiting (default 150)
//   --concurrency <n>         entries processed in parallel on the SEDRA-fetch side (default 6)
//   --sabro-rate <perMin>     self-imposed cap on Sabro API calls per minute (default 90, server limit is 100)
//   --progress-every <n>      print a summary line every n entries processed (default 200)

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const nfc = (s) => (s ?? '').normalize('NFC');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const SEDRA_API = 'https://sedra.bethmardutho.org/api';

// Known SEDRA data-entry slips: a handful of "western" transliterations use
// an Arabic combining mark where the visually-identical Syriac one belongs
// (e.g. lexeme 13698's ftoho written with Arabic fatha U+064E instead of
// Syriac pthaha U+0730). These are one-for-one visual mix-ups in SEDRA's own
// data, not a Syriac orthography convention — fixed here rather than by
// loosening what Sabro's validator accepts as Syriac.
const SEDRA_MARK_FIXUPS = new Map([
  [0x064e, 0x0730], // ARABIC FATHA -> SYRIAC PTHAHA (ftoho)
]);
function fixSedraMarks(s) {
  return [...s].map((ch) => {
    const fixed = SEDRA_MARK_FIXUPS.get(ch.codePointAt(0));
    return fixed ? String.fromCodePoint(fixed) : ch;
  }).join('');
}

// SEDRA's CategoryType -> Sabro's GrammaticalCategory enum. SEDRA has no
// "conjunction" category (folded into "particle" there); anything not listed
// falls back to "Other".
const CATEGORY_MAP = {
  'adjective': 'Adjective',
  'adjective of place': 'Adjective',
  'participle adjective': 'Adjective',
  'adverb': 'Adverb',
  'adverb ending with aiyt': 'Adverb',
  'noun': 'Noun',
  'substantive': 'Noun',
  'proper noun': 'Noun',
  "proper noun (individual's name; e.g. ephrem)": 'Noun',
  'proper noun (place name)': 'Noun',
  'proper noun (nations; e.g. huns)': 'Noun',
  'demonym': 'Noun',
  'verb': 'Verb',
  'pronoun': 'Pronoun',
  'preposition': 'Preposition',
  'particle': 'Particle',
  'numeral': 'Numeral',
  'interjection': 'Interjection',
  'denominative': 'Other',
  'idiom': 'Other',
};

function mapCategory(sedraCategory) {
  const key = (sedraCategory ?? '').toLowerCase().replace(/&#039;/g, "'");
  return CATEGORY_MAP[key] ?? 'Other';
}

function parseArgs(argv) {
  const args = {
    dryRun: false,
    file: null,
    range: null,
    api: process.env.SABRO_API_URL || 'http://localhost:5082',
    delay: 150,
    concurrency: 6,
    sabroRatePerMin: 90,
    progressEvery: 200,
  };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--dry-run') args.dryRun = true;
    else if (a === '--file') args.file = resolve(process.cwd(), argv[++i]);
    else if (a === '--range') args.range = argv[++i];
    else if (a === '--api') args.api = argv[++i];
    else if (a === '--delay') args.delay = Number(argv[++i]);
    else if (a === '--concurrency') args.concurrency = Number(argv[++i]);
    else if (a === '--sabro-rate') args.sabroRatePerMin = Number(argv[++i]);
    else if (a === '--progress-every') args.progressEvery = Number(argv[++i]);
    else throw new Error(`Unknown argument: ${a}`);
  }
  if (!args.file && !args.range) args.file = resolve(here, 'sedra-import-list.json');
  return args;
}

function buildEntryList(args) {
  if (args.range) {
    const m = /^(\d+)-(\d+)$/.exec(args.range);
    if (!m) throw new Error(`--range must look like "1-38812", got "${args.range}"`);
    const [, startStr, endStr] = m;
    const start = Number(startStr);
    const end = Number(endStr);
    if (!(start >= 1) || !(end >= start)) throw new Error(`Invalid --range "${args.range}"`);
    const entries = [];
    for (let id = start; id <= end; id++) entries.push(String(id));
    return entries;
  }
  return null; // caller falls back to reading args.file
}

// --- HTTP with retry/backoff ------------------------------------------------

const RETRYABLE_STATUS = new Set([408, 429, 500, 502, 503, 504]);

async function fetchWithRetry(url, options, { retries = 5, baseDelayMs = 2000, maxDelayMs = 60000, label } = {}) {
  let attempt = 0;
  for (;;) {
    let res;
    try {
      res = await fetch(url, options);
    } catch (err) {
      attempt++;
      if (attempt > retries) throw err;
      const delay = Math.min(baseDelayMs * 2 ** (attempt - 1), maxDelayMs);
      console.error(`  ~ ${label ?? url}: network error (${err.message}), retry ${attempt}/${retries} in ${delay}ms`);
      await sleep(delay);
      continue;
    }
    if (!RETRYABLE_STATUS.has(res.status)) return res;
    attempt++;
    if (attempt > retries) return res;
    const retryAfterHeader = res.headers.get('retry-after');
    const retryAfterMs = retryAfterHeader ? Number(retryAfterHeader) * 1000 : NaN;
    const delay = Number.isFinite(retryAfterMs) ? retryAfterMs : Math.min(baseDelayMs * 2 ** (attempt - 1), maxDelayMs);
    console.error(`  ~ ${label ?? url}: ${res.status}, retry ${attempt}/${retries} in ${delay}ms`);
    await sleep(delay);
  }
}

// --- Sabro token management (auto-refresh for multi-hour runs) -------------

class TokenManager {
  constructor({ staticToken, clientId, clientSecret, tokenUrl, resource }) {
    this.staticToken = staticToken;
    this.clientId = clientId;
    this.clientSecret = clientSecret;
    this.tokenUrl = tokenUrl;
    this.resource = resource;
    this.token = staticToken ?? null;
    this.expiresAt = staticToken ? Infinity : 0; // static token: assume caller-managed lifetime
    this.refreshPromise = null;
  }

  canRefresh() {
    return Boolean(this.clientId && this.clientSecret && this.tokenUrl && this.resource);
  }

  async getToken() {
    if (this.token && Date.now() < this.expiresAt - 120_000) return this.token;
    if (!this.canRefresh()) {
      if (!this.token) throw new Error('No Sabro token available and no client credentials configured to mint one.');
      return this.token; // static token past our best-guess expiry; let the caller's 401 handling force a retry
    }
    // Coalesce concurrent refreshers into a single request.
    this.refreshPromise ??= this.refresh();
    try {
      return await this.refreshPromise;
    } finally {
      this.refreshPromise = null;
    }
  }

  async refresh() {
    const body = new URLSearchParams({
      grant_type: 'client_credentials',
      client_id: this.clientId,
      client_secret: this.clientSecret,
      resource: this.resource,
      scope: 'api:v1:admin api:v1:write api:v1:read',
    });
    const res = await fetchWithRetry(
      this.tokenUrl,
      { method: 'POST', headers: { 'content-type': 'application/x-www-form-urlencoded' }, body },
      { label: 'token refresh' },
    );
    if (!res.ok) throw new Error(`Token refresh failed: ${res.status} ${await res.text()}`);
    const json = await res.json();
    this.token = json.access_token;
    this.expiresAt = Date.now() + (json.expires_in ?? 3600) * 1000;
    console.log(`  * refreshed Sabro token, expires in ${json.expires_in ?? 3600}s`);
    return this.token;
  }

  forceExpire() {
    this.expiresAt = 0;
  }
}

// --- Rate limiter (paces all Sabro calls to stay under the server limit) ---

class RateLimiter {
  constructor(perMinute) {
    this.minIntervalMs = 60_000 / perMinute;
    this.nextSlot = 0;
    this.queue = Promise.resolve();
  }

  async acquire() {
    // Serialize slot assignment so concurrent callers don't race the same gap.
    const mySlot = (this.queue = this.queue.then(() => {
      const now = Date.now();
      const slot = Math.max(now, this.nextSlot);
      this.nextSlot = slot + this.minIntervalMs;
      return slot;
    }));
    const slot = await mySlot;
    const wait = slot - Date.now();
    if (wait > 0) await sleep(wait);
  }
}

// --- Bounded concurrency pool -----------------------------------------------

async function runPool(items, concurrency, worker) {
  let cursor = 0;
  async function run() {
    for (;;) {
      const i = cursor++;
      if (i >= items.length) return;
      await worker(items[i], i);
    }
  }
  await Promise.all(Array.from({ length: Math.min(concurrency, items.length) }, run));
}

async function sedraFetch(path, delayMs) {
  const res = await fetchWithRetry(`${SEDRA_API}${path}`, { headers: { accept: 'application/json' } }, { label: `SEDRA ${path}` });
  if (delayMs > 0) await sleep(delayMs);
  if (res.status === 404) return [];
  if (!res.ok) throw new Error(`SEDRA GET ${path} -> ${res.status} ${res.statusText}`);
  const body = await res.json();
  return Array.isArray(body) ? body : [body];
}

async function sabroFetch(ctx, method, path, body) {
  await ctx.rateLimiter.acquire();
  const token = await ctx.tokenManager.getToken();
  const doRequest = async (bearer) =>
    fetchWithRetry(
      `${ctx.api}${path}`,
      {
        method,
        headers: { 'content-type': 'application/json', authorization: `Bearer ${bearer}` },
        body: body === undefined ? undefined : JSON.stringify(body),
      },
      { label: `${method} ${path}` },
    );

  let res = await doRequest(token);
  if (res.status === 401 && ctx.tokenManager.canRefresh()) {
    ctx.tokenManager.forceExpire();
    const fresh = await ctx.tokenManager.getToken();
    res = await doRequest(fresh);
  }
  const text = await res.text();
  let json;
  try { json = text ? JSON.parse(text) : undefined; } catch { json = undefined; }
  if (!res.ok) {
    const detail = json?.detail || json?.title || text || `${res.status} ${res.statusText}`;
    throw new Error(`${method} ${path} -> ${res.status}: ${detail}`);
  }
  return json;
}

async function listAll(ctx, path) {
  const items = [];
  const pageSize = 200;
  for (let page = 1; ; page++) {
    const result = await sabroFetch(ctx, 'GET', `${path}?page=${page}&pageSize=${pageSize}`);
    items.push(...result.items);
    if (page * pageSize >= result.total || result.items.length === 0) break;
  }
  return items;
}

// Resolves an input entry (a numeric SEDRA lexeme id, or Syriac consonantal
// text) to a single SEDRA lexeme id. Returns null (and logs) on no match or
// unresolved ambiguity.
async function resolveLexemeId(entry, delayMs) {
  if (/^\d+$/.test(entry)) return Number(entry);

  const matches = await sedraFetch(`/word/${encodeURIComponent(nfc(entry))}`, delayMs);
  if (matches.length === 0) {
    console.error(`  ! ${entry}: no SEDRA match found`);
    return null;
  }
  const lexical = matches.filter((m) => m.isLexicalForm === true || m.isLexicalForm === 'true');
  const candidates = lexical.length > 0 ? lexical : matches;
  const lexemeIds = [...new Set(candidates.map((m) => m.lexeme?.id).filter((id) => id != null))];
  if (lexemeIds.length !== 1) {
    console.error(`  ! ${entry}: ambiguous (${lexemeIds.length} candidate lexeme id(s): ${lexemeIds.join(', ')}) — resolve manually with an explicit lexeme id`);
    return null;
  }
  return lexemeIds[0];
}

// SEDRA gloss strings can embed raw HTML (e.g. "<span class=\"selectableFont\">...
// </span>" around an inline Syriac collocation example), HTML entities, and
// stray bidi control characters (marks/embeddings/isolates, U+200B-U+200F /
// U+202A-U+202E / U+2066-U+2069). Strip all of it so the stored text is plain,
// and let Sabro's own <SyriacText> component own directionality instead of
// carrying SEDRA's embedded marks. Code points are listed explicitly (not as a
// literal-character regex range) so the source stays plain ASCII.
const BIDI_CONTROL_RE = new RegExp(
  `[${
    [0x200b, 0x200c, 0x200d, 0x200e, 0x200f, 0x202a, 0x202b, 0x202c, 0x202d, 0x202e, 0x2066, 0x2067, 0x2068, 0x2069]
      .map((cp) => String.fromCodePoint(cp))
      .join('')
  }]`,
  'g',
);

function sanitizeGloss(text) {
  return text
    .replace(/<[^>]+>/g, '')
    .replace(/&#0?39;/g, "'")
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(BIDI_CONTROL_RE, '')
    .replace(/\s+/g, ' ')
    .trim();
}

function buildMeaning(glosses, sedraLanguage, sabroLanguage) {
  const values = (glosses?.[sedraLanguage] ?? []).map(sanitizeGloss).filter(Boolean);
  if (values.length === 0) return null;
  return { language: sabroLanguage, text: values.join('; ') };
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const rangeEntries = buildEntryList(args);
  const entries = rangeEntries ?? JSON.parse(await readFile(args.file, 'utf8')).entries ?? [];
  console.log(`Loaded ${entries.length} entries from ${args.range ? `range ${args.range}` : args.file}`);
  console.log(`concurrency=${args.concurrency} sabroRate=${args.sabroRatePerMin}/min delay=${args.delay}ms progressEvery=${args.progressEvery}`);

  const tokenManager = new TokenManager({
    staticToken: process.env.SABRO_ADMIN_TOKEN,
    clientId: process.env.SABRO_ADMIN_CLIENT_ID,
    clientSecret: process.env.SABRO_ADMIN_CLIENT_SECRET,
    tokenUrl: process.env.SABRO_TOKEN_URL,
    resource: process.env.SABRO_API_RESOURCE,
  });
  if (!args.dryRun && !tokenManager.staticToken && !tokenManager.canRefresh()) {
    console.error('Provide SABRO_ADMIN_TOKEN, or all of SABRO_ADMIN_CLIENT_ID/SABRO_ADMIN_CLIENT_SECRET/SABRO_TOKEN_URL/SABRO_API_RESOURCE. Use --dry-run to preview without either.');
    process.exit(1);
  }
  if (!args.dryRun && tokenManager.canRefresh()) {
    await tokenManager.getToken(); // fail fast if credentials are bad
  }

  const ctx = { api: args.api, tokenManager, rateLimiter: new RateLimiter(args.sabroRatePerMin) };

  let existingByForm = new Map();
  let rootIdByForm = new Map();
  const rootCreationInFlight = new Map(); // form -> Promise<rootId>, dedupes concurrent workers
  if (!args.dryRun) {
    console.log(`Sabro API: ${args.api}\nFetching existing entries and roots for idempotency (this can take a while on a resumed large run)...`);
    for (const e of await listAll(ctx, '/api/v1/admin/lexicon')) {
      existingByForm.set(nfc(e.syriacUnvocalized), e);
    }
    for (const r of await listAll(ctx, '/api/v1/lexicon-roots')) {
      rootIdByForm.set(nfc(r.form), r.id);
    }
    console.log(`Found ${existingByForm.size} existing entr${existingByForm.size === 1 ? 'y' : 'ies'}, ${rootIdByForm.size} existing root(s).\n`);
  }

  const summary = { created: 0, skipped: 0, unresolved: 0, failed: 0 };
  const startedAt = Date.now();
  let processed = 0;

  function printProgress(force = false) {
    if (!force && processed % args.progressEvery !== 0) return;
    const elapsedMin = (Date.now() - startedAt) / 60_000;
    const rate = processed / Math.max(elapsedMin, 1e-6);
    const remaining = entries.length - processed;
    const etaMin = rate > 0 ? remaining / rate : NaN;
    console.log(
      `-- progress ${processed}/${entries.length} ` +
      `created=${summary.created} skipped=${summary.skipped} unresolved=${summary.unresolved} failed=${summary.failed} ` +
      `elapsed=${elapsedMin.toFixed(1)}m rate=${rate.toFixed(1)}/min eta=${Number.isFinite(etaMin) ? etaMin.toFixed(0) + 'm' : '—'}`,
    );
  }

  async function processOne(rawEntry) {
    const label = String(rawEntry);
    try {
      const lexemeId = await resolveLexemeId(rawEntry, args.delay);
      if (lexemeId == null) {
        summary.unresolved++;
        return;
      }

      const [lexeme] = await sedraFetch(`/lexeme/${lexemeId}`, args.delay);
      if (!lexeme) {
        console.error(`  ! ${label}: SEDRA lexeme ${lexemeId} not found`);
        summary.unresolved++;
        return;
      }

      const form = nfc(lexeme.syriac);

      if (!args.dryRun && existingByForm.has(form)) {
        summary.skipped++;
        return;
      }

      // The lexical (citation) form carries the dictionary-headword vocalization —
      // it is NOT reliably the first entry in lexeme.words (inflected/proclitic
      // forms like "and air" can sort earlier), so fetch until it's found.
      let lexicalWord = null;
      let fallbackWord = null;
      for (const w of lexeme.words ?? []) {
        const [fetched] = await sedraFetch(`/word/${w.id}`, args.delay);
        if (!fetched) continue;
        fallbackWord ??= fetched;
        if (fetched.isLexicalForm === true || fetched.isLexicalForm === 'true') {
          lexicalWord = fetched;
          break;
        }
      }
      lexicalWord ??= fallbackWord;

      let rootId = null;
      let rootForm = null;
      if (lexeme.root?.id != null) {
        const [root] = await sedraFetch(`/root/${lexeme.root.id}`, args.delay);
        if (root?.syriac) {
          rootForm = nfc(root.syriac);
          if (!args.dryRun) {
            rootId = rootIdByForm.get(rootForm) ?? null;
            if (rootId == null) {
              // Dedupe concurrent workers racing to create the same root.
              if (!rootCreationInFlight.has(rootForm)) {
                rootCreationInFlight.set(
                  rootForm,
                  (async () => {
                    const created = await sabroFetch(ctx, 'POST', '/api/v1/lexicon-roots', { form: rootForm });
                    rootIdByForm.set(rootForm, created.id);
                    console.log(`  + root ${rootForm} -> ${created.id}`);
                    return created.id;
                  })(),
                );
              }
              rootId = await rootCreationInFlight.get(rootForm);
            }
          }
        }
      }

      const meanings = [
        buildMeaning(lexeme.glosses, 'English', 'en'),
        buildMeaning(lexeme.glosses, 'French', 'fr'),
      ].filter(Boolean);

      const draft = {
        syriacUnvocalized: form,
        syriacVocalized: lexicalWord?.western ? nfc(fixSedraMarks(lexicalWord.western)) : null,
        sblTransliteration: null,
        grammaticalCategory: mapCategory(lexeme.category),
        rootId,
        meanings,
      };

      if (args.dryRun) {
        console.log(
          `  ${form.padEnd(10)} category=${draft.grammaticalCategory.padEnd(11)} ` +
          `vocalized=${(draft.syriacVocalized ?? '—').padEnd(12)} root=${rootForm ?? '—'} ` +
          `en="${meanings.find((m) => m.language === 'en')?.text ?? ''}" fr="${meanings.find((m) => m.language === 'fr')?.text ?? ''}"`,
        );
        summary.created++; // counted as "would create" in dry-run
        return;
      }

      const created = await sabroFetch(ctx, 'POST', '/api/v1/admin/lexicon', draft);
      existingByForm.set(form, created);
      summary.created++;
      console.log(`  + ${form} (SEDRA lexeme ${lexemeId})`);
    } catch (err) {
      summary.failed++;
      console.error(`  ! ${label}: ${err.message}`);
    } finally {
      processed++;
      printProgress();
    }
  }

  await runPool(entries, args.dryRun ? args.concurrency : args.concurrency, processOne);
  printProgress(true);

  console.log(
    `\nDone. ${args.dryRun ? 'wouldCreate' : 'created'}=${summary.created} ` +
    `alreadyInSabro=${summary.skipped} unresolved=${summary.unresolved} failed=${summary.failed}`,
  );
  process.exit(summary.failed > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
