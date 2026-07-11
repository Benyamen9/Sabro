<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()

useSeoMeta({
  title: () => t('seo.library.title'),
  description: () => t('seo.library.description'),
  ogTitle: () => t('seo.library.title'),
  ogDescription: () => t('seo.library.description'),
})

// One library, two views: the dictionary (every published word) and the words
// Meltho has shown. The active view is carried in the URL (?view=meltho) so
// both surfaces stay linkable; each view owns the rest of its query string.
const activeView = computed<'dictionary' | 'meltho'>(() =>
  route.query.view === 'meltho' ? 'meltho' : 'dictionary',
)

const tabs = [
  { key: 'dictionary' as const, to: { query: {} }, labelKey: 'library.tabs.dictionary' },
  { key: 'meltho' as const, to: { query: { view: 'meltho' } }, labelKey: 'library.tabs.meltho' },
]

// The dictionary view reports its unfiltered total for the living lede.
const dictionaryTotal = ref<number | null>(null)
</script>

<template>
  <section class="mx-auto max-w-4xl">
    <header class="mb-6 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('library.title') }}
      </p>
      <h1 class="mt-3 font-serif text-4xl font-semibold tracking-[-0.02em] sm:text-[2.75rem]">{{ t('library.heading') }}</h1>
      <p class="mt-3 max-w-2xl font-serif text-[17px] text-[var(--color-text-muted)]">
        <template v-if="activeView === 'dictionary'">
          <template v-if="dictionaryTotal">
            <strong class="font-semibold text-[var(--color-text)]">{{ t('library.count', { count: dictionaryTotal }) }}</strong>
            {{ t('library.dictionary.ledeCount') }}
          </template>
          <template v-else>{{ t('library.dictionary.lede') }}</template>
        </template>
        <template v-else>{{ t('library.lede') }}</template>
      </p>
    </header>

    <!-- The two views as underline tabs, in the header's own visual language. -->
    <nav
      class="mb-6 flex gap-1 border-b border-[var(--color-border)] font-sans text-sm"
      :aria-label="t('library.tabs.label')"
    >
      <NuxtLink
        v-for="tab in tabs"
        :key="tab.key"
        :to="tab.to"
        replace
        class="relative px-3 pb-2.5 pt-1 no-underline transition-colors"
        :class="activeView === tab.key
          ? 'font-medium text-[var(--color-text)] after:absolute after:inset-x-2 after:-bottom-px after:h-0.5 after:rounded-full after:bg-[var(--color-accent)]'
          : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'"
        :aria-current="activeView === tab.key ? 'page' : undefined"
      >{{ t(tab.labelKey) }}</NuxtLink>
    </nav>

    <LibraryDictionaryView
      v-if="activeView === 'dictionary'"
      @total="dictionaryTotal = $event"
    />
    <LibraryMelthoView v-else />
  </section>
</template>
