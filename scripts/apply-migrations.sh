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

# Active-module DbContexts only — Lexicon, Identity, Historical, Play, Reviews.
# Translations and Biblical have migrations in the tree but are NOT part of the
# launch and must not create their schema in production; add them here when
# those modules are un-deferred.
#
# ⚠️ Every module registers its DbContext in DI, deferred ones included, so DI is
# no signal of what belongs here. This list is the only place that says which
# schemas exist in production — a module shipped without an entry here works
# everywhere except production, and fails there at the first write with
# 42P01 "relation does not exist".
#
# Reviews was added 2026-08-03, after the reviewer workflow shipped and every
# proposal in production failed on a missing `reviews.suggested_edits`. The
# module was built while Reviews was still marked deferred, and un-deferring it
# in the code never reached this list.
#
# Historical precedes Play: Play's Shmo daily puzzle points at a historical
# figure, so the roster's schema must exist before Play's does. Reviews has no
# cross-module foreign keys at all — its target is a string discriminator, by
# design — so its position here is free.
#
# "ContextName:project path" — keep in sync with the active module DbContexts.
contexts=(
  "LexiconDbContext:src/Modules/Sabro.Lexicon"
  "IdentityDbContext:src/Modules/Sabro.Identity"
  "HistoricalDbContext:src/Modules/Sabro.Historical"
  "PlayDbContext:src/Modules/Sabro.Play"
  "ReviewsDbContext:src/Modules/Sabro.Reviews"
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
