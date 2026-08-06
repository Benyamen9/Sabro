// Finding one Lexicon entry by its unvocalized form, safely.
//
// `GET /api/v1/admin/lexicon` is served by Meilisearch, and **Meilisearch stops
// returning results after 1,000**. Both seeding scripts used to page through the
// unfiltered list to build a form -> entry map for their idempotency check. That
// was correct when the Lexicon held 42 words and became silently wrong the day
// the SEDRA import took it past 32,000: page 6 comes back empty, the loop breaks,
// and the map holds the first 1,000 entries — which do not include the launch
// pool. Every launch word then looks absent.
//
// For the enricher that meant doing nothing and reporting 42 words missing. For
// the seeder it meant something far worse: not-found is its signal to CREATE, so
// a re-run would have added a duplicate of all 42 published, playable words.
//
// The fix is to stop enumerating. A search narrowed to one form returns a handful
// of rows, nowhere near the cap, and the caller matches on exact NFC equality.
//
// Exactness is the second half of it. Searching is fuzzy by design — SEDRA holds
// bare twins of the pool's plurals, so a search for `ܡܝ̈ܐ` also returns `ܡܝܐ`, a
// different word that once received another word's German and Swedish glosses.
// Ranking must never decide which entry gets written to; only an exact match on
// the unvocalized form may.

/** NFC everywhere: the API stores normalised text, so comparisons must normalise too. */
export const nfc = (value) => (value ?? '').normalize('NFC');

/**
 * Every entry whose unvocalized form is exactly `form`.
 *
 * Returns an array because "exactly one" is a fact the caller has to check
 * rather than assume — duplicates are precisely what the bug above created, and
 * a script that silently picks the first of two would hide them.
 *
 * @param {(method: string, path: string, body?: unknown) => Promise<any>} apiFetch
 * @param {string} form unvocalized Syriac, any normalisation
 * @param {{ status?: 'Draft' | 'Published' }} [options] narrows the search server-side
 */
export async function findEntriesByForm(apiFetch, form, options = {}) {
  const wanted = nfc(form);
  const pageSize = 200;
  const query = new URLSearchParams({ search: wanted, page: '1', pageSize: String(pageSize) });
  if (options.status) query.set('status', options.status);

  const result = await apiFetch('GET', `/api/v1/admin/lexicon?${query}`);
  const items = result?.items ?? [];

  // A form matching more than a page of entries means the search did not narrow
  // anything and an exact match may be sitting on a page we never asked for.
  // Refusing beats matching against a slice.
  if ((result?.total ?? 0) > items.length) {
    throw new Error(
      `Search for "${wanted}" reports ${result.total} matches but returned ${items.length}. `
      + 'Narrow the search or raise pageSize — never match against a partial result.',
    );
  }

  return items.filter((entry) => nfc(entry.syriacUnvocalized) === wanted);
}

/**
 * The one entry with this form, or null.
 *
 * Throws when several share it. That is a real state — the duplicate-creating
 * bug above could have produced it — and no script should guess which of them
 * to publish, enrich, or overwrite.
 */
export async function findEntryByForm(apiFetch, form, options = {}) {
  const matches = await findEntriesByForm(apiFetch, form, options);
  if (matches.length > 1) {
    const ids = matches.map((entry) => `${entry.id} (${entry.status})`).join(', ');
    throw new Error(
      `"${nfc(form)}" matches ${matches.length} entries: ${ids}. `
      + 'Resolve the duplicates before running this script.',
    );
  }
  return matches[0] ?? null;
}

/**
 * A whole small collection in one request, refusing anything it could not fetch
 * entirely. For Postgres-backed lists (roots), where paging is honest but a
 * silent truncation would still be invisible.
 */
export async function fetchAllOrThrow(apiFetch, path, { pageSize = 500, label = path } = {}) {
  const separator = path.includes('?') ? '&' : '?';
  const result = await apiFetch('GET', `${path}${separator}page=1&pageSize=${pageSize}`);
  const items = result?.items ?? [];

  if ((result?.total ?? 0) > items.length) {
    throw new Error(
      `${label} holds ${result.total} rows but only ${items.length} came back. `
      + 'Raise pageSize rather than let this run against a partial list.',
    );
  }

  return items;
}
