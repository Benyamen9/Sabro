#!/usr/bin/env node
// Enriches the already-seeded Meltho launch pool with the West-Syriac vocalized
// form, the corrected SBL transliteration, and the Semitic root — through Sabro's
// validated admin API (the same write path the backoffice uses).
//
// For each word it:
//   1. (roots) ensures every distinct root in the dataset exists, creating the
//      missing ones via  POST /api/v1/lexicon-roots { form }  (idempotent on form).
//   2. (entries) fetches the existing entry, then PUTs a FULL-REPLACE update that
//      sets syriacVocalized + sblTransliteration + rootId while RE-SENDING the
//      existing grammaticalCategory, meanings, transliteration variants, and
//      morphology — because the update endpoint replaces all editable fields and
//      a null meanings list would wipe the glosses. Status and the playable flag
//      are untouched by the update, so published+playable entries stay that way.
//
// Idempotent: a word whose vocalized form, transliteration, and root already
// match the dataset is skipped. Safe to re-run (e.g. after a 429 rate-limit).
//
// Usage:
//   SABRO_ADMIN_TOKEN=<jwt> node scripts/enrich-lexicon.mjs [--dry-run] [--file <path>] [--api <url>] [--delay <ms>]
//
// Env / flags:
//   SABRO_ADMIN_TOKEN   required (unless --dry-run): a Logto access token with the
//                       api:v1:admin AND api:v1:write scopes (the sabro-seeder-app
//                       M2M token carries both).
//   SABRO_API_URL       API base URL (default http://localhost:5082); --api overrides
//   --file <path>       dataset path (default ./lexicon-enrichment.json next to this script)
//   --dry-run           print the planned changes and the roots to create; no API calls
//   --delay <ms>        pause between mutating calls to stay under the rate limit (default 120)

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const nfc = (s) => (s ?? '').normalize('NFC');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function parseArgs(argv) {
  const args = {
    dryRun: false,
    file: resolve(here, 'lexicon-enrichment.json'),
    api: process.env.SABRO_API_URL || 'http://localhost:5082',
    delay: 120,
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

async function apiFetch(api, token, method, path, body) {
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
    const result = await apiFetch(api, token, 'GET', `${path}?page=${page}&pageSize=${pageSize}`);
    items.push(...result.items);
    if (page * pageSize >= result.total || result.items.length === 0) break;
  }
  return items;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const raw = JSON.parse(await readFile(args.file, 'utf8'));
  const words = (raw.words ?? []).map((w) => ({
    form: nfc(w.syriacUnvocalized),
    vocalized: nfc(w.syriacVocalized),
    translit: w.sblTransliteration ?? null,
    root: w.root ? nfc(w.root) : null,
  }));
  console.log(`Loaded ${words.length} words from ${args.file}`);

  // Local validation before touching the API.
  const problems = [];
  for (const w of words) {
    if (!w.form) problems.push(`missing syriacUnvocalized: ${JSON.stringify(w)}`);
    if (!w.vocalized) problems.push(`"${w.form}" (${w.translit}) missing syriacVocalized`);
    if (!w.translit) problems.push(`"${w.form}" missing sblTransliteration`);
  }
  if (problems.length) {
    console.error(`\nDataset has ${problems.length} problem(s):`);
    for (const p of problems) console.error(`  - ${p}`);
    process.exit(1);
  }

  const distinctRoots = [...new Set(words.filter((w) => w.root).map((w) => w.root))];
  const primitives = words.filter((w) => !w.root).length;

  if (args.dryRun) {
    console.log('\n--dry-run: planned enrichment (no API calls).\n');
    for (const w of words) {
      console.log(
        `  ${w.form.padEnd(7)} ${w.translit.padEnd(9)} vocalized=${w.vocalized.padEnd(8)} ` +
        `root=${w.root ?? '— (primitive)'}`,
      );
    }
    console.log(`\nDistinct roots to ensure (${distinctRoots.length}): ${distinctRoots.join(' ')}`);
    console.log(`Primitives (no root): ${primitives}`);
    console.log(`\nRe-run without --dry-run (with SABRO_ADMIN_TOKEN set) to apply.`);
    return;
  }

  const token = process.env.SABRO_ADMIN_TOKEN;
  if (!token) {
    console.error('SABRO_ADMIN_TOKEN is required (Logto access token with api:v1:admin + api:v1:write). Use --dry-run to validate without it.');
    process.exit(1);
  }

  // --- Roots: ensure each distinct root exists, create the missing ones. ---
  console.log(`API: ${args.api}\nFetching existing roots...`);
  const rootIdByForm = new Map();
  for (const r of await listAll(args.api, token, '/api/v1/lexicon-roots')) {
    rootIdByForm.set(nfc(r.form), r.id);
  }
  console.log(`Found ${rootIdByForm.size} existing root(s).`);

  let rootsCreated = 0;
  for (const form of distinctRoots) {
    if (rootIdByForm.has(form)) continue;
    const created = await apiFetch(args.api, token, 'POST', '/api/v1/lexicon-roots', { form });
    rootIdByForm.set(form, created.id);
    rootsCreated++;
    console.log(`  + root ${form} -> ${created.id}`);
    if (args.delay > 0) await sleep(args.delay);
  }
  console.log(`Roots ready (created ${rootsCreated}, reused ${distinctRoots.length - rootsCreated}).\n`);

  // --- Entries: fetch existing, then full-replace PUT with the enrichment merged in. ---
  console.log('Fetching existing entries...');
  const entryByForm = new Map();
  for (const e of await listAll(args.api, token, '/api/v1/admin/lexicon')) {
    entryByForm.set(nfc(e.syriacUnvocalized), e);
  }
  console.log(`Found ${entryByForm.size} existing entr${entryByForm.size === 1 ? 'y' : 'ies'}.\n`);

  const summary = { updated: 0, skipped: 0, missing: 0, failed: 0 };
  for (const w of words) {
    const label = `${w.form} (${w.translit})`;
    try {
      const entry = entryByForm.get(w.form);
      if (!entry) {
        summary.missing++;
        console.error(`  ! ${label}: no existing entry found — run seed-lexicon.mjs first`);
        continue;
      }

      const wantRootId = w.root ? rootIdByForm.get(w.root) : null;

      // Idempotency: skip if vocalized form, transliteration, and root already match.
      const same =
        nfc(entry.syriacVocalized) === w.vocalized &&
        (entry.sblTransliteration ?? null) === w.translit &&
        (entry.rootId ?? null) === (wantRootId ?? null);
      if (same) {
        summary.skipped++;
        console.log(`  = ${label}: already enriched`);
        continue;
      }

      // Full replacement — re-send the fields we are NOT changing so they survive.
      const body = {
        syriacUnvocalized: nfc(entry.syriacUnvocalized),
        sblTransliteration: w.translit,
        grammaticalCategory: entry.grammaticalCategory,
        syriacVocalized: w.vocalized,
        rootId: wantRootId ?? null,
        transliterationVariants: entry.transliterationVariants ?? [],
        morphology: entry.morphology ?? null,
        meanings: (entry.meanings ?? []).map((m) => ({ language: m.language, text: m.text })),
      };

      await apiFetch(args.api, token, 'PUT', `/api/v1/admin/lexicon/${entry.id}`, body);
      summary.updated++;
      console.log(`  + ${label}: vocalized=${w.vocalized} root=${w.root ?? '—'}`);
      if (args.delay > 0) await sleep(args.delay);
    } catch (err) {
      summary.failed++;
      console.error(`  ! ${label}: ${err.message}`);
    }
  }

  console.log(
    `\nDone. rootsCreated=${rootsCreated} updated=${summary.updated} ` +
    `alreadyEnriched=${summary.skipped} missingEntry=${summary.missing} failed=${summary.failed}`,
  );
  process.exit(summary.failed > 0 || summary.missing > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
