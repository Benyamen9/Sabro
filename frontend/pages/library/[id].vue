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
      <header class="mt-6 flex flex-wrap items-baseline gap-x-5 gap-y-2">
        <SyriacText :text="data.syriacUnvocalized" class="!text-5xl text-[var(--color-accent)]" />
        <p class="font-serif text-xl text-[var(--color-text-muted)]">{{ preferredMeaning(data.meanings) }}</p>
      </header>

      <!-- Info table -->
      <dl class="mt-8 grid grid-cols-[max-content_1fr] gap-x-6 gap-y-3 font-sans text-sm">
        <dt class="text-[var(--color-text-faint)]">{{ t('library.fields.unvocalized') }}</dt>
        <dd><SyriacText :text="data.syriacUnvocalized" class="!text-xl" /></dd>

        <template v-if="data.syriacVocalized">
          <dt class="text-[var(--color-text-faint)]">{{ t('library.fields.vocalized') }}</dt>
          <dd><SyriacText :text="data.syriacVocalized" class="!text-xl" /></dd>
        </template>

        <template v-if="data.sblTransliteration">
          <dt class="text-[var(--color-text-faint)]">{{ t('library.fields.transliteration') }}</dt>
          <dd class="italic">{{ data.sblTransliteration }}</dd>
        </template>

        <dt class="text-[var(--color-text-faint)]">{{ t('library.fields.category') }}</dt>
        <dd>{{ categoryLabel(data.grammaticalCategory) }}</dd>

        <dt class="text-[var(--color-text-faint)]">{{ t('library.fields.length') }}</dt>
        <dd>{{ data.playableLength }}</dd>

        <dt class="text-[var(--color-text-faint)]">{{ t('library.fields.meanings') }}</dt>
        <dd>
          <span v-for="m in data.meanings" :key="m.language" class="mr-3 inline-block">
            <span class="text-[var(--color-text-faint)] uppercase">{{ m.language }}</span> {{ m.text }}
          </span>
        </dd>

        <dt class="text-[var(--color-text-faint)]">{{ t('library.playedOn') }}</dt>
        <dd>{{ data.playedOn.join(', ') }}</dd>
      </dl>

      <!-- Composition -->
      <section class="mt-12">
        <h2 class="font-serif text-2xl">{{ t('library.composition.heading') }}</h2>
        <div v-if="hasComposition" class="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <!-- Cards read right-to-left to match the Syriac word order. -->
          <LetterCard
            v-for="(letter, index) in [...data.composition].reverse()"
            :key="index"
            :letter="letter"
          />
        </div>
        <p v-else class="mt-4 font-serif text-[var(--color-text-muted)]">
          {{ t('library.composition.none') }}
        </p>
      </section>
    </template>
  </section>
</template>
