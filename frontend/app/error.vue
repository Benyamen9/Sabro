<script setup lang="ts">
import type { NuxtError } from '#app'
import type { DictionaryWord } from '~/composables/useDictionary'

/**
 * The error page — in practice almost always a 404.
 *
 * Rather than a dead end, a missing page hands over a real entry from the
 * library: this is a lexicon, so the most useful thing it can offer someone who
 * landed nowhere is a word that does exist, linked to its own page.
 *
 * Nuxt renders this outside the normal page tree, so the layout is applied
 * explicitly — without it the error page loses the header, footer and switchers
 * and stops looking like the site at all.
 */
const props = defineProps<{ error: NuxtError }>()

const { t, locale } = useI18n()
const { listWords } = useDictionary()
const preferredMeaning = usePreferredMeaning()
const { fontFamily } = useScriptVariant()

const isNotFound = computed(() => props.error?.statusCode === 404)

/**
 * A random published word.
 *
 * Deliberately best-effort: an error page that throws is a far worse failure
 * than one without a word, so any API trouble resolves to null and the card is
 * simply omitted. `useAsyncData` (not `$fetch`) so the pick happens once during
 * SSR rather than changing between server and client render.
 *
 * The pool is only the published entries — a few dozen today, all ordinary
 * nouns — so the word is a small surprise rather than a comment on being lost.
 * It widens by itself as more of the lexicon is published.
 */
const { data: word } = await useAsyncData<DictionaryWord | null>(
  'error-word',
  async () => {
    if (!isNotFound.value) return null
    try {
      const first = await listWords({ page: 1, pageSize: 1 })
      if (first.total === 0) return null
      const page = Math.floor(Math.random() * first.total) + 1
      const picked = await listWords({ page, pageSize: 1 })
      return picked.items[0] ?? null
    }
    catch {
      return null
    }
  },
  { default: () => null },
)

const gloss = computed(() => (word.value ? preferredMeaning(word.value.meanings) : ''))

// The hub styles buttons with utility strings rather than a .btn class, so these
// mirror the home page's pair exactly — a shared class would be the better fix,
// but inventing one here would leave two conventions in the codebase.
const primaryButton
  = 'inline-flex items-center gap-2 rounded-xl bg-[var(--color-accent)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(140_47_57/0.25)] transition-all hover:-translate-y-px hover:bg-[var(--color-accent-hover)] hover:shadow-[0_5px_14px_rgb(140_47_57/0.3)]'

const secondaryButton
  = 'inline-flex items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-5 py-3 font-sans text-sm font-semibold text-[var(--color-text)] no-underline transition-colors hover:border-[var(--color-accent)]'

// Errors must not be indexed, and a 404 should not advertise a title that
// reads like real content.
useHead({
  title: () => (isNotFound.value ? t('error.notFound.title') : t('error.generic.title')),
  meta: [{ name: 'robots', content: 'noindex' }],
  htmlAttrs: { lang: locale },
})
</script>

<template>
  <NuxtLayout>
    <div class="mx-auto flex max-w-xl flex-col items-center py-10 text-center sm:py-16">
      <p class="font-sans text-[11px] font-bold uppercase tracking-[0.14em] text-[var(--color-text-faint)]">
        {{ error?.statusCode ?? 404 }}
      </p>

      <h1 class="mt-2 font-serif text-3xl font-bold tracking-tight sm:text-4xl">
        {{ isNotFound ? t('error.notFound.title') : t('error.generic.title') }}
      </h1>

      <p class="mt-3 max-w-[44ch] font-sans text-sm leading-relaxed text-[var(--color-text-muted)]">
        {{ word ? t('error.notFound.butThisWord') : (isNotFound ? t('error.notFound.body') : t('error.generic.body')) }}
      </p>

      <!-- The word only appears when one was actually resolved. -->
      <NuxtLink
        v-if="word"
        :to="`/library/${word.id}`"
        class="mt-6 block w-full max-w-xs rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-5 shadow-sm transition-colors hover:border-[var(--color-accent)]"
      >
        <span class="font-sans text-[9.5px] font-bold uppercase tracking-[0.12em] text-[var(--color-text-faint)]">
          {{ t('error.notFound.fromLibrary') }}
        </span>
        <span
          dir="rtl"
          lang="syr"
          class="mt-1.5 block text-[40px] leading-[1.5]"
          :style="{ fontFamily }"
        >{{ word.syriacVocalized || word.syriacUnvocalized }}</span>
        <span v-if="word.sblTransliteration" class="block font-serif text-[15px] italic text-[var(--color-accent)]">
          {{ word.sblTransliteration }}
        </span>
        <span v-if="gloss" class="mt-0.5 block font-sans text-[13px] text-[var(--color-text-muted)]">
          {{ gloss }}
        </span>
      </NuxtLink>

      <div class="mt-7 flex flex-wrap justify-center gap-2.5">
        <NuxtLink to="/library" :class="primaryButton">{{ t('error.actions.library') }}</NuxtLink>
        <NuxtLink to="/" :class="secondaryButton">{{ t('error.actions.home') }}</NuxtLink>
      </div>
    </div>
  </NuxtLayout>
</template>
