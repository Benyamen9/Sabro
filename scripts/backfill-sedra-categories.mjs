#!/usr/bin/env node
// Cross-references SEDRA IV's grammatical category data against every Sabro
// Lexicon entry currently classified as "Other", and (with --apply) corrects
// the ones SEDRA can confidently classify.
//
// Why this is narrow in scope, not a blind re-check of the whole lexicon:
// every entry the importer itself CREATED already got mapCategory(SEDRA's
// category) applied at creation time, and CATEGORY_MAP (duplicated below,
// see import-sedra.mjs) has never changed since it was introduced — so a
// created entry cannot disagree with SEDRA today. The only entries that
// never went through that mapping are the ~4,631 the importer SKIPPED
// because their syriacUnvocalized form already matched something already in
// Sabro (hand-curated or an earlier partial import) — those are the ones
// this script can actually improve.
//
// Two-phase, both against the public/unauthenticated SEDRA API and Sabro's
// admin API:
//   1. Crawl every SEDRA lexeme id (default 1-38812), building an index of
//      nfc(syriac form) -> set of mapped categories (a form can appear on
//      multiple lexeme ids with different meanings/categories).
//   2. Page through every Sabro Lexicon entry, filter to grammaticalCategory
//      === "Other", and look each one's form up in the index:
//        - no match, or the only mapped category is also "Other" (SEDRA
//          itself has no category, or genuinely says idiom/denominative):
//          nothing to fix, skipped.
//        - exactly one non-Other mapped category: a confident fix candidate.
//        - 2+ distinct non-Other mapped categories for the same form
//          (homographs with different parts of speech): reported as
//          ambiguous, never auto-applied.
//
// Report-only by default. Pass --apply to actually PUT the corrected
// grammaticalCategory (full-replacement PUT, so the rest of the entry is
// read back from Sabro's own list response and sent through unchanged).
//
// Usage:
//   node scripts/backfill-sedra-categories.mjs [--apply] [--range <start>-<end>] [--api <url>] [--delay <ms>] [--concurrency <n>] [--sabro-rate <perMin>] [--out <path>]
//
// Env (same as import-sedra.mjs / audit-sedra-existing.mjs):
//   SABRO_ADMIN_TOKEN / SABRO_ADMIN_CLIENT_ID / SABRO_ADMIN_CLIENT_SECRET / SABRO_TOKEN_URL / SABRO_API_RESOURCE / SABRO_API_URL

import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { writeFile } from 'node:fs/promises';

const here = dirname(fileURLToPath(import.meta.url));
const nfc = (s) => (s ?? '').normalize('NFC');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const SEDRA_API = 'https://sedra.bethmardutho.org/api';

// Kept identical to import-sedra.mjs's CATEGORY_MAP on purpose — verified
// complete against a full 1-38812 crawl (20/20 distinct SEDRA category
// strings covered) on 2026-07-24, see sedra-categories.json.
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
    apply: false,
    range: '1-38812',
    api: process.env.SABRO_API_URL || 'http://localhost:5082',
    delay: 100,
    concurrency: 10,
    sabroRatePerMin: 90,
    progressEvery: 1000,
    out: resolve(here, '..', 'sedra-category-backfill-report.json'),
  };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--apply') args.apply = true;
    else if (a === '--range') args.range = argv[++i];
    else if (a === '--api') args.api = argv[++i];
    else if (a === '--delay') args.delay = Number(argv[++i]);
    else if (a === '--concurrency') args.concurrency = Number(argv[++i]);
    else if (a === '--sabro-rate') args.sabroRatePerMin = Number(argv[++i]);
    else if (a === '--progress-every') args.progressEvery = Number(argv[++i]);
    else if (a === '--out') args.out = resolve(process.cwd(), argv[++i]);
    else throw new Error(`Unknown argument: ${a}`);
  }
  return args;
}

function buildRange(rangeStr) {
  const m = /^(\d+)-(\d+)$/.exec(rangeStr);
  if (!m) throw new Error(`--range must look like "1-38812", got "${rangeStr}"`);
  const start = Number(m[1]);
  const end = Number(m[2]);
  const ids = [];
  for (let id = start; id <= end; id++) ids.push(id);
  return ids;
}

