<script setup lang="ts">
// The chosen Syriac script drives --font-syriac at the root, so the override
// cascades to every Syriac surface — including chrome like the nav brand mark
// and the script-switcher icon — not just <SyriacText>.
const { fontFamily } = useScriptVariant()

// Undefined for "system" omits the attribute so main.css's prefers-color-scheme
// query decides; SSR renders this into the initial HTML so there's no flash.
const { htmlAttr: themeAttr } = useTheme()

// --- App-wide SEO baseline; pages override title/description per surface. ---
const { t, locale } = useI18n()
const route = useRoute()
const config = useRuntimeConfig()
const siteUrl = config.public.siteUrl.replace(/\/$/, '')
// no_prefix i18n: one URL serves every language, so the canonical is simply
// the path (queries like ?page= are near-duplicates and stay uncanonical).
const canonical = computed(() => `${siteUrl}${route.path}`)

// Self-hosted, cookieless analytics. Injected here rather than from
// nuxt.config's static head so the endpoint stays a runtime value — the same
// image can run tracked in production and untracked anywhere else. Omitted
// entirely unless both values are configured, so there is never a dangling
// request to a nonexistent host.
const umami = computed(() => {
  const { umamiUrl, umamiWebsiteId } = config.public
  if (!umamiUrl || !umamiWebsiteId) return []
  return [{
    key: 'umami',
    src: `${umamiUrl.replace(/\/$/, '')}/script.js`,
    'defer': true,
    'data-website-id': umamiWebsiteId,
  }]
})

useHead({
  htmlAttrs: { lang: locale, 'data-theme': themeAttr },
  titleTemplate: (title?: string | null) =>
    title ? `${title} — ${t('site.title')}` : t('seo.home.title'),
  link: [{ rel: 'canonical', href: canonical }],
  script: [
    ...umami.value,
    {
      // Sitewide WebSite entity; the SearchAction points crawlers at the
      // library's typo-tolerant dictionary search.
      type: 'application/ld+json',
      innerHTML: JSON.stringify({
        '@context': 'https://schema.org',
        '@type': 'WebSite',
        'name': 'Sabro',
        'url': `${siteUrl}/`,
        'inLanguage': ['en', 'fr', 'nl'],
        'potentialAction': {
          '@type': 'SearchAction',
          'target': {
            '@type': 'EntryPoint',
            'urlTemplate': `${siteUrl}/library?q={search_term_string}`,
          },
          'query-input': 'required name=search_term_string',
        },
      }),
    },
  ],
})

useSeoMeta({
  description: () => t('seo.home.description'),
  ogSiteName: () => t('site.title'),
  ogType: 'website',
  ogUrl: () => canonical.value,
  ogTitle: () => t('seo.home.title'),
  ogDescription: () => t('seo.home.description'),
  ogImage: `${siteUrl}/brand/og-image.png`,
  twitterCard: 'summary_large_image',
})
</script>

<template>
  <div :style="{ '--font-syriac': fontFamily }">
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </div>
</template>
