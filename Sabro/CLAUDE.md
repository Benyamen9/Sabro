# CLAUDE.md — Sabro Project Briefing

This file is the authoritative reference for the Sabro project. Read it fully at the start of every session before making any suggestions or changes.

---

## 1. Project Overview

**Sabro** is a personal academic platform to host the English translation of the biblical commentaries of **Dionysios bar Ṣalibi** (Syriac Orthodox Metropolitan of Amid, d. 1171), translated from Classical Syriac by the project owner.

### Core philosophy
- Quality over quantity
- Organic growth — the translator adds content at their own pace (verse by verse, annotation by annotation)
- All translations are original (Syriac → English) — no third-party copyrighted text
- Academic peer review by invited experts
- Free public reading, no paywall ever

### The project is lifelong
Bar Ṣalibi commented on the entire Bible. The translator intends to translate all of it over time. The platform must support sparse, progressively growing content gracefully.

---

## 2. Tech Stack (Non-Negotiable)

### Backend
- ASP.NET Core 8.0+
- Entity Framework Core 8.0+ with SQLite
- ASP.NET Core Identity (authentication)
- Markdig (Markdown → HTML)
- HtmlSanitizer (Ganss.Xss)
- FluentValidation
- Serilog (structured logging)
- xUnit + Moq + FluentAssertions (testing)

### Frontend
- React 18 + TypeScript
- Vite
- react-i18next (internationalization)
- Vitest + @testing-library/react (testing)

### DevOps
- GitHub Actions (CI/CD)
- Docker (optional)

### Architecture principles
- Clean separation: DTOs → Validators → Services → Mappers → Controllers
- Services use DbContext directly — no Repository pattern (acceptable at this scale)
- Global exception middleware with correlation IDs
- Structured logging with Serilog throughout all layers
- All code comments in English
- No hardcoded secrets — environment variables or user-secrets only

---

## 3. Data Model — Key Decisions

### 3.1 Versification
- **Follows the Peshitta** (the Syriac source text Bar Ṣalibi commented on)
- Verse IDs are **permanent** — they must never be renumbered after creation
- Breaking a verse ID breaks all annotation anchors attached to it
- Verses are entered manually as the translator works through the text — no bulk import

### 3.2 AnnotationAnchor — multi-verse spanning
- One `Annotation` can anchor to **one or more verses**
- Implemented as a one-to-many: `Annotation` → `AnnotationAnchor[]`
- Each `AnnotationAnchor` references one `Segment` (verse)
- The UI must support selecting a range of verses when creating an annotation

### 3.3 Text versions — four languages
Four `TextVersion` records are supported per passage:
| Language | Status at launch |
|----------|-----------------|
| English  | Active — translator's primary output |
| French   | Empty → shows "Coming soon" in reader |
| Dutch    | Empty → shows "Coming soon" in reader |
| Syriac   | Empty → shows "Coming soon" in reader |

- The reader detects whether a TextVersion has content for a given verse
- If empty: display a "Coming soon" placeholder, never an error
- Syriac requires **RTL rendering** (`dir="rtl"`) in the frontend

### 3.4 User model
- Single `ApplicationUser` table (extends ASP.NET Core IdentityUser)
- Role is a **column**, not a separate account — one person, one account, one role
- Roles: `Reader`, `Translator`, `Reviewer`, `Admin`
- The project owner holds the `Admin` role, which implicitly covers `Translator` permissions
- `IsReviewer` boolean flag on the user record
- `ReviewerQualifications` field on the user record

### 3.5 IsTrusted on reviewers
- Default rule: a reviewer **cannot approve content they themselves suggested an edit for** (separate-person enforcement)
- Exception: if `IsTrusted = true` on the reviewer's record, this check is bypassed
- `IsTrusted` is set by the Admin

---

## 4. Validation Workflow

### Four states (in order)
1. **Draft** — translator is working
2. **Self-Review** — translator has reviewed their own work
3. **Final Review** — submitted to an expert reviewer
4. **Approved** — validated and publicly visible

### Three independent validation levels
- **Verse (Segment)** — each verse validated independently
- **Chapter (ChapterValidation)** — auto-calculated from verse statuses; can also bulk-approve entire chapter
- **Annotation** — validated independently from biblical text

### Cascade rule
If ANY verse in a chapter is in "Needs Revision" → chapter status cascades to "Needs Revision"

### Suggested edits
- Reviewers propose corrections via `SuggestedEdit` entity
- Translator accepts (auto-applies + creates history entry) or rejects (with optional reason)
- All decisions tracked in history

### Versioning
- Every change to a `Segment` creates a `SegmentHistory` record
- Every change to an `Annotation` creates an `AnnotationHistory` record
- Fields: old content, new content, timestamp, changed-by user ID, reason

### Visibility
- Public sees all content with validation badges (Draft / Pending / Approved)
- Public can filter to show Approved only
- Reviewer comments are private — visible to translator only

---

## 5. Features by Phase

