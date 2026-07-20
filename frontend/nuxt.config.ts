import tailwindcss from '@tailwindcss/vite'

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-05-01',
  devtools: { enabled: true },
  modules: ['@nuxtjs/i18n', '@nuxt/eslint', '@logto/nuxt'],
  css: ['~/assets/css/main.css'],
  vite: {
    plugins: [tailwindcss()],
  },
  app: {
    head: {
      // <html lang> is set reactively from the active locale in app.vue —
      // hardcoding it here would leave FR/NL sessions declared as English.
      link: [
        // Brand favicons — rendered from the header badge (see public/brand/,
        // regenerated via the render-brand assets pipeline, not hand-drawn).
        { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/favicon-32.png' },
        { rel: 'icon', type: 'image/png', sizes: '16x16', href: '/favicon-16.png' },
        { rel: 'apple-touch-icon', sizes: '180x180', href: '/apple-touch-icon.png' },
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        {
          rel: 'stylesheet',
          href: 'https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:ital,opsz,wght@0,8..60,400;0,8..60,600;1,8..60,400&family=Noto+Sans+Syriac:wght@400;500;700&family=Noto+Sans+Syriac+Eastern:wght@400;500;700&family=Noto+Sans+Syriac+Western:wght@400;500;700&display=swap',
        },
      ],
    },
  },
  typescript: {
    strict: true,
  },
  i18n: {
    bundle: {
      optimizeTranslationDirective: false,
    },
    defaultLocale: 'en',
    strategy: 'no_prefix',
    locales: [
      { code: 'en', language: 'en-US', file: 'en.json', name: 'English' },
      { code: 'fr', language: 'fr-FR', file: 'fr.json', name: 'Français' },
      { code: 'nl', language: 'nl-BE', file: 'nl.json', name: 'Nederlands' },
      { code: 'de', language: 'de-DE', file: 'de.json', name: 'Deutsch' },
      { code: 'sv', language: 'sv-SE', file: 'sv.json', name: 'Svenska' },
    ],
    detectBrowserLanguage: {
      useCookie: true,
      cookieKey: 'sabro_locale',
      redirectOn: 'root',
      // Share the locale cookie across the ecosystem (sabro.be + *.sabro.be) so
      // a language chosen in one app holds in the others. Read at nuxt.config
      // eval (build time): the prod image is built with NUXT_PUBLIC_COOKIE_DOMAIN
      // set (see Dockerfile / CD build-args); empty in dev → host-only cookie.
      // Secure only when a domain is set, so http localhost dev still works.
      cookieDomain: process.env.NUXT_PUBLIC_COOKIE_DOMAIN || undefined,
      cookieSecure: Boolean(process.env.NUXT_PUBLIC_COOKIE_DOMAIN),
    },
  },
  // Logto OIDC bindings. Values come from NUXT_LOGTO_* env vars; the empty
  // defaults let the frontend build and boot without a running Logto
  // instance — useAuth() detects the unconfigured state and degrades to
  // the existing "Sign in required" UX. See frontend/.env.example.
  runtimeConfig: {
    logto: {
      endpoint: '',
      appId: '',
      appSecret: '',
      // Park the module's built-in sign-in route on an unused path: /sign-in
      // is served by our own server route (server/routes/sign-in.get.ts) so
      // the redirect to Logto can carry the user's chosen language via the
      // OIDC ui_locales parameter. Sign-out and the callback stay with the
      // module's handler.
      pathnames: {
        signIn: '/logto/sign-in',
        signOut: '/sign-out',
        callback: '/callback',
      },
      // Non-empty placeholder so the Logto event handler doesn't throw on
      // every request when running unconfigured (CookieStorage requires a
      // non-empty key). The cookie has no signed-in user when Logto isn't
      // configured, so this placeholder is harmless. ALWAYS override in
      // real deployments via NUXT_LOGTO_COOKIE_ENCRYPTION_KEY.
      cookieEncryptionKey: 'sabro-unconfigured-placeholder-do-not-use-in-prod',
      // Resources + scopes the SDK requests at sign-in. Without the Sabro
      // API resource here, getAccessToken(resource) can never return a
      // resource-bound token. Resources is an array, so it can't be set via
      // a single NUXT_LOGTO_* env override — derive it from the same
      // NUXT_PUBLIC_SABRO_API_RESOURCE used elsewhere (loaded from .env
      // before this config is evaluated). Empty array when unconfigured.
      resources: process.env.NUXT_PUBLIC_SABRO_API_RESOURCE
        ? [process.env.NUXT_PUBLIC_SABRO_API_RESOURCE]
        : [],
      // The three Sabro API scopes. Requesting a scope the signed-in user
      // hasn't been granted (e.g. api:v1:admin for a non-admin) is harmless —
      // Logto simply omits it from the issued token.
      scopes: ['api:v1:read', 'api:v1:write', 'api:v1:admin'],
    },
    public: {
      apiBaseUrl: 'http://localhost:5082/api/v1',
      // Canonical origin for SEO surfaces (canonical links, og:url, absolute
      // og:image, sitemap <loc>s). Override with NUXT_PUBLIC_SITE_URL.
      siteUrl: 'https://sabro.be',
      // Deployed build identity, served at /version (server/routes/version.get.ts)
      // and asserted by CD post-deploy. Baked into the prod image as the
      // NUXT_PUBLIC_BUILD_SHA env (Dockerfile BUILD_SHA build-arg); empty in dev.
      buildSha: '',
      // Shared cookie domain for ecosystem-wide preferences (locale + Syriac
      // script variant). Empty = host-only (dev); ".sabro.be" in prod so the
      // cookies are shared with Meltho and future apps. Baked from the same
      // build arg as the i18n locale cookie (NUXT_PUBLIC_COOKIE_DOMAIN, read at
      // nuxt.config eval / build time) so the script cookie gets the domain from
      // the image alone — no runtime compose env required. Still overridable at
      // runtime via NUXT_PUBLIC_COOKIE_DOMAIN.
      cookieDomain: process.env.NUXT_PUBLIC_COOKIE_DOMAIN || '',
      // Mirror of runtimeConfig.logto.endpoint so client code can detect
      // whether Logto has been configured without leaking the secret.
      logtoEndpoint: '',
      // The API resource (audience) the backend expects in the access
      // token's `aud` claim. Matches Sabro.API's Logto:Audience config.
      sabroApiResource: '',
      // Meltho, the first ecosystem app, linked from the hub header. Defaults
      // to prod so the link works without extra config; override with
      // NUXT_PUBLIC_MELTHO_URL.
      melthoUrl: 'https://meltho.sabro.be',
      // Mno, the numbers game, same convention (NUXT_PUBLIC_MNO_URL).
      mnoUrl: 'https://mno.sabro.be',
    },
  },
})
