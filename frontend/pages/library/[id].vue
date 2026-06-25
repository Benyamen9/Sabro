<script setup lang="ts">
import type { FetchError } from 'ofetch'

const { t } = useI18n()
const route = useRoute()
const { getWord } = useMelthoLibrary()
const preferredMeaning = usePreferredMeaning()

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
      <!-- Hero: the word, its transliteration, gloss, and (when known) its root. -->
      <header class="mt-6 flex flex-wrap items-baseline gap-x-5 gap-y-2">
        <SyriacText :text="heroForm" class="!text-5xl text-[var(--color-accent)]" />
        <p v-if="data.sblTransliteration" class="font-serif text-xl text-[var(--color-text-muted)] italic">
          {{ data.sblTransliteration }}
        </p>
        <p class="font-serif text-xl">{{ preferredMeaning(data.meanings) }}</p>
        <span
          v-if="data.root"
          class="ml-auto inline-flex items-center gap-2 rounded-full border border-[var(--color-border)] bg-[var(--color-bg-subtle)] px-3 py-1 font-sans text-xs text-[var(--color-text-muted)]"
        >
          {{ t('library.fields.root') }}
          <SyriacText :text="data.root" class="!text-lg text-[var(--color-accent)]" />
        </span>
      </header>

      <!-- Info cards -->
      <dl class="mt-8 grid grid-cols-2 gap-3 sm:grid-cols-3">
        <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4">
          <dt class="font-sans text-[0.7rem] uppercase tracking-wide text-[var(--color-text-faint)]">{{ t('library.fields.unvocalized') }}</dt>
          <dd class="mt-1"><SyriacText :text="data.syriacUnvocalized" class="!text-2xl text-[var(--color-accent)]" /></dd>
        </div>
        <div v-if="data.syriacVocalized" class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4">
          <dt class="font-sans text-[0.7rem] uppercase tracking-wide text-[var(--color-text-faint)]">{{ t('library.fields.vocalized') }}</dt>
          <dd class="mt-1"><SyriacText :text="data.syriacVocalized" class="!text-2xl text-[var(--color-accent)]" /></dd>
        </div>
        <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4">
          <dt class="font-sans text-[0.7rem] uppercase tracking-wide text-[var(--color-text-faint)]">{{ t('library.fields.category') }}</dt>
          <dd class="mt-1 font-serif text-lg">{{ categoryLabel(data.grammaticalCategory) }}</dd>
        </div>
        <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4">
          <dt class="font-sans text-[0.7rem] uppercase tracking-wide text-[var(--color-text-faint)]">{{ t('library.fields.length') }}</dt>
          <dd class="mt-1 font-serif text-lg">{{ data.playableLength }}</dd>
        </div>
        <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4">
          <dt class="font-sans text-[0.7rem] uppercase tracking-wide text-[var(--color-text-faint)]">{{ t('library.playedOn') }}</dt>
          <dd class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">{{ data.playedOn.join(' · ') }}</dd>
        </div>
      </dl>

      <!-- Meanings, one row per language -->
      <div class="mt-4 overflow-hidden rounded-xl border border-[var(--color-border)]">
        <div
          v-for="m in orderedMeanings"
          :key="m.language"
          class="flex gap-4 border-t border-[var(--color-border)] px-4 py-3 first:border-t-0"
        >
          <span class="w-7 shrink-0 font-sans text-xs font-semibold uppercase text-[var(--color-text-faint)]">{{ m.language }}</span>
          <span class="font-serif">{{ m.text }}</span>
        </div>
      </div>

      <!-- Composition -->
      <section class="mt-12">
        <h2 class="font-serif text-2xl">{{ t('library.composition.heading') }}</h2>
        <template v-if="hasComposition">
          <!-- Connected strip: the word as it reads, with soft (rukkokho) letters tinted. RTL so
               it matches the Syriac reading direction. -->
          <div class="mt-5 flex flex-wrap justify-center gap-1" dir="rtl">
            <span
              v-for="(letter, index) in data.composition"
              :key="`strip-${index}`"
              class="rounded-lg px-2 py-1 font-syriac text-3xl"
              :class="letter.hardening === 'Rukkokho'
                ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                : 'text-[var(--color-text)]'"
            >{{ letter.letter }}</span>
          </div>
          <!-- Per-letter cards, left-to-right in word order (Kaph, Taw, Beth, Alaph for ktobo). -->
          <div class="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <LetterCard
              v-for="(letter, index) in data.composition"
              :key="index"
              :letter="letter"
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
