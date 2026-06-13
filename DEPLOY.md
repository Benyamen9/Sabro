# Deploying Sabro

Production runbook for the Sabro ecosystem hub. Single-VPS deployment (Hetzner
CPX32) with all services co-located behind Caddy; images built in CI and pulled
from GHCR. This document covers the **one-time setup** and the **ongoing deploy
flow**.

See `CLAUDE.md` → *Hosting & Deployment* and *CI/CD* for the rationale behind
these choices.

## Architecture at a glance

```
GitHub Actions (sabro-ci  -> sabro-cd)
   build + push images to ghcr.io        SSH to VPS
                 │                            │
                 ▼                            ▼
        ghcr.io/<owner>/sabro-{api,frontend,migrator}
                                              │
                          docker compose pull / migrate / up -d
                                              │
   ┌──────────────────────────── VPS (CPX32) ───────────────────────────┐
   │  Caddy (:80/:443, auto-TLS)                                         │
   │    ├── sabro.<domain>        -> frontend (Nuxt SSR :3000)           │
   │    ├── api.sabro.<domain>    -> api (:8080)                         │
   │    ├── auth.<domain>         -> logto (:3001)                       │
   │    └── auth-admin.<domain>   -> logto (:3002)                       │
   │  postgres (shared DB)  meilisearch  seq  logto-db                   │
   └────────────────────────────────────────────────────────────────────┘
```

Relevant files in this repo:

| File | Purpose |
|---|---|
| `src/Sabro.API/Dockerfile` | API image — `build` / `migrator` / `runtime` targets |
| `frontend/Dockerfile` | Nuxt SSR image |
| `docker-compose.prod.yml` | the production stack |
| `Caddyfile` | reverse proxy, one block per hostname |
| `.env.prod.example` | template for the VPS `.env` |
| `scripts/apply-migrations.sh` | applies every module's EF migrations (migrator) |
| `scripts/vps-bootstrap.sh` | one-time host setup |
| `.github/workflows/sabro-cd.yml` | CD pipeline |

---

## One-time setup

### Phase 0 — Merge to `main` (builds the images)

Merging to `main` runs `sabro-ci`; on success `sabro-cd` builds and pushes
`sabro-api`, `sabro-frontend`, and `sabro-migrator` to GHCR (tagged with the
commit SHA and `latest`). The **deploy** job fails the first time because the
VPS isn't ready — that is expected. The images in GHCR are the goal of this
phase; re-run the deploy job at Phase 8.

### Phase 1 — Provision (Hetzner)

1. Create a **CPX32** server (Shared vCPU → CPX), image **Ubuntu 24.04**; add
   your SSH key.
2. Attach a **Cloud Firewall**: allow inbound **22/tcp, 80/tcp, 443/tcp,
   443/udp**; deny the rest.
3. (Soon after) Order a **BX11 Storage Box** for pgBackRest off-site backups.

### Phase 2 — Base host setup

Run `scripts/vps-bootstrap.sh` **once as root** on the server. It installs
Docker, creates the deploy user (in the `docker` group, with your SSH key),
creates the app dir, and logs that user into GHCR:

```bash
DEPLOY_USER=deploy \
DEPLOY_USER_PUBKEY="ssh-ed25519 AAAA... you@host" \
APP_DIR=/opt/sabro \
GHCR_USER=<github-user> \
GHCR_PAT=<token-with-read:packages> \
bash vps-bootstrap.sh
```

> **GHCR login is required.** The images are private; without this login,
> `docker compose pull` on the VPS fails. The bootstrap script handles it.

### Phase 3 — DNS

Point four **A** records (and **AAAA** if using IPv6) at the server. Caddy
auto-provisions TLS for each once they resolve:

| Hostname | Serves |
|---|---|
| `sabro.<domain>` | hub frontend |
| `api.sabro.<domain>` | API (`/api/v1`) |
| `auth.<domain>` | Logto (OIDC) |
| `auth-admin.<domain>` | Logto admin console |

Wait for resolution before bringing Caddy up, or ACME issuance fails.

### Phase 4 — Place files on the VPS

Copy into `APP_DIR` (e.g. `/opt/sabro`): `docker-compose.prod.yml`, `Caddyfile`,
and a filled **`.env`** (from `.env.prod.example`). Fill domains,
`CADDY_ACME_EMAIL`, strong `POSTGRES_PASSWORD` / `LOGTO_DB_PASSWORD`,
`MEILI_MASTER_KEY`, and `SABRO_API_RESOURCE`. Leave `NUXT_LOGTO_*` blank for now.

### Phase 5 — Bring up Logto first, then configure it

Production Logto is a **fresh instance** — local console setup does not carry
over.

```bash
cd /opt/sabro
docker compose -f docker-compose.prod.yml up -d logto-db logto caddy
```

In `https://auth-admin.<domain>`:
1. Create the admin account.
2. Create the **API resource** with identifier = `SABRO_API_RESOURCE`; add scopes
   `api:v1:read`, `api:v1:write`, `api:v1:admin`.
