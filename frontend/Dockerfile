# syntax=docker/dockerfile:1
#
# Multi-stage build for the Sabro Nuxt frontend (SSR / Nitro node server).
# Build context is ./frontend:
#
#   docker build -f frontend/Dockerfile -t sabro-frontend ./frontend
#
# Tailwind v4 / @tailwindcss/vite pull in lightningcss native binaries, so the
# build must run on Linux against a Linux-generated package-lock.json — which
# is exactly what this image does (node:22 bookworm).

# ---- Build: install deps + nuxt build ----------------------------------------
FROM node:22-bookworm-slim AS build
WORKDIR /app

# NUXT_PUBLIC_SABRO_API_RESOURCE is read at nuxt.config eval (build) time to
# populate the Logto `resources` array; a runtime override cannot add it back,
# so it must be present at build time. Everything else is overridable at run
# time via runtimeConfig env vars.
ARG NUXT_PUBLIC_SABRO_API_RESOURCE=""
ENV NUXT_PUBLIC_SABRO_API_RESOURCE=${NUXT_PUBLIC_SABRO_API_RESOURCE}

# Install dependencies first for layer caching. --ignore-scripts skips the
# `postinstall: nuxt prepare` lifecycle hook, which would fail before the
# project sources are copied; `nuxt build` runs prepare itself below.
COPY package.json package-lock.json ./
# --legacy-peer-deps mirrors CI: npm 10.x incorrectly requires optional peer
# deps under `npm ci`. --ignore-scripts skips `postinstall: nuxt prepare`,
# which would fail before sources are copied (`nuxt build` runs prepare itself).
RUN npm ci --legacy-peer-deps --ignore-scripts

COPY . .
RUN npm run build

# ---- Runtime: Nitro node server ----------------------------------------------
FROM node:22-bookworm-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production
# Nitro binds $HOST:$PORT — listen on all interfaces inside the container.
ENV HOST=0.0.0.0
ENV PORT=3000
EXPOSE 3000

COPY --from=build /app/.output ./.output

USER node
ENTRYPOINT ["node", ".output/server/index.mjs"]
