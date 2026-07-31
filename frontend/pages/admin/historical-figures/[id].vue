<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { CreateHistoricalFigureRequest } from '~/types/api'
import { formatEra } from '~/utils/historicalFigures'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { getById, update, remove, publish, unpublish, setPlayable } = useHistoricalFiguresAdmin()
const route = useRoute()
const router = useRouter()

const id = computed(() => route.params.id as string)

await refreshAdmin()

const { data: figure, pending, error, refresh } = await useAsyncData(
  () => `admin-historical-figure-${id.value}`,
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
  if (!figure.value) return 'notFound'
  return 'ready'
})

const isPublished = computed(() => figure.value?.status === 'Published')

// Publishing requires a tradition — including the explicit "not applicable",
// which is a real answer for pre-Christian and pre-Chalcedonian figures rather
// than a missing one. Surfaced here so the editor sees why publish is blocked.
const hasTradition = computed(() => figure.value?.tradition != null)
const canPublish = computed(() => !isPublished.value && hasTradition.value)

async function onSubmit(payload: CreateHistoricalFigureRequest) {
  if (!figure.value) return
  submitting.value = true
  errorMessage.value = null
  try {
    await update(figure.value.id, payload)
    await refresh()
  }
  catch {
    errorMessage.value = t('admin.historicalFigures.saveFailed')
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
  if (!figure.value) return
  runAction(() => publish(figure.value!.id), 'admin.historicalFigures.actionFailed')
}

function onUnpublish() {
  if (!figure.value) return
  runAction(() => unpublish(figure.value!.id), 'admin.historicalFigures.actionFailed')
}

function onTogglePlayable() {
  if (!figure.value) return
  const next = !figure.value.playableInShmo
  runAction(() => setPlayable(figure.value!.id, next), 'admin.historicalFigures.actionFailed')
}

async function onDelete() {
  if (!figure.value) return
  if (!window.confirm(t('admin.historicalFigures.deleteConfirm'))) return
  acting.value = true
  errorMessage.value = null
  try {
    await remove(figure.value.id)
    await router.push('/admin/historical-figures')
  }
  catch {
    errorMessage.value = t('admin.historicalFigures.deleteFailed')
    acting.value = false
  }
}

const actionButtonClass
  = 'inline-flex items-center rounded-md border border-[var(--color-border-strong)] px-4 py-2 font-sans text-sm font-medium transition-colors hover:bg-[var(--color-bg-subtle)] disabled:cursor-not-allowed disabled:opacity-50'
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <AdminSectionNav />

    <NuxtLink
      to="/admin/historical-figures"
      class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]"
    >← {{ t('admin.historicalFigures.backToList') }}</NuxtLink>

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
      :message="t('admin.historicalFigures.notFound')"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('admin.historicalFigures.loadFigureFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <template v-else-if="figure">
      <p class="mt-4 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="mb-1 mt-2 font-serif text-3xl font-semibold tracking-[-0.015em]">
        {{ figure.name }}
      </h1>
      <p class="mb-6 font-sans text-sm text-[var(--color-text-muted)]">
        {{ formatEra(Number(figure.era), t) }} · {{ t(`admin.historicalFigures.period.${figure.period}`) }} · {{ t(`admin.historicalFigures.category.${figure.category}`) }}
      </p>

      <p
        v-if="errorMessage"
        class="mb-5 rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
      >{{ errorMessage }}</p>

      <HistoricalFigureForm
        :key="figure.updatedAt"
        :figure="figure"
        :submitting="submitting"
        :submit-label="t('common.save')"
        @submit="onSubmit"
        @cancel="router.push('/admin/historical-figures')"
      />

      <!-- Lifecycle -->
      <div class="mt-10 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-subtle)] p-6">
        <h2 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.historicalFigures.lifecycle.heading') }}
        </h2>

        <div class="mt-4 flex flex-wrap items-center gap-3">
          <span
            class="inline-flex items-center rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
            :class="
              isPublished
                ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                : 'bg-[var(--color-bg-elevated)] text-[var(--color-text-muted)] ring-1 ring-[var(--color-border-strong)]'
            "
          >{{ t(`admin.historicalFigures.status.${figure.status}`) }}</span>

          <button
            v-if="!isPublished"
            type="button"
            :disabled="acting || !canPublish"
            :class="actionButtonClass"
            @click="onPublish"
          >{{ t('admin.historicalFigures.actions.publish') }}</button>
          <button
            v-else
            type="button"
            :disabled="acting"
            :class="actionButtonClass"
            @click="onUnpublish"
          >{{ t('admin.historicalFigures.actions.unpublish') }}</button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ !isPublished && !hasTradition
            ? t('admin.historicalFigures.lifecycle.traditionRequired')
            : t('admin.historicalFigures.lifecycle.publishHint') }}
        </p>

        <hr class="my-5 border-t border-[var(--color-border)]">

        <h3 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.historicalFigures.lifecycle.poolHeading') }}
        </h3>
        <div class="mt-3 flex flex-wrap items-center gap-3">
          <span class="font-sans text-sm text-[var(--color-text-muted)]">
            {{ figure.playableInShmo
              ? t('admin.historicalFigures.lifecycle.inPool')
              : t('admin.historicalFigures.lifecycle.notInPool') }}
          </span>
          <button
            type="button"
            :disabled="acting || !isPublished"
            :class="actionButtonClass"
            @click="onTogglePlayable"
          >
            {{ figure.playableInShmo
              ? t('admin.historicalFigures.actions.removeFromPool')
              : t('admin.historicalFigures.actions.addToPool') }}
          </button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ t('admin.historicalFigures.lifecycle.poolHint') }}
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
