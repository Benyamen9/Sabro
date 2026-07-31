#!/bin/sh
# Weekly automated restore test — a backup only exists once it has restored.
#
# Takes the newest daily dump of each database, restores it into a scratch
# Postgres cluster inside this container (unix socket only, never touches the
# real servers), and runs sanity checks: the lexicon must have entries, the
# play results table must be readable, and the logto schema must have tables.
# Failure pings <BACKUP_HEARTBEAT_URL>/fail so it alerts like a missed backup.
# Runs from cron (see `crontab`) or by hand:
#   docker compose exec backup restore-test.sh
#
# POSIX sh (alpine/busybox) — no bashisms.
set -eu

BACKUP_ROOT=${BACKUP_ROOT:-/backups}
SCRATCH=/tmp/restore-test.$$
PGPORT_TEST=5599

log() { echo "[restore-test] $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }

as_pg() { su-exec postgres "$@"; }

cleanup() {
  status=$?
  as_pg pg_ctl -D "$SCRATCH/data" -m immediate stop >/dev/null 2>&1 || true
  rm -rf "$SCRATCH"
  if [ "$status" -ne 0 ]; then
    log "FAILED with status $status"
    if [ -n "${BACKUP_HEARTBEAT_URL:-}" ]; then
      wget -q -T 10 -O /dev/null "${BACKUP_HEARTBEAT_URL}/fail" || true
    fi
  fi
}
trap cleanup EXIT

latest_dump() {
  name=$(ls -1 "$BACKUP_ROOT/daily" 2>/dev/null | grep "^$1-" | grep -v '\.partial$' | sort -r | head -n 1)
  [ -n "$name" ] || { log "no $1 dump found in $BACKUP_ROOT/daily" >&2; exit 1; }
  echo "$BACKUP_ROOT/daily/$name"
}

SABRO_DUMP=$(latest_dump sabro)
LOGTO_DUMP=$(latest_dump logto)
log "testing $SABRO_DUMP and $LOGTO_DUMP"

# --- Media archive --------------------------------------------------------------
# backup.sh verifies the tarball when it writes it; this re-verifies the newest
# one a week later, which is what catches bit rot on the backup volume. A missing
# archive is not a failure — media is skipped entirely when the volume is not
# mounted, and the first archive only appears after the next nightly run.
media_archive=$(ls -1 "$BACKUP_ROOT/daily" 2>/dev/null |
  grep '^media-' | grep -v '\.partial$' | sort -r | head -n 1 || true)
if [ -n "$media_archive" ]; then
  tar -tzf "$BACKUP_ROOT/daily/$media_archive" >/dev/null ||
    { log "sanity failed: $media_archive is unreadable" >&2; exit 1; }
  # Media archiving skips itself (loudly, but only into the log) when the volume
  # is missing. Once it has worked at least once, a newest-archive older than a
  # week means it has silently stopped — fail so it alerts through the same
  # heartbeat as any other backup failure, instead of rotting unnoticed.
  if [ -z "$(find "$BACKUP_ROOT/daily" -name 'media-*.tar.gz' -mtime -8)" ]; then
    log "sanity failed: newest media archive ($media_archive) is over a week old — media backup has stopped" >&2
    exit 1
  fi
  log "sanity: $media_archive lists $(tar -tzf "$BACKUP_ROOT/daily/$media_archive" | grep -cv '/$') files"
else
  # Not a failure: media has never been archived here yet (first week after the
  # change, or a deliberately unmounted MEDIA_ROOT).
  log "no media archive yet — skipping media check"
fi

# Scratch cluster: socket-only on a non-standard port, torn down afterwards.
mkdir -p "$SCRATCH"
chown postgres "$SCRATCH"
as_pg initdb --auth=trust -D "$SCRATCH/data" >/dev/null
as_pg pg_ctl -D "$SCRATCH/data" -w -l "$SCRATCH/server.log" \
  -o "-p $PGPORT_TEST -k $SCRATCH -c listen_addresses=''" start >/dev/null

PSQL="psql -h $SCRATCH -p $PGPORT_TEST"

# restore <name> <dumpfile>
restore() {
  as_pg createdb -h "$SCRATCH" -p "$PGPORT_TEST" "check_$1"
  # --no-owner/--no-privileges: the scratch cluster has none of the prod roles.
  as_pg pg_restore --no-owner --no-privileges -h "$SCRATCH" -p "$PGPORT_TEST" \
    -d "check_$1" "$2"
  log "$1 restored into check_$1"
}

restore sabro "$SABRO_DUMP"
restore logto "$LOGTO_DUMP"

# --- Sanity checks -------------------------------------------------------------

entries=$(as_pg $PSQL -d check_sabro -Atc "SELECT count(*) FROM lexicon.lexicon_entries")
[ "$entries" -gt 0 ] || { log "sanity failed: lexicon.lexicon_entries is empty" >&2; exit 1; }
log "sanity: $entries lexicon entries"

# Readable proves the table restored; play data may legitimately be small.
results=$(as_pg $PSQL -d check_sabro -Atc "SELECT count(*) FROM play.game_results")
log "sanity: $results game results"

logto_tables=$(as_pg $PSQL -d check_logto -Atc \
  "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'")
[ "$logto_tables" -gt 10 ] || { log "sanity failed: logto restored only $logto_tables tables" >&2; exit 1; }
log "sanity: $logto_tables logto tables"

log "restore test passed"
