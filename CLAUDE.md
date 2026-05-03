# CLAUDE.md — Sabro

## Project Overview

Sabro is an academic web platform for publishing original English translations of 12th-century Syriac patristic commentaries by Dionysios bar Ṣalibi (Metropolitan of Amid, d. 1171, Syriac Orthodox). It serves as the central backend in an ecosystem of applications dedicated to Syriac language and patristic studies, exposing a versioned REST API consumed by client applications (starting with Melthā, a Syriac Wordle game).

The long-term scope includes translating the entire Bible (Peshitta) and works of various Syriac Church Fathers.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 (LTS) |
| Database | PostgreSQL (via Entity Framework Core) |
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

```
        ┌─────────────────────────┐
        │   Logto (IDP central)   │
        │   self-hosted, OIDC     │
        └────────────┬────────────┘
                     │ JWT validated via JWKS
        ┌────────────┼────────────┐
        ▼            ▼            ▼
     [Sabro]      [Melthā]    [Future clients]
```

### Application Level — Modular Monolith
The backend is organized as a modular monolith. Each module is self-contained with its own domain, application, infrastructure, and public interface. Modules communicate only through explicit public interfaces — never through direct internal references.

```
Sabro/
├── src/
│   ├── Sabro.API/                  ← Entry point, controllers, middleware
│   ├── Sabro.Shared/               ← Shared types, interfaces, base classes
│   └── Modules/
│       ├── Sabro.Lexicon/          ← Words, roots, morphology, transliteration
│       ├── Sabro.Translations/     ← Translations, versioning, multilingual content
│       ├── Sabro.Reviews/          ← Peer review (3 levels), suggested edits workflow
│       ├── Sabro.Biblical/         ← Biblical passages (Peshitta), cross-references
│       └── Sabro.Identity/         ← User profiles, roles (Logto integration)
├── frontend/                       ← Nuxt application
│   ├── pages/
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
Manages the Syriac lexical database. Each entry includes the Syriac form (canonical, unvocalized), an optional vocalized form, the Semitic root, the SBL transliteration with accepted variants, grammatical category, morphology, and meanings (multilingual). This is the foundational data layer consumed by all other modules and by client applications.

### Translations
Manages original English translations of biblical books (Peshitta) and patristic works (starting with Dionysios bar Ṣalibi's commentaries). All content is added progressively (chapter by chapter, verse by verse). Every change creates a new version — full history is preserved. Content is authored in Markdown (rendered via Markdig). The schema supports multilingual content from day one (EN at MVP, FR + NL planned).

### Reviews
Three-level peer review system:
- **Verse-level** — individual verse translations
- **Chapter-level** — chapter-wide validation with cascade logic to verses
- **Annotation-level** — inline annotations and cross-references

Includes a suggested edits workflow: invited expert reviewers propose corrections; the translator (Owner) accepts or rejects each suggestion. Suggestions never modify content directly.

### Biblical
Manages Syriac biblical passages from the Peshitta. Stores passage references and links them to lexicon entries and translation annotations. For MVP: biblical cross-references only. Patristic and external citations deferred.

### Identity
User profiles and roles. Authentication itself is delegated to Logto via OIDC. Sabro stores only the user profile data it needs (preferences, language, script variant choice) linked by Logto user ID. Roles: Owner (translator), Expert Reviewer (invited), Reader (public, optional account for personal features like notes and favorites).

---

## API Design

- All endpoints versioned from day one: `/api/v1/...`
- RESTful conventions throughout
- JSON responses with consistent envelope structure
- Authentication via JWT bearer tokens validated against Logto's JWKS endpoint (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Scope-based authorization (e.g. `api:v1:read`, `api:v1:write`)
- Rate limiting applied to all public endpoints
- OpenAPI/Swagger documentation generated automatically (used to generate TypeScript types for the frontend)

---

## Search (Meilisearch)

Three Meilisearch indexes, kept in sync with PostgreSQL:
- `lexicon` — Syriac words, roots, transliterations, meanings
- `translations` — English translation text (FR/NL when available)
- `biblical_passages` — Peshitta passages with metadata

**Synchronization strategy:** synchronous at MVP — every write to PostgreSQL triggers a Meilisearch update in the same operation. May be moved to async (queue-based) if write volume grows.

**Transliteration synonyms:** declared in Meilisearch (e.g. `meltho` ≡ `meltā` ≡ `melthā` ≡ `meltha`) so users find the right entry regardless of romanization input.

PostgreSQL remains the source of truth — Meilisearch is a search optimization layer only.

---

## Syriac / Unicode Handling

### Script Variants
Sabro supports the three traditional Syriac scripts: **Estrangela** (default, used for patristic texts), **Serto** (Western), and **Madnhaya** (Eastern). The Unicode content is identical across variants — only the rendering font differs. User chooses default variant; switcher available everywhere syriac text is displayed.

Recommended fonts (free, academic):
- Beth Mardutho fonts: `Estrangelo Edessa`, `Serto Jerusalem`, `East Syriac Adiabene`
- Or Google's Noto Sans Syriac family

### Vocalization
Two separate fields stored per text:
- `syriac_unvocalized` — base text without vowel points
- `syriac_vocalized` — optional, with vowel points

The unvocalized field is **not** generated by stripping points — it is independently authored. Search defaults to the unvocalized field for tolerance.

### Transliteration
Provisional standard: **SBL** (Society of Biblical Literature). Stored alongside the canonical Syriac form, with accepted variants for search tolerance. May be revised after consultation with a Syriacist — the field is plain text, the decision is reversible.

### Unicode Technical Rules
- **Encoding everywhere**: UTF-8 (DB, network, files), UTF-16 internal (.NET)
- **Normalization**: NFC applied to all input before storage (`text.Normalize(NormalizationForm.FormC)`)
- **Validated Unicode ranges**: U+0700–U+074F (Syriac), U+0860–U+086F (Syriac Supplement)
- **PostgreSQL collation**: `und-x-icu` (ICU-based, language-agnostic Unicode sorting)
- **Direction**: dedicated `<SyriacText>` Vue component applies `dir="rtl"` automatically

---

## Internationalization (i18n)

### UI
All interface strings in `@nuxtjs/i18n` from day one. Three locale files prepared (`en.json`, `fr.json`, `nl.json`). EN filled at MVP; FR and NL translated later. No hardcoded UI strings anywhere — everything goes through `$t('key')`.

### Content
Schema is multilingual from day one (`language` column on `Translation` and `LexiconMeaning` tables). At MVP, only English content exists. UI displays a "coming soon" message for FR and NL until content exists. Adding new languages later requires no migration — just new content rows.

---

## Authentication (Logto)

Each application in the ecosystem (Sabro, Melthā, future apps) is declared as a separate OIDC application in the central Logto instance. Sabro's API validates JWT bearer tokens via Logto's JWKS endpoint using the standard `Microsoft.AspNetCore.Authentication.JwtBearer` middleware — no Logto-specific SDK needed on the backend.

Login UI is themed in Logto's admin console with Syriac-inspired branding. Sign-up methods: email/password, optional Google/GitHub social login. No academic federation (eduGAIN/Shibboleth) needed — this is a personal project.

When a new client application is added (e.g. a future history platform), it is registered in Logto's console with no modification to Sabro itself.

---

## Logging & Monitoring

### Logging (Serilog + Seq)
Structured logging via Serilog, shipped to a self-hosted Seq instance for visualization and querying.

**Always log:** errors and exceptions, critical write operations (translation creation, chapter approval, lexicon edits), failed auth attempts.

**Never log:** passwords, full JWT tokens, personally identifiable information (GDPR).

**Always include context:** user ID, request ID, timestamp.

**Log levels:** Debug (dev only) / Information (normal operations) / Warning (non-blocking anomalies) / Error (recoverable errors) / Fatal (app crash imminent).

### Monitoring
- ASP.NET Core health check endpoint at `/health`
- UptimeRobot pings the endpoint every 5 minutes; alerts on downtime via email

Stack Prometheus/Grafana deferred — current solution is sufficient for the project's scale.

---

## Backups (pgBackRest)

Sabro's translations are original work and irreplaceable — backup discipline is non-negotiable.

**Strategy:**
- **Daily full backup** of PostgreSQL automated via pgBackRest
- **Continuous WAL archiving** for point-in-time recovery
- **Retention**: 30 daily backups + 12 monthly backups
- **Weekly automated restore test** to verify backup integrity
- **3-2-1 rule**: 3 copies, 2 supports, 1 off-site (off-site location chosen with hosting decision)

**Also backed up:**
- `wwwroot/media/` (bibliography images) — synced separately to off-site storage
- Logto database (if separate from main DB)

Meilisearch indexes are not backed up — they are rebuilt from PostgreSQL on demand.

---

## Testing Strategy

Test pyramid with TDD-first approach for Domain and Application layers.

### Distribution
- **~70% Unit tests** — Domain rules, validators, mappers, pure logic
- **~25% Integration tests** — modules tested with real PostgreSQL via Testcontainers
- **~5% E2E tests** — critical user flows (login, suggest edit, approve translation)

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

### CI Pipelines
- `sabro-ci.yml` — build, unit tests, integration tests, coverage, lint, format
- `meltha-ci.yml` — build, Vitest, Playwright
- `pr-validation.yml` — Conventional Commits check, lint, format

### CD
Deferred — depends on hosting decision. CI alone is in place from day one.

### Branching Strategy
- `main` — protected, always deployable, requires PR + green CI
- `develop` — integration branch
- `feature/short-description` — feature branches off `develop`
- `fix/short-description` — bug fix branches

### Commits — Conventional Commits
```
feat(translations): add versioning to chapter content
fix(reviews): correct cascade logic for chapter approval
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
- Interfaces prefixed with `I`: `ITranslationService`, `ILexiconRepository`
- Async methods suffixed with `Async`: `GetTranslationAsync()`
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
- Component names: PascalCase (`TranslationViewer.vue`)
- Composables prefixed with `use`: `useTranslation()`, `useLexicon()`
- All user-facing strings go through i18n — no hardcoded UI text
- ESLint + Prettier enforced via CI

