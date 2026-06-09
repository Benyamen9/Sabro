<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { CreateLexiconEntryRequest } from '~/types/api'

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { getById, update, remove, publish, unpublish, setPlayable } = useLexiconAdmin()
const route = useRoute()
const router = useRouter()

const id = computed(() => route.params.id as string)

await refreshAdmin()

const { data: entry, pending, error, refresh } = await useAsyncData(
  () => `admin-lexicon-${id.value}`,
  () => getById(id.value),
  { lazy: true, default: () => null, immediate: isAdmin.value === true },
)

const submitting = ref(false)
const acting = ref(false)
const errorMessage = ref<string | null>(null)

const status = computed<'loading' | 'unauthorized' | 'failed' | 'notFound' | 'ready'>(() => {
  if (isAdmin.value === false) return 'unauthorized'
  if (isAdmin.value === null || pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    if (fetchError.statusCode === 404) return 'notFound'
    return 'failed'
  }
  if (!entry.value) return 'notFound'
  return 'ready'
})

const isPublished = computed(() => entry.value?.status === 'Published')
const lengthInRange = computed(() => {
  const length = Number(entry.value?.playableLength ?? 0)
  return length >= 2 && length <= 8
})
const canTogglePool = computed(() => isPublished.value && lengthInRange.value)

async function onSubmit(payload: CreateLexiconEntryRequest) {
  if (!entry.value) return
  submitting.value = true
  errorMessage.value = null
  try {
    await update(entry.value.id, payload)
    await refresh()
  }
  catch {
    errorMessage.value = t('admin.lexicon.saveFailed')
  }
  finally {
    submitting.value = false
  }
}

async function runAction(action: () => Promise<unknown>, failKey: string) {
  acting.value = true
  errorMessage.value = null
  try {
    await action()
    await refresh()
  }
  catch {
    errorMessage.value = t(failKey)
  }
  finally {
    acting.value = false
  }
}

function onPublish() {
  if (!entry.value) return
  runAction(() => publish(entry.value!.id), 'admin.lexicon.actionFailed')
}

function onUnpublish() {
  if (!entry.value) return
  runAction(() => unpublish(entry.value!.id), 'admin.lexicon.actionFailed')
}

function onTogglePlayable() {
  if (!entry.value) return
  const next = !entry.value.playableInMeltha
  runAction(() => setPlayable(entry.value!.id, next), 'admin.lexicon.actionFailed')
}

async function onDelete() {
  if (!entry.value) return
  if (!window.confirm(t('admin.lexicon.deleteConfirm'))) return
  acting.value = true
  errorMessage.value = null
  try {
    await remove(entry.value.id)
    await router.push('/admin/lexicon')
  }
  catch {
    errorMessage.value = t('admin.lexicon.deleteFailed')
    acting.value = false
  }
}

const actionButtonClass
  = 'inline-flex items-center rounded-md border border-[var(--color-border-strong)] px-4 py-2 font-sans text-sm font-medium transition-colors hover:bg-[var(--color-bg-subtle)] disabled:cursor-not-allowed disabled:opacity-50'
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <NuxtLink
      to="/admin/lexicon"
      class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]"
    >← {{ t('admin.lexicon.backToList') }}</NuxtLink>

    <StateMessage
      v-if="status === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="status === 'unauthorized'"
      variant="unauthorized"
      :message="t('admin.adminRequired')"
      :hint="t('admin.adminRequiredHint')"
    />
    <StateMessage
      v-else-if="status === 'notFound'"
      variant="notFound"
      :message="t('admin.lexicon.notFound')"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('admin.lexicon.loadEntryFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <template v-else-if="entry">
      <h1 class="mb-6 mt-4 font-serif text-3xl">{{ t('admin.lexicon.form.editTitle') }}</h1>

      <p
        v-if="errorMessage"
        class="mb-5 rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
      >{{ errorMessage }}</p>

      <LexiconEntryForm
        :key="entry.updatedAt"
        :entry="entry"
        :submitting="submitting"
        :submit-label="t('common.save')"
        @submit="onSubmit"
        @cancel="router.push('/admin/lexicon')"
      />

      <!-- Lifecycle -->
      <div class="mt-10 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-subtle)] p-6">
        <h2 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.lexicon.lifecycle.heading') }}
        </h2>

        <div class="mt-4 flex flex-wrap items-center gap-3">
          <span
            class="inline-flex items-center rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
            :class="
              isPublished
                ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                : 'bg-[var(--color-bg-elevated)] text-[var(--color-text-muted)] ring-1 ring-[var(--color-border-strong)]'
            "
          >{{ t(`admin.lexicon.status.${entry.status}`) }}</span>

          <button
            v-if="!isPublished"
            type="button"
            :disabled="acting"
            :class="actionButtonClass"
            @click="onPublish"
          >{{ t('admin.lexicon.actions.publish') }}</button>
          <button
            v-else
            type="button"
            :disabled="acting"
            :class="actionButtonClass"
            @click="onUnpublish"
          >{{ t('admin.lexicon.actions.unpublish') }}</button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ t('admin.lexicon.lifecycle.publishHint') }}
        </p>

        <hr class="my-5 border-t border-[var(--color-border)]">

        <h3 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.lexicon.lifecycle.poolHeading') }}
        </h3>
        <div class="mt-3 flex flex-wrap items-center gap-3">
          <span class="font-sans text-sm text-[var(--color-text-muted)]">
            {{ entry.playableInMeltha ? t('admin.lexicon.lifecycle.inPool') : t('admin.lexicon.lifecycle.notInPool') }}
          </span>
          <button
            type="button"
            :disabled="acting || !canTogglePool"
            :class="actionButtonClass"
            @click="onTogglePlayable"
          >
            {{ entry.playableInMeltha ? t('admin.lexicon.actions.removeFromPool') : t('admin.lexicon.actions.addToPool') }}
          </button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ t('admin.lexicon.lifecycle.poolHint') }}
        </p>

        <hr class="my-5 border-t border-[var(--color-border)]">

        <button
          type="button"
          :disabled="acting"
          class="inline-flex items-center rounded-md border border-[color-mix(in_oklab,var(--color-accent)_40%,transparent)] px-4 py-2 font-sans text-sm font-medium text-[var(--color-accent)] transition-colors hover:bg-[var(--color-accent-faint)] disabled:cursor-not-allowed disabled:opacity-50"
          @click="onDelete"
        >{{ t('common.delete') }}</button>
      </div>
    </template>
  </section>
</template>