// --- HTTP with retry/backoff (same shape as the sibling scripts) ----------

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

class TokenManager {
  constructor({ staticToken, clientId, clientSecret, tokenUrl, resource }) {
    this.staticToken = staticToken;
    this.clientId = clientId;
    this.clientSecret = clientSecret;
    this.tokenUrl = tokenUrl;
    this.resource = resource;
    this.token = staticToken ?? null;
    this.expiresAt = staticToken ? Infinity : 0;
    this.refreshPromise = null;
  }

  canRefresh() {
    return Boolean(this.clientId && this.clientSecret && this.tokenUrl && this.resource);
  }

  async getToken() {
    if (this.token && Date.now() < this.expiresAt - 120_000) return this.token;
    if (!this.canRefresh()) {
      if (!this.token) throw new Error('No Sabro token available and no client credentials configured to mint one.');
      return this.token;
    }
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

class RateLimiter {
  constructor(perMinute) {
    this.minIntervalMs = 60_000 / perMinute;
    this.nextSlot = 0;
    this.queue = Promise.resolve();
  }

  async acquire() {
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

// --- Phase 1: SEDRA index (form -> set of mapped categories) --------------

async function buildSedraIndex(ids, args) {
  const index = new Map(); // nfc(form) -> Set<mappedCategory>
  const summary = { checked: 0, found: 0, missing: 0 };
  const startedAt = Date.now();
  let processed = 0;

  function printProgress(force = false) {
    if (!force && processed % args.progressEvery !== 0) return;
    const elapsedMin = (Date.now() - startedAt) / 60_000;
    const rate = processed / Math.max(elapsedMin, 1e-6);
    const remaining = ids.length - processed;
    const etaMin = rate > 0 ? remaining / rate : NaN;
    console.log(
      `-- [sedra] progress ${processed}/${ids.length} found=${summary.found} missing=${summary.missing} ` +
      `elapsed=${elapsedMin.toFixed(1)}m rate=${rate.toFixed(1)}/min eta=${Number.isFinite(etaMin) ? etaMin.toFixed(0) + 'm' : '—'}`,
    );
  }

  async function processOne(id) {
    try {
      const [lexeme] = await sedraFetch(`/lexeme/${id}`, args.delay);
      if (!lexeme) {
        summary.missing++;
        return;
      }
      summary.found++;
      const form = nfc(lexeme.syriac);
      if (!form) return;
      const mapped = mapCategory(lexeme.category);
      if (!index.has(form)) index.set(form, new Set());
      index.get(form).add(mapped);
    } catch (err) {
      console.error(`  ! sedra ${id}: ${err.message}`);
    } finally {
      processed++;
      summary.checked = processed;
      printProgress();
    }
  }

  await runPool(ids, args.concurrency, processOne);
  printProgress(true);
  console.log(`[sedra] Done. checked=${summary.checked} found=${summary.found} missing=${summary.missing} distinctForms=${index.size}\n`);
  return index;
}

// --- Phase 2: cross-reference + optional apply -----------------------------

async function main() {
  const args = parseArgs(process.argv.slice(2));
  console.log(`Mode: ${args.apply ? 'REPORT + APPLY' : 'REPORT ONLY (pass --apply to write corrections)'}`);
  console.log(`Sabro API: ${args.api}\n`);

  const tokenManager = new TokenManager({
    staticToken: process.env.SABRO_ADMIN_TOKEN,
    clientId: process.env.SABRO_ADMIN_CLIENT_ID,
    clientSecret: process.env.SABRO_ADMIN_CLIENT_SECRET,
    tokenUrl: process.env.SABRO_TOKEN_URL,
    resource: process.env.SABRO_API_RESOURCE,
  });
  if (!tokenManager.staticToken && !tokenManager.canRefresh()) {
    console.error('Provide SABRO_ADMIN_TOKEN, or all of SABRO_ADMIN_CLIENT_ID/SABRO_ADMIN_CLIENT_SECRET/SABRO_TOKEN_URL/SABRO_API_RESOURCE.');
    process.exit(1);
  }
  if (tokenManager.canRefresh()) await tokenManager.getToken();

  const ctx = { api: args.api, tokenManager, rateLimiter: new RateLimiter(args.sabroRatePerMin) };

  const ids = buildRange(args.range);
  console.log(`Phase 1: crawling ${ids.length} SEDRA lexeme ids (range ${args.range})...`);
  const sedraIndex = await buildSedraIndex(ids, args);

  console.log('Phase 2: fetching all Sabro Lexicon entries...');
  const entries = await listAll(ctx, '/api/v1/admin/lexicon');
  console.log(`Found ${entries.length} total Sabro entries.\n`);

  const otherEntries = entries.filter((e) => e.grammaticalCategory === 'Other');
  console.log(`${otherEntries.length} entries currently classified as "Other".\n`);

  const fixes = [];
  const ambiguous = [];
  let noMatch = 0;
  let correctlyOther = 0;

  for (const entry of otherEntries) {
    const form = nfc(entry.syriacUnvocalized);
    const categories = sedraIndex.get(form);
    if (!categories) {
      noMatch++;
      continue;
    }
    const nonOther = [...categories].filter((c) => c !== 'Other');
    if (nonOther.length === 0) {
      correctlyOther++;
      continue;
    }
    if (nonOther.length > 1) {
      ambiguous.push({ id: entry.id, form: entry.syriacUnvocalized, candidates: nonOther });
      continue;
    }
    fixes.push({ id: entry.id, form: entry.syriacUnvocalized, from: 'Other', to: nonOther[0], entry });
  }

  console.log(`Cross-reference summary:`);
  console.log(`  confident fixes:        ${fixes.length}`);
  console.log(`  ambiguous (skipped):    ${ambiguous.length}`);
  console.log(`  correctly Other:        ${correctlyOther}`);
  console.log(`  no SEDRA match:         ${noMatch}\n`);

  const byCategory = new Map();
  for (const f of fixes) byCategory.set(f.to, (byCategory.get(f.to) ?? 0) + 1);
  console.log('Fixes by target category:');
  for (const [cat, count] of [...byCategory.entries()].sort((a, b) => b[1] - a[1])) {
    console.log(`  ${String(count).padStart(6)}  Other -> ${cat}`);
  }
  console.log();

  let applied = 0;
  let failed = 0;
  if (args.apply && fixes.length > 0) {
    console.log(`Applying ${fixes.length} corrections...`);
    let processed = 0;
    async function applyOne(fix) {
      try {
        const e = fix.entry;
        const payload = {
          syriacUnvocalized: e.syriacUnvocalized,
          sblTransliteration: e.sblTransliteration,
          grammaticalCategory: fix.to,
          syriacVocalized: e.syriacVocalized,
          rootId: e.rootId,
          transliterationVariants: e.transliterationVariants,
          morphology: e.morphology,
          meanings: e.meanings,
        };
        await sabroFetch(ctx, 'PUT', `/api/v1/admin/lexicon/${fix.id}`, payload);
        applied++;
      } catch (err) {
        failed++;
        console.error(`  ! apply ${fix.id} (${fix.form}): ${err.message}`);
      } finally {
        processed++;
        if (processed % args.progressEvery === 0 || processed === fixes.length) {
          console.log(`-- [apply] ${processed}/${fixes.length} applied=${applied} failed=${failed}`);
        }
      }
    }
    await runPool(fixes, args.concurrency, applyOne);
    console.log(`\nApply done. applied=${applied} failed=${failed}`);
  }

  const report = {
    mode: args.apply ? 'report+apply' : 'report-only',
    summary: {
      totalSabroEntries: entries.length,
      totalOther: otherEntries.length,
      confidentFixes: fixes.length,
      ambiguous: ambiguous.length,
      correctlyOther,
      noMatch,
      applied,
      failed,
    },
    fixes: fixes.map(({ id, form, from, to }) => ({ id, form, from, to })),
    ambiguous,
  };
  await writeFile(args.out, JSON.stringify(report, null, 2), 'utf8');
  console.log(`\nReport written to ${args.out}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
