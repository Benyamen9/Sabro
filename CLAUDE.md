# CLAUDE.md — Sabro

## Project Overview

Sabro is an academic web platform for publishing original English translations of 12th-century Syriac patristic commentaries by Dionysios bar Ṣalibi (Metropolitan of Amid, d. 1171, Syriac Orthodox). It serves as the central backend in an ecosystem of applications dedicated to Syriac language and patristic studies, exposing a versioned REST API consumed by client applications — **four at present: Meltho (words), Mno (numerals), Shmo (figures) and Nahlo (chants)**, each in its own repository. Sabro also acts as the **ecosystem hub**: it owns shared identity (via Logto), user profiles, and cross-application play data.

The long-term scope includes translating the entire Bible (Peshitta) and works of various Syriac Church Fathers.

---

## Build Sequencing (current focus)

The long-term scope (full Peshitta + Church Fathers) is unchanged, but the build order has been re-sequenced. Translation work is set aside for now; the immediate goal is a launched, living ecosystem.

1. ✅ Sabro shipped as the ecosystem hub + API: **Lexicon**, **Identity/Profile**, **Play**, the `/api/v1/` contract, Logto, and the hub frontend.
2. ✅ **Meltho** launched against that API, then **Mno**, **Shmo** and **Nahlo** followed on the same foundation — the multi-game model held, with no rework.
3. **Translations and Biblical remain deferred** — fully specified below, but not on any current critical path. No game depends on translated content. **Reviews is partly active**: its field-proposal workflow shipped with the backoffice and runs in production; only its three-level prose review stays deferred.

**Current state.** Four games are deployed behind Caddy and watched by UptimeRobot. Three are in the daily circuit; **Nahlo is deployed but not yet offered** — its treasury holds no recordings, so `GET /play/nahlo/today` answers 409 and handing a player there would end their circuit on a closed door. See *Ecosystem Clients* for the standing instruction that unblocks it.

**Hub philosophy — wide model, narrow surface.** The data foundation was built to see far (multi-game results, profile, cross-project shape) while the launch UI stayed lean. That bet paid: three further games landed on the same `GameResult` shape without a migration. Keep new surfaces additive for the same reason.

---

## Outstanding Worklist

Live handoff list, accurate as of **2026-08-24**. Each item is written to be
executed cold — context, exact change, how to verify, and what needs a human
decision. **Delete an item from this file the moment it lands**; a worklist that
outlives its work is the same drift this file exists to prevent.

To pick this up in a fresh session, paste:

> Read the *Outstanding Worklist* in CLAUDE.md and start with item N. Follow the
> "Do" steps, run the "Verify" checks before pushing, and stop and ask me on
> anything the item marks **Decide**. One PR per item unless it says otherwise.

Ordered by what unblocks most. Item 1 needs a human and an external service; 2 and
3 do not; 4 waits on the recordings themselves.

---

### 1. Nahlo and analytics are unmonitored ⚠️ **Decide** (external service)

Caddy serves `{$NAHLO_DOMAIN}` and the container is deployed, but the five
UptimeRobot monitors predate Nahlo's slot (#199), so its downtime is invisible.
`analytics.sabro.be` likewise. Add a sixth monitor on `https://nahlo.sabro.be`
(HTTP(s), 5-minute interval, alerting to the Owner's personal mailbox — **not**
`contact@sabro.be`, which forwards to Hotmail and is dropped silently).

Cannot be done from a Claude Code session: UptimeRobot is an external service with
no credentials in the repo, and `*.sabro.be` is unreachable from the sandboxed web
environment. Desktop or the UptimeRobot console only.

### 2. The integration fixtures do not run the production versions

The suite signs off migrations and index behaviour against software production
does not run:

| Fixture | Pins | Production runs |
|---|---|---|
| `PostgresFixture` | `postgres:16-alpine` | `postgres:17-alpine` |
| `MeilisearchFixture` | `getmeili/meilisearch:v1.13` | `v1.53` |

**Do — two separate PRs, Postgres first.** If something breaks you want to know
which bump did it.

1. `tests/Sabro.IntegrationTests/PostgresFixture.cs` → `new PostgreSqlBuilder("postgres:17-alpine")`
2. `tests/Sabro.IntegrationTests/MeilisearchFixture.cs` → `new ContainerBuilder("getmeili/meilisearch:v1.53")`

