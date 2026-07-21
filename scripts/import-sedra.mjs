#!/usr/bin/env node
// Imports candidate Lexicon entries from the SEDRA IV API (Beth Mardutho: The
// Syriac Institute — https://sedra.bethmardutho.org/api, Apache-2.0 licensed,
// public, unauthenticated, CORS-open) and creates them as Draft entries in
// Sabro through the validated admin API (the same write path the backoffice
// uses). Attribution is rendered sitewide in the footer (site.attribution).
//
// SEDRA has no Dutch glosses, so imported drafts always carry only en/fr
// meanings — nl (and any vocalization/root correction) is left for the Owner
// to complete and confirm in the backoffice before publishing. Vocalization
// choices (qushoyo/rukkokho) are per-word lexical judgment calls, not derived
// here — SEDRA's own "western" spelling is taken as-is, not reprocessed.
//
// For each entry in the input list (a SEDRA lexeme id, or Syriac consonantal
// text to resolve via SEDRA's word search) it:
//   1. Resolves a SEDRA lexeme id (direct, or via GET /api/word/{text},
//      preferring the match with isLexicalForm === true; logs and skips on
//      ambiguity so the Owner can disambiguate with an explicit lexeme id).
//   2. Fetches GET /api/lexeme/{id} for glosses, category, and the root ref.
//   3. Fetches GET /api/root/{id} (if present) and ensures it exists in Sabro
//      via POST /api/v1/lexicon-roots { form } (idempotent on form).
//   4. Creates the entry as Draft via POST /api/v1/admin/lexicon with
//      syriacUnvocalized, syriacVocalized (SEDRA's western spelling),
//      grammaticalCategory (mapped from SEDRA's CategoryType), rootId, and
//      en/fr meanings (joined from SEDRA's gloss arrays).
//
// Never publishes and never sets playableInMeltho — that stays a deliberate
// Owner action once nl is added and the entry is reviewed.
//
// Idempotent: skips any Syriac form that already exists as a Sabro entry.
// Safe to re-run.
//
// Usage:
//   SABRO_ADMIN_TOKEN=<jwt> node scripts/import-sedra.mjs [--dry-run] [--file <path>] [--api <url>] [--delay <ms>]
//
// Env / flags:
//   SABRO_ADMIN_TOKEN   required (unless --dry-run): a Logto access token with
//                       the api:v1:admin scope (the sabro-seeder-app M2M token).
//   SABRO_API_URL       Sabro API base URL (default http://localhost:5082); --api overrides
//   --file <path>       input list path (default ./sedra-import-list.json next to this script)
//   --dry-run           resolve + fetch from SEDRA and print the planned drafts; no Sabro API calls
//   --delay <ms>        pause between SEDRA and Sabro calls, courtesy rate-limiting (default 200)

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const nfc = (s) => (s ?? '').normalize('NFC');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const SEDRA_API = 'https://sedra.bethmardutho.org/api';

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
    file: resolve(here, 'sedra-import-list.json'),
    api: process.env.SABRO_API_URL || 'http://localhost:5082',
    delay: 200,
  };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--dry-run') args.dryRun = true;
    else if (a === '--file') args.file = resolve(process.cwd(), argv[++i]);
    else if (a === '--api') args.api = argv[++i];
    else if (a === '--delay') args.delay = Number(argv[++i]);
    else throw new Error(`Unknown argument: ${a}`);
  }
  return args;
}

async function sedraFetch(path) {
  const res = await fetch(`${SEDRA_API}${path}`, { headers: { accept: 'application/json' } });
  if (res.status === 404) return [];
  if (!res.ok) throw new Error(`SEDRA GET ${path} -> ${res.status} ${res.statusText}`);
  const body = await res.json();
  return Array.isArray(body) ? body : [body];
}

