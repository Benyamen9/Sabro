#!/usr/bin/env node
// Read-only: crawls SEDRA IV's lexeme records and tallies every distinct
// grammatical category string SEDRA actually uses, so it can be checked
// against import-sedra.mjs's CATEGORY_MAP (which was built from a handful
// of samples, not a full crawl — this confirms it's complete and finds any
// SEDRA category spelling that would otherwise silently fall back to "Other").
//
// Makes ZERO writes anywhere — GET requests to the public SEDRA API only,
// no Sabro API calls, no auth needed.
//
// Usage:
//   node scripts/list-sedra-categories.mjs [--range <start>-<end>] [--delay <ms>] [--concurrency <n>] [--out <path>]
//
// --range defaults to the full SEDRA IV lexeme id space (1-38812).

import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { writeFile } from 'node:fs/promises';

const here = dirname(fileURLToPath(import.meta.url));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const SEDRA_API = 'https://sedra.bethmardutho.org/api';

function parseArgs(argv) {
  const args = {
    range: '1-38812',
    delay: 100,
    concurrency: 10,
    progressEvery: 1000,
    out: resolve(here, '..', 'sedra-categories.json'),
  };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--range') args.range = argv[++i];
    else if (a === '--delay') args.delay = Number(argv[++i]);
    else if (a === '--concurrency') args.concurrency = Number(argv[++i]);
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

async function fetchLexeme(id, delayMs) {
  const res = await fetchWithRetry(
    `${SEDRA_API}/lexeme/${id}`,
    { headers: { accept: 'application/json' } },
    { label: `SEDRA /lexeme/${id}` },
  );
  if (delayMs > 0) await sleep(delayMs);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`SEDRA GET /lexeme/${id} -> ${res.status} ${res.statusText}`);
  const body = await res.json();
  return Array.isArray(body) ? (body[0] ?? null) : body;
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

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const ids = buildRange(args.range);
  console.log(`Crawling ${ids.length} SEDRA lexeme ids (range ${args.range}) for distinct categories.`);
  console.log(`concurrency=${args.concurrency} delay=${args.delay}ms progressEvery=${args.progressEvery}`);

  const countByCategory = new Map();
  // Track one example lexeme id + Syriac form per category, for a manual spot-check.
  const exampleByCategory = new Map();
  const summary = { checked: 0, found: 0, missing: 0, noCategory: 0, failed: 0 };
  const startedAt = Date.now();
  let processed = 0;

  function printProgress(force = false) {
    if (!force && processed % args.progressEvery !== 0) return;
    const elapsedMin = (Date.now() - startedAt) / 60_000;
    const rate = processed / Math.max(elapsedMin, 1e-6);
    const remaining = ids.length - processed;
    const etaMin = rate > 0 ? remaining / rate : NaN;
    console.log(
      `-- progress ${processed}/${ids.length} found=${summary.found} missing=${summary.missing} ` +
      `distinctCategories=${countByCategory.size} elapsed=${elapsedMin.toFixed(1)}m rate=${rate.toFixed(1)}/min ` +
      `eta=${Number.isFinite(etaMin) ? etaMin.toFixed(0) + 'm' : '—'}`,
    );
  }

  async function processOne(id) {
    try {
      const lexeme = await fetchLexeme(id, args.delay);
      if (!lexeme) {
        summary.missing++;
        return;
      }
      summary.found++;
      const category = lexeme.category ?? null;
      if (category == null) {
        summary.noCategory++;
        return;
      }
      countByCategory.set(category, (countByCategory.get(category) ?? 0) + 1);
      if (!exampleByCategory.has(category)) {
        exampleByCategory.set(category, { lexemeId: id, syriac: lexeme.syriac ?? null });
      }
    } catch (err) {
      summary.failed++;
      console.error(`  ! ${id}: ${err.message}`);
    } finally {
      processed++;
      summary.checked = processed;
      printProgress();
    }
  }

  await runPool(ids, args.concurrency, processOne);
  printProgress(true);

  const rows = [...countByCategory.entries()]
    .map(([category, count]) => ({ category, count, example: exampleByCategory.get(category) }))
    .sort((a, b) => b.count - a.count);

  console.log(`\nDone. checked=${summary.checked} found=${summary.found} missing=${summary.missing} noCategory=${summary.noCategory} failed=${summary.failed}`);
  console.log(`Distinct categories: ${rows.length}\n`);
  for (const row of rows) {
    console.log(`  ${String(row.count).padStart(6)}  ${row.category}`);
  }

  await writeFile(args.out, JSON.stringify({ summary, categories: rows }, null, 2), 'utf8');
  console.log(`\nReport written to ${args.out}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
