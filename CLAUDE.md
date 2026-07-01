# CLAUDE.md — Sabro

## Project Overview

Sabro is an academic web platform for publishing original English translations of 12th-century Syriac patristic commentaries by Dionysios bar Ṣalibi (Metropolitan of Amid, d. 1171, Syriac Orthodox). It serves as the central backend in an ecosystem of applications dedicated to Syriac language and patristic studies, exposing a versioned REST API consumed by client applications (starting with Meltho, a Syriac Wordle game). Sabro also acts as the **ecosystem hub**: it owns shared identity (via Logto), user profiles, and cross-application play data.

The long-term scope includes translating the entire Bible (Peshitta) and works of various Syriac Church Fathers.

---

## Build Sequencing (current focus)

The long-term scope (full Peshitta + Church Fathers) is unchanged, but the build order has been re-sequenced. Translation work is set aside for now; the immediate goal is a launched, living ecosystem.

1. Ship Sabro as the ecosystem hub + API: **Lexicon**, **Identity/Profile**, **Play**, the `/api/v1/` contract, Logto, and a lean hub frontend.
2. Launch **Meltho** first, against that API.
3. **Translations, Reviews, and Biblical modules are deferred** to after Meltho's launch — fully specified below, but not on the launch critical path. Meltho depends only on the Lexicon, never on translated content.

**Launch critical path:**
Sabro deployed (Lexicon + Identity/Profile + Play + API + Logto) → backoffice (word CRUD) → populate a small launch pool (~30–50 published, playable words is enough to launch; the full 500–600 is a growth target, not a launch gate) → Meltho frontend (game + login + raw profile stats).

**Hub philosophy — wide model, narrow surface.** The data foundation is built to see far (multi-game results, profile, cross-project shape), but the launch UI stays lean: login + profile + raw Meltho stats. The rich dashboard, leaderboards, and any Shmo surface grow later on the same foundation, with no model rework.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 (LTS) |
| Database | PostgreSQL (via Entity Framework Core) — single shared database for the whole ecosystem |
| Search | Meilisearch (typo-tolerant, dedicated service) |
| Frontend | Nuxt (Vue 3 + TypeScript) |
| Auth | Logto (self-hosted IDP, OIDC) |
| Validation | FluentValidation |
| Markdown | Markdig |
| i18n | @nuxtjs/i18n (EN at MVP, FR + NL prepared) |
| Logging | Serilog + Seq |
| Monitoring | Health check endpoint + UptimeRobot |
| Backups | pgBackRest (point-in-time recovery) |

**Development environment:** Windows + Visual Studio

---

## Architecture

### System Level
Sabro exposes a versioned REST API (`/api/v1/`) consumed directly by client applications. There is no API gateway or hub component — clients call Sabro's API directly. Authentication is delegated to a self-hosted Logto instance shared across all applications in the ecosystem.

**Single source of truth.** There is **one shared PostgreSQL database** for the entire ecosystem, owned by Sabro. Client applications (Meltho, future apps) do **not** have their own application database and never connect to PostgreSQL directly. They read content and write their own play data exclusively through Sabro's API. (Logto keeps its own internal store for auth — that is infrastructure, not ecosystem application data.)

