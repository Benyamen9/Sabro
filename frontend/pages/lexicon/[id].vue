<script setup lang="ts">
import type { FetchError } from 'ofetch'

const { t } = useI18n()
const route = useRoute()
const { getById } = useLexicon()

const id = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))

const { data: entry, pending, error } = await useAsyncData(
  () => `lexicon-entry-${id.value}`,
  () => getById(id.value),
  { watch: [id], lazy: true, default: () => null },
)

const status = computed<'loading' | 'unauthorized' | 'not-found' | 'failed' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    if (fetchError.statusCode === 404) return 'not-found'
    return 'failed'
  }
  if (!entry.value) return 'not-found'
  return 'ready'
})
</script>

<template>
  <section class="lexicon-detail">
    <p class="lexicon-detail__back">
      <NuxtLink to="/lexicon">← {{ t('lexicon.back') }}</NuxtLink>
    </p>

    <p v-if="status === 'loading'">…</p>
    <p v-else-if="status === 'unauthorized'">{{ t('lexicon.signInRequired') }}</p>
    <p v-else-if="status === 'not-found'">{{ t('lexicon.notFound') }}</p>
    <p v-else-if="status === 'failed'" class="lexicon-detail__error">
      {{ t('lexicon.loadFailed') }}
    </p>
    <article v-else-if="entry" class="lexicon-detail__entry">
      <header>
        <SyriacText :text="entry.syriacUnvocalized" class="lexicon-detail__headword" />
        <p v-if="entry.syriacVocalized" class="lexicon-detail__vocalized">
          <span class="lexicon-detail__label">{{ t('lexicon.fields.syriacVocalized') }}:</span>
          <SyriacText :text="entry.syriacVocalized" />
        </p>
      </header>

      <dl class="lexicon-detail__fields">
        <dt>{{ t('lexicon.fields.sblTransliteration') }}</dt>
        <dd>{{ entry.sblTransliteration }}</dd>

        <template v-if="entry.transliterationVariants.length > 0">
          <dt>{{ t('lexicon.fields.transliterationVariants') }}</dt>
          <dd>{{ entry.transliterationVariants.join(', ') }}</dd>
        </template>

        <dt>{{ t('lexicon.fields.grammaticalCategory') }}</dt>
        <dd>{{ t(`category.${entry.grammaticalCategory}`) }}</dd>

        <template v-if="entry.morphology">
          <dt>{{ t('lexicon.fields.morphology') }}</dt>
          <dd>{{ entry.morphology }}</dd>
        </template>

        <template v-if="entry.meanings.length > 0">
          <dt>{{ t('lexicon.fields.meanings') }}</dt>
          <dd>
            <ul class="lexicon-detail__meanings">
              <li v-for="(meaning, idx) in entry.meanings" :key="idx">
                <span class="lexicon-detail__meaning-lang">{{ meaning.language }}</span>
                <span class="lexicon-detail__meaning-text">{{ meaning.text }}</span>
              </li>
            </ul>
          </dd>
        </template>
      </dl>
    </article>
  </section>
</template>

<style scoped>
.lexicon-detail__back {
  margin-bottom: 1rem;
}

.lexicon-detail__back a {
  color: inherit;
  text-decoration: none;
}

.lexicon-detail__back a:hover {
  text-decoration: underline;
}

.lexicon-detail__headword {
  font-size: 2.5rem;
}

.lexicon-detail__vocalized {
  margin-top: 0.5rem;
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
}

.lexicon-detail__label {
  font-size: 0.875rem;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.lexicon-detail__fields {
  margin-top: 2rem;
  display: grid;
  grid-template-columns: 220px 1fr;
  gap: 0.75rem 1.5rem;
}

.lexicon-detail__fields dt {
  font-weight: 600;
  color: #444;
}

.lexicon-detail__fields dd {
  margin: 0;
}

.lexicon-detail__meanings {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.lexicon-detail__meanings li {
  display: flex;
  gap: 0.75rem;
  align-items: baseline;
}

.lexicon-detail__meaning-lang {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #888;
  min-width: 2rem;
}

.lexicon-detail__error {
  color: #b00020;
}
</style>
