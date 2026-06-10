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
      htmlAttrs: { lang: 'en' },
      link: [
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        {
          rel: 'stylesheet',
          href: 'https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:ital,opsz,wght@0,8..60,400;0,8..60,600;1,8..60,400&family=Noto+Sans+Syriac:wght@400;500;700&display=swap',
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
    ],
    detectBrowserLanguage: {
      useCookie: true,
      cookieKey: 'sabro_locale',
      redirectOn: 'root',
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
      // Mirror of runtimeConfig.logto.endpoint so client code can detect
      // whether Logto has been configured without leaking the secret.
      logtoEndpoint: '',
      // The API resource (audience) the backend expects in the access
      // token's `aud` claim. Matches Sabro.API's Logto:Audience config.
      sabroApiResource: '',
    },
  },
})
