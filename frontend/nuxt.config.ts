// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-05-01',
  devtools: { enabled: true },
  modules: ['@nuxtjs/i18n', '@nuxt/eslint', '@logto/nuxt'],
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
