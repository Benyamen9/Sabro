#!/usr/bin/env bash
set -euo pipefail

# Applies EF Core migrations for every module DbContext against the shared
# Sabro database. Run as a one-off container in CD *before* the app containers
# swap (expand -> migrate -> contract; forward-compatible migrations only).
#
# The connection string is read from ConnectionStrings__Sabro in the
# environment (the same variable the API uses) by each module's design-time
# DbContext factory. There is one DbContext per module; each tracks its own
# migration history, so they are applied independently into the one database.

if [[ -z "${ConnectionStrings__Sabro:-}" ]]; then
  echo "ERROR: ConnectionStrings__Sabro is not set; refusing to migrate." >&2
  exit 1
fi

cd /src

# "ContextName:project path" — keep in sync with the module DbContexts.
contexts=(
  "LexiconDbContext:src/Modules/Sabro.Lexicon"
  "IdentityDbContext:src/Modules/Sabro.Identity"
  "PlayDbContext:src/Modules/Sabro.Play"
  "TranslationsDbContext:src/Modules/Sabro.Translations"
  "ReviewsDbContext:src/Modules/Sabro.Reviews"
  "BiblicalDbContext:src/Modules/Sabro.Biblical"
)

for entry in "${contexts[@]}"; do
  context="${entry%%:*}"
  project="${entry##*:}"
  echo ">>> Applying migrations for ${context} (${project})"
  dotnet ef database update \
    --project "${project}" \
    --startup-project "${project}" \
    --context "${context}" \
    --configuration Release
done

echo ">>> All module migrations applied."