3. Register the **Sabro hub frontend** as a **Traditional Web** app. Copy its
   **App ID** / **App Secret** into `.env` (`NUXT_LOGTO_APP_ID` /
   `NUXT_LOGTO_APP_SECRET`); set `NUXT_LOGTO_COOKIE_ENCRYPTION_KEY`
   (`openssl rand -hex 32`); add redirect URIs for `https://sabro.<domain>`.
4. Create the **Owner/admin role** mapping to `api:v1:admin`; assign it to your
   user.

### Phase 6 — First full bring-up

```bash
cd /opt/sabro
echo "SABRO_IMAGE_TAG=<merged-sha>" > .env.deploy
COMPOSE="docker compose --env-file .env --env-file .env.deploy -f docker-compose.prod.yml"
$COMPOSE pull
$COMPOSE --profile migrate run --rm migrator   # creates all module schemas
$COMPOSE up -d
curl -fsS https://api.sabro.<domain>/health     # expect 200
```

### Phase 7 — Seed the launch word pool

The prod DB has schema but **no words** — migrations carry no data, so Meltho
has nothing to serve until you seed it. Recreate a **seeder M2M app** in prod
Logto carrying `api:v1:admin`, then run the existing seeder against the prod API:

```bash
# from a machine with the repo + node; uses client-credentials for an admin token
node scripts/seed-lexicon.mjs   # point it at the prod API + prod Logto
curl -fsS https://api.sabro.<domain>/api/v1/play/meltho/today   # expect a word
```

### Phase 8 — Hand CD the keys

Add these GitHub repo **secrets**, then re-run the failed `sabro-cd` deploy job
(Actions UI) or push any commit to `main`:

| Secret | Value |
|---|---|
| `VPS_HOST` | server IP / hostname |
| `VPS_USER` | the deploy user (e.g. `deploy`) |
| `SSH_PRIVATE_KEY` | private key matching the pubkey from Phase 2 |
| `APP_DIR` | e.g. `/opt/sabro` |
| `NUXT_PUBLIC_SABRO_API_RESOURCE` | same value as `SABRO_API_RESOURCE` (baked into the frontend image at build) |

From here, every merge to `main` auto-deploys.

---

## Ongoing deploys

Triggered automatically: **merge to `main`** → `sabro-ci` (green) → `sabro-cd`:

1. Build + push `sabro-api` / `sabro-frontend` / `sabro-migrator` to GHCR,
   tagged with the commit SHA.
2. SSH to the VPS, pin `SABRO_IMAGE_TAG` to that SHA, `docker compose pull`.
3. **Migrate first** (`--profile migrate run --rm migrator`), then `up -d`.
4. Health-check `https://api.sabro.<domain>/health` through Caddy.

Caddy buffers the ~2–3 s API restart, so no blue-green is needed at this scale.

### Rollback

Re-pin to the previous good SHA and bring up:

```bash
cd /opt/sabro
echo "SABRO_IMAGE_TAG=<previous-good-sha>" > .env.deploy
COMPOSE="docker compose --env-file .env --env-file .env.deploy -f docker-compose.prod.yml"
$COMPOSE pull && $COMPOSE up -d
```

> Migrations are **forward-compatible only** (expand → migrate → contract; no
> `DROP`/rename/narrowing in a single deploy), so rolling the app image back is
> safe against the already-migrated schema.

### Meilisearch

Indexes are **not** rebuilt on deploy and **not** backed up — rebuild on demand
from Postgres via the admin endpoints (`POST /api/v1/admin/search/rebuild/{index}`).

---

## Common pitfalls

- **No GHCR login on the VPS** → `docker compose pull` fails on private images.
  Fixed by Phase 2 (`vps-bootstrap.sh`).
- **Forgetting Phase 7** → the stack is healthy but Meltho has no words; the
  prod DB starts empty.
- **Caddy up before DNS resolves** → ACME cert issuance fails; wait for DNS.
- **`NUXT_PUBLIC_SABRO_API_RESOURCE` only set at runtime** → it is consumed at
  Nuxt build time to populate the Logto `resources` array, so it must be a build
  arg (CD passes it from the secret) and match `SABRO_API_RESOURCE`.

---

## Follow-ups (post-launch, not blocking)

- **pgBackRest** to the BX11: daily full + WAL archiving (PITR), 30 daily / 12
  monthly retention, weekly restore test. Back up the Logto DB and
  `wwwroot/media/` too. (Backup discipline is non-negotiable — see `CLAUDE.md`.)
- **UptimeRobot** pinging `/health` every 5 min.
- **Meltho**: point it at the live API and add its hostname to the API CORS
  origins (`MELTHO_DOMAIN` in `.env`).
- **Logto split** (when redeploys start disrupting other apps): move Logto to
  its own small VM (CPX11 class) so the central IDP doesn't restart with Sabro.
