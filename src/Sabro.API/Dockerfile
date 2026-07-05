# syntax=docker/dockerfile:1
#
# Multi-stage build for the Sabro API. Build context is the repository root
# (the project graph spans src/ plus the central Directory.*.props), so build
# this from the repo root:
#
#   docker build -f src/Sabro.API/Dockerfile -t sabro-api .
#
# Three stages:
#   build    - restore + publish the API (SDK image)
#   migrator - SDK image with dotnet-ef; applies every module's migrations.
#              Run as a one-off container in CD *before* swapping app containers.
#   runtime  - slim ASP.NET image that actually serves traffic.

# ---- Build: restore + publish ------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central build configuration first so the restore layer caches independently
# of source edits. .editorconfig carries the analyzer severities (StyleCop +
# CA rules) — without it, TreatWarningsAsErrors turns default-severity rules
# into build errors.
COPY global.json Directory.Build.props Directory.Packages.props stylecop.json .editorconfig ./

# The whole module graph is referenced by Sabro.API.csproj; a partial copy
# would break `dotnet restore`, so copy the full source tree.
COPY src/ ./src/

RUN dotnet restore src/Sabro.API/Sabro.API.csproj

RUN dotnet publish src/Sabro.API/Sabro.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Migrator: applies EF Core migrations for every module DbContext ---------
# Kept on the SDK image because `dotnet ef` needs the SDK + design-time build.
# The design-time factories read ConnectionStrings__Sabro from the environment
# (falling back to the local dev string), so the prod connection is supplied at
# run time via the same env the API uses.
FROM build AS migrator
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.0
COPY scripts/apply-migrations.sh /usr/local/bin/apply-migrations.sh
# Repo is edited on Windows — strip CRLF and make the entrypoint executable.
RUN sed -i 's/\r$//' /usr/local/bin/apply-migrations.sh \
    && chmod +x /usr/local/bin/apply-migrations.sh
ENTRYPOINT ["/usr/local/bin/apply-migrations.sh"]

# ---- Runtime: slim ASP.NET image ---------------------------------------------
# Debian-based (not Alpine): Directory.Build.props sets InvariantGlobalization
# = false, so the image must ship ICU for correct Syriac/Unicode handling.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Listen on plain HTTP inside the container; Caddy terminates TLS in front.
ENV ASPNETCORE_URLS=http://+:8080
# Honour X-Forwarded-Proto from Caddy so UseHttpsRedirection sees the original
# https scheme and does not issue a redirect loop behind the proxy.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080

COPY --from=build /app/publish ./

# Run as a non-root user.
RUN useradd --create-home --uid 1001 sabro \
    && chown -R sabro:sabro /app
USER sabro

# Deployed build identity, served at /version and asserted by CD post-deploy.
# Declared last so the per-commit value only invalidates this final layer.
ARG BUILD_SHA=unknown
ENV BUILD_SHA=${BUILD_SHA}

ENTRYPOINT ["dotnet", "Sabro.API.dll"]