```
        ┌─────────────────────────┐
        │   Logto (IDP central)   │
        │   self-hosted, OIDC     │
        └────────────┬────────────┘
                     │ JWT validated via JWKS
        ┌────────────┼────────────┐
        ▼            ▼            ▼
     [Sabro]      [Meltho]    [Future clients]
        │            │            │
        │   reads content / writes play results
        │            │            │
        └──────► /api/v1/ ◄───────┘
                     │
                     ▼
        ┌─────────────────────────┐
        │  PostgreSQL (single DB)  │  ← Sabro is the only writer of record
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
│       ├── Sabro.Identity/         ← User profiles, roles (Logto integration)
│       ├── Sabro.Play/             ← Cross-game results + Meltho daily-puzzle state
│       ├── Sabro.Translations/     ← Translations, versioning, multilingual content (DEFERRED)
│       ├── Sabro.Reviews/          ← Peer review (3 levels), suggested edits workflow (DEFERRED)
│       └── Sabro.Biblical/         ← Biblical passages (Peshitta), cross-references (DEFERRED)
├── frontend/                       ← Nuxt application (public hub + admin backoffice)
│   ├── pages/
│   │   └── admin/                  ← Backoffice (role-gated)
│   ├── components/
│   ├── composables/
│   └── locales/                    ← i18n files (en, fr, nl)
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

**Required vs optional fields.** The unvocalized Syriac form is required. The vocalized form and SBL transliteration are optional enrichment — they do not gate publication. Meanings are required in all three languages (EN + FR + NL) for an entry to be publishable.

**Entry lifecycle — `Draft` → `Published`.** An entry may be saved as `Draft` with partial data (Syriac form today, FR/NL glosses later) without loss. It becomes `Published` only when all three glosses are present. Only `Published` entries may be marked playable or served to clients. This is how the three-language rule holds without forcing every entry to be finished in one sitting — the pool can be populated incrementally.

**Playable flag (`PlayableInMeltho`).** A manual editorial boolean set by the Owner. The Lexicon is broader than the puzzle pool — the Owner decides which published words make good puzzles. This is editorial curation, not an automatic property.

**Playable length — computed, read-only.** Derived from the unvocalized form as the count of base Syriac letters (Unicode letter-category characters). Combining marks (vowel points, seyame, diacritics) are not counted. Shown read-only in the backoffice so the editor can see at a glance whether a word falls in the 2–8 window. Never hand-entered.

**Eligible pool definition.** An entry is in the Meltho pool **iff** `Published` AND `PlayableInMeltho == true` AND `playable length ∈ [2, 8]`. The daily-selection logic (Play module) additionally enforces the 2–8 bound server-side as a hard guard, so a mis-flagged out-of-range entry can never reach the game.

### Identity / Profile
Owns **who the user is**. Authentication itself is delegated to Logto via OIDC; Sabro stores only the profile data it needs, linked by Logto user ID: display name, preferred language, default script variant, and personal preferences. Roles: Owner (translator/admin), Expert Reviewer (invited), Reader (public, optional account for personal features like notes, favorites, and game profile).

The hub's "my profile" surface reads from this module. Play history (what the user played) lives in the **Play** module, not here — Identity is identity, Play is activity. The dashboard composes both.

### Play
Owns **ecosystem play data**: cross-game results and Meltho's daily-puzzle state. This module exists because, with a single shared database and no per-client database, shared play state must live in Sabro. It is deliberately lean and is built multi-game from day one so Shmo can reuse it without rework.

**`GameResult` — generic, multi-game.** Keyed by Logto user ID + a `GameId` string discriminator (`meltho`, later `shmo`, …). Fields: `PlayedOn` (date), `Solved` (bool), `Attempts` (int), and an optional `DetailJson` for game-specific extras. Unique constraint on (`LogtoUserId`, `GameId`, `PlayedOn`) — one result per user, per game, per day. Streaks and aggregates are **derived** from results, not stored. This generic shape is the cross-project equivalent of the savant/ludic split used elsewhere: do not model a Meltho-specific scores table.

**Meltho daily puzzle — shared server state.** Records which Lexicon entry was served on which day (`GameId`, `Date`, `LexiconEntryId`). Selection is **get-or-create per date** (idempotent): the first request for a given day picks, records, and returns that day's word; subsequent requests return the recorded one, so every player gets the same puzzle. Selection draws from the eligible pool and excludes any word served within the **anti-repetition window**.

**Anti-repetition window — configurable, never hardcoded.** A configuration value (`Meltho:AntiRepetitionWindowDays`). Start it low so a small launch pool (~30–50 words) never starves, and raise it toward 365 as the pool grows past that size. A hardcoded 365 with a 40-word pool would leave the selector with no eligible word after 40 days and break the game — this must remain a config value.

**Boundary with Meltho.** Sabro answers *"what is today's word"* (shared state, single DB, central anti-repetition). Meltho owns the actual **game mechanics**: guess evaluation, hint coloring (green/yellow/grey), attempt UI, and result sharing. The daily-word selection is the one piece of shared game state that lives in Sabro rather than the client, precisely because it must be identical for all players and persisted.

### Translations
**Status: deferred (post-Meltho-launch). Spec retained, not built first.**

Manages original English translations of biblical books (Peshitta) and patristic works (starting with Dionysios bar Ṣalibi's commentaries). All content is added progressively (chapter by chapter, verse by verse). Every change creates a new version — full history is preserved. Content is authored in Markdown (rendered via Markdig). The schema supports multilingual content from day one (EN at MVP, FR + NL planned).

### Reviews
**Status: deferred (post-Meltho-launch). Spec retained, not built first.**

Three-level peer review system:
- **Verse-level** — individual verse translations
- **Chapter-level** — chapter-wide validation with cascade logic to verses
- **Annotation-level** — inline annotations and cross-references

Includes a suggested edits workflow: invited expert reviewers propose corrections; the translator (Owner) accepts or rejects each suggestion. Suggestions never modify content directly.

### Biblical
**Status: deferred (post-Meltho-launch). Spec retained, not built first.**

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

## Backoffice (Editorial Admin)

The editorial write surface for Sabro's own content. It is **part of Sabro, not a client** — the "clients are read-only consumers" rule does not apply to it. It is the first content-write surface in the ecosystem and sits on the launch critical path (populating the word pool depends on it).

**Placement.** Admin routes inside the existing Sabro Nuxt frontend (e.g. `/admin/lexicon`), gated by an admin role from Logto. No separate admin app — separation is by authorization, not by deployment.

**Write path.** All writes go through the same Application layer and FluentValidation as the rest of Sabro, via admin-scope API endpoints. No parallel, unvalidated write path.

**v1 scope (to launch Meltho): Lexicon word CRUD only.**
- Create / edit / delete a Lexicon entry: unvocalized + optional vocalized Syriac (NFC on input), optional SBL transliteration, EN + FR + NL glosses.
- `Draft` ↔ `Published` lifecycle (publish gated on all three glosses).
- `PlayableInMeltho` toggle (only on `Published` entries).
- Computed playable length shown read-only.

**Deferred (model kept ready, no UI at launch):** liturgical calendar / manual daily-word pinning; reviewer moderation; player statistics dashboards. None of these block Meltho's launch.

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

### Key endpoints (launch set)

**Content (read):**
- `GET /api/v1/lexicon/...` — published lexicon reads.

**Meltho puzzle (read, shared state):**
- `GET /api/v1/play/meltho/today` — returns today's puzzle for Meltho (get-or-create per date; identical for all players; respects the anti-repetition window).

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

Meilisearch indexes are kept in sync with PostgreSQL. At launch, only the **`lexicon`** index is active; the others come online with their (deferred) modules:
- `lexicon` — Syriac words, roots, transliterations, meanings **(active at launch)**
- `translations` — English translation text (FR/NL when available) *(with Translations module)*
- `annotations` — inline annotations with denormalized parent (source/chapter/verse) coordinates *(with Reviews/Translations)*
- `biblical_passages` — Peshitta passages with metadata *(with Biblical module)*

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
- **Validated Unicode ranges**: U+0700–U+074F (Syriac), U+0860–U+086F (Syriac Supplement)
- **PostgreSQL collation**: `und-x-icu` (ICU-based, language-agnostic Unicode sorting)
- **Direction**: dedicated `<SyriacText>` Vue component applies `dir="rtl"` automatically
- **Letter counting**: playable length counts Unicode letter-category code points only; combining marks are excluded

---

## Internationalization (i18n)

### UI
All interface strings in `@nuxtjs/i18n` from day one. Three locale files prepared (`en.json`, `fr.json`, `nl.json`). EN filled at MVP; FR and NL translated later. No hardcoded UI strings anywhere — everything goes through `$t('key')`.

### Content
Schema is multilingual from day one (`language` column on `Translation` and `LexiconMeaning` tables). Lexicon meanings require EN + FR + NL to publish (see Lexicon). For deferred translation content, only English exists at first, with a "coming soon" message for FR and NL. Adding new languages later requires no migration — just new content rows.

---

## Authentication (Logto)

Each application in the ecosystem (Sabro, Meltho, future apps) is declared as a separate OIDC application in the central Logto instance. Sabro's API validates JWT bearer tokens via Logto's JWKS endpoint using the standard `Microsoft.AspNetCore.Authentication.JwtBearer` middleware — no Logto-specific SDK needed on the backend.

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
- ASP.NET Core health check endpoint at `/health`
- UptimeRobot pings the endpoint every 5 minutes; alerts on downtime via email

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
- `main` — protected, always deployable, requires PR + green CI
- `develop` — integration branch
- `feature/short-description` — feature branches off `develop`
- `fix/short-description` — bug fix branches

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
- A Lexicon entry is publishable only with EN + FR + NL meanings; only published entries can be marked playable or served to clients
- **Client read/write rule.** Client applications (Meltho, future apps) are **read-only consumers of Sabro's content** — they never edit curated content and never connect to the database directly. They **may write their own play data** (game results) through controlled, authenticated API endpoints. All writes — content and play — go exclusively through Sabro's validated API.
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
8. Run migrations for the active modules (Lexicon, Identity, Play), e.g. `dotnet ef database update --project src/Modules/Sabro.Lexicon` (repeat per module)
9. Start the API: F5 in Visual Studio
10. Start the frontend: `cd frontend && npm install && npm run dev`

---

## What Sabro Is Not

- Not a CMS — content is scholarly and curated, not crowdsourced
- Not a social platform — user features are personal and private (notes, favorites, game profile)
- Not a microservices architecture — it is a modular monolith; do not split modules into separate deployable services unless a clear scaling need arises
- Not responsible for client game logic — Meltho's **guess evaluation, hint coloring, and presentation** live in Meltho, not in Sabro. (Sabro owns only the shared daily-word selection, because that state must be identical for all players and persisted in the single database.)
- Not federated with academic identity providers (eduGAIN/Shibboleth) — this is a personal project, not affiliated with an institution

---

## Deferred Decisions

These decisions are intentionally deferred and will be made when relevant:
- **Translations, Reviews, Biblical modules** — deferred to post-Meltho-launch. Specs retained above; not on the launch critical path.
- **Liturgical calendar / manual daily-word pinning** — data model kept ready (a scheduled date → word override); no backoffice UI at launch. Algorithmic get-or-create selection with the anti-repetition window covers launch.
- **Leaderboard, aggregate player stats, score sharing** — deferred; launch surface is profile + raw Meltho stats only.
- **Rich cross-project dashboard** — deferred; the `GameResult` model is already multi-game, so this is additive UI, not a model change.
- **Daily-puzzle selection trigger** — lazy get-or-create on first request at launch; may move to a small scheduled job if/when an async job queue (Hangfire or similar) is introduced.
- **Bibliography page covers** — copyright vs pragmatic display, decided at page creation time
- **Async Meilisearch sync** — only if write volume grows
- **Async job queue** — Hangfire or similar, only if needed for background processing (would also host the daily-puzzle scheduler)