### Database
- Migrations managed via EF Core — never edit the database manually
- Table names: snake_case (`translation_versions`, `lexicon_entries`)
- All tables have `created_at` and `updated_at` timestamps
- Soft delete not used — hard delete with versioning as safety net

---

## Key Business Rules

- All translations are original work by the project owner — no copyrighted third-party content
- Every translation edit creates a new version — previous versions are never deleted
- Chapter-level approval cascades validation to all its verses (unless individually overridden)
- Suggested edits from reviewers create pending proposals — they never modify content directly
- Only the Owner accepts or rejects proposals
- Client applications (Melthā, etc.) are read-only consumers of Sabro's API — they never write to Sabro's database
- Bibliography images are stored locally under `wwwroot/media/` (sources directory) — small volume, no S3 needed at this scale

---

## Environment Setup (Windows + Visual Studio)

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Install [PostgreSQL](https://www.postgresql.org/download/windows/) and create a `sabro_dev` database
3. Install [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Meilisearch, Seq, Logto, Testcontainers)
4. Install [Node.js LTS](https://nodejs.org/) for the Nuxt frontend
5. Clone the repo and open `Sabro.sln` in Visual Studio
6. Copy `appsettings.Development.example.json` to `appsettings.Development.json` and fill in connection strings, Logto config, Meilisearch URL
7. Start auxiliary services: `docker-compose up -d` (Meilisearch + Seq + Logto)
8. Run migrations: `dotnet ef database update --project src/Modules/Sabro.Translations`
9. Start the API: F5 in Visual Studio
10. Start the frontend: `cd frontend && npm install && npm run dev`

---

## What Sabro Is Not

- Not a CMS — content is scholarly and curated, not crowdsourced
- Not a social platform — user features are personal and private (notes, favorites)
- Not a microservices architecture — it is a modular monolith; do not split modules into separate deployable services unless a clear scaling need arises
- Not responsible for client app logic — Melthā's game mechanics live in Melthā, not in Sabro
- Not federated with academic identity providers (eduGAIN/Shibboleth) — this is a personal project, not affiliated with an institution

---

## Deferred Decisions

These decisions are intentionally deferred and will be made when relevant:
- **Hosting** — VPS choice, Docker orchestration, deployment target
- **Off-site backup storage** — depends on hosting (likely Hetzner Storage Box, Backblaze B2, or Cloudflare R2)
- **CD pipelines** — depend on hosting
- **Bibliography page covers** — copyright vs pragmatic display, decided at page creation time
- **Async Meilisearch sync** — only if write volume grows
- **Async job queue** — Hangfire or similar, only if needed for background processing