### MVP — Phases 1-4 (~10-12 weeks)
- Admin interface: add Bible versions, segments, sources, annotations
- Public reader (no login required)
- Three-level validation workflow with suggested edits
- Complete versioning and history
- Biblical cross-references
- Full-text search
- Filters: author, source, century, language, validation status

### Phase 5 — User accounts (~3-4 weeks)
- Optional registration and login
- Favorites (verses + annotations)
- Private personal notes (never shared)
- Custom reading lists
- Reading history tracking

### Phase 6 — Internationalization (~1-2 weeks)
- react-i18next for all UI strings
- French interface translation
- Dutch interface translation
- French and Dutch Bible text versions supported (shows "Coming soon" until added)

### Future phases — Planned, undesigned
These are confirmed for the future but implementation details are not yet specified:

**Monetization — Voluntary subscription**
- Support-the-project model only — no paywall, all content stays free
- Payment processor TBD (Stripe is the standard candidate)
- Will need: `Subscription` table, `Payment` table

**Social features**
- Comments on verses and annotations (moderation model TBD)
- Passage sharing — stable permalinks with rich preview (book/chapter/verse + snippet)
- Academic citation export — formats TBD (candidates: Chicago, SBL, Zotero/RIS)
- Will need: `Comment` table with `targetType` / `targetId` for polymorphic targets

**Also planned but not yet designed**
- Patristic citation network (Bar Ṣalibi quoting Ephrem, Chrysostom, etc.)
- External references (Talmud, Greek manuscripts, etc.)

---

## 6. Quality Standards

### Testing
| Layer | Minimum coverage |
|-------|-----------------|
| Overall | > 70% |
| Service layer | > 80% |
| Validators | > 90% |
| Critical paths | 100% |

- Framework: xUnit
- Mocking: Moq
- Assertions: FluentAssertions
- Pattern: AAA (Arrange, Act, Assert)
- Naming: `MethodName_Scenario_ExpectedBehavior`

### CI/CD
- GitHub Actions pipeline on every push to `main` and `develop`
- Pipeline fails if coverage < 70%
- Production deployment requires manual approval gate

### Logging
- Serilog with structured properties — never string interpolation in log calls
- Never log: passwords, tokens, personal data, full request/response bodies
- Include correlation ID on every request

### Error handling
- Global exception middleware catches all unhandled exceptions
- Standardized error response: `{ error, details, timestamp, path, correlationId }`
- Custom exception types: `NotFoundException`, `ValidationException`, `UnauthorizedException`, `ForbiddenException`

### Security
- Rate limiting: 100 req/min anonymous, 200 req/min authenticated, 5/min for login
- Response compression: Brotli + Gzip
- Security headers middleware: X-Content-Type-Options, X-Frame-Options, CSP
- HTTPS enforced in all non-development environments

---

## 7. Key Entities Reference

| Entity | Purpose |
|--------|---------|
| `TextVersion` | A Bible translation (one per language) |
| `Segment` | A single verse with content + ValidationStatus |
| `SegmentHistory` | Full version history of every Segment change |
| `Author` | A Church Father (initially only Bar Ṣalibi) |
| `Source` | A patristic work with copyright metadata |
| `Annotation` | A commentary linked to one or more verses |
| `AnnotationAnchor` | Links one Annotation to one Segment (many per annotation) |
| `AnnotationCrossReference` | Biblical cross-references within annotations |
| `ChapterValidation` | Chapter-level validation overview (auto-calculated) |
| `AnnotationHistory` | Full version history of every Annotation change |
| `SuggestedEdit` | Reviewer-proposed correction awaiting translator decision |
| `ApplicationUser` | Single user table with Role column |
| `UserFavorite` | Bookmarked verses and annotations |
| `UserNote` | Private personal notes on verses (never shared) |
| `ReadingList` | Custom reading path |
| `ReadingListItem` | Individual item in a reading list |
| `ReadingHistory` | Record of what the user has read and when |

---

## 8. What to Flag During Code Review

When reviewing existing code, always check for:

1. **Versification integrity** — verse IDs must be immutable after creation; warn if any code allows updating BookId, ChapterNumber, or VerseNumber on an existing Segment
2. **Anchor model** — AnnotationAnchor must be one-to-many (many anchors per annotation); flag if it is modelled as one-to-one
3. **Role model** — roles must be a column/Identity role on the single user table; flag any separate user type tables
4. **IsTrusted bypass** — the reviewer self-approval check must exist; flag if it is missing
5. **Language fallback** — the reader must show "Coming soon" for empty TextVersions, never throw or show null
6. **Syriac RTL** — any rendering of Syriac text must include RTL direction; flag missing `dir="rtl"`
7. **Secret handling** — no hardcoded connection strings, JWT secrets, or passwords anywhere in code
8. **Structured logging** — flag any `$"..."` string interpolation inside log calls
9. **History tracking** — every Segment and Annotation update must create a history record; flag missing history writes
10. **Validation state machine** — state transitions must be validated; flag any code that sets ValidationStatus directly without checking the current state

---

*This document was generated from the Sabro project specification v3 (April 2026).*
*Update this file whenever architectural decisions change.*
