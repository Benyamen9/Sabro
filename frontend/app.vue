<script setup lang="ts">
// The chosen Syriac script drives --font-syriac at the root, so the override
// cascades to every Syriac surface — including chrome like the nav brand mark
// and the script-switcher icon — not just <SyriacText>.
const { fontFamily } = useScriptVariant()

// --- App-wide SEO baseline; pages override title/description per surface. ---
const { t, locale } = useI18n()
const route = useRoute()
const config = useRuntimeConfig()
const siteUrl = config.public.siteUrl.replace(/\/$/, '')
// no_prefix i18n: one URL serves every language, so the canonical is simply
// the path (queries like ?page= are near-duplicates and stay uncanonical).
const canonical = computed(() => `${siteUrl}${route.path}`)

useHead({
  htmlAttrs: { lang: locale },
  titleTemplate: (title?: string | null) =>
    title ? `${title} — ${t('site.title')}` : t('seo.home.title'),
  link: [{ rel: 'canonical', href: canonical }],
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
