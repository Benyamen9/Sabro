#!/bin/sh
# Disk headroom watch for the VPS.
#
# On 2026-07-31 /dev/sda1 reached 100%, Postgres crash-looped on "No space left on
# device", and every data endpoint returned 500 for about fifteen minutes. Nothing
# warned beforehand: UptimeRobot can only see HTTP, and the disk had been filling for
# hours. This is the missing sensor.
#
# Runs from cron in the backup sidecar (see `crontab`) or by hand:
# `docker compose exec backup disk-check.sh`.
#
# It measures the filesystem backing $CHECK_PATH, which defaults to the backup volume
# — a real Docker volume, so it lives on the same partition as the images, the
# Postgres data directory and everything else that filled up. No host mount and no
# Docker socket needed: the container sees the underlying filesystem's true numbers
# through any volume on it.
#
# POSIX sh (alpine/busybox) — no bashisms.
set -eu

CHECK_PATH=${CHECK_PATH:-/backups}
DISK_USAGE_THRESHOLD=${DISK_USAGE_THRESHOLD:-80}

log() { echo "[disk-check] $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }

# -P forces single-line POSIX output; without it a long device name wraps and the
# percentage lands in a different column.
usage=$(df -P "$CHECK_PATH" | awk 'NR==2 { gsub("%", "", $5); print $5 }')

if [ -z "$usage" ]; then
  log "ERROR could not read disk usage for $CHECK_PATH"
  exit 1
fi

human=$(df -Ph "$CHECK_PATH" | awk 'NR==2 { print $3 " used of " $2 ", " $4 " free" }')

# Heartbeat (e.g. healthchecks.io), same pattern as the backup job: ping on OK, ping
# <url>/fail when over threshold. The check's own period/grace also covers the case
# this script stops running at all — silence alerts too, not just failure.
ping_heartbeat() {
  [ -n "${DISK_HEARTBEAT_URL:-}" ] || return 0
  wget -q -T 10 -O /dev/null "$1" || true
}

if [ "$usage" -ge "$DISK_USAGE_THRESHOLD" ]; then
  log "ALERT $CHECK_PATH is ${usage}% full (threshold ${DISK_USAGE_THRESHOLD}%) — $human"
  # Most likely culprit by far, and the one that caused the outage. Cheap to say here
  # so the alert email arrives with its own first diagnostic step attached.
  log "most likely unpruned Docker images: run \`docker system df\`, then \`docker image prune -af --filter \"until=24h\"\`"
  ping_heartbeat "${DISK_HEARTBEAT_URL:-}/fail"
  exit 1
fi

log "OK $CHECK_PATH is ${usage}% full (threshold ${DISK_USAGE_THRESHOLD}%) — $human"
ping_heartbeat "${DISK_HEARTBEAT_URL:-}"
