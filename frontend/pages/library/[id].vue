<script setup lang="ts">
import type { FetchError } from 'ofetch'

const { t } = useI18n()
const route = useRoute()
const { getWord } = useMelthoLibrary()

const id = computed(() => route.params.id as string)

const { data, pending, error, refresh } = await useAsyncData(
  () => `meltho-library-${id.value}`,
  () => getWord(id.value),
  { watch: [id], lazy: true, default: () => null },
)

const status = computed<'loading' | 'notFound' | 'failed' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    return fetchError.statusCode === 404 ? 'notFound' : 'failed'
  }
  if (!data.value) return 'failed'
  return 'ready'
})

const hasComposition = computed(() => (data.value?.composition ?? []).length > 0)

// The form shown large in the hero: the vocalized spelling when authored, else the bare word.
const heroForm = computed(() => data.value?.syriacVocalized || data.value?.syriacUnvocalized || '')

// Word-specific title/description ("bartho · daughter — Sabro") for the
// long-tail search surface; app-level defaults hold while loading or on 404.
const preferredMeaning = usePreferredMeaning()
const seoWord = computed(() => {
  const word = data.value
  if (!word) return null
  const translit = word.sblTransliteration || word.syriacUnvocalized
  const meaning = preferredMeaning(word.meanings)
  return meaning ? { translit, meaning } : null
})
useSeoMeta({
  title: () => (seoWord.value ? t('seo.libraryWord.title', seoWord.value) : null),
  description: () => (seoWord.value ? t('seo.libraryWord.description', seoWord.value) : null),
  ogTitle: () => (seoWord.value ? t('seo.libraryWord.title', seoWord.value) : null),
  ogDescription: () => (seoWord.value ? t('seo.libraryWord.description', seoWord.value) : null),
})

const meaningLanguages = ['en', 'fr', 'nl'] as const
const orderedMeanings = computed(() => {
  const meanings = data.value?.meanings ?? []
  const known = meaningLanguages
    .map(lang => meanings.find(m => m.language === lang))
    .filter((m): m is NonNullable<typeof m> => Boolean(m))
  const extra = meanings.filter(m => !meaningLanguages.includes(m.language as typeof meaningLanguages[number]))
  return [...known, ...extra]
})

function categoryLabel(category: string | undefined) {
  if (!category) return ''
  const key = `categories.${category}`
  const label = t(key)
  return label === key ? category : label
}
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <NuxtLink to="/library" class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]">
      ← {{ t('library.back') }}
    </NuxtLink>

    <StateMessage
      v-if="status === 'loading'"
      variant="loading"
      :message="t('common.loading')"
      class="mt-6"
    />
    <StateMessage
      v-else-if="status === 'notFound'"
      variant="notFound"
      :message="t('library.notFound')"
      class="mt-6"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('library.loadFailed')"
      :action-label="t('common.retry')"
      class="mt-6"
      @action="refresh()"
    />
    <template v-else-if="data">
      <!-- The word as a dictionary entry: form, pronunciation, meta chips, and
           senses in one composed block — not a metadata grid. -->
      <article class="mt-5 overflow-hidden rounded-[20px] border border-[var(--color-border)] bg-[var(--color-bg-elevated)] shadow-[var(--shadow-soft)]">
        <div class="px-5 pb-7 pt-9 text-center sm:px-9">
          <SyriacText :text="heroForm" class="!text-[3.1rem] leading-[1.4] text-[var(--color-accent)] sm:!text-[4.25rem]" />
          <p v-if="data.sblTransliteration" class="mt-1.5 font-serif text-[21px] text-[var(--color-text-muted)] italic">
            {{ data.sblTransliteration }}
          </p>
          <div class="mt-4 flex flex-wrap items-center justify-center gap-2 font-sans text-[12.5px]">
            <span class="rounded-full border border-[var(--color-border)] bg-[var(--color-bg-subtle)] px-3 py-1 text-[var(--color-text-muted)]">
              {{ categoryLabel(data.grammaticalCategory) }}
            </span>
            <span class="rounded-full border border-[var(--color-border)] px-3 py-1 text-[var(--color-text-muted)]">
              {{ t('library.lettersCount', { count: data.playableLength }) }}
            </span>
            <span
              v-if="data.syriacVocalized"
              class="inline-flex items-center gap-1.5 rounded-full border border-[var(--color-border)] px-3 py-1 text-[var(--color-text-muted)]"
            >
              {{ t('library.fields.unvocalized') }}
              <SyriacText :text="data.syriacUnvocalized" class="!text-[17px] leading-none text-[var(--color-accent)]" />
            </span>
            <span
              v-if="data.root"
              class="inline-flex items-center gap-1.5 rounded-full border border-[var(--color-border)] px-3 py-1 text-[var(--color-text-muted)]"
            >
              {{ t('library.fields.root') }}
              <SyriacText :text="data.root" class="!text-[17px] leading-none text-[var(--color-accent)]" />
            </span>
          </div>
        </div>
        <!-- Senses: the meanings belong to the entry itself, one row per language. -->
        <div class="border-t border-[var(--color-border)]">
          <div
            v-for="m in orderedMeanings"
            :key="m.language"
            class="flex gap-4 border-t border-[var(--color-border)] px-6 py-3 first:border-t-0 sm:px-9"
          >
            <span class="w-7 shrink-0 pt-1 font-sans text-[11px] font-bold uppercase text-[var(--color-text-faint)]">{{ m.language }}</span>
            <span class="font-serif text-[16.5px]">{{ m.text }}</span>
          </div>
        </div>
      </article>

      <!-- Letter by letter: the page's centrepiece — one right-to-left row of
           cards, read in the same direction as the word itself. -->
      <section class="mt-11">
        <div class="flex flex-wrap items-baseline gap-x-3.5 gap-y-1">
          <h2 class="font-serif text-[26px] font-semibold tracking-[-0.01em]">{{ t('library.composition.heading') }}</h2>
          <span v-if="hasComposition" class="font-sans text-[12.5px] text-[var(--color-text-faint)]">
            {{ t('library.composition.hint') }}
          </span>
        </div>
        <template v-if="hasComposition">
          <!-- Phones cap the row at 3 columns so the cards keep readable width;
               wrapped rows still read right-to-left, row by row. -->
          <div
            class="composition-grid mt-4 grid gap-2.5"
            dir="rtl"
            :style="{
              '--cols-base': Math.min(data.composition.length, 3),
              '--cols-sm': Math.min(data.composition.length, 5),
            }"
          >
            <LetterCard
              v-for="(letter, index) in data.composition"
              :key="index"
              :letter="letter"
              dir="ltr"
            />
          </div>
        </template>
        <p v-else class="mt-4 font-serif text-[var(--color-text-muted)]">
          {{ t('library.composition.none') }}
        </p>
      </section>
    </template>
  </section>
</template>

<style scoped>
/* The letter row: one right-to-left line of up to five cards on larger
   screens; phones wrap at three so each card keeps readable width. */
.composition-grid {
  grid-template-columns: repeat(var(--cols-base), minmax(0, 1fr));
}

@media (min-width: 640px) {
  .composition-grid {
    grid-template-columns: repeat(var(--cols-sm), minmax(0, 1fr));
  }
}
</style>
