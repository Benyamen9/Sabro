#!/usr/bin/env node
// Seeds the Meltho launch word pool through Sabro's validated admin API
// (the backoffice write path, scripted). For each word it runs the same
// three steps the backoffice UI performs:
//   1. POST   /api/v1/admin/lexicon            -> create as Draft
//   2. POST   /api/v1/admin/lexicon/{id}/publish   -> Draft -> Published (needs en/fr/nl)
//   3. PUT    /api/v1/admin/lexicon/{id}/playable  -> mark PlayableInMeltho (needs Published + length 2-8)
//
// Idempotent: it first lists existing entries and, per word, only does the
// steps still missing. Safe to re-run.
//
// Usage:
//   SABRO_ADMIN_TOKEN=<jwt> node scripts/seed-lexicon.mjs [--dry-run] [--file <path>] [--api <url>]
//
// Env / flags:
//   SABRO_ADMIN_TOKEN   required (unless --dry-run): a Logto access token with the api:v1:admin scope
//   SABRO_API_URL       API base URL (default http://localhost:5082); --api overrides
//   --file <path>       dataset path (default ./launch-words.json next to this script)
//   --dry-run           validate the dataset and print computed playable lengths; no API calls

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));

function parseArgs(argv) {
  const args = { dryRun: false, file: resolve(here, 'launch-words.json'), api: process.env.SABRO_API_URL || 'http://localhost:5082' };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--dry-run') args.dryRun = true;
    else if (a === '--file') args.file = resolve(process.cwd(), argv[++i]);
    else if (a === '--api') args.api = argv[++i];
    else throw new Error(`Unknown argument: ${a}`);
  }
  return args;
}

// Base-letter count: Unicode letter-category code points only (combining marks
// excluded) — mirrors the server-side playable-length rule.
const baseLetterCount = (s) => [...s.normalize('NFC')].filter((ch) => /\p{L}/u.test(ch)).length;

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

async function listAllEntries(api, token) {
  const byForm = new Map();
  const pageSize = 200;
  for (let page = 1; ; page++) {
    const result = await apiFetch(api, token, 'GET', `/api/v1/admin/lexicon?page=${page}&pageSize=${pageSize}`);
    for (const e of result.items) byForm.set(e.syriacUnvocalized.normalize('NFC'), e);
    if (page * pageSize >= result.total || result.items.length === 0) break;
  }
  return byForm;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const raw = JSON.parse(await readFile(args.file, 'utf8'));
  const words = raw.words ?? [];
  console.log(`Loaded ${words.length} words from ${args.file}`);

  // Validate dataset locally before touching the API.
  const problems = [];
  for (const w of words) {
    const form = (w.syriacUnvocalized ?? '').normalize('NFC');
    const len = baseLetterCount(form);
    if (!form) problems.push(`missing syriacUnvocalized: ${JSON.stringify(w)}`);
    if (len < 2 || len > 8) problems.push(`"${form}" (${w.sblTransliteration}) has playable length ${len}, outside [2,8]`);
    const langs = new Set((w.meanings ?? []).map((m) => m.language));
    for (const required of ['en', 'fr', 'nl']) {
      if (!langs.has(required)) problems.push(`"${form}" (${w.sblTransliteration}) missing ${required} meaning`);
    }
  }
  if (problems.length) {
    console.error(`\nDataset has ${problems.length} problem(s):`);
    for (const p of problems) console.error(`  - ${p}`);
    process.exit(1);
  }

  if (args.dryRun) {
    console.log('\n--dry-run: dataset is valid. Computed playable lengths:\n');
    for (const w of words) {
      const form = w.syriacUnvocalized.normalize('NFC');
      console.log(`  ${form.padEnd(8)} len=${baseLetterCount(form)}  ${w.sblTransliteration.padEnd(9)} ${w.meanings.find((m) => m.language === 'en').text}`);
    }
    console.log(`\n${words.length} words ready. Re-run without --dry-run (and with SABRO_ADMIN_TOKEN set) to publish.`);
    return;
  }

  const token = process.env.SABRO_ADMIN_TOKEN;
  if (!token) {
    console.error('SABRO_ADMIN_TOKEN is required (a Logto access token with the api:v1:admin scope). Use --dry-run to validate without it.');
    process.exit(1);
  }

  console.log(`API: ${args.api}\nFetching existing entries for idempotency...`);
  const existing = await listAllEntries(args.api, token);
  console.log(`Found ${existing.size} existing entr${existing.size === 1 ? 'y' : 'ies'}.\n`);

  const summary = { created: 0, published: 0, madePlayable: 0, alreadyDone: 0, failed: 0 };
  for (const w of words) {
    const form = w.syriacUnvocalized.normalize('NFC');
    const label = `${form} (${w.sblTransliteration})`;
    try {
      let entry = existing.get(form);
      const actions = [];

      if (!entry) {
        entry = await apiFetch(args.api, token, 'POST', '/api/v1/admin/lexicon', {
          syriacUnvocalized: form,
          sblTransliteration: w.sblTransliteration ?? null,
          grammaticalCategory: w.grammaticalCategory,
          meanings: w.meanings,
        });
        summary.created++;
        actions.push('created');
      }

      if (entry.status !== 'Published') {
        entry = await apiFetch(args.api, token, 'POST', `/api/v1/admin/lexicon/${entry.id}/publish`);
        summary.published++;
        actions.push('published');
      }

      if (!entry.playableInMeltho) {
        entry = await apiFetch(args.api, token, 'PUT', `/api/v1/admin/lexicon/${entry.id}/playable`, { playable: true });
        summary.madePlayable++;
        actions.push('playable');
      }

      if (actions.length === 0) {
        summary.alreadyDone++;
        console.log(`  = ${label}: already published + playable`);
      } else {
        console.log(`  + ${label}: ${actions.join(' -> ')} (len ${entry.playableLength})`);
      }
    } catch (err) {
      summary.failed++;
      console.error(`  ! ${label}: ${err.message}`);
    }
  }

  console.log(
    `\nDone. created=${summary.created} published=${summary.published} madePlayable=${summary.madePlayable} ` +
    `alreadyComplete=${summary.alreadyDone} failed=${summary.failed}`,
  );
  process.exit(summary.failed > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
