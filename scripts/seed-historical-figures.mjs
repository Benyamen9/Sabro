#!/usr/bin/env node
// Seeds the Shmo figure roster through Sabro's validated admin API (the
// backoffice write path, scripted). For each figure it runs the same three
// steps the backoffice UI performs:
//   1. POST /api/v1/admin/historical-figures                -> create as Draft
//   2. POST /api/v1/admin/historical-figures/{id}/publish   -> Draft -> Published (needs a tradition)
//   3. PUT  /api/v1/admin/historical-figures/{id}/playable  -> mark PlayableInShmo (needs Published)
//
// Idempotent: it first lists existing figures and, per figure, only does the
// steps still missing. Safe to re-run. Matching is by exact name, which is also
// what the game treats as the answer, so two figures may not share one.
//
// Keys starting with "_" in the dataset (e.g. _note) are editorial annotations
// for the Owner's review and are stripped before the payload is sent.
//
// Usage:
//   SABRO_ADMIN_TOKEN=<jwt> node scripts/seed-historical-figures.mjs [--dry-run] [--file <path>] [--api <url>]
//
// Env / flags:
//   SABRO_ADMIN_TOKEN   required (unless --dry-run): a Logto access token with the api:v1:admin scope
//   SABRO_API_URL       API base URL (default http://localhost:5082); --api overrides
//   --file <path>       dataset path (default ./shmo-figures.json next to this script)
//   --dry-run           validate the dataset and print the roster breakdown; no API calls
//   --drafts-only       create figures but do not publish or mark them playable

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));

// Mirrors the enums in src/Modules/Sabro.Historical/Domain. Kept in sync by
// hand: the API rejects unknown values anyway, but failing here means a typo
// costs a dry run rather than a half-seeded roster.
const CATEGORIES = ['BiblicalOldTestament', 'BiblicalNewTestament', 'Patristic'];
const ROLES = [
  'Prophet', 'King', 'Judge', 'Apostle', 'Evangelist', 'Patriarch',
  'Bishop', 'Translator', 'Commentator', 'Monk', 'Martyr', 'Other',
];
const REGIONS = ['IsraelJudah', 'Mesopotamia', 'Syria', 'Persia', 'Egypt', 'AsiaMinor', 'Other'];
const TRADITIONS = ['WestSyriac', 'EastSyriac', 'ByzantineChalcedonian', 'NotApplicable'];
const GENDERS = ['Male', 'Female'];

// Matches HistoricalFigure.MinEra/MaxEra. Signed century, no century zero.
const MIN_ERA = -40;
const MAX_ERA = 21;

function parseArgs(argv) {
  const args = {
    dryRun: false,
    draftsOnly: false,
    file: resolve(here, 'shmo-figures.json'),
    api: process.env.SABRO_API_URL || 'http://localhost:5082',
  };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--dry-run') args.dryRun = true;
    else if (a === '--drafts-only') args.draftsOnly = true;
    else if (a === '--file') args.file = resolve(process.cwd(), argv[++i]);
    else if (a === '--api') args.api = argv[++i];
    else throw new Error(`Unknown argument: ${a}`);
  }
  return args;
}

/** Strips editorial-only keys (_note and friends) so only API fields are sent. */
const toPayload = (figure) =>
  Object.fromEntries(Object.entries(figure).filter(([key]) => !key.startsWith('_')));

const eraLabel = (era) => (era < 0 ? `${Math.abs(era)}th c. BC` : `${era}th c. AD`);

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

async function listAllFigures(api, token) {
  const byName = new Map();
  const pageSize = 200;
  for (let page = 1; ; page++) {
    const result = await apiFetch(api, token, 'GET', `/api/v1/admin/historical-figures?page=${page}&pageSize=${pageSize}`);
    for (const f of result.items) byName.set(f.name, f);
    if (page * pageSize >= result.total || result.items.length === 0) break;
  }
  return byName;
}

