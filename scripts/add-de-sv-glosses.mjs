#!/usr/bin/env node
// Backfills German + Swedish meanings onto the launch pool through Sabro's
// validated admin API (the same write path the backoffice uses).
//
// Context: the launch pool was originally published under the en/fr/nl-only
// publish gate. A later change requires en, fr, nl, de, and sv to keep any
// Published entry valid on update — so these entries currently fail ANY
// update (including unrelated field changes) until de/sv exist. This script
// closes that gap: fetch existing entry, then PUT a FULL-REPLACE update that
// appends de/sv to the existing meanings while re-sending every other
// editable field unchanged (the update endpoint replaces all of them).
//
// Idempotent: a word that already carries de and sv is skipped. Safe to re-run.
//
// Usage:
//   SABRO_ADMIN_TOKEN=<jwt> node scripts/add-de-sv-glosses.mjs [--dry-run] [--file <path>] [--api <url>] [--delay <ms>]
//
// Env / flags:
//   SABRO_ADMIN_TOKEN   required (unless --dry-run): a Logto access token with
//                       the api:v1:admin scope.
//   SABRO_API_URL       API base URL (default http://localhost:5082); --api overrides
//   --file <path>       dataset path (default ./de-sv-glosses.json next to this script)
//   --dry-run           print the planned changes; no API calls
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
    file: resolve(here, 'de-sv-glosses.json'),
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
    de: w.de,
    sv: w.sv,
  }));
  console.log(`Loaded ${words.length} words from ${args.file}`);

  if (args.dryRun) {
    console.log('\n--dry-run: planned de/sv additions (no API calls).\n');
    for (const w of words) {
      console.log(`  ${w.form.padEnd(8)} de="${w.de}" sv="${w.sv}"`);
    }
    console.log(`\nRe-run without --dry-run (with SABRO_ADMIN_TOKEN set) to apply.`);
    return;
  }

  const token = process.env.SABRO_ADMIN_TOKEN;
  if (!token) {
    console.error('SABRO_ADMIN_TOKEN is required (Logto access token with api:v1:admin scope). Use --dry-run to validate without it.');
    process.exit(1);
  }

  console.log(`API: ${args.api}\nFetching existing entries...`);
  const entryByForm = new Map();
  for (const e of await listAll(args.api, token, '/api/v1/admin/lexicon')) {
    entryByForm.set(nfc(e.syriacUnvocalized), e);
  }
  console.log(`Found ${entryByForm.size} existing entr${entryByForm.size === 1 ? 'y' : 'ies'}.\n`);

  const summary = { updated: 0, skipped: 0, missing: 0, failed: 0 };
  for (const w of words) {
    const label = w.form;
    try {
      const entry = entryByForm.get(w.form);
      if (!entry) {
        summary.missing++;
        console.error(`  ! ${label}: no existing entry found`);
        continue;
      }

      const meaningsByLang = new Map((entry.meanings ?? []).map((m) => [m.language, m.text]));
      const alreadyHasBoth = meaningsByLang.has('de') && meaningsByLang.has('sv');
      if (alreadyHasBoth) {
        summary.skipped++;
        console.log(`  = ${label}: already has de+sv`);
        continue;
      }

      meaningsByLang.set('de', w.de);
      meaningsByLang.set('sv', w.sv);

      // Full replacement — re-send every other editable field unchanged.
      const body = {
        syriacUnvocalized: nfc(entry.syriacUnvocalized),
        sblTransliteration: entry.sblTransliteration ?? null,
        grammaticalCategory: entry.grammaticalCategory,
        syriacVocalized: entry.syriacVocalized ?? null,
        rootId: entry.rootId ?? null,
        transliterationVariants: entry.transliterationVariants ?? [],
        morphology: entry.morphology ?? null,
        meanings: [...meaningsByLang.entries()].map(([language, text]) => ({ language, text })),
      };

      await apiFetch(args.api, token, 'PUT', `/api/v1/admin/lexicon/${entry.id}`, body);
      summary.updated++;
      console.log(`  + ${label}: added de="${w.de}" sv="${w.sv}"`);
      if (args.delay > 0) await sleep(args.delay);
    } catch (err) {
      summary.failed++;
      console.error(`  ! ${label}: ${err.message}`);
    }
  }

  console.log(
    `\nDone. updated=${summary.updated} alreadyHadBoth=${summary.skipped} ` +
    `missingEntry=${summary.missing} failed=${summary.failed}`,
  );
  process.exit(summary.failed > 0 || summary.missing > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
