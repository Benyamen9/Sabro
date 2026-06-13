#!/usr/bin/env bash
set -euo pipefail

# Sabro VPS bootstrap — one-time base setup for the production host (Hetzner
# CPX32, Ubuntu 24.04). Run this ONCE as root on a fresh server. It is
# idempotent: safe to re-run.
#
# It does NOT deploy the stack — it prepares the host so the CD pipeline
# (.github/workflows/sabro-cd.yml) can SSH in, pull the GHCR images, and run
# docker compose. After this script, follow DEPLOY.md from Phase 3 (DNS).
#
# What it does:
#   1. Installs Docker Engine + the compose plugin.
#   2. Creates an unprivileged deploy user (the CD `VPS_USER`) in the docker
#      group, with your SSH public key.
#   3. Creates the app directory (the CD `APP_DIR`).
#   4. Logs that deploy user into GHCR so `docker compose pull` can fetch the
#      private images.
#   5. Optionally configures a host firewall (ufw) — off by default, since the
#      Hetzner Cloud Firewall is the recommended layer.
#
# Usage (fill the env vars, then run as root):
#
#   DEPLOY_USER=deploy \
#   DEPLOY_USER_PUBKEY="ssh-ed25519 AAAA... you@host" \
#   APP_DIR=/opt/sabro \
#   GHCR_USER=benyamen9 \
#   GHCR_PAT=ghp_xxx_read_packages \
#   bash vps-bootstrap.sh
#
# GHCR_PAT must be a GitHub PAT (classic) with the `read:packages` scope, or a
# fine-grained token with read access to the repo's packages. It is used only
# to log in; the credential is stored in the deploy user's ~/.docker/config.json.

# --- Required configuration ---------------------------------------------------
DEPLOY_USER="${DEPLOY_USER:?set DEPLOY_USER (the CD VPS_USER), e.g. deploy}"
DEPLOY_USER_PUBKEY="${DEPLOY_USER_PUBKEY:?set DEPLOY_USER_PUBKEY to your SSH public key}"
APP_DIR="${APP_DIR:?set APP_DIR (the CD APP_DIR), e.g. /opt/sabro}"
GHCR_USER="${GHCR_USER:?set GHCR_USER (your GitHub username), e.g. benyamen9}"
GHCR_PAT="${GHCR_PAT:?set GHCR_PAT (GitHub token with read:packages)}"

# --- Optional configuration ---------------------------------------------------
# Set SETUP_UFW=1 to configure a host firewall (allow 22/80/443, deny the rest).
# Leave unset if you use the Hetzner Cloud Firewall (recommended).
SETUP_UFW="${SETUP_UFW:-0}"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "ERROR: run this script as root." >&2
  exit 1
fi

echo ">>> [1/5] Installing Docker Engine + compose plugin"
if ! command -v docker >/dev/null 2>&1; then
  apt-get update -y
  apt-get install -y ca-certificates curl
  curl -fsSL https://get.docker.com | sh
fi
systemctl enable --now docker
docker --version
docker compose version

echo ">>> [2/5] Creating deploy user '${DEPLOY_USER}'"
if ! id "$DEPLOY_USER" >/dev/null 2>&1; then
  adduser --disabled-password --gecos "" "$DEPLOY_USER"
fi
usermod -aG docker "$DEPLOY_USER"

user_home="$(getent passwd "$DEPLOY_USER" | cut -d: -f6)"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "${user_home}/.ssh"
auth_keys="${user_home}/.ssh/authorized_keys"
# Add the key only if not already present (idempotent).
touch "$auth_keys"
if ! grep -qxF "$DEPLOY_USER_PUBKEY" "$auth_keys"; then
  echo "$DEPLOY_USER_PUBKEY" >> "$auth_keys"
fi
chmod 600 "$auth_keys"
chown "$DEPLOY_USER:$DEPLOY_USER" "$auth_keys"

echo ">>> [3/5] Creating app directory '${APP_DIR}'"
install -d -m 750 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$APP_DIR"

echo ">>> [4/5] Logging deploy user into GHCR"
echo "$GHCR_PAT" | sudo -u "$DEPLOY_USER" docker login ghcr.io -u "$GHCR_USER" --password-stdin

if [[ "$SETUP_UFW" == "1" ]]; then
  echo ">>> [5/5] Configuring ufw (22/80/443)"
  apt-get install -y ufw
  ufw allow 22/tcp
  ufw allow 80/tcp
  ufw allow 443/tcp
  ufw allow 443/udp
  ufw --force enable
else
  echo ">>> [5/5] Skipping ufw (SETUP_UFW != 1; use the Hetzner Cloud Firewall)"
fi

cat <<EOF

Done. Host is ready for deployment.

Next steps (see DEPLOY.md):
  - Phase 3: point DNS for the 4 hostnames at this server's IP.
  - Phase 4: copy docker-compose.prod.yml, Caddyfile, and a filled .env into:
        ${APP_DIR}
  - Phases 5-7: bring up Logto, configure it, first full bring-up, seed the pool.
  - Phase 8: add the GitHub secrets and re-run the sabro-cd deploy job.

Deploy user: ${DEPLOY_USER}   (use this as the CD secret VPS_USER)
App dir:     ${APP_DIR}        (use this as the CD secret APP_DIR)
EOF
