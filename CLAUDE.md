# CLAUDE.md — Sabro

## Project Overview

Sabro is an academic web platform for publishing original English translations of 12th-century Syriac patristic commentaries by Dionysios bar Ṣalibi (Metropolitan of Amid, d. 1171, Syriac Orthodox). It serves as the central backend in an ecosystem of applications dedicated to Syriac language and patristic studies, exposing a versioned REST API consumed by client applications — **four live: Meltho (words), Mno (numerals), Shmo (figures) and Nahlo (chants)**, plus **Sofro (writing), live since 2026-10-15** — free practice plus, since 2026-10-28, a word a day — each in its own repository. Sabro also acts as the **ecosystem hub**: it owns shared identity (via Logto), user profiles, and cross-application play data.

The long-term scope includes translating the entire Bible (Peshitta) and works of various Syriac Church Fathers.

---

## Build Sequencing (current focus)

The long-term scope (full Peshitta + Church Fathers) is unchanged, but the build order has been re-sequenced. Translation work is set aside for now; the immediate goal is a launched, living ecosystem.

1. ✅ Sabro shipped as the ecosystem hub + API: **Lexicon**, **Identity/Profile**, **Play**, the `/api/v1/` contract, Logto, and the hub frontend.
2. ✅ **Meltho** launched against that API, then **Mno**, **Shmo** and **Nahlo** followed on the same foundation — the multi-game model held, with no rework.
3. **Translations and Biblical remain deferred** — fully specified below, but not on any current critical path. No game depends on translated content. **Reviews is partly active**: its field-proposal workflow shipped with the backoffice and runs in production; only its three-level prose review stays deferred.

**Current state.** Five games are deployed behind Caddy and watched by UptimeRobot. The four puzzle games are in the daily circuit; **Sofro went live on 2026-10-15** as free practice and **joined the circuit on 2026-10-28** as its fifth game, with a word of the day to write (see *Ecosystem Clients*). **Nahlo went live on 2026-10-12** with its first melody group — *Zodeq dnehwe* in all eight modes (Qole shahroye), the Owner's own recordings — and joined the circuit hand-off the same day. On 2026-10-15 the Owner recorded three more melody groups, each in all eight modes — *Hablo l'itokh*, *Mo rhimin* and *More dramsho* — and the same day all 24 were uploaded, published and made playable, so Nahlo's pool is now **32 chants**. Their incipits are not read from the book yet, which is why the incipit became optional (#300; see *BethGazo*).

**Hub philosophy — wide model, narrow surface.** The data foundation was built to see far (multi-game results, profile, cross-project shape) while the launch UI stayed lean. That bet paid: three further games landed on the same `GameResult` shape without a migration. Keep new surfaces additive for the same reason.

---

## Outstanding Worklist

Live handoff list, accurate as of **2026-10-01**. Each item is written to be
executed cold — context, exact change, how to verify, and what needs a human
decision. **Delete an item from this file the moment it lands**; a worklist that
outlives its work is the same drift this file exists to prevent.

To pick this up in a fresh session, paste:

> Read the *Outstanding Worklist* in CLAUDE.md and start with item N. Follow the
> "Do" steps, run the "Verify" checks before pushing, and stop and ask me on
> anything the item marks **Decide**. One PR per item unless it says otherwise.

The one remaining item waits on an upstream release. Everything code-only is done.
The chant-recordings item closed on 2026-10-12 (Nahlo live; see *Ecosystem Clients*). The old item 1 (UptimeRobot) closed on 2026-10-01; see *Monitoring*. On 2026-10-01 every open npm advisory
was closed in all five repos (`js-yaml`, `svgo`, `undici`, `brace-expansion`), leaving
`npm audit` at zero, and CI gained a minimum-test-count floor.

