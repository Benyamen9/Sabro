<script setup lang="ts">
import type { CreateHistoricalFigureRequest } from '~/types/api'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { create } = useHistoricalFiguresAdmin()
const router = useRouter()

await refreshAdmin()

const submitting = ref(false)
const errorMessage = ref<string | null>(null)

async function onSubmit(payload: CreateHistoricalFigureRequest) {
  submitting.value = true
  errorMessage.value = null
  try {
    const figure = await create(payload)
    await router.push(`/admin/historical-figures/${figure.id}`)
  }
  catch {
    errorMessage.value = t('admin.historicalFigures.saveFailed')
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <NuxtLink
      to="/admin/historical-figures"
      class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]"
    >← {{ t('admin.historicalFigures.backToList') }}</NuxtLink>

    <StateMessage
      v-if="isAdmin === null"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="isAdmin === false"
      variant="unauthorized"
      :message="t('admin.adminRequired')"
      :hint="t('admin.adminRequiredHint')"
    />
    <template v-else>
      <p class="mt-4 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="mb-6 mt-2 font-serif text-3xl font-semibold tracking-[-0.015em]">
        {{ t('admin.historicalFigures.form.createTitle') }}
      </h1>

      <p
        v-if="errorMessage"
        class="mb-5 rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
      >{{ errorMessage }}</p>

      <HistoricalFigureForm
        :submitting="submitting"
        :submit-label="t('admin.historicalFigures.actions.create')"
        @submit="onSubmit"
        @cancel="router.push('/admin/historical-figures')"
      />
    </template>
  </section>
</template>
