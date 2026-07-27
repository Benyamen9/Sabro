# Shmo — what's done, and what needs your machine

Branch: `feature/shmo-historical-module` (3 commits on top of `main`).

This file is a handoff note, not permanent documentation — delete it once the
checklist below is done.

## What could not be verified here, and why

The container this was built in has **no .NET SDK**, and its network policy
blocks Microsoft's download hosts (`builds.dotnet.microsoft.com` → 403), so the
SDK could not be installed either. Nothing C# was ever compiled or run.

Node *was* available, so everything JavaScript/TypeScript **is** verified — see
the table.

| Area | Status |
|---|---|
| C# — module, services, controllers, tests | **Written, never compiled** |
| EF migrations (both) | **Hand-written, never generated or applied** |
| Frontend backoffice | eslint + `nuxt typecheck` + vitest all pass |
| Seed dataset + seeder script | `--dry-run` passes; validator negative-tested |
| i18n | 95 keys × 5 locales, key sets verified identical |

## Checklist for you

### 1. Build and test the backend

```
dotnet build
dotnet test
```

Expect compiler errors to be the likeliest problem — roughly 3,250 lines of C#
went in without a compiler ever seeing them.

### 2. Verify the hand-written migrations (do not skip)

These were written by copying the exact generated format from
`AddMnoDailyPuzzle` and `InitialLexiconSchema`, including the `.Designer.cs`
files and the model snapshots. They look right, but "looks right" is not the
same as "matches what EF would emit", and a snapshot that silently disagrees
with the model corrupts the *next* migration you generate.

```
dotnet ef migrations has-pending-model-changes \
  --project src/Modules/Sabro.Historical --startup-project src/Sabro.API \
  --context HistoricalDbContext

dotnet ef migrations has-pending-model-changes \
  --project src/Modules/Sabro.Play --startup-project src/Sabro.API \
  --context PlayDbContext
```

If either reports pending changes: delete my migration files for that context
and regenerate with `dotnet ef migrations add ...`. The domain and EF
configuration code is the source of truth and is unaffected.

Files involved:
- `src/Modules/Sabro.Historical/Infrastructure/Migrations/` (all three files)
- `src/Modules/Sabro.Play/Infrastructure/Migrations/20260727120500_AddShmoDailyPuzzle*`
- `src/Modules/Sabro.Play/Infrastructure/Migrations/PlayDbContextModelSnapshot.cs`
  (the `ShmoDailyPuzzle` block was appended by hand)

### 3. Regenerate the API contract

The OpenAPI spec is emitted by the backend build, which never ran here.

```
dotnet build                        # re-emits frontend/openapi/Sabro.API.json
cd frontend && npm run generate:api-types
```

Then retire the hand-written block in `frontend/types/api.ts` — it is marked
`TEMPORARILY HAND-WRITTEN` and carries its own instructions. Replace each
interface with a `Schemas['...']` re-export. Everything imports from
`~/types/api`, so this stays local to that one file.

### 4. Manual smoke test

With a temp M2M admin token (same pattern as prior backoffice work):

```
cd frontend
SABRO_ADMIN_TOKEN=<jwt> node ../scripts/seed-historical-figures.mjs --dry-run
SABRO_ADMIN_TOKEN=<jwt> node ../scripts/seed-historical-figures.mjs --drafts-only
```

Then check:
- `GET /api/v1/play/shmo/today` twice on the same day returns the same figure
- `POST /api/v1/play/results` with `gameId: "shmo"` records, and
  `GET /api/v1/play/results/me?gameId=shmo` returns it
- `GET /api/v1/historical-figures` is anonymous and published-only, and its
  payload carries no `status` or `playableInShmo`
- `/admin/historical-figures` renders and the publish button stays disabled
  until a tradition is set

### 5. Push and open the PR

The repo here was cloned from a local seed bundle with **no git remote**, so
nothing could be pushed and no PR was opened. Fetch the branch from the bundle
sent in chat, or re-apply the commits, then push as usual.

## Content review — the part only you can do

`scripts/shmo-figures.json` holds 150 figures. **Every attribute is a game hint**,
so an approximate era is a wrong answer, not a rounding error. 124 entries carry
a `_note` recording what is conventional, contested, or spans a century
boundary. Read those before publishing anything:

- **Six figures were renamed** so the answer name stays unambiguous — the name
  *is* the answer in Shmo. `Joseph` → `Joseph son of Jacob`, `James` →
  `James son of Zebedee`, `Mary` → `Mary the Mother of Jesus` (true duplicates,
  forced), and `John` → `John the Apostle`, `Simeon` → `Simeon the Righteous`,
  `Timothy` → `Timothy of Ephesus` (judgement calls, too close to a qualified
  namesake). `Isaac`, `Jacob`, `Thomas` and `Paul` were left bare on the view
  that the unqualified name conventionally means the biblical figure. Worth a
  playtest.
- **Relatives are dated with the figure they attach to**, so a household shares
  a century — Rachel, Leah, Esau and Laban all read -18 with Jacob. Honest (they
  are contemporaries) but it means era is a coarse filter in the biblical
  categories and a sharp one in the patristic.

- **The ten primeval figures** (Adam, Eve, Cain, Abel, Seth, Enoch, Methuselah,
  Noah, Shem, Nimrod) — marked `PRIMEVAL` in their notes. Chronology-dependent
  rather than historical; see point 6 below. Anchored to birth, so Noah reads as
  -30 (born c. 2948 BC) rather than -24 (the flood). If you would rather the
  hint point at the event a player thinks of, Noah and Nimrod are the two to
  revisit.
- **Jeremiah** — called c. 627 (7th c.), anchored here to 586 (6th c.). Either
  is defensible; pick one.
- **Moses** — follows the late-date Exodus (13th c.). Early-date would be -15.
- **Daniel** — narrative setting is 6th c.; the book's composition is 2nd c. BC.
- **Timothy I** — catholicos 780–823, anchored to the start (8th c.).
- **Jacob of Serugh, Moshe bar Kepha, Jacob of Edessa** — lives spanning two
  centuries, anchored to death or episcopate.
- The patriarchal dates (Abraham through Joseph) are conventional Middle Bronze
  placements and are genuinely debated.

`--drafts-only` exists so you can create everything and publish selectively
after review.

## Six model/domain mismatches worth a decision

Populating the roster surfaced these. None block anything; each is a data choice
or a small enum change.

1. **`Patriarch` is ambiguous.** It reads as the ecclesiastical office, so the
   biblical patriarchs (Abraham, Isaac, Jacob) are `Other`. Category already
   separates them, so this is cosmetic — but it is a visible hint in the game.
2. **Pre-Chalcedonian fathers have no honest `Tradition`.** Ephrem, Aphrahat,
   Rabbula, Simeon the Stylite, Bardaisan, Mar Awgin, Febronia, Ibas all predate
   the West/East split, so they share `NotApplicable` with pre-Christian
   biblical figures. A `PreChalcedonian` member would separate them and make the
   hint more informative.
3. **No `Priest` role** — Aaron and Ezra are `Other`.
4. **No India in `Region`** — Thomas, the apostle of the East, is `Persia`.
5. **Single primary role forces picks** — John and Matthew are `Apostle` rather
   than `Evangelist`; Jacob of Edessa is `Bishop` rather than `Translator`.
6. **~~`MinEra` excludes primeval figures~~ — resolved, but read this.** `MinEra`
   was widened from -40 to **-60** and ten primeval figures (Adam through
   Nimrod) added. Two consequences worth your sign-off:
   - Their eras are **chronology, not history**. They follow the
     Masoretic/Ussher reckoning (creation c. 4004 BC). A Septuagint-based
     chronology puts creation near 5500 BC and would move all ten by roughly
     fourteen centuries. These are the weakest hints in the roster.
   - Widening the bound **relaxes a typo guard**. A mistyped year like -1500 in
     the era field used to be rejected and now passes. That was the bound's
     original purpose.

   If either bothers you, the cheapest fix is to drop the ten primeval entries
   from `shmo-figures.json` and set `MinEra` back to -40.

## Two judgment calls made without you

- **The public roster projection omits `Status` and `PlayableInShmo`**
  (`HistoricalFigureListItem`, separate from `HistoricalFigureDto`). Without
  this, clients could enumerate the future puzzle pool — the exact leak
  `DictionaryEntryListItem` already guards against for Meltho. There is an
  integration test pinning it.
- **`scripts/apply-migrations.sh` gained `HistoricalDbContext`**, ordered before
  Play since the Shmo puzzle points at a figure. Without it the schema would
  never be created in production. This was not in the plan and is easy to miss
  in review.

## Not started

Stage 4, the Shmo client app, is a separate future session. When it happens:
the `markPlayed` / daily-circuit mechanics must be checked against the Meltho
and Mno frontend repos directly. This repo's `useDailyCircuit.ts` is read-only
(`hasPlayed` / `nextUnplayed`) and has no `markPlayed` at all, so the plan's
description of "three copies to update" does not match what is actually here.