> **What 2026-09-28 landed, so nobody redoes it:** the rate limiter partitions per
> caller instead of globally (#268 — see *API Design → Rate limiting*); `devalue`
> 5.9.4 closed a runtime DoS advisory in all five repos (#269 here); Dependabot
> **security alerts** are now enabled on all five repos — they were off everywhere;
> and the MTP `--nologo` trap is documented (#270 — see *Testing Strategy*). Prod
> was verified on `0c1e3f4` via `/version` on api and hub.

---

### 1. TypeScript 7 is blocked by the API codegen

**All five frontends are on TypeScript 6** since 2026-08-27 (#253). The hub used to
lag at 5.9; it no longer does.

**TS 7 is the wall, and it is the same wall for all five.**
`openapi-typescript@7.13.0` drives the TypeScript **compiler factory API** to
generate `app/types/api.generated.ts`, and on TS 7 the generator crashes outright:
`TypeError: Cannot read properties of undefined (reading 'createKeywordTypeNode')`.
That is a hard break in codegen, not a peer-range warning. 7.13.0 is the current
latest, so there is nothing to upgrade into. Wait for upstream TS 7 support, or
replace the generator. **Dependabot will keep offering TS 7 — closing those PRs is
the expected outcome, not a to-do.**

**What to watch upstream** (`openapi-ts/openapi-typescript`, checked 2026-10-01): issue
#2841 tracks the TS 7 break. Two competing fixes are open and unmerged: #2867 (a
package-owned compiler) and #2872 (no runtime compiler dependency at all). Nothing has
been published since 7.13.0 on 2026-06-15. When a release lands, re-run the same proof
used for TS 6: regenerate `api.generated.ts`, and it must come out **byte-identical**;
then run `nuxt typecheck` and the unit tests, in all five repos.

> ⚠️ **Do not read the `^5.x` peer range as the constraint.** That is what made the
> hub sit on 5.9 longer than it had to: the peer says `"typescript": "^5.x"`, so
> TS 6 *looks* unsupported. It is stale metadata, not enforcement — npm resolves the
> install without complaint and the generator runs fine. Verified rather than
> assumed before moving: codegen exits 0 and emits an `api.generated.ts`
> **byte-identical** to the committed one, `nuxt typecheck` exits 0, 108 unit tests
> pass. Test the tool, not its manifest.

> **`@types/node` is no longer part of this — settled 2026-08-27.** It had drifted
> to 25.x while every container runs `node:22-bookworm-slim`, and Dependabot
> offered 26. Now pinned to `^22.20.1` in all five repos so the types match the
> runtime (Sabro #244, Meltho #76, Mno #36, Nahlo #17, Shmo #24). Verified with a
> real install rather than assumed: `nuxt typecheck` exits 0 and the unit tests
> pass on the downgrade. **Keep it on the 22.x line while the Dockerfiles say 22** —
> it moves when the node image moves, not before. Note `@types/node` and the
> TypeScript compiler version are independent; this was never what held the hub back.

Related, and the reason this is worth acting on rather than drifting: the same
"newest is not best" trap already produced an **end-of-life** proposal — see the
`node` majors ignore in `.github/dependabot.yml`.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 (LTS) |
| Database | PostgreSQL (via Entity Framework Core) — single shared database for the whole ecosystem |
| Search | Meilisearch (typo-tolerant, dedicated service) |
| Frontend | Nuxt 4 (Vue 3 + TypeScript strict) — the hub and all five game clients; Tailwind v4 in the clients |
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

**`GameResult` — generic, multi-game.** Keyed by Logto user ID + a `GameId` string discriminator. The identifiers are constants on `Sabro.Play.Domain.Games` — `meltho`, `mno`, `shmo`, `nahlo`, `sofro` — never string literals at call sites. Fields: `PlayedOn` (date), `Solved` (bool), `Attempts` (int), and an optional `DetailJson` for game-specific extras. Unique constraint on (`LogtoUserId`, `GameId`, `PlayedOn`) — one result per user, per game, per day. Streaks and aggregates are **derived** from results, not stored. This generic shape is the cross-project equivalent of the savant/ludic split used elsewhere: do not model a Meltho-specific scores table.

**Four daily puzzles — shared server state.** One entity per game (`MelthoDailyPuzzle`, `MnoDailyPuzzle`, `ShmoDailyPuzzle`, `NahloDailyPuzzle`), each recording what was served on which day. Selection is **get-or-create per date** (idempotent): the first request for a given day picks, records, and returns it; subsequent requests return the recorded one, so every player gets the same puzzle.

Three of the four draw from a curated pool and exclude anything served inside the **anti-repetition window**. **Mno is the exception** — its equations are *generated* (`MnoEquationGenerator`), so there is no pool to exhaust and it carries no window option at all.

**Anti-repetition window — per game, configurable, never hardcoded.** Each pool-backed game owns its own options class and its own config key, because the pools are different sizes:

| Game | Config key | Default | Why |
|---|---|---|---|
| Meltho | `Meltho:AntiRepetitionWindowDays` | 30 | Pool grown past launch size |
| Shmo | `Shmo:AntiRepetitionWindowDays` | 30 | Same |
| Nahlo | `Nahlo:AntiRepetitionWindowDays` | **4** | The treasury opened with one melody group of eight chants — 7 against 8 would leave one candidate a day. The pool is 32 since 2026-10-15; raising the window (14 or 21) awaits an Owner decision |
| Mno | *(none)* | — | Generated, not drawn from a pool |

Start a window low so a small pool never starves, and raise it as the pool grows — but keep it well under the pool size: a window at or above the pool answers 409 the day one item drops out. A hardcoded 365 against a 40-item pool would leave the selector with nothing eligible after 40 days and break the game — these must remain config values.

**Boundary with the clients.** Sabro answers *"what is today's puzzle"* (shared state, single DB, central anti-repetition) and stores the final daily result. Every client owns its own **game mechanics**: guess evaluation, feedback colouring, the attempt ladder, and share text. **The full answer ships with the puzzle** — evaluation is client logic, and Sabro does not score guesses. It does see them: Shmo and Nahlo put a round's guesses in the result's `DetailJson`. Sabro reads Shmo's to build the player's figure collection, and **checks that a solved Shmo or Nahlo result ends on that day's answer** before recording it (a found card and the circuit streak rest on that, and a signed-in Shmo or Nahlo also sends in rounds played signed out). Daily selection is the one piece of shared game state that lives in Sabro rather than the client, precisely because it must be identical for all players and persisted.

**Beyond the puzzle.** Play also serves `meltho/library` (past words, public and anonymous — today's word is never included, it would spoil the live puzzle) and `meltho/leaderboard` (signed-in only, ranked by longest streak; appearing in it requires an explicit opt-in on the player's profile, while the caller's own standing is always shown).

### Historical
Owns the roster of **historical figures** — biblical figures through the Syriac Church Fathers and beyond — and is the content behind **Shmo**. Each figure carries the attributes Shmo scores a guess on: `Category`, `Era` (a numeric year, diffed as higher/lower with near/far tiers), `Period` (a chronological but non-numeric enum, diffed by position in the declared sequence), `Role`, `Region`, `Tradition` (nullable), and `Gender`.

Same lifecycle as the Lexicon: `Draft` → `Published`, plus a manual `PlayableInShmo` editorial flag that only a published figure may carry.

**Card fields (2026-10-25)** — three optional fields for the hub's figure collection (Beth Ṣalme), none of them a Shmo hint: `Emblem` (`HistoricalFigureEmblem`, the sign on the figure's card — a traditional iconographic attribute or one from the figure's story, chosen per figure by the Owner; null shows the emblem of the role), `SyriacName` (Syriac script, NFC) and `ReadMoreUrl` (absolute http(s), e.g. a Syriaca.org entry). The migration that added them pre-filled the Owner-approved emblem for 140 figures, keyed by name. They are in the public roster because the hub draws every card from it, and so are the **descriptions**: every card of Beth Ṣalme shows its story, met or not (Owner, 2026-10-25). Nothing there gives Shmo away; the roster was public already.

> **The enum order is a contract.** `HistoricalPeriod` is diffed *by ordinal position*, so reordering its members silently changes every near/far verdict in Shmo. Append new periods; never reorder. Shmo hand-copies these enums into `app/types/figure.ts` and guards the copy with `test/figure.test.ts` — see *Ecosystem Clients*.

### BethGazo
Owns the **Beth Gazo** — the treasury of chants — and is the content behind **Nahlo**.

**A chant is identified by four things, not one:** its melody name, its section, its mode where the section has one, and its variant. A melody name recurs across modes, so "Maryam yoldath Aloho" names a family rather than a chant; only "Maryam yoldath Aloho, Tlithoyo" picks one out. That is the whole reason the game works — were the mode derivable from the name, naming the melody would hand the player the mode for free.

**Sections and modes are reference tables**, editable by the Owner, not hardcoded sets. There are ten sections and nine modes (the farde 1–8 *plus* mshaḥelfotho). `Chant.ModeId` is **nullable**, and its null means *"this section has no modes"* — never *"nobody has filled it in yet"*: the madroshe have no mode at all, and mshaḥelfotho belongs to the farde alone. Both rules are carried by `BethGazoSection.AllowedModes`.

> **Never hardcode eight modes.** The list is served in full by `GET /chants/answer-options` and can grow again. Nothing in Sabro or in Nahlo counts modes, and nothing should.

**`ChantVariantKind` distinguishes two things the book prints.** A **shuḥlofo** is a variation *of the melody itself* — the same qolo sung another way. A **ḥrino** (ܐܚܪܢܐ, "another"; pl. *ḥrone*) is simply another chant standing in the same mode, not a variant of anything. They must be told apart or a *shuḥlofo 1* and a *ḥrino 1* under one melody and mode collide on the same four values, making the second unsaveable. A string-converted enum, per the house rule.

Recordings are uploaded through the backoffice and stored via `IChantAudioStorage` (`FileSystemChantAudioStorage` at this scale). Lifecycle mirrors the Lexicon: `Draft` → `Published` + a manual `PlayableInNahlo` flag.

**Section and mode text (2026-10-27).** Each section and mode may carry a `SyriacName` and one line per
content language (`BethGazoDescription`, held by `BethGazoText`), both optional and edited in
`/admin/chants/sections`. They are for the hub's Beth Gazo page; Nahlo's answer options leave them null.

**The recording is the publish gate; the incipit is optional** (since 2026-10-15, #300). The Owner recorded 24 chants before their incipits were read from the book, so a chant can be saved and published from its recording alone and the incipit added later from the backoffice. A vocalized incipit still requires the plain one. Nahlo receives a `null` incipit and simply offers no reading hint for that chant.

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

Five game clients consume Sabro's API, each in **its own repository**, each its own OIDC application in the shared Logto tenant, each its own container behind Caddy. All five are Nuxt 4 + Tailwind v4 + Vitest, and all five ship **EN, FR, NL, DE and SV**.

| App | Repo | Game | Dev port | Sabro content behind it |
|---|---|---|---|---|
| **Meltho** (ܡܠܬܐ, "word") | `Benyamen9/Meltho` | Wordle on Syriac words; 6 guesses, green/yellow/grey | 3100 | Lexicon |
| **Mno** (ܡܢܐ, "he counted") | `Benyamen9/Mno` | Mathler on additive Syriac alphabetic numerals; 6 guesses | 3200 | *(generated)* |
| **Shmo** (ܫܡܐ, "name") | `Benyamen9/Shmo` | Pokédle on historical figures; **unlimited guesses, no losing state** | 3300 | Historical |
| **Nahlo** (ܢܚܠܐ) | `Benyamen9/Nahlo` | Name the chant — melody + mode + shuḥlofo (yes/no); 6 guesses, right/wrong only — **purple**, not green | 3400 | BethGazo |
| **Sofro** (ܣܦܪܐ, "scribe") | `Benyamen9/Sofro` | Write Syriac letters and words by hand, step by step — free practice, plus **a word of the day in the daily circuit** (since 2026-10-28); **charcoal** | 3500 | *(none yet — Lexicon later)* |

The hub runs on :3000 and Logto on :3001/:3002.

> **Sofro is live at `sofro.sabro.be` since 2026-10-15** (repo created 2026-10-14):
> its own CD, Logto app, Caddy block, DNS record and UptimeRobot monitor, brought
> online by DEPLOY.md's "fifth game" order. Its Logto app registers **production
> URIs only**, like Meltho, Mno and Shmo — local dev signs in against the local
> Logto. Owner rulings of 2026-10-14:
> ~~not in the daily circuit~~ — **superseded 2026-10-28**, see below;
> **scores stay on the device** at first, so no `Games.Sofro` id and no Sabro
> endpoint yet — and only a score would ever be stored, **never drawings** (lifted on
> 2026-10-28 for today's word only, and only until the word changes: see below); letter
> data ships **from the font pipeline first**, built into the client, with an
> owner-only **dot editor** in the backoffice to replace it later; six levels with
> **hand-picked word lists**, moving to a Lexicon playable-in-Sofro flag later.
> Its one-line header carries an **app launcher in place of the Sabro pill**, and
> since 2026-10-15 so do the other five frontends — see *Mirrored contracts*.
> The full design lives in `Hito/scribe-prototype/SOFRO_SPEC.md` until it is folded
> into Sofro's README piece by piece.

> **Sofro's word of the day (Owner, 2026-10-28)** makes it the **fifth game of the daily
> circuit**, after the four puzzles: `sofro` is in `CIRCUIT_GAMES` and last in
> `CIRCUIT_HANDOFF`. The word is one of the level-2 words, **picked in the client from the
> UTC date** (`dailyWordIndex`), the same for everyone, with no Sabro endpoint. Sofro opens
> on it until it is written; written with a pass, it marks Sofro played in the cookie and
> hands on to the next unplayed game. **Since the same day it is also a result on Sabro**
> (`gameId: 'sofro'`, the score, word, script and vowels in `detailJson`) for a signed-in
> player, so it follows them across devices like the puzzles; written signed out, it goes up
> on the next signed-in visit that day. **The strokes of today's word ride along, but only
> until the word changes** (Owner, same day: "just the daily vector data, kept till the word
> resets"), with each step's score, so another device redraws the player's own ink.
> `GameResultService` clears `strokes` and `stepScores` from every Sofro result of an
> earlier day before any result is recorded or read, so no past day's strokes are ever
> served; this is why `Games.Sofro` exists. **The hub's streak counts all
> five** (Owner, same day, knowingly: Sofro's results only begin on 2026-10-28, so every
> streak started again from that day). Free practice sends nothing.
> **Once written, the card is the result** (Owner, same day, mockup B of three): the
> player's ink with a score stamp, the meaning and each step's score, then Share · the next
> game · the pen back to practice. Sofro reopens on it all day. Another device redraws the
> ink from Sabro; with none there (signed out, or a day recorded before the strokes were),
> it shows the book's word and the score. **More on Sabro** sits under the row, as on the
> other games' result pages.

> **Nahlo's spelling came from the Owner** and is not derived from anything — do not "correct" it against a lexicon. Likewise the Owner's ruling that the third answer part is **yes/no** ("does this melody have a variation at all"), never *which* shuḥlofo: the cost is known and accepted.
>
> **Nahlo breaks the shared feedback colour on purpose (Owner, 2026-10-12).** Meltho, Mno and Shmo mark a right part green; Nahlo's right part is its theme purple and a wrong part a pale tint of it — tiles, attempt strip and share grid (🟪/⬜) alike. Same day, its "night vigil" redesign: the answer is asked one question at a time (melody → mode → shuḥlofo), and a mode an earlier guess scored wrong is dimmed, never removed. Do not "restore" the green. Details in the Nahlo README.
>
> **Nahlo's result page ends with a link to Sofro** (Owner, 2026-10-28), always, below the
> "More on Sabro" line.
>
> **Shmo (Owner, 2026-10-28).** *How to play* now says that only the **era and period**
> arrows strike out names (Shmo #7's rule; gender and the other categorical columns are left
> to the player). Figures that share **every** scored attribute (149 of 289 that day, 52
> groups) get a hint: once a guess has the answer's whole profile without being it, the board
> names everyone left with that profile, each a button that guesses it. The **result page
> shows the answer as its Beth Ṣalme card** (colour when found, a sketch when given up) and
> the day's other guesses as small sketch cards. Details in Shmo's README.
>
> **Nahlo scores points (Owner, 2026-10-27)**, out of 100: 50/42/34/26/18/10 by the solving guess, plus up to 50 for speed (full within 30 s of the first press of play, nothing at 5 min). Opening the incipit gives up the speed points; a blind solve takes them all; a lost round earns 5 per part found. It answers the open Beth Gazo: knowing the chant beats looking it up. **Computed in the client from the round's ingredients, never stored** — Sabro has no points field and needs none; the formula can change without a migration. Result screen and share text only, so far.

### This file is the ecosystem's memory — fold changes back into it

The four client repos carry no `CLAUDE.md`. Their **`README.md` is each app's memory**, and it records Owner rulings and reasoning that exist nowhere else. Sabro's `CLAUDE.md` is the only file loaded automatically, so:

**When anything in a client README changes — a rule, an Owner ruling, a port, a config default, a language — fold it into this file in the same change.** Each client README carries a matching pointer back here. A fact that lives only in a client repo is a fact this file will eventually contradict.

### Mirrored contracts — duplicated across repo boundaries

Seven pieces of logic are deliberately implemented twice, on either side of a repo boundary. Only two have a drift guard; treat the rest as hand-maintained and check them whenever the Sabro side moves.

| Mirror | Sabro side | Client side | Guard |
|---|---|---|---|
| Historical enums | `Sabro.Historical.Domain` | Shmo `app/types/figure.ts` | ✅ `test/figure.test.ts` |
| Syriac letter counting | `playableLength` | Meltho `app/utils/syriac.ts` | ❌ none |
| Numeral speller | Sabro's C# speller | Mno `app/utils/numerals.ts` | ❌ none |
| Daily circuit | hub `useDailyCircuit.ts` | one copy in **each** of the five clients | ❌ comment only |
| Figure cards (emblem drawings + picture) | hub `utils/emblems.ts` + `FigureArt.vue` | Shmo `app/utils/emblems.ts` + `FigureArt.vue` | ❌ none: change both together |
| App launcher | hub `utils/apps.ts` + `AppLauncher.vue` | one copy in **each** of the five clients | ✅ `apps` unit test per repo (order, self, Sofro's badge) |
| Nahlo's scoring rule (is a guess right?) | `NahloAnswer` (checks a claimed win) | Nahlo `app/utils/compare.ts` | ❌ none: change both together |

**The daily circuit** is one cookie shared across `*.sabro.be` (`sabro_daily_played`), and the composable exists in **six** copies — hub, Meltho, Mno, Shmo, Nahlo, Sofro. `CIRCUIT_GAMES` lists all five games (the four puzzles, then Sofro since 2026-10-28) in all six copies, and so does `CIRCUIT_HANDOFF`. A cookie is per browser, so **the hub also marks today's games from a signed-in player's saved results** (`TodayCircuit.vue`, 2026-10-28): a round played on the phone shows as done on the computer, and the cookie it writes is the one every game reads. Sofro's word does the same since it became a result on Sabro (2026-10-28). The two lists stay separate so a game that cannot serve a puzzle (its `/today` answering 409) can be withheld from the hand-off without touching the cookie's vocabulary.

> ⚠️ **Any change to `CIRCUIT_HANDOFF` goes into all six repos at once.** Nothing fails if a copy is missed — the copies simply disagree about which door to open next.

**The app launcher** (Owner, 2026-10-13; everywhere since 2026-10-15) is the 3×3 button that replaced
the "Sabro" pill in the games and the row of game pills in the hub, so every header is **one line at
every width**. Six copies of the list exist — hub, Meltho, Mno, Shmo, Nahlo, Sofro — each **listing
itself first, marked as here**, then the others in launch order (Sabro, Meltho, Mno, Shmo, Nahlo,
Sofro). **A new app goes into all six at once**, with its `<app>Url` runtime-config key. On phones the
hub's page links (Library, Beth Gazo, Beth Ṣalme, Backoffice) live **inside the launcher panel**, under the apps (Owner,
2026-10-15); from `sm` up they stay in the header. Under 340 px the hub shows its ܣ badge alone.

**Beth Ṣalme** (ܒܝܬ ܨܠܡ̈ܐ, "house of images", `/beth-salme`; Owner, 2026-10-25) is the hub's figure page: a
prosopography of Shmo's roster as a **card collection**. A figure is **met** when the player guesses it in
Shmo (a sketch card, attributes shown) and **found** when they solve it as the day's answer (the card in
colour). **Every card shows its attributes and story**, met or not (Owner, 2026-10-25): the page is a
reference first, and finding a figure colours its card in. A find is **illuminated** 1 time in 20 (a
rainbow foil). Cards run A–Z, numbered through time; unmet cards are dark but **show the name**. Each card
carries the figure's **emblem** (`utils/emblems.ts`, one drawing per `HistoricalFigureEmblem` and per role,
typed so a new value fails the typecheck until drawn). On a computer the figure's page sits beside the
collection; on a phone the card lifts out and turns over. Signed out, every card is dark with a sign-in
line. Each "library" keeps its own style: collecting belongs to Beth Ṣalme alone, never to Beth Gazo.
**No card rarity** (Owner, 2026-10-27): the idea was dropped, not parked. Every card is equal; the only special card is the illuminated one.

**Beth Gazo** (ܒܝܬ ܓܙܐ, `/beth-gazo`; Owner, 2026-10-25, layout 2026-10-27) is the hub's chant page: the
treasury as an **open book**, fed by `GET /chants/book`. **One view, no tabs, closed chapters:** the sections in
the book's order (numbered, red initials, each with its optional Syriac name and line) open one by one, so a
full book stays short. Inside, **each melody is one line** with a numbered button per mode (a dot marks a mode
that also has a shuḥlofo or ḥrino); tapping the melody's name **unfolds** it into its chants, one per line,
with mode, variant and **incipit**. A section without modes (the madroshe) lists its chants by incipit straight
away, since nothing else tells them apart (Owner). A search (melody or incipit; an incipit match unfolds the
melody) or a **mode pill** opens the sections with a match and shows the mode's card. The modes every moded
section admits are numbered; one only some admit (the mshaḥelfotho) is marked ✶, derived from the section
rows like Nahlo's conditional modes, never from a count. **One player bar** at the bottom (`BethGazoPlayer`).
**The ribbon** (Owner, 2026-10-27) remembers the last chant played, in `localStorage` on that device only,
and the page reopens there with a "where you left off" line. No collecting, unlocking or illumination here.
Every published chant **with a recording** is listed; nothing is held back for the day's Nahlo.

**The hub's home page** (reworked by the Owner on 2026-10-23 for practicality; the look of
2026-10-15 stays) has two parts under the hero:

- **"Today"** (`TodayCircuit.vue`) — one line with the next unplayed game and its button; under it a
  square per daily game (done ✓ / next / waiting, each a link), "n of 5", **Sofro's word last**
  (since 2026-10-28; before that it sat apart, "any time"). Once all five are played the line offers
  Sofro's free practice ("Keep writing"), a countdown to the next puzzles (midnight UTC, client-only),
  and for a signed-in player a **streak: days in a row all five were _solved_** (not just
  played; Sofro's word since 2026-10-28, which restarted every streak), shown from day 1 (`computeCircuitStreak` in
  `usePlayStats.ts`). A signed-in player's saved results mark the squares too, wherever they played. Side by side on a wide screen. It replaced the hero's
  single button. On phones the hero's four letter tiles shrink to one line so "Today" is on the first
  screen.
- **"Meet the games"** — the five apps as a deck (`GameDeck.vue`). It **never moves by itself** (the
  2 s self-deal was too fast to read and moved under clicks). Touch screens **swipe** the top card
  (left = next, right = back) with a hint under the deck; a mouse gets **‹ › under the deck** instead,
  chosen by `(pointer: coarse)` / `(hover: hover) and (pointer: fine)`, never by width. Game badges
  between them jump to a game. Each card carries its game's little board; **Mno's is drawn the way Mno
  draws a guess** — five equal tiles, signs included, each green / amber / grey. It opens on the next
  unplayed game; Sofro is "daily" like the rest since 2026-10-28.

## Backoffice (Editorial Admin)

The editorial write surface for Sabro's own content. It is **part of Sabro, not a client** — the "clients are read-only consumers" rule does not apply to it. It is the content-write surface for the whole ecosystem: every game's pool is populated here.

**Placement.** Admin routes inside the existing Sabro Nuxt frontend (`/admin/...`), gated by an area grant from Logto. No separate admin app — separation is by authorization, not by deployment.

**Write path.** All writes go through the same Application layer and FluentValidation as the rest of Sabro, via admin-scope API endpoints. No parallel, unvalidated write path.

**Current surface** — one section per content type, each following the same shape (list → edit, `Draft` ↔ `Published`, a playable toggle only on published rows):

| Section | Feeds | Notes |
|---|---|---|
| `/admin/lexicon` | Meltho | Unvocalized + optional vocalized Syriac (NFC on input), optional SBL transliteration, a gloss per configured language. Publish gated on a gloss in **every** configured language; computed playable length shown read-only |
| `/admin/historical-figures` | Shmo | The scoreable attributes; `PlayableInShmo` |
| `/admin/chants` | Nahlo | Melody, section, mode, variant, optional incipit + **recording upload**; `PlayableInNahlo`. `/admin/chants/sections` edits the sections and their allowed modes, and each section's and mode's Syriac name and line |
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
- Rate limiting applied to all public endpoints, **per caller** — see below
- OpenAPI/Swagger documentation generated automatically (used to generate TypeScript types for the frontend)

### Rate limiting

One fixed-window limiter, partitioned per caller: the Logto `sub` when the request is
authenticated, otherwise the client IP (`RateLimitPartitioning.ResolvePartitionKey`).
Until #268 (2026-09-28) it partitioned **nothing** — every request in the ecosystem
shared one 100/min window, and a live audit had recorded it as "per IP".

Configurable, never hardcoded, for the same reason as the anti-repetition windows:

| Config key | Default | What it is |
|---|---|---|
| `RateLimit:PermitLimit` | 100 | Requests per window, per partition |
| `RateLimit:WindowSeconds` | 60 | Window length |
| `TrustedProxies:Networks` | loopback + RFC 1918 | Networks whose `X-Forwarded-For` is believed |

**Two pipeline orderings in `Program.cs` are load-bearing.** Moving either one
silently re-collapses the limiter into a single bucket:
- **`UseForwardedHeaders` is first.** Caddy proxies from its own container, so without
  it every request's address is Caddy's — one bucket for the whole internet, and the
  proxy logged as the client on every Serilog line. `ForwardLimit = 1`.
- **`UseRateLimiter` comes after `UseAuthentication`.** Before it, `User` is still
  anonymous when the key is computed and the per-user branch can never fire.

`TrustedProxies` must stay an **allowlist**: `X-Forwarded-For` is client-supplied, so
trusting it from anywhere would let a caller choose their own partition. The wide
RFC 1918 default is safe only because the API publishes no host port — `caddy` is
the only service with a `ports:` mapping. Narrow it to the proxy's address in any
deployment that exposes the API directly. In prod Caddy sat at `172.18.0.13` on
2026-09-28, inside `172.16.0.0/12`; re-check if the compose network is ever recreated.

> ⚠️ **SSR shares one bucket.** All five frontends call the API at its public URL, so
> their server-side render fetches arrive from the host's address, not the visitor's
> — SSR across the hub and all four games is one partition, while browser traffic
> splits per visitor. It is the bucket that will hit the ceiling first: raise
> `RateLimit:PermitLimit` if SSR starts seeing 429s.

`RateLimitPartitioningTests` (unit + integration) pin this; the integration test fails
against the old ordering with the real symptom — a quiet caller gets 429 from someone
else's flood.

### Key endpoints

**Content (read):**
- `GET /api/v1/lexicon/...` — published lexicon reads.

**Daily puzzles (read, shared state)** — get-or-create per date, identical for all players, each respecting its own anti-repetition window:
- `GET /api/v1/play/meltho/today`
- `GET /api/v1/play/mno/today`
- `GET /api/v1/play/shmo/today`
- `GET /api/v1/play/nahlo/today` — **answers `409` when no chant is eligible** (empty pool, or one too small for the window). That is a normal state, not an error: clients render it as "no chant today".

**Client support reads:**
- `GET /api/v1/chants/answer-options` — Nahlo's suggestion lists. **They are unjoined**, so Nahlo's own form never hands over the mode: the audio already names the melody to anyone who recognises it.
  **One owner-ruled exception (2026-10-12):** `conditionalModeIds` + `melodyModeHints`. A *conditional* mode is one some moded section does not admit — today only the mshaḥelfotho (farde-only), derived from the section rows, never named. The form offers it (unnumbered) only once the player names a melody with a published chant in it, and a melody with no mode at all (a madrosho) hides the mode question. The always-offered modes are never paired with a melody. The same day the **section stopped being asked** in Nahlo: the round scores melody, mode and shuḥlofo.
- `GET /api/v1/chants/book` — the whole Beth Gazo for the hub's book page: every section in order with its published, recorded chants grouped by melody (mode, variant, incipit, recording), and every mode, each with its text. **This one is joined, on purpose (Owner, 2026-10-25):** the treasury is open to anyone who wants to learn or listen, and the Owner accepts that today's Nahlo can be looked up in it. It supersedes the old rule that no public payload may pair a melody with its mode; only the answer options above stay unjoined. Do **not** hide just today's chant from it: the gap would name the answer by elimination.
- `GET /api/v1/historical-figures` — Shmo's published roster (with each figure's descriptions and card fields, for Beth Ṣalme), fetched once per session and filtered client-side. No search endpoint yet; a `search` query param is the next step if the roster outgrows that.
- `GET /api/v1/play/meltho/library` — past Meltho words, public and anonymous. Today's word is never included.
- `GET /api/v1/play/meltho/leaderboard` (`api:v1:write`) — signed-in only, ranked by longest streak; appearing requires an opt-in on the profile, the caller's own standing is always shown.

**Play results (authenticated user writes / reads):**
- `POST /api/v1/play/results` (`api:v1:write`) — the authenticated user records their result; one per user/game/day (idempotent on the unique key).
- `GET /api/v1/play/results/me` (`api:v1:write`) — the authenticated user's own results, for the profile/dashboard surface.
- `GET /api/v1/play/shmo/collection` (`api:v1:write`) — the player's figure collection for the hub's Beth Ṣalme: every figure met (guessed, or revealed in a round given up) or found (solved), worked out from their Shmo results. Each card says whether it is **illuminated**: a 1 in 20 chance per find, derived from player, figure and day, never stored.

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

Each application in the ecosystem (Sabro, Meltho, Mno, Shmo, Nahlo, Sofro, and any future app) is declared as a separate OIDC application in the central Logto instance. Sabro's API validates JWT bearer tokens via Logto's JWKS endpoint using the standard `Microsoft.AspNetCore.Authentication.JwtBearer` middleware — no Logto-specific SDK needed on the backend.

The admin role used to gate the backoffice is carried in the token (Logto role / scope mapped to `api:v1:admin`).

Login UI is themed in Logto's admin console with Syriac-inspired branding. Sign-up methods: email/password, optional Google/GitHub social login. No academic federation (eduGAIN/Shibboleth) needed — this is a personal project.

When a new client application is added (e.g. a future history platform), it is registered in Logto's console with no modification to Sabro itself.

**Sign in once, signed in everywhere (Owner, 2026-10-12).** Each app keeps its own Logto session, so a sign-in on one app is invisible to the others. The bridge is `sabro_account`, a hint cookie on `.sabro.be` that only says "this browser has an account" — no identity, never an authority. A signed-out app that sees it sends the player through the ordinary `/sign-in` once, and Logto's existing session completes the round trip with no password. Rationed to one attempt per app, per browser, per day (localStorage), so backing out of a sign-in page never loops. **Deliberately not `prompt=none`**: `@logto/nuxt`'s callback throws a 500 on a failed silent attempt (why the Owner rejected it on 2026-08-08). The same `plugins/ecosystem-account.client.ts` is in all five frontends; change them together.

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
- **UptimeRobot — live since 2026-07-28.** Eight HTTP(s) monitors on a 5-minute
  interval (the free plan's floor), emailing on downtime:

  | Monitor | URL |
  |---|---|
  | Sabro API | `https://api.sabro.be/health` |
  | Sabro hub | `https://sabro.be` |
  | Meltho | `https://meltho.sabro.be` |
  | Mno | `https://mno.sabro.be` |
  | Shmo | `https://shmo.sabro.be` |
  | Nahlo | `https://nahlo.sabro.be` |
  | Sofro | `https://sofro.sabro.be` (added 2026-10-15) |
  | Umami analytics | `https://analytics.sabro.be` |

  > **Nahlo was monitored long before the docs said so.** Until 2026-10-01 this file
  > called Nahlo unmonitored, but checking the console showed a `nahlo.sabro.be` monitor
  > already up for 1 month 22 days. It was added around the time of Nahlo's deployment
  > slot (#199, 2026-08-09) and never recorded here. `analytics.sabro.be` really was
  > unwatched; it got its monitor on 2026-10-01. **Check the console before recording a
  > monitor as missing.** UptimeRobot itself is the source of truth for this table, and
  > the table can drift from it.

  Alerts go to the **Owner's personal mailbox directly**, deliberately *not* via
  `contact@sabro.be` — that address forwards to Hotmail and Microsoft drops the
  forwarded mail silently, which would give detection with no delivery.

  Verified end to end at setup: the API monitor's pings are visible server-side
  (`HTTP HEAD /health` every 5 min in `docker logs sabro-api`), all five URLs
  answer `HEAD` with 200, and a deliberately-failing throwaway monitor confirmed
  an alert email actually **arrives**. The free plan has a single account user, so
  every monitor alerts the same contact: the Owner's personal mailbox, which is the
  address the account signs in with.

  > UptimeRobot probes with **HEAD**, not GET. Grepping logs for `GET /health`
  > will show zero hits and look like the monitor is dead.

> **`/health` is not a freshness check.** It answers "can this instance serve
> requests", not "is the site serving the code we shipped". A stale container
> passes it happily — on 2026-07-28 production silently served a two-week-old
> image with `/health` green throughout. Use `/version` on both `api.sabro.be` and
> `sabro.be` to prove prod matches `main`; CD asserts this after every deploy.

> **`/health` cannot see Meilisearch either.** From 2026-10-01 to 2026-10-12 the
> engine crash-looped (15,000+ restarts) because the floating `v1.53` tag was
> repointed to 1.53.3 and refused the 1.53.1 volume — `/health` and UptimeRobot
> stayed green throughout. The tag is now pinned to the patch; a meilisearch
> image bump of any size is an upgrade ritual, never a routine tag PR.

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
- **Reverse proxy**: **Caddy** in frontal — automatic Let's Encrypt HTTPS, one `reverse_proxy` block per domain. Holds requests across a container swap during deploys with `lb_try_duration` (the `swap_retry` snippet), which replaces blue-green at this scale. It did **not** do this by default: until 2026-10-01 the Caddyfile had no retry, and a deploy measured ~14 s of 502 on the API.
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

> ⚠️ **Keep the integration fixtures on the versions production runs.**
> `PostgresFixture` is `postgres:17-alpine` and `MeilisearchFixture` is
> `getmeili/meilisearch:v1.55.0`, matching `docker-compose.prod.yml` (moved together
> since 2026-08-27, #249, #250; last bumped 2026-10-25 with the v1.55.0 upgrade). They had drifted to Postgres 16 and Meilisearch v1.13,
> which meant every module's migrations and all index behaviour were signed off
> against software we do not ship — and it is part of why the Meilisearch client
> bump reached CI before its API change was noticed. **Move these whenever the
> compose pins move.**
>
> Neither fixture keeps a volume: each run gets a fresh container, so they never
> meet the data-directory incompatibility that governs a production upgrade.

**Testcontainers version note.** `Testcontainers` 4.14.0 obsoletes the
parameterless `ContainerBuilder()` / `PostgreSqlBuilder()` constructors in favour
of ones taking the image, and `TreatWarningsAsErrors` turns that CS0618 into a
build failure — so the image goes in the constructor, not in a following
`WithImage` call.

> ⚠️ **Never pass `--nologo` to `dotnet test`.** The test projects run on
> Microsoft.Testing.Platform, which does not reject the VSTest flag as unknown — it
> forwards it to the test app, which refuses the run and reports **`Zero tests ran`,
> exit 5**. That reads like a broken SDK or project; it is neither. Plain
> `dotnet test` runs all 660 unit tests. When a local run fails and CI passes, diff
> the *command* before the environment. Documented in `sabro-ci.yml` (#270);
> upstream: dotnet/sdk#55309. For a subset, run the test exe with `-class`.
> CI now also asserts a **minimum test count** (`--minimum-expected-tests`, 600 unit /
> 480 integration, set 2026-10-01): too few tests fails with exit 9 and "Minimum
> expected tests policy violation", so a filter or discovery break cannot pass quietly.
> Raise the floors as the suites grow; never lower them to get a build through.

### Coverage Targets
- Domain + Application: **80–90%**
- Infrastructure: **50–60%**
- API/Controllers: **40–50%** (mostly covered by integration tests)
- **Global target: 70–75%**

### CI Enforcement
The *intent* is that a coverage drop blocks CI on **Domain and Application** layers only — other layers report coverage but do not block merges, avoiding contortions to inflate metrics on infrastructure code.

> ⚠️ **That gate does not actually exist yet.** Checked 2026-08-27: CI collects coverage and uploads `*.cobertura.xml` as an artifact, and nothing reads it — there is no threshold, no ReportGenerator step and no failure condition anywhere in `sabro-ci.yml`. Coverage is *observable*, not *enforced*. Treat the targets above as aspirations until something actually asserts them, and do not assume a merge was gated on them.

### TDD Discipline
- **Strict TDD** for Domain and Application: write the failing test first
- **Test-after** for Infrastructure (EF Core, repositories) and Controllers — covered by integration tests rather than unit tests

---

## CI/CD (GitHub Actions)

### CI Pipelines (this repo)
- `sabro-ci.yml` — build, unit tests, integration tests, coverage, lint, format
- `pr-validation.yml` — Conventional Commits check, lint, format

(Meltho lives in its own repository and carries its own `meltho-ci.yml` — build, Vitest, Playwright. It is not part of Sabro's pipelines.)

**Dependabot** watches npm and NuGet in all five repos (minor + patch grouped), and
**security alerts are enabled on all five** since 2026-09-28 — they had been off
everywhere, so advisories surfaced only when someone ran `npm audit`. Secret scanning
is on for public Sabro only; the four private client repos would need paid Advanced
Security.

**npm advisories after the 2026-10-12 sweep.** None of what remains is in `.output`, so the live
sites run none of it. npm's suggested `nuxt@3.7.4` "fix" is a downgrade: never apply it.
- **node-forge — stubbed out.** No fixed release exists (GHSA-86w9-cpqp-85rv). Its only user is
  listhen, for `nuxt dev --https` certificates, so all five frontends replace it with
  `vendor/node-forge/`, our own stand-in that throws if used: a `file:` dev dependency plus
  `overrides: { "node-forge": "$node-forge" }` (a bare relative `file:` in `overrides` resolves
  against listhen's folder and breaks `npm ci`). The Dockerfiles `COPY vendor` before `npm ci`.
  Cost: `nuxt dev --https` is unavailable. **Remove all of it when node-forge ships a fix.**
- **simple-git — shimmed to v4.** Every 3.x carries critical advisories; the fix is simple-git 4,
  which dropped the default export Nuxt DevTools 3 imports (`import Git from 'simple-git'`), so a
  plain override to 4 crashes `nuxt dev`. `vendor/simple-git/` re-exports the released 4.0.2
  (`npm:simple-git@4.0.2`) with that default export, wired the same way as node-forge
  (`file:` dev dependency + `"$simple-git"` override). Local DevTools only. **Remove it once
  DevTools 4 is stable and depends on simple-git 4** (Owner, 2026-10-12: shim now, DevTools 4 later).
- **braces — still open, the only one.** No fixed release exists (GHSA-vfj7-8cjw-p6xm); its fix is
  an unmerged upstream PR (micromatch/braces#88). The Owner chose to wait for the release over
  building with unreleased code. Build-time only (i18n plugin and nitropack's globbing).
  Dependabot security alerts flag it as fixable the day a release lands.

Both stand-ins live in `vendor/` in all five frontends; change them together.

Not every Dependabot PR is meant to merge: TS 7 and `@types/node` majors
are expected closes (*Outstanding Worklist* item 1), and image bumps for Meilisearch
and Logto need their upgrade rituals — green CI cannot see those.

### CD
GitHub Actions builds Docker images for the API and frontend, pushes them to **GitHub Container Registry** (`ghcr.io`), then SSHes to the production VPS to pull and run `docker compose up -d`.

**Pipeline shape:**
1. CI (build, tests, coverage) gates the deploy job — a red CI blocks deploy.
2. Build multi-stage Dockerfiles for `Sabro.API` (.NET 10) and the Nuxt frontend. Tag images with the commit SHA.
3. Push to `ghcr.io/...` (free for private repos, native `GITHUB_TOKEN` auth, no Docker Hub rate limits).
4. SSH to the VPS, `docker compose pull`, run `docker compose run --rm api dotnet ef database update` (one-off migration container) **before** swapping app containers, then `docker compose up -d`.
5. Health-check `/health` post-deploy. Rollback = retag the previous image SHA and `docker compose up -d` (~30 s).

**Migrations rule — forward-compatible only.** No `DROP COLUMN` / rename / type-narrowing in a single deploy. Destructive changes go through an **expand → migrate → contract** sequence over multiple deploys. There is no blue-green at MVP scale. Caddy holds and retries requests across the API restart (`swap_retry`, up to 15 s), and CD swaps api/frontend *before* the general `up -d` so the API restarts once and never waits on another container.

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
- Not responsible for client game logic — **guess evaluation, feedback colouring, and presentation** live in each client, not in Sabro. The full answer ships with the puzzle and Sabro never scores a guess. (Sabro owns only the shared daily selection, because that state must be identical for all players and persisted.)
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
