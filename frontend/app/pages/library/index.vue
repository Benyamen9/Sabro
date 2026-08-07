<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()
const router = useRouter()

useSeoMeta({
  title: () => t('seo.library.title'),
  description: () => t('seo.library.description'),
  ogTitle: () => t('seo.library.title'),
  ogDescription: () => t('seo.library.description'),
})

// Remembers the last-chosen filter across visits, same pattern as sabro_locale/sabro_script_variant.
// The URL always wins when it carries an explicit ?view=meltho (e.g. the cross-link from a word's
// detail page), the cookie wins over the hardcoded "off" default on a fresh visit with no query.
const cookie = useCookie<boolean>('sabro_library_meltho_filter', {
  default: () => false,
  maxAge: 60 * 60 * 24 * 365,
  sameSite: 'lax',
})

const meltho = ref(route.query.view === 'meltho' ? true : cookie.value)

watch(meltho, (value) => {
  cookie.value = value
  router.replace({ query: value ? { view: 'meltho' } : {} })
})

// Retints every `--color-accent` reference inside (cards, sort pills, the switch itself) to
// Meltho's established teal — same tokens the word-detail page's cross-link card already uses —
// by overriding the custom property locally rather than threading conditional classes through
// every child.
const retintStyle = computed(() => meltho.value
  ? {
      '--color-accent': 'var(--color-meltho)',
      '--color-accent-hover': 'var(--color-meltho-dark)',
      '--color-accent-faint': 'var(--color-meltho-faint)',
    }
  : undefined)

// The dictionary state reports its unfiltered total for the living lede count; the Meltho state's
// lede is static text, unchanged from before the merge.
const dictionaryTotal = ref<number | null>(null)
</script>

<template>
  <section class="mx-auto max-w-4xl" :style="retintStyle">
    <header class="mb-6 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('library.title') }}
      </p>
      <h1 class="mt-3 font-serif text-4xl font-semibold tracking-[-0.02em] sm:text-[2.75rem]">{{ t('library.heading') }}</h1>
      <p class="mt-3 max-w-2xl font-serif text-[17px] text-[var(--color-text-muted)]">
        <template v-if="!meltho">
          <template v-if="dictionaryTotal">
            <strong class="font-semibold text-[var(--color-text)]">{{ t('library.count', { count: dictionaryTotal }) }}</strong>
            {{ t('library.dictionary.ledeCount') }}
          </template>
          <template v-else>{{ t('library.dictionary.lede') }}</template>
        </template>
        <template v-else>{{ t('library.lede') }}</template>
      </p>
    </header>

    <button
      type="button"
      role="switch"
      :aria-checked="meltho"
      :aria-label="t('library.toggle.label')"
      class="mb-6 inline-flex items-center gap-3 rounded-full border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] py-1.5 pl-1.5 pr-4 font-sans text-sm transition-colors hover:border-[var(--color-accent)]"
      @click="meltho = !meltho"
    >
      <span
        class="relative h-6 w-11 shrink-0 rounded-full transition-colors"
        :class="meltho ? 'bg-[var(--color-accent)]' : 'bg-[var(--color-border-strong)]'"
      >
        <span
          class="absolute top-0.5 left-0.5 size-5 rounded-full bg-white shadow-[var(--shadow-soft)] transition-transform"
          :class="{ 'translate-x-[20px]': meltho }"
        />
      </span>
      <span class="font-medium text-[var(--color-text)]">{{ meltho ? t('library.tabs.meltho') : t('library.tabs.dictionary') }}</span>
    </button>

    <LibraryView :key="meltho ? 'meltho' : 'dictionary'" :meltho="meltho" @total="dictionaryTotal = $event" />
  </section>
</template>
