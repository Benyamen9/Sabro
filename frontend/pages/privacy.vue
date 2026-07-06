<script setup lang="ts">
const { t } = useI18n()

useHead({ title: () => t('privacy.title') })
useSeoMeta({
  description: () => t('privacy.metaDescription'),
  ogTitle: () => t('privacy.title'),
  ogDescription: () => t('privacy.metaDescription'),
})

// Section keys drive the render so the three locales stay in lockstep: every
// section is a title + a list of body paragraphs (b1..bN present per section).
const sections = [
  { key: 'who', items: ['b1'] },
  { key: 'data', items: ['b1', 'b2', 'b3', 'b4', 'b5'] },
  { key: 'cookies', items: ['b1'] },
  { key: 'third', items: ['b1', 'b2'] },
  { key: 'storage', items: ['b1'] },
  { key: 'rights', items: ['b1', 'b2', 'b3'] },
  { key: 'contact', items: ['b1'] },
] as const
</script>

<template>
  <main class="mx-auto max-w-2xl px-4 py-10 sm:px-6 sm:py-14">
    <h1 class="font-sans text-3xl font-semibold tracking-tight">
      {{ t('privacy.title') }}
    </h1>
    <p class="mt-2 font-sans text-sm text-[var(--color-text-muted)]">
      {{ t('privacy.updated') }}
    </p>

    <p class="mt-6 font-serif text-[17px] leading-relaxed text-[var(--color-text)]">
      {{ t('privacy.intro') }}
    </p>

    <section v-for="section in sections" :key="section.key" class="mt-8">
      <h2 class="font-sans text-lg font-semibold tracking-tight">
        {{ t(`privacy.${section.key}.title`) }}
      </h2>
      <p
        v-for="item in section.items"
        :key="item"
        class="mt-3 font-serif text-[16px] leading-relaxed text-[var(--color-text-muted)]"
      >
        {{ t(`privacy.${section.key}.${item}`) }}
      </p>
    </section>
  </main>
</template>
