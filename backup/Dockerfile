# Backup sidecar for the production stack: nightly logical dumps of both
# Postgres databases (sabro + logto), local rotation, optional off-site sync to
# a Hetzner Storage Box over SFTP, and a weekly automated restore test.
#
# Based on the same postgres major as the servers so pg_dump/pg_restore match,
# and so the restore test can spin up a scratch cluster with the bundled
# server binaries. openssh-client provides sftp for the off-site sync.
FROM postgres:18-alpine

# openssh-client: sftp for the off-site sync. su-exec: run the restore-test
# scratch cluster as the postgres user (initdb refuses to run as root).
RUN apk add --no-cache openssh-client su-exec

COPY backup.sh restore-test.sh /usr/local/bin/
COPY crontab /etc/crontabs/root
RUN chmod +x /usr/local/bin/backup.sh /usr/local/bin/restore-test.sh

# This container runs cron, not a Postgres server — drop the inherited
# entrypoint (docker-entrypoint.sh would try to initdb and start a cluster).
ENTRYPOINT []
CMD ["crond", "-f", "-l", "8"]
