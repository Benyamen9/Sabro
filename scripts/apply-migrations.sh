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

# Active-module DbContexts only — Lexicon, Identity, Play. The deferred modules
# (Translations, Reviews, Biblical) have migrations in the tree but are NOT part
# of the launch and must not create their schema in production; add them here
# when those modules are un-deferred.
#
# "ContextName:project path" — keep in sync with the active module DbContexts.
contexts=(
  "LexiconDbContext:src/Modules/Sabro.Lexicon"
  "IdentityDbContext:src/Modules/Sabro.Identity"
  "PlayDbContext:src/Modules/Sabro.Play"
)

# Sabro.API is the single --startup-project for every context: it references the
# EF Core Design package and the module projects, driving the design-time build.
# (Individual modules don't all reference Microsoft.EntityFrameworkCore.Design,
# so using a module as its own startup project fails for Identity/Play.) Each
# module's IDesignTimeDbContextFactory still supplies the DbContext + connection
# string from ConnectionStrings__Sabro.
for entry in "${contexts[@]}"; do
  context="${entry%%:*}"
  project="${entry##*:}"
  echo ">>> Applying migrations for ${context} (${project})"
  dotnet ef database update \
    --project "${project}" \
    --startup-project src/Sabro.API \
    --context "${context}" \
    --configuration Release
done

echo ">>> All active-module migrations applied."