function validate(figures) {
  const problems = [];
  const seen = new Set();

  for (const f of figures) {
    const name = (f.name ?? '').trim();
    const label = name || JSON.stringify(f);

    if (!name) problems.push(`missing name: ${JSON.stringify(f)}`);
    if (name.length > 256) problems.push(`"${label}": name exceeds 256 characters`);
    if (seen.has(name)) problems.push(`"${label}": duplicate name — the answer name must be unique`);
    seen.add(name);

    if (!CATEGORIES.includes(f.category)) problems.push(`"${label}": category "${f.category}" is not one of ${CATEGORIES.join(', ')}`);
    if (!ROLES.includes(f.role)) problems.push(`"${label}": role "${f.role}" is not one of ${ROLES.join(', ')}`);
    if (!REGIONS.includes(f.region)) problems.push(`"${label}": region "${f.region}" is not one of ${REGIONS.join(', ')}`);
    if (!GENDERS.includes(f.gender)) problems.push(`"${label}": gender "${f.gender}" is not one of ${GENDERS.join(', ')}`);

    // Tradition is nullable on a draft but required to publish, so a roster
    // meant for the playable pool must carry one for every figure.
    if (f.tradition === undefined || f.tradition === null) {
      problems.push(`"${label}": missing tradition — required to publish (use NotApplicable where the West/East split does not apply)`);
    } else if (!TRADITIONS.includes(f.tradition)) {
      problems.push(`"${label}": tradition "${f.tradition}" is not one of ${TRADITIONS.join(', ')}`);
    }

    if (!Number.isInteger(f.era)) problems.push(`"${label}": era must be an integer century`);
    else if (f.era === 0) problems.push(`"${label}": era must not be zero — there is no century zero`);
    else if (f.era < MIN_ERA || f.era > MAX_ERA) problems.push(`"${label}": era ${f.era} is outside [${MIN_ERA}, ${MAX_ERA}]`);
  }

  return problems;
}

function printBreakdown(figures) {
  const tally = (key) => {
    const counts = new Map();
    for (const f of figures) counts.set(f[key], (counts.get(f[key]) ?? 0) + 1);
    return [...counts.entries()].sort((a, b) => b[1] - a[1]);
  };

  for (const key of ['category', 'tradition', 'region', 'role', 'gender']) {
    const line = tally(key).map(([value, n]) => `${value}=${n}`).join('  ');
    console.log(`  ${key.padEnd(9)} ${line}`);
  }

  const annotated = figures.filter((f) => f._note).length;
  console.log(`\n  ${annotated} of ${figures.length} figures carry a _note flagging contested or approximate data.`);
  console.log('  Every era is a game hint. Review them before publishing.');
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const raw = JSON.parse(await readFile(args.file, 'utf8'));
  const figures = raw.figures ?? [];
  console.log(`Loaded ${figures.length} figures from ${args.file}`);

  const problems = validate(figures);
  if (problems.length) {
    console.error(`\nDataset has ${problems.length} problem(s):`);
    for (const p of problems) console.error(`  - ${p}`);
    process.exit(1);
  }

  if (args.dryRun) {
    console.log('\n--dry-run: dataset is valid.\n');
    printBreakdown(figures);
    console.log(`\n${figures.length} figures ready. Re-run without --dry-run (and with SABRO_ADMIN_TOKEN set) to publish.`);
    return;
  }

  const token = process.env.SABRO_ADMIN_TOKEN;
  if (!token) {
    console.error('SABRO_ADMIN_TOKEN is required (a Logto access token with the api:v1:admin scope). Use --dry-run to validate without it.');
    process.exit(1);
  }

  console.log(`API: ${args.api}\nFetching existing figures for idempotency...`);
  const existing = await listAllFigures(args.api, token);
  console.log(`Found ${existing.size} existing figure${existing.size === 1 ? '' : 's'}.\n`);

  const summary = { created: 0, published: 0, madePlayable: 0, alreadyDone: 0, failed: 0 };
  for (const f of figures) {
    const label = `${f.name} (${eraLabel(f.era)}, ${f.role})`;
    try {
      let figure = existing.get(f.name);
      const actions = [];

      if (!figure) {
        figure = await apiFetch(args.api, token, 'POST', '/api/v1/admin/historical-figures', toPayload(f));
        summary.created++;
        actions.push('created');
      }

      if (!args.draftsOnly) {
        if (figure.status !== 'Published') {
          figure = await apiFetch(args.api, token, 'POST', `/api/v1/admin/historical-figures/${figure.id}/publish`);
          summary.published++;
          actions.push('published');
        }

        if (!figure.playableInShmo) {
          figure = await apiFetch(args.api, token, 'PUT', `/api/v1/admin/historical-figures/${figure.id}/playable`, { playable: true });
          summary.madePlayable++;
          actions.push('playable');
        }
      }

      if (actions.length === 0) {
        summary.alreadyDone++;
        console.log(`  = ${label}: already ${args.draftsOnly ? 'created' : 'published + playable'}`);
      } else {
        console.log(`  + ${label}: ${actions.join(' -> ')}`);
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
