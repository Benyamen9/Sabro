#!/bin/sh
# Nightly logical backup of the ecosystem databases.
#
# Dumps both databases (sabro from `postgres`, logto from `logto-db`) in
# pg_dump custom format, keeps 30 daily + 12 monthly copies locally in the
# backup volume, and mirrors the same layout + retention to a Hetzner Storage
# Box over SFTP when STORAGE_BOX_HOST is configured. Runs from cron (see
# `crontab`) or by hand: `docker compose exec backup backup.sh`.
#
# POSIX sh (alpine/busybox) — no bashisms.
set -eu

BACKUP_ROOT=${BACKUP_ROOT:-/backups}
DAILY_KEEP=${DAILY_KEEP:-30}
MONTHLY_KEEP=${MONTHLY_KEEP:-12}
STAMP=$(date -u +%Y-%m-%d)

# Hosts are overridable so the script can be tested outside the prod stack.
SABRO_DB_HOST=${SABRO_DB_HOST:-postgres}
LOGTO_DB_HOST=${LOGTO_DB_HOST:-logto-db}

STORAGE_BOX_PORT=${STORAGE_BOX_PORT:-23}
STORAGE_BOX_REMOTE_DIR=${STORAGE_BOX_REMOTE_DIR:-sabro-backups}
STORAGE_BOX_KEY=${STORAGE_BOX_KEY:-/run/secrets/storagebox_ed25519}

log() { echo "[backup] $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }

# Heartbeat (e.g. healthchecks.io): ping on success, ping <url>/fail on
# failure, so a silently-dead backup job raises an alert instead of nothing.
finish() {
  status=$?
  if [ -n "${BACKUP_HEARTBEAT_URL:-}" ]; then
    if [ "$status" -eq 0 ]; then
      wget -q -T 10 -O /dev/null "$BACKUP_HEARTBEAT_URL" || true
    else
      wget -q -T 10 -O /dev/null "${BACKUP_HEARTBEAT_URL}/fail" || true
    fi
  fi
  [ "$status" -eq 0 ] && log "done" || log "FAILED with status $status"
}
trap finish EXIT

mkdir -p "$BACKUP_ROOT/daily" "$BACKUP_ROOT/monthly" "$BACKUP_ROOT/.ssh"

# --- Dump --------------------------------------------------------------------

# dump <name> <host> <db> <user> <password>
dump() {
  out="$BACKUP_ROOT/daily/$1-$STAMP.dump"
  log "dumping $1 ($3@$2) -> $out"
  # Write to a partial file first so a crash never leaves a truncated file
  # that looks like a valid backup.
  PGPASSWORD=$5 pg_dump --format=custom --host="$2" --username="$4" --dbname="$3" \
    --file="$out.partial"
  mv "$out.partial" "$out"
  log "$1: $(du -h "$out" | cut -f1) written"
}

dump sabro "$SABRO_DB_HOST" "${POSTGRES_DB:-sabro}" "${POSTGRES_USER:-sabro}" "$POSTGRES_PASSWORD"
dump logto "$LOGTO_DB_HOST" logto logto "$LOGTO_DB_PASSWORD"

# On the first of the month, keep a monthly copy of today's dumps.
if [ "$(date -u +%d)" = "01" ]; then
  for name in sabro logto; do
    cp "$BACKUP_ROOT/daily/$name-$STAMP.dump" "$BACKUP_ROOT/monthly/$name-$STAMP.dump"
    log "monthly copy kept: $name-$STAMP.dump"
  done
fi

# --- Local retention -----------------------------------------------------------

# prune_local <dir> <name> <keep>
prune_local() {
  ls -1 "$1" 2>/dev/null | grep "^$2-" | sort -r | tail -n +"$(($3 + 1))" |
    while read -r f; do
      rm -f "$1/$f"
      log "pruned local $1/$f"
    done
}

for name in sabro logto; do
  prune_local "$BACKUP_ROOT/daily" "$name" "$DAILY_KEEP"
  prune_local "$BACKUP_ROOT/monthly" "$name" "$MONTHLY_KEEP"
done

# --- Off-site sync (Hetzner Storage Box, SFTP) --------------------------------

if [ -z "${STORAGE_BOX_HOST:-}" ]; then
  log "STORAGE_BOX_HOST not set — skipping off-site sync (local backup only)"
  exit 0
fi

# ssh refuses group/world-readable identity files, and a bind-mounted secret
# can arrive with loose permissions — always work from a private 0600 copy.
KEY_COPY=$(mktemp)
cat "$STORAGE_BOX_KEY" > "$KEY_COPY"
chmod 600 "$KEY_COPY"

# Runs an sftp batch fed on stdin against the Storage Box.
run_sftp() {
  sftp -b - -i "$KEY_COPY" -P "$STORAGE_BOX_PORT" \
    -o StrictHostKeyChecking=accept-new \
    -o UserKnownHostsFile="$BACKUP_ROOT/.ssh/known_hosts" \
    "$STORAGE_BOX_USER@$STORAGE_BOX_HOST"
}

log "uploading to $STORAGE_BOX_USER@$STORAGE_BOX_HOST:$STORAGE_BOX_REMOTE_DIR"

# Upload today's dumps (and monthly copies on the 1st). The -mkdir lines are
# idempotent: the leading dash tells sftp to ignore "already exists" errors.
{
  echo "-mkdir $STORAGE_BOX_REMOTE_DIR"
  echo "-mkdir $STORAGE_BOX_REMOTE_DIR/daily"
  echo "-mkdir $STORAGE_BOX_REMOTE_DIR/monthly"
  for name in sabro logto; do
    echo "put $BACKUP_ROOT/daily/$name-$STAMP.dump $STORAGE_BOX_REMOTE_DIR/daily/$name-$STAMP.dump.partial"
    echo "rename $STORAGE_BOX_REMOTE_DIR/daily/$name-$STAMP.dump.partial $STORAGE_BOX_REMOTE_DIR/daily/$name-$STAMP.dump"
    if [ -f "$BACKUP_ROOT/monthly/$name-$STAMP.dump" ]; then
      echo "put $BACKUP_ROOT/monthly/$name-$STAMP.dump $STORAGE_BOX_REMOTE_DIR/monthly/$name-$STAMP.dump.partial"
      echo "rename $STORAGE_BOX_REMOTE_DIR/monthly/$name-$STAMP.dump.partial $STORAGE_BOX_REMOTE_DIR/monthly/$name-$STAMP.dump"
    fi
  done
} | run_sftp >/dev/null

# Mirror the retention remotely: list each remote dir, drop everything beyond
# the newest N per database. `.partial` leftovers from interrupted uploads
# sort below the real dumps and age out with everything else.
# prune_remote <subdir> <name> <keep>
prune_remote() {
  echo "ls -1 $STORAGE_BOX_REMOTE_DIR/$1" | run_sftp 2>/dev/null |
    grep -o "$2-[0-9-]*\.dump\(\.partial\)*$" | sort -r | tail -n +"$(($3 + 1))" |
    while read -r f; do
      echo "rm $STORAGE_BOX_REMOTE_DIR/$1/$f" | run_sftp >/dev/null
      log "pruned remote $1/$f"
    done
}

for name in sabro logto; do
  prune_remote daily "$name" "$DAILY_KEEP"
  prune_remote monthly "$name" "$MONTHLY_KEEP"
done

log "off-site sync complete"