**v1.53**, not v1.51 — production moved on 2026-08-24 (#223). Keep the fixture and
the compose pin moving together from here.

**Verify.** `dotnet test` locally — this is the one item that genuinely needs the
real suite, since the point is to find behaviour that differs between versions.
The Postgres bump exercises every module's migrations; a failure here is a real
finding about production, not a test problem.

### 3. NuGet drift

**3a — patch bumps, mechanical, one PR. In flight as PR #224.** Sixteen packages sit one patch behind in
`Directory.Packages.props`: everything pinned `10.0.10` → `10.0.11` (the
`Microsoft.Extensions.*` set, `Microsoft.AspNetCore.Authentication.JwtBearer`,
`Microsoft.AspNetCore.OpenApi`, `Microsoft.Extensions.ApiDescription.Server`,
`System.Security.Cryptography.Xml`, the three `Microsoft.EntityFrameworkCore*`,
`Microsoft.AspNetCore.Mvc.Testing`), plus `coverlet.collector` 10.0.0 → 10.0.1,
`FluentAssertions` 8.9.0 → 8.10.0 and `Microsoft.NET.Test.Sdk` 18.5.1 → 18.9.0
(both test projects; missed by the first pass of this list, re-verified
2026-08-24 with `dotnet list package --outdated`).

**3b — majors, one PR each, in this order.** `Markdig` 1.1.3 → 1.3.2 ·
`Meilisearch` 0.18.0 → 0.20.0 · `NSubstitute` 5.3.0 → 6.2.0 · `Serilog.AspNetCore`
9.0.0 → 10.0.0 · `xunit.v3` 3.2.2 → 4.0.0 with `xunit.runner.visualstudio`
3.1.5 → 4.0.0 (together) · `Asp.Versioning.Mvc` + `.ApiExplorer` 8.1.0 → 10.2.1
(together, two majors — expect real work).

> **`Microsoft.OpenApi` stays at 2.9.0.** Deliberate, and the comment above it says
> why: the 2.x line is what `Microsoft.AspNetCore.OpenApi` 10.0.0 is compatible
> with. Do not "fix" it to 3.x.

Remember `TreatWarningsAsErrors` is on: a new obsoletion in any of these becomes a
build failure, exactly as Testcontainers 4.14.0's builder constructors did (#218).

### 4. When the chant recordings land

Not a code task on its own, but it is the keystone — it opens Nahlo and unblocks
item 1 and the circuit note. In one pass:

1. Upload recordings via `/admin/chants`, publish, set `PlayableInNahlo`.
2. Confirm `GET /api/v1/play/nahlo/today` stops answering 409.
3. Put `'nahlo'` back into `CIRCUIT_HANDOFF` in **all five** copies of
   `useDailyCircuit.ts` — Sabro hub, Meltho, Mno, Shmo, Nahlo.
4. Add the UptimeRobot monitor from item 1.
5. Consider raising `Nahlo:AntiRepetitionWindowDays` from 7 toward the siblings'
   30 as the treasury grows.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 (LTS) |
| Database | PostgreSQL (via Entity Framework Core) — single shared database for the whole ecosystem |
| Search | Meilisearch (typo-tolerant, dedicated service) |
| Frontend | Nuxt 4 (Vue 3 + TypeScript strict) — the hub and all four game clients; Tailwind v4 in the clients |
| Auth | Logto (self-hosted IDP, OIDC) |
| Validation | FluentValidation |
| Markdown | Markdig |
| i18n | @nuxtjs/i18n — **EN, FR, NL, DE, SV in every frontend**, hub and clients alike |
| Logging | Serilog + Seq |
| Monitoring | Health check endpoint (live) + UptimeRobot (live since 2026-07-28) |
| Backups | pgBackRest (point-in-time recovery) |

**Development environment:** Windows + Visual Studio

---

## Architecture

### System Level
Sabro exposes a versioned REST API (`/api/v1/`) consumed directly by client applications. There is no API gateway or hub component — clients call Sabro's API directly. Authentication is delegated to a self-hosted Logto instance shared across all applications in the ecosystem.

**Single source of truth.** There is **one shared PostgreSQL database** for the entire ecosystem, owned by Sabro. Client applications (Meltho, Mno, Shmo, Nahlo, and any future app) do **not** have their own application database and never connect to PostgreSQL directly. They read content and write their own play data exclusively through Sabro's API. (Logto keeps its own internal store for auth — that is infrastructure, not ecosystem application data.)

```
        ┌─────────────────────────┐
        │   Logto (IDP central)   │
        │   self-hosted, OIDC     │
        └────────────┬────────────┘
                     │ JWT validated via JWKS
     ┌──────────┬─────────┼─────────┬──────────┐
     ▼          ▼         ▼         ▼          ▼
 [Sabro hub] [Meltho]   [Mno]    [Shmo]    [Nahlo]
     │          │         │         │          │
     └──────────┴─────────┴─────────┴──────────┘
         reads content / writes play results
                     │
                     ▼
        ┌─────────────────────────┐
        │    /api/v1/  (Sabro)    │
        └────────────┬────────────┘
                     ▼
        ┌─────────────────────────┐
        │ PostgreSQL (single DB)  │  ← Sabro is the only writer of record
        └─────────────────────────┘
```

Clients are read-only consumers of Sabro's **content** (lexicon, translations). They may write their own **play data** (game results) through controlled, authenticated API endpoints — never by touching the database. See *Key Business Rules*.

### Application Level — Modular Monolith
The backend is organized as a modular monolith. Each module is self-contained with its own domain, application, infrastructure, and public interface. Modules communicate only through explicit public interfaces — never through direct internal references.

```
Sabro/
├── src/
│   ├── Sabro.API/                  ← Entry point, controllers, middleware
│   ├── Sabro.Shared/               ← Shared types, interfaces, base classes
│   └── Modules/
│       ├── Sabro.Lexicon/          ← Words, roots, morphology, transliteration, playable pool
│       ├── Sabro.Identity/         ← User profiles, area grants (Logto integration)
│       ├── Sabro.Historical/       ← Historical figures and their attributes (feeds Shmo)
│       ├── Sabro.BethGazo/         ← Chants, modes, sections, recordings (feeds Nahlo)
│       ├── Sabro.Play/             ← Cross-game results + the four daily-puzzle states
│       ├── Sabro.Translations/     ← Translations, versioning, multilingual content (DEFERRED)
│       ├── Sabro.Reviews/          ← Field proposals (ACTIVE); peer review 3 levels (DEFERRED)
│       └── Sabro.Biblical/         ← Biblical passages (Peshitta), cross-references (DEFERRED)
├── frontend/                       ← Nuxt application (public hub + admin backoffice)
│   ├── pages/
│   │   └── admin/                  ← Backoffice (role-gated)
│   ├── components/
│   ├── composables/
│   └── i18n/locales/               ← i18n files (en, fr, nl)
├── tests/
│   ├── Sabro.UnitTests/
│   ├── Sabro.IntegrationTests/
│   └── Sabro.E2ETests/             ← Playwright
├── wwwroot/
│   └── media/                      ← Local media (bibliography source covers)
├── .github/workflows/              ← CI pipelines
└── CLAUDE.md
```

### Internal Module Structure
Each module follows the same internal layout:

```
Sabro.{ModuleName}/
├── Domain/           ← Entities, value objects, domain rules
├── Application/      ← Use cases, commands, queries, DTOs
├── Infrastructure/   ← EF Core DbContext, repositories, migrations, Meilisearch sync
└── Public/           ← Public interface exposed to other modules
```

---

## Modules

### Lexicon
Manages the Syriac lexical database. Each entry includes the Syriac form (canonical, unvocalized), an optional vocalized form, the Semitic root, the SBL transliteration with accepted variants, grammatical category, morphology, and meanings (multilingual). Foundational data layer consumed by all other modules and by client applications — including Meltho, whose word pool is drawn entirely from here.

**Required vs optional fields.** The unvocalized Syriac form is required. The vocalized form and SBL transliteration are optional enrichment — they do not gate publication. Meanings are required in **every configured content language** for an entry to be publishable — currently **five**: EN, FR, NL, DE, SV.

The list is not hardcoded: it is `SupportedLanguages:Codes` (`SupportedLanguagesOptions`, default `["en","fr","nl","de","sv"]`), and the publish check asks for all of them. Adding a language therefore raises the publication bar for every future entry, which is the intended behaviour — but note it does **not** unpublish anything: the gate is applied at publish time only.

> **The 42 published entries are mixed** — some carry all five glosses, most only EN/FR/NL, from when the rule was three languages. They are grandfathered, not broken. Do not "fix" them by unpublishing; do not read the three-language ones as evidence that the rule is three.

**Entry lifecycle — `Draft` → `Published`.** An entry may be saved as `Draft` with partial data (Syriac form today, the other glosses later) without loss. It becomes `Published` only when every configured language has a gloss. Only `Published` entries may be marked playable or served to clients. This is how the language rule holds without forcing every entry to be finished in one sitting — the pool can be populated incrementally.

**Playable flag (`PlayableInMeltho`).** A manual editorial boolean set by the Owner. The Lexicon is broader than the puzzle pool — the Owner decides which published words make good puzzles. This is editorial curation, not an automatic property.

**Playable length — computed, read-only.** Derived from the unvocalized form as the count of base Syriac letters (Unicode letter-category characters). Combining marks (vowel points, seyame, diacritics) are not counted. Shown read-only in the backoffice so the editor can see at a glance whether a word falls in the 2–8 window. Never hand-entered.

**Eligible pool definition.** An entry is in the Meltho pool **iff** `Published` AND `PlayableInMeltho == true` AND `playable length ∈ [2, 8]`. The daily-selection logic (Play module) additionally enforces the 2–8 bound server-side as a hard guard, so a mis-flagged out-of-range entry can never reach the game.

### Identity / Profile
Owns **who the user is**. Authentication itself is delegated to Logto via OIDC; Sabro stores only the profile data it needs, linked by Logto user ID: display name, preferred language, default script variant, and personal preferences. Roles: Owner (translator/admin), Expert Reviewer (invited), Reader (public, optional account for personal features like notes, favorites, and game profile).

The hub's "my profile" surface reads from this module. Play history (what the user played) lives in the **Play** module, not here — Identity is identity, Play is activity. The dashboard composes both.

### Play
Owns **ecosystem play data**: cross-game results and the daily-puzzle state of all four games. This module exists because, with a single shared database and no per-client database, shared play state must live in Sabro. It was built multi-game from day one, and that has now been proved three times over — Mno, Shmo and Nahlo each landed on the same `GameResult` shape without a model change.

**`GameResult` — generic, multi-game.** Keyed by Logto user ID + a `GameId` string discriminator. The identifiers are constants on `Sabro.Play.Domain.Games` — `meltho`, `mno`, `shmo`, `nahlo` — never string literals at call sites. Fields: `PlayedOn` (date), `Solved` (bool), `Attempts` (int), and an optional `DetailJson` for game-specific extras. Unique constraint on (`LogtoUserId`, `GameId`, `PlayedOn`) — one result per user, per game, per day. Streaks and aggregates are **derived** from results, not stored. This generic shape is the cross-project equivalent of the savant/ludic split used elsewhere: do not model a Meltho-specific scores table.

**Four daily puzzles — shared server state.** One entity per game (`MelthoDailyPuzzle`, `MnoDailyPuzzle`, `ShmoDailyPuzzle`, `NahloDailyPuzzle`), each recording what was served on which day. Selection is **get-or-create per date** (idempotent): the first request for a given day picks, records, and returns it; subsequent requests return the recorded one, so every player gets the same puzzle.

Three of the four draw from a curated pool and exclude anything served inside the **anti-repetition window**. **Mno is the exception** — its equations are *generated* (`MnoEquationGenerator`), so there is no pool to exhaust and it carries no window option at all.

**Anti-repetition window — per game, configurable, never hardcoded.** Each pool-backed game owns its own options class and its own config key, because the pools are different sizes:

| Game | Config key | Default | Why |
|---|---|---|---|
| Meltho | `Meltho:AntiRepetitionWindowDays` | 30 | Pool grown past launch size |
| Shmo | `Shmo:AntiRepetitionWindowDays` | 30 | Same |
| Nahlo | `Nahlo:AntiRepetitionWindowDays` | **7** | The chant treasury starts at zero — a 30-day window would starve it immediately |
| Mno | *(none)* | — | Generated, not drawn from a pool |

Start a window low so a small pool never starves, and raise it as the pool grows. A hardcoded 365 against a 40-item pool would leave the selector with nothing eligible after 40 days and break the game — these must remain config values.

**Boundary with the clients.** Sabro answers *"what is today's puzzle"* (shared state, single DB, central anti-repetition) and stores the final daily result. Every client owns its own **game mechanics**: guess evaluation, feedback colouring, the attempt ladder, and share text. **The full answer ships with the puzzle** — evaluation is client logic, and Sabro never learns what a player guessed, only whether they solved it and in how many attempts. Daily selection is the one piece of shared game state that lives in Sabro rather than the client, precisely because it must be identical for all players and persisted.

**Beyond the puzzle.** Play also serves `meltho/library` (past words, public and anonymous — today's word is never included, it would spoil the live puzzle) and `meltho/leaderboard` (signed-in only, ranked by longest streak; appearing in it requires an explicit opt-in on the player's profile, while the caller's own standing is always shown).

### Historical
Owns the roster of **historical figures** — biblical figures through the Syriac Church Fathers and beyond — and is the content behind **Shmo**. Each figure carries the attributes Shmo scores a guess on: `Category`, `Era` (a numeric year, diffed as higher/lower with near/far tiers), `Period` (a chronological but non-numeric enum, diffed by position in the declared sequence), `Role`, `Region`, `Tradition` (nullable), and `Gender`.

Same lifecycle as the Lexicon: `Draft` → `Published`, plus a manual `PlayableInShmo` editorial flag that only a published figure may carry.

> **The enum order is a contract.** `HistoricalPeriod` is diffed *by ordinal position*, so reordering its members silently changes every near/far verdict in Shmo. Append new periods; never reorder. Shmo hand-copies these enums into `app/types/figure.ts` and guards the copy with `test/figure.test.ts` — see *Ecosystem Clients*.

### BethGazo
Owns the **Beth Gazo** — the treasury of chants — and is the content behind **Nahlo**.

**A chant is identified by four things, not one:** its melody name, its section, its mode where the section has one, and its variant. A melody name recurs across modes, so "Maryam yoldath Aloho" names a family rather than a chant; only "Maryam yoldath Aloho, Tlithoyo" picks one out. That is the whole reason the game works — were the mode derivable from the name, naming the melody would hand the player the mode for free.

**Sections and modes are reference tables**, editable by the Owner, not hardcoded sets. There are ten sections and nine modes (the farde 1–8 *plus* mshaḥelfotho). `Chant.ModeId` is **nullable**, and its null means *"this section has no modes"* — never *"nobody has filled it in yet"*: the madroshe have no mode at all, and mshaḥelfotho belongs to the farde alone. Both rules are carried by `BethGazoSection.AllowedModes`.

> **Never hardcode eight modes.** The list is served in full by `GET /chants/answer-options` and can grow again. Nothing in Sabro or in Nahlo counts modes, and nothing should.

**`ChantVariantKind` distinguishes two things the book prints.** A **shuḥlofo** is a variation *of the melody itself* — the same qolo sung another way. A **ḥrino** (ܐܚܪܢܐ, "another"; pl. *ḥrone*) is simply another chant standing in the same mode, not a variant of anything. They must be told apart or a *shuḥlofo 1* and a *ḥrino 1* under one melody and mode collide on the same four values, making the second unsaveable. A string-converted enum, per the house rule.

Recordings are uploaded through the backoffice and stored via `IChantAudioStorage` (`FileSystemChantAudioStorage` at this scale). Lifecycle mirrors the Lexicon: `Draft` → `Published` + a manual `PlayableInNahlo` flag.

### Translations
**Status: deferred. Spec retained, not built first.**

Manages original English translations of biblical books (Peshitta) and patristic works (starting with Dionysios bar Ṣalibi's commentaries). All content is added progressively (chapter by chapter, verse by verse). Every change creates a new version — full history is preserved. Content is authored in Markdown (rendered via Markdig). The schema supports multilingual content from day one (EN at MVP, FR + NL planned).

### Reviews
**Status: PARTLY ACTIVE.** The **field-proposal workflow is live** — area reviewers propose a
correction to one field of a Lexicon entry or historical figure, and the Owner accepts or rejects it.
That means `ReviewsDbContext` is an **active schema in production** and must stay in
`scripts/apply-migrations.sh`; it was missing there until 2026-08-03, which made every proposal in
production fail on `relation "reviews.suggested_edits" does not exist` while working locally.

The three-level peer review below (verse / chapter / annotation) is still **deferred**, because it
reviews translated prose and the Translations module is deferred.

Three-level peer review system *(deferred)*:
- **Verse-level** — individual verse translations
- **Chapter-level** — chapter-wide validation with cascade logic to verses
- **Annotation-level** — inline annotations and cross-references

Includes a suggested edits workflow: invited expert reviewers propose corrections; the translator (Owner) accepts or rejects each suggestion. Suggestions never modify content directly.

### Biblical
**Status: deferred. Spec retained, not built first.**

Manages Syriac biblical passages from the Peshitta. Stores passage references and links them to lexicon entries and translation annotations.

Cross-references are typed on two independent axes:

- **Source** — who originated the reference:
  - `Author` — the commentator (bar Ṣalibi) cites it within the source text itself, marked in the manuscript by a citation siglum. Part of the translated work; evidence of how the Father reads the passage.
  - `Editorial` — a parallel added by the translator as apparatus (e.g. "cf. Ps 22:8"), not present in the commentator's text.
- **Kind** — the nature of the reference:
  - `Quotation` — explicit, verbatim or near-verbatim, typically siglum-marked.
  - `Allusion` — an unmarked echo or substructure; the passage is in view but not quoted or named.

Both stored as **string-converted enums** (`reference_source`, `reference_kind`), not native PostgreSQL enum types — so new values are added with a plain code change and an ordinary migration, never raw `ALTER TYPE` SQL. Both surface in the cross-reference API DTO and are therefore part of the `/api/v1/` contract: adding values later is safe, renaming existing ones (`Author`/`Editorial`/`Quotation`/`Allusion`) is a breaking change for clients.

For MVP: biblical cross-references only. Patristic and external citations deferred — the two-axis typing applies within biblical cross-references and does not bring deferred citation categories forward.

**Worked example — gloss on Psalm 3:3** ("...you have no salvation in your God"): the commentator's gloss produces two cross-references on the same multi-verse `AnnotationAnchor`:
- Matthew 27:40, 43 → `Author` + `Quotation` (he quotes it; siglum in the text; anchor spans two verses)
- Psalm 22:8 → `Editorial` + `Allusion` (the substructure behind the Matthew taunt; the editor records it, the commentator does not name it)

---

## Ecosystem Clients

Four game clients consume Sabro's API, each in **its own repository**, each its own OIDC application in the shared Logto tenant, each its own container behind Caddy. All four are Nuxt 4 + Tailwind v4 + Vitest, and all four ship **EN, FR, NL, DE and SV**.

| App | Repo | Game | Dev port | Sabro content behind it |
|---|---|---|---|---|
| **Meltho** (ܡܠܬܐ, "word") | `Benyamen9/Meltho` | Wordle on Syriac words; 6 guesses, green/yellow/grey | 3100 | Lexicon |
| **Mno** (ܡܢܐ, "he counted") | `Benyamen9/Mno` | Mathler on additive Syriac alphabetic numerals; 6 guesses | 3200 | *(generated)* |
| **Shmo** (ܫܡܐ, "name") | `Benyamen9/Shmo` | Pokédle on historical figures; **unlimited guesses, no losing state** | 3300 | Historical |
| **Nahlo** (ܢܚܠܐ) | `Benyamen9/Nahlo` | Name the chant — melody + mode + shuḥlofo (yes/no); 6 guesses, green/grey only | 3400 | BethGazo |

The hub runs on :3000 and Logto on :3001/:3002.

> **Nahlo's spelling came from the Owner** and is not derived from anything — do not "correct" it against a lexicon. Likewise the Owner's ruling that the third answer part is **yes/no** ("does this melody have a variation at all"), never *which* shuḥlofo: the cost is known and accepted.

### This file is the ecosystem's memory — fold changes back into it

The four client repos carry no `CLAUDE.md`. Their **`README.md` is each app's memory**, and it records Owner rulings and reasoning that exist nowhere else. Sabro's `CLAUDE.md` is the only file loaded automatically, so:

**When anything in a client README changes — a rule, an Owner ruling, a port, a config default, a language — fold it into this file in the same change.** Each client README carries a matching pointer back here. A fact that lives only in a client repo is a fact this file will eventually contradict.

### Mirrored contracts — duplicated across repo boundaries

Four pieces of logic are deliberately implemented twice, on either side of a repo boundary. Only one has a drift guard; treat the rest as hand-maintained and check them whenever the Sabro side moves.

| Mirror | Sabro side | Client side | Guard |
|---|---|---|---|
| Historical enums | `Sabro.Historical.Domain` | Shmo `app/types/figure.ts` | ✅ `test/figure.test.ts` |
| Syriac letter counting | `playableLength` | Meltho `app/utils/syriac.ts` | ❌ none |
| Numeral speller | Sabro's C# speller | Mno `app/utils/numerals.ts` | ❌ none |
| Daily circuit | hub `useDailyCircuit.ts` | one copy in **each** client | ❌ comment only |

**The daily circuit** is one cookie shared across `*.sabro.be` (`sabro_daily_played`), and the composable exists in **five** copies — hub, Meltho, Mno, Shmo, Nahlo. `CIRCUIT_GAMES` lists all four games in all five copies; `CIRCUIT_HANDOFF` currently omits `nahlo`, because handing a player to a game that answers 409 ends their circuit on a closed door.

> ⚠️ **When the chant recordings land, put `'nahlo'` back into `CIRCUIT_HANDOFF` in all five repos.** Nothing fails if a copy is missed — the copies simply disagree about which door to open next. *Tracked as item 4 of the Outstanding Worklist.*

## Backoffice (Editorial Admin)

The editorial write surface for Sabro's own content. It is **part of Sabro, not a client** — the "clients are read-only consumers" rule does not apply to it. It is the content-write surface for the whole ecosystem: every game's pool is populated here.

**Placement.** Admin routes inside the existing Sabro Nuxt frontend (`/admin/...`), gated by an area grant from Logto. No separate admin app — separation is by authorization, not by deployment.

**Write path.** All writes go through the same Application layer and FluentValidation as the rest of Sabro, via admin-scope API endpoints. No parallel, unvalidated write path.

**Current surface** — one section per content type, each following the same shape (list → edit, `Draft` ↔ `Published`, a playable toggle only on published rows):

| Section | Feeds | Notes |
|---|---|---|
| `/admin/lexicon` | Meltho | Unvocalized + optional vocalized Syriac (NFC on input), optional SBL transliteration, a gloss per configured language. Publish gated on a gloss in **every** configured language; computed playable length shown read-only |
| `/admin/historical-figures` | Shmo | The scoreable attributes; `PlayableInShmo` |
| `/admin/chants` | Nahlo | Melody, section, mode, variant + **recording upload**; `PlayableInNahlo`. `/admin/chants/sections` edits the sections and their allowed modes |
| `/admin/proposals` | — | Field proposals from area reviewers; the Owner accepts (applying the change) or rejects |
| `/admin/people` | — | Area grants; owner changes require a confirmation step |

**Still deferred (model kept ready, no UI):** liturgical calendar / manual daily-puzzle pinning; player statistics dashboards.

---

## API Design

- All endpoints versioned from day one: `/api/v1/...`
- RESTful conventions throughout
- JSON responses with consistent envelope structure
- Authentication via JWT bearer tokens validated against Logto's JWKS endpoint (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Scope-based authorization:
  - `api:v1:read` — public content reads
  - `api:v1:write` — authenticated user writes (e.g. recording own game results)
  - `api:v1:admin` — Owner-only editorial and operational endpoints (backoffice, search rebuilds)
- Rate limiting applied to all public endpoints
- OpenAPI/Swagger documentation generated automatically (used to generate TypeScript types for the frontend)

### Key endpoints

**Content (read):**
- `GET /api/v1/lexicon/...` — published lexicon reads.

**Daily puzzles (read, shared state)** — get-or-create per date, identical for all players, each respecting its own anti-repetition window:
- `GET /api/v1/play/meltho/today`
- `GET /api/v1/play/mno/today`
- `GET /api/v1/play/shmo/today`
- `GET /api/v1/play/nahlo/today` — **answers `409` when no chant is eligible** (empty pool, or one too small for the window). That is a normal state, not an error: clients render it as "no chant today".

**Client support reads:**
- `GET /api/v1/chants/answer-options` — Nahlo's three suggestion lists. **They are unjoined and must stay that way**: pairing a melody with its mode in a public payload would end the game, since the audio already names the melody to anyone who recognises it.
- `GET /api/v1/historical-figures` — Shmo's published roster, fetched once per session and filtered client-side. No search endpoint yet; a `search` query param is the next step if the roster outgrows that.
- `GET /api/v1/play/meltho/library` — past Meltho words, public and anonymous. Today's word is never included.
- `GET /api/v1/play/meltho/leaderboard` (`api:v1:write`) — signed-in only, ranked by longest streak; appearing requires an opt-in on the profile, the caller's own standing is always shown.

**Play results (authenticated user writes / reads):**
- `POST /api/v1/play/results` (`api:v1:write`) — the authenticated user records their result; one per user/game/day (idempotent on the unique key).
- `GET /api/v1/play/results/me` (`api:v1:write`) — the authenticated user's own results, for the profile/dashboard surface.

**Profile (authenticated):**
- `GET /api/v1/profile/me` — current user's profile.
- `PUT /api/v1/profile/me` — update display name, preferred language, default script variant.

**Backoffice (admin):**
- `POST/PUT/DELETE /api/v1/admin/lexicon/...` (`api:v1:admin`) — Lexicon CRUD, draft/publish, playable toggle.

---

## Search (Meilisearch)

Meilisearch indexes are kept in sync with PostgreSQL. **Exactly one is active — `lexicon`.** The rest come online with their (deferred) modules:
- `lexicon` — Syriac words, roots, transliterations, meanings **(active — the only one)**
- `translations` — English translation text (FR/NL when available) *(with Translations module)*
- `annotations` — inline annotations with denormalized parent (source/chapter/verse) coordinates *(with Reviews/Translations)*
- `biblical_passages` — Peshitta passages with metadata *(with Biblical module)*

> **Shmo and Nahlo do not use Meilisearch at all.** `Sabro.Historical` and
> `Sabro.BethGazo` contain no search code and register no `ISearchRebuilder` —
> they query Postgres directly. There is no `chants` or `historical_figures`
> index, and `POST /admin/search/rebuild/chants` answers
> `404 "Search index 'chants' is not registered."` The rebuild dispatcher matches
> on the `IndexName` of a *registered* rebuilder, so the only name that does
> anything today is `lexicon`.

**Synchronization strategy:** synchronous at MVP — every write to PostgreSQL triggers a Meilisearch update in the same operation. May be moved to async (queue-based) if write volume grows.

**Transliteration synonyms:** declared in Meilisearch (e.g. `meltho` ≡ `meltā` ≡ `melthā` ≡ `meltha`) so users find the right entry regardless of romanization input.

PostgreSQL remains the source of truth — Meilisearch is a search optimization layer only.

**Rebuild-from-Postgres:** Meilisearch indexes are not backed up — they are rebuilt on demand from PostgreSQL. Owner-only admin endpoints under `/api/v1/admin/search/`:
- `POST /api/v1/admin/search/rebuild/{indexName}` — wipes the named index and rebuilds it from Postgres. Valid index names: `lexicon`, `translations`, `annotations`, `biblical_passages`.
- `POST /api/v1/admin/search/republish-annotation-approvals` — replays the latest annotation-targeted Approval per `AnnotationId` from `reviews.approvals` through `IAnnotationApprovalIndexer` so the `annotations` index regains its `approvalStatus` field.

Operator recovery sequence (once the relevant modules exist): rebuild `lexicon` → `translations` → `annotations` → `biblical_passages` → `republish-annotation-approvals`. The last step is required because the annotation rebuild emits `approvalStatus = null` (verdicts live in Reviews, not Translations). Skipping it leaves `?approvalStatus=approved` queries returning nothing for genuinely approved annotations.

---

## Syriac / Unicode Handling

### Script Variants
Sabro supports the three traditional Syriac scripts: **Serto** (Western / Maherboyo — the default, matching the launch's West-Syriac lean), **Estrangela** (used for patristic texts, when the Translations module lands), and **Madnhaya** (Eastern). The Unicode content is identical across variants — only the rendering font differs. The default is set in code (frontend cookie default + `UserProfile.Create`); the user can override it with the switcher, available everywhere Syriac text is displayed.

Recommended fonts (free, academic):
- Beth Mardutho fonts: `Estrangelo Edessa`, `Serto Jerusalem`, `East Syriac Adiabene`
- Or Google's Noto Sans Syriac family

### Vocalization
Two separate fields stored per text:
- `syriac_unvocalized` — base text without vowel points
- `syriac_vocalized` — optional, with vowel points

The unvocalized field is **not** generated by stripping points — it is independently authored. Search defaults to the unvocalized field for tolerance. Meltho's playable length is computed from the unvocalized field (see Lexicon).

### Transliteration
Provisional standard: **SBL** (Society of Biblical Literature). Stored alongside the canonical Syriac form, with accepted variants for search tolerance. May be revised after consultation with a Syriacist — the field is plain text, the decision is reversible. Optional enrichment; does not gate publication.

### Unicode Technical Rules
- **Encoding everywhere**: UTF-8 (DB, network, files), UTF-16 internal (.NET)
- **Normalization**: NFC applied to all input before storage (`text.Normalize(NormalizationForm.FormC)`)
- **Validated Unicode ranges**: U+0700–U+074F (Syriac), U+0860–U+086F (Syriac Supplement), plus standard exceptions with no dedicated Syriac-block code point — seyame (U+0308 COMBINING DIAERESIS), the linea occultans marking a silent letter (U+0331 COMBINING MACRON BELOW, or U+0304 COMBINING MACRON when SEDRA places it above the letter instead), a hyphen joining a compound idiom's two halves (U+002D), the generic qushoyo/rukkokho dots SEDRA uses in place of the dedicated Syriac marks (U+0307 COMBINING DOT ABOVE, U+0323 COMBINING DOT BELOW), and a zero width joiner from SEDRA's cursive-joining artifacts (U+200D) — all implemented in `Sabro.Shared.Text.SyriacText.IsSyriacOnly`
- **PostgreSQL collation**: `und-x-icu` (ICU-based, language-agnostic Unicode sorting)
- **Direction**: dedicated `<SyriacText>` Vue component applies `dir="rtl"` automatically
- **Letter counting**: playable length counts Unicode letter-category code points only; combining marks are excluded

---

## Internationalization (i18n)

### UI
All interface strings in `@nuxtjs/i18n` from day one. **Five** locale files (`en.json`, `fr.json`, `nl.json`, `de.json`, `sv.json`) — a new string must be added to all five or the build ships a missing key. No hardcoded UI strings anywhere — everything goes through `$t('key')`.

**This holds in all five frontends** — the hub and every game client — and it is already true in the code: each ships all five locale files with matching key counts. Earlier client READMEs claimed narrower coverage ("EN at launch, FR + NL prepared"); that was documentation lagging the code, corrected on 2026-08-19. **Five, everywhere, is the rule** — a new client starts with all five.

### Content
Schema is multilingual from day one (`language` column on `Translation` and `LexiconMeaning` tables). Lexicon meanings require a gloss in every configured language to publish — currently EN + FR + NL + DE + SV (see Lexicon). For deferred translation content, only English exists at first, with a "coming soon" message for the others. Adding new languages later requires no migration — just new content rows.

---

## Authentication (Logto)

Each application in the ecosystem (Sabro, Meltho, Mno, Shmo, Nahlo, and any future app) is declared as a separate OIDC application in the central Logto instance. Sabro's API validates JWT bearer tokens via Logto's JWKS endpoint using the standard `Microsoft.AspNetCore.Authentication.JwtBearer` middleware — no Logto-specific SDK needed on the backend.

The admin role used to gate the backoffice is carried in the token (Logto role / scope mapped to `api:v1:admin`).

Login UI is themed in Logto's admin console with Syriac-inspired branding. Sign-up methods: email/password, optional Google/GitHub social login. No academic federation (eduGAIN/Shibboleth) needed — this is a personal project.

When a new client application is added (e.g. a future history platform), it is registered in Logto's console with no modification to Sabro itself.

---

## Logging & Monitoring

### Logging (Serilog + Seq)
Structured logging via Serilog, shipped to a self-hosted Seq instance for visualization and querying.

**Always log:** errors and exceptions, critical write operations (lexicon edits, publish/unpublish, daily-puzzle selection, and — when those modules exist — translation creation and chapter approval), failed auth attempts.

**Never log:** passwords, full JWT tokens, personally identifiable information (GDPR).

**Always include context:** user ID, request ID, timestamp.

**Log levels:** Debug (dev only) / Information (normal operations) / Warning (non-blocking anomalies) / Error (recoverable errors) / Fatal (app crash imminent).

### Monitoring
- ASP.NET Core health checks — **live**, split by intent:
  - `/health` — **readiness**: runs a `SELECT 1` against Postgres and returns 503
    with a per-check breakdown when it fails. This is what UptimeRobot watches.
  - `/health/live` — **liveness**: runs no checks, depends on nothing. The only one
    safe for a Docker `healthcheck:` or a `depends_on: service_healthy` gate —
    pointing those at `/health` would turn a database blip into a restart loop.
- **UptimeRobot — live since 2026-07-28.** Five HTTP(s) monitors on a 5-minute
  interval, emailing on downtime:

  | Monitor | URL |
  |---|---|
  | Sabro API | `https://api.sabro.be/health` |
  | Sabro hub | `https://sabro.be` |
  | Meltho | `https://meltho.sabro.be` |
  | Mno | `https://mno.sabro.be` |
  | Shmo | `https://shmo.sabro.be` |

  > ⚠️ **Nahlo is not monitored.** Caddy serves `{$NAHLO_DOMAIN}` and the container
  > is deployed, but no UptimeRobot monitor watches it — the five above predate
  > Nahlo's deployment slot (#199, 2026-08-09). Add a sixth monitor on
  > `https://nahlo.sabro.be` when the recordings land and the game opens; until
  > then its downtime is invisible. `analytics.sabro.be` is likewise unwatched.
  > *Tracked as item 1 of the Outstanding Worklist.*

  Alerts go to the **Owner's personal mailbox directly**, deliberately *not* via
  `contact@sabro.be` — that address forwards to Hotmail and Microsoft drops the
  forwarded mail silently, which would give detection with no delivery.

  Verified end to end at setup: the API monitor's pings are visible server-side
  (`HTTP HEAD /health` every 5 min in `docker logs sabro-api`), all five URLs
  answer `HEAD` with 200, and a deliberately-failing throwaway monitor confirmed
  an alert email actually **arrives**.

  > UptimeRobot probes with **HEAD**, not GET. Grepping logs for `GET /health`
  > will show zero hits and look like the monitor is dead.

> **`/health` is not a freshness check.** It answers "can this instance serve
> requests", not "is the site serving the code we shipped". A stale container
> passes it happily — on 2026-07-28 production silently served a two-week-old
> image with `/health` green throughout. Use `/version` on both `api.sabro.be` and
> `sabro.be` to prove prod matches `main`; CD asserts this after every deploy.

**Disk headroom** — UptimeRobot cannot see the disk, and on 2026-07-31 an
unpruned image pile filled it to 100%, crash-looping Postgres for ~15 minutes with
no warning. Two independent guards now cover it:
- CD runs `docker image prune -af --filter "until=24h"` after every container swap
  (`until=24h` keeps the previous SHA on the box, so rollback stays instant) and
  logs `df -h /` + `docker system df` — `Images TOTAL` must stop climbing.
- The backup sidecar runs `disk-check.sh` every 15 minutes, pinging
  `DISK_HEARTBEAT_URL` (`<url>/fail` at or above `DISK_USAGE_THRESHOLD`, default
  80%). Heartbeat silence alerts too, so it also covers the box disappearing.
  **Blank `DISK_HEARTBEAT_URL` means the check logs but nothing alerts.**

Stack Prometheus/Grafana deferred — current solution is sufficient for the project's scale.

---

## Hosting & Deployment

Single-VPS hosting at MVP — the modular-monolith philosophy extends to the deployment topology.

**Target stack:**
- **VPS**: Hetzner Cloud **CPX32** (4 vCPU AMD shared, 8 GB RAM, 160 GB NVMe, 20 TB traffic). All services co-located: Postgres, Meilisearch, Logto, Seq, the ASP.NET API, and the Nuxt frontend. Estimated 3–5 GB RAM in use, leaving 3–5 GB margin.
- **Off-site storage**: Hetzner **Storage Box BX11** (1 TB) for pgBackRest backups and `wwwroot/media/`. Free internal traffic with the VPS; supports SFTP/rsync/Borg/restic.
- **Reverse proxy**: **Caddy** in frontal — automatic Let's Encrypt HTTPS, one `reverse_proxy` block per domain. Buffers the brief API restart window during deploys (replaces blue-green at this scale).
- **Container runtime**: `docker compose` on the VPS. Compose files in the repo (`docker-compose.prod.yml`); production secrets in a `.env` next to it on the VPS, never committed.

**Single shared database.** The entire ecosystem uses **one PostgreSQL database**, owned by Sabro and the only writer of record. Client apps (Meltho, future small sites) do **not** get their own application database — they read and write through Sabro's API. (Earlier drafts said "each app has its own Postgres database"; that is superseded by the single-database decision.)

**Mutualisation with other ecosystem apps** on the same VPS: each client app runs as its **own container** behind Caddy on a distinct port and registers as a **separate OIDC application** in Logto — but they all share Sabro's single PostgreSQL database via the API rather than owning their own. The 8 GB RAM ceiling is the planning constant — sites that would push past it move to their own VM.

**Logto's own store** is separate infrastructure (it manages auth, not ecosystem data) and is not part of the shared application database.

**Planned split point (not at MVP):** when ecosystem write load grows or Sabro redeploys become disruptive to other apps, **move Logto to its own small VM first** (CPX11 class). Logto is the central IDP for the entire ecosystem and must not restart when Sabro redeploys.

**Rejected at this scale:** PaaS layers (Coolify / Dokku / CapRover) — added maintenance surface and resident overhead with no compensating benefit on a single VPS. Kubernetes / k3s — already explicitly forbidden under "What Sabro Is Not".

---

## Backups (pgBackRest)

Sabro's translations are original work and irreplaceable — backup discipline is non-negotiable. Player accounts and results, while less precious, share the same database and are protected by the same policy.

**Strategy:**
- **Daily full backup** of PostgreSQL automated via pgBackRest
- **Continuous WAL archiving** for point-in-time recovery
- **Retention**: 30 daily backups + 12 monthly backups
- **Weekly automated restore test** to verify backup integrity
- **3-2-1 rule**: 3 copies, 2 supports, 1 off-site — off-site is the **Hetzner Storage Box BX11**, driven by pgBackRest over SFTP (see Hosting & Deployment)

**Also backed up:**
- `wwwroot/media/` (bibliography images) — synced separately to off-site storage
- Logto database (separate from the main application DB)

Meilisearch indexes are not backed up — they are rebuilt from PostgreSQL on demand.

---

## Testing Strategy

Test pyramid with TDD-first approach for Domain and Application layers.

### Distribution
- **~70% Unit tests** — Domain rules, validators, mappers, pure logic (e.g. playable-length computation, eligible-pool predicate, anti-repetition selection)
- **~25% Integration tests** — modules tested with real PostgreSQL via Testcontainers
- **~5% E2E tests** — critical user flows (login, record a Meltho result, admin word CRUD)

### Tools
- **xUnit** — test framework
- **FluentAssertions** — readable assertions (`result.Should().Be(...)`)
- **NSubstitute** — mocking
- **Testcontainers** — real PostgreSQL + Meilisearch in Docker for integration tests
- **Playwright** — E2E tests against the Nuxt frontend
- **Vitest** — Nuxt-side unit tests (composables, components)

> ⚠️ **The integration fixtures do not run the production versions.** As of
> 2026-08-19 `PostgresFixture` pins `postgres:16-alpine` while both compose files
> run `postgres:17-alpine`, and `MeilisearchFixture` pins
> `getmeili/meilisearch:v1.13` while production runs `v1.53`. Migrations and
> index behaviour are therefore verified against a different major Postgres and a
> far older Meilisearch than they meet in production. Raise both to match, and
> keep them matched when the compose pins move. *Tracked as item 2 of the
> Outstanding Worklist.*

**Testcontainers version note.** `Testcontainers` 4.14.0 obsoletes the
parameterless `ContainerBuilder()` / `PostgreSqlBuilder()` constructors in favour
of ones taking the image, and `TreatWarningsAsErrors` turns that CS0618 into a
build failure — so the image goes in the constructor, not in a following
`WithImage` call.

### Coverage Targets
- Domain + Application: **80–90%**
- Infrastructure: **50–60%**
- API/Controllers: **40–50%** (mostly covered by integration tests)
- **Global target: 70–75%**

### CI Enforcement
Coverage drop blocks CI on **Domain and Application** layers only — other layers report coverage but do not block merges. Avoids contortions to inflate metrics on infrastructure code.

### TDD Discipline
- **Strict TDD** for Domain and Application: write the failing test first
- **Test-after** for Infrastructure (EF Core, repositories) and Controllers — covered by integration tests rather than unit tests

---

## CI/CD (GitHub Actions)

### CI Pipelines (this repo)
- `sabro-ci.yml` — build, unit tests, integration tests, coverage, lint, format
- `pr-validation.yml` — Conventional Commits check, lint, format

(Meltho lives in its own repository and carries its own `meltho-ci.yml` — build, Vitest, Playwright. It is not part of Sabro's pipelines.)

### CD
GitHub Actions builds Docker images for the API and frontend, pushes them to **GitHub Container Registry** (`ghcr.io`), then SSHes to the production VPS to pull and run `docker compose up -d`.

**Pipeline shape:**
1. CI (build, tests, coverage) gates the deploy job — a red CI blocks deploy.
2. Build multi-stage Dockerfiles for `Sabro.API` (.NET 10) and the Nuxt frontend. Tag images with the commit SHA.
3. Push to `ghcr.io/...` (free for private repos, native `GITHUB_TOKEN` auth, no Docker Hub rate limits).
4. SSH to the VPS, `docker compose pull`, run `docker compose run --rm api dotnet ef database update` (one-off migration container) **before** swapping app containers, then `docker compose up -d`.
5. Health-check `/health` post-deploy. Rollback = retag the previous image SHA and `docker compose up -d` (~30 s).

**Migrations rule — forward-compatible only.** No `DROP COLUMN` / rename / type-narrowing in a single deploy. Destructive changes go through an **expand → migrate → contract** sequence over multiple deploys. There is no blue-green at MVP scale — Caddy in frontal buffers the ~2–3 s API restart window.

**Build always in CI, never on the VPS** — the shared vCPU is too constrained to run `dotnet publish` while serving traffic.

**Meilisearch is NOT rebuilt on deploy.** Index rebuilds and `republish-annotation-approvals` stay as operator-initiated actions via the admin endpoints (see the Search section). Putting them in the pipeline would wipe search during every deploy.

**Secrets:** GitHub Actions secrets for `SSH_PRIVATE_KEY`, `VPS_HOST`, GHCR token (often `GITHUB_TOKEN` suffices). Production app secrets live in a `.env` next to `docker-compose.prod.yml` on the VPS, plus a mounted `appsettings.Production.json` — never committed.

**Rejected approaches:** Coolify / Dokku / CapRover (PaaS overhead + maintenance surface), Watchtower (skips migrations + health checks), Kubernetes / k3s (no justification at modular-monolith / single-VPS scale).

### Branching Strategy
- `main` — protected, always deployable, requires PR + green CI; every merge auto-deploys
- `feature/short-description` — feature branches off `main`
- `fix/short-description` — bug fix branches off `main`

(No `develop` integration branch — trunk-based off `main`; an earlier draft documented one, it was never used.)

### Commits — Conventional Commits
```
feat(lexicon): add draft/published lifecycle to entries
feat(play): add configurable anti-repetition window to daily selection
fix(identity): correct profile language default
chore(deps): update EF Core to latest patch
docs(api): update OpenAPI examples
```

### Versioning
Semantic Versioning (`major.minor.patch`). Git tags on each release. Changelog generated automatically from Conventional Commits.

### Pre-Commit Local Checks
- All tests pass: `dotnet test`
- No compiler warnings
- EF Core migrations up to date: `dotnet ef migrations list`
- Frontend builds cleanly: `npm run build`

---

## Coding Conventions

### General
- Language: English for all code, comments, and commit messages
- Interfaces prefixed with `I`: `ILexiconService`, `ILexiconRepository`
- Async methods suffixed with `Async`: `GetTodaysPuzzleAsync()`
- No abbreviations in naming — clarity over brevity

### Backend (C#)
- Standard C# conventions (PascalCase for types/methods, camelCase for locals)
- One class per file
- DTOs are immutable records where possible
- FluentValidation for all input validation — no data annotations for business rules
- Never expose domain entities directly via API — always map to DTOs
- StyleCop + .editorconfig enforced via CI

### Frontend (Nuxt / Vue 3)
- Composition API only — no Options API
- TypeScript strict mode enabled
- Component names: PascalCase (`SyriacText.vue`)
- Composables prefixed with `use`: `useLexicon()`, `useProfile()`
- All user-facing strings go through i18n — no hardcoded UI text
- ESLint + Prettier enforced via CI

### Database
- Migrations managed via EF Core — never edit the database manually
- Table names: snake_case (`lexicon_entries`, `game_results`, `meltho_daily_puzzles`)
- All tables have `created_at` and `updated_at` timestamps
- Soft delete not used — hard delete with versioning as safety net

---

## Key Business Rules

- All translations are original work by the project owner — no copyrighted third-party content
- Every translation edit creates a new version — previous versions are never deleted *(applies once the Translations module is built)*
- Chapter-level approval cascades validation to all its verses unless individually overridden *(Reviews module)*
- Suggested edits from reviewers create pending proposals — they never modify content directly *(Reviews module)*
- Only the Owner accepts or rejects proposals; only the Owner edits the Lexicon and publishes entries
- A Lexicon entry is publishable only with a meaning in every configured language (`SupportedLanguages:Codes`, currently five); only published entries can be marked playable or served to clients
- **Client read/write rule.** Client applications (Meltho, Mno, Shmo, Nahlo, and any future app) are **read-only consumers of Sabro's content** — they never edit curated content and never connect to the database directly. They **may write their own play data** (game results) through controlled, authenticated API endpoints. All writes — content and play — go exclusively through Sabro's validated API.
- Bibliography images are stored locally under `wwwroot/media/` — small volume, no S3 needed at this scale

---

## Environment Setup (Windows + Visual Studio)

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Install [PostgreSQL](https://www.postgresql.org/download/windows/) and create a `sabro_dev` database
3. Install [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Meilisearch, Seq, Logto, Testcontainers)
4. Install [Node.js LTS](https://nodejs.org/) for the Nuxt frontend
5. Clone the repo and open `Sabro.slnx` in Visual Studio
6. Copy `appsettings.Development.example.json` to `appsettings.Development.json` and fill in connection strings, Logto config, Meilisearch URL, and `Meltho:AntiRepetitionWindowDays`
7. Start auxiliary services: `docker-compose up -d` (Meilisearch + Seq + Logto)
8. Run migrations for the active modules — **Lexicon, Identity, Historical, BethGazo, Play, Reviews** — e.g. `dotnet ef database update --project src/Modules/Sabro.Lexicon --startup-project src/Sabro.API --context LexiconDbContext` (repeat per module). The authoritative list is `scripts/apply-migrations.sh`, which is what CD runs; a module missing from it works locally and fails in production. `ModuleMigrationCoverageTests` fails the build if that list and the modules disagree.
9. Start the API: F5 in Visual Studio
10. Start the frontend: `cd frontend && npm install && npm run dev`

---

## What Sabro Is Not

- Not a CMS — content is scholarly and curated, not crowdsourced
- Not a social platform — user features are personal and private (notes, favorites, game profile)
- Not a microservices architecture — it is a modular monolith; do not split modules into separate deployable services unless a clear scaling need arises
- Not responsible for client game logic — **guess evaluation, feedback colouring, and presentation** live in each client, not in Sabro. The full answer ships with the puzzle and Sabro never learns what a player guessed. (Sabro owns only the shared daily selection, because that state must be identical for all players and persisted.)
- Not federated with academic identity providers (eduGAIN/Shibboleth) — this is a personal project, not affiliated with an institution

---

## Deferred Decisions

These decisions are intentionally deferred and will be made when relevant:
- **Translations and Biblical modules** — still deferred. Specs retained above; not on any current critical path. (Reviews is no longer on this list: its field-proposal half is live.)
- **Liturgical calendar / manual daily-word pinning** — data model kept ready (a scheduled date → word override); no backoffice UI at launch. Algorithmic get-or-create selection with the anti-repetition window covers launch.
- **Cross-game aggregate stats** — still deferred. *(No longer deferred: Meltho's opt-in streak leaderboard and the clients' share cards/share text both shipped.)*
- **Rich cross-project dashboard** — deferred; the `GameResult` model is already multi-game, so this is additive UI, not a model change.
- **Daily-puzzle selection trigger** — lazy get-or-create on first request at launch; may move to a small scheduled job if/when an async job queue (Hangfire or similar) is introduced.
- **Bibliography page covers** — copyright vs pragmatic display, decided at page creation time
- **Async Meilisearch sync** — only if write volume grows
- **Async job queue** — Hangfire or similar, only if needed for background processing (would also host the daily-puzzle scheduler)
