// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-05-01',
  devtools: { enabled: true },
  modules: ['@nuxtjs/i18n', '@nuxt/eslint'],
  typescript: {
    strict: true,
    typeCheck: true,
  },
  i18n: {
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
  runtimeConfig: {
    public: {
      apiBaseUrl: 'http://localhost:5082/api/v1',
    },
  },
})