async function sabroFetch(api, token, method, path, body) {
  const res = await fetch(`${api}${path}`, {
    method,
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json;
  try { json = text ? JSON.parse(text) : undefined; } catch { json = undefined; }
  if (!res.ok) {
    const detail = json?.detail || json?.title || text || `${res.status} ${res.statusText}`;
    throw new Error(`${method} ${path} -> ${res.status}: ${detail}`);
  }
  return json;
}

async function listAll(api, token, path) {
  const items = [];
  const pageSize = 200;
  for (let page = 1; ; page++) {
    const result = await sabroFetch(api, token, 'GET', `${path}?page=${page}&pageSize=${pageSize}`);
    items.push(...result.items);
    if (page * pageSize >= result.total || result.items.length === 0) break;
  }
  return items;
}

// Resolves an input entry (a numeric SEDRA lexeme id, or Syriac consonantal
// text) to a single SEDRA lexeme id. Returns null (and logs) on no match or
// unresolved ambiguity.
async function resolveLexemeId(entry) {
  if (/^\d+$/.test(entry)) return Number(entry);

  const matches = await sedraFetch(`/word/${encodeURIComponent(nfc(entry))}`);
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
  const raw = JSON.parse(await readFile(args.file, 'utf8'));
  const entries = raw.entries ?? [];
  console.log(`Loaded ${entries.length} entries from ${args.file}`);

  const token = process.env.SABRO_ADMIN_TOKEN;
  if (!args.dryRun && !token) {
    console.error('SABRO_ADMIN_TOKEN is required (a Logto access token with the api:v1:admin scope). Use --dry-run to preview without it.');
    process.exit(1);
  }

  let existingByForm = new Map();
  let rootIdByForm = new Map();
  if (!args.dryRun) {
    console.log(`Sabro API: ${args.api}\nFetching existing entries and roots for idempotency...`);
    for (const e of await listAll(args.api, token, '/api/v1/admin/lexicon')) {
      existingByForm.set(nfc(e.syriacUnvocalized), e);
    }
    for (const r of await listAll(args.api, token, '/api/v1/lexicon-roots')) {
      rootIdByForm.set(nfc(r.form), r.id);
    }
    console.log(`Found ${existingByForm.size} existing entr${existingByForm.size === 1 ? 'y' : 'ies'}, ${rootIdByForm.size} existing root(s).\n`);
  }

  const summary = { created: 0, skipped: 0, unresolved: 0, failed: 0 };
  for (const rawEntry of entries) {
    const label = String(rawEntry);
    try {
      const lexemeId = await resolveLexemeId(rawEntry);
      if (args.delay > 0) await sleep(args.delay);
      if (lexemeId == null) {
        summary.unresolved++;
        continue;
      }

      const [lexeme] = await sedraFetch(`/lexeme/${lexemeId}`);
      if (args.delay > 0) await sleep(args.delay);
      if (!lexeme) {
        console.error(`  ! ${label}: SEDRA lexeme ${lexemeId} not found`);
        summary.unresolved++;
        continue;
      }

      const form = nfc(lexeme.syriac);

      if (!args.dryRun && existingByForm.has(form)) {
        summary.skipped++;
        console.log(`  = ${form} (SEDRA lexeme ${lexemeId}): already in Sabro`);
        continue;
      }

      // The lexical (citation) form carries the dictionary-headword vocalization —
      // it is NOT reliably the first entry in lexeme.words (inflected/proclitic
      // forms like "and air" can sort earlier), so fetch until it's found.
      let lexicalWord = null;
      let fallbackWord = null;
      for (const w of lexeme.words ?? []) {
        const [fetched] = await sedraFetch(`/word/${w.id}`);
        if (args.delay > 0) await sleep(args.delay);
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
        const [root] = await sedraFetch(`/root/${lexeme.root.id}`);
        if (args.delay > 0) await sleep(args.delay);
        if (root?.syriac) {
          rootForm = nfc(root.syriac);
          if (!args.dryRun) {
            rootId = rootIdByForm.get(rootForm) ?? null;
            if (rootId == null) {
              const created = await sabroFetch(args.api, token, 'POST', '/api/v1/lexicon-roots', { form: rootForm });
              rootId = created.id;
              rootIdByForm.set(rootForm, rootId);
              console.log(`  + root ${rootForm} -> ${rootId}`);
              if (args.delay > 0) await sleep(args.delay);
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
        syriacVocalized: lexicalWord?.western ? nfc(lexicalWord.western) : null,
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
        continue;
      }

      const created = await sabroFetch(args.api, token, 'POST', '/api/v1/admin/lexicon', draft);
      existingByForm.set(form, created);
      summary.created++;
      console.log(`  + ${form} (SEDRA lexeme ${lexemeId}): created as Draft — add nl gloss and review before publishing`);
    } catch (err) {
      summary.failed++;
      console.error(`  ! ${label}: ${err.message}`);
    }
  }

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
