<script setup lang="ts">
import type { CreateLexiconEntryRequest } from '~/types/api'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { canEdit, refresh: refreshAccess } = useMyAccess()
const { create } = useLexiconAdmin()
const router = useRouter()

await refreshAdmin()
await refreshAccess()

// Creating an entry is an editor action. A reviewer corrects what exists; they
// do not add to the Lexicon.
const mayEdit = computed(() => canEdit('Lexicon'))

const submitting = ref(false)
const errorMessage = ref<string | null>(null)
const fieldErrors = ref<Record<string, string[]> | null>(null)

async function onSubmit(payload: CreateLexiconEntryRequest) {
  submitting.value = true
  errorMessage.value = null
  fieldErrors.value = null
  try {
    const entry = await create(payload)
    await router.push(`/admin/lexicon/${entry.id}`)
  }
  catch (error) {
    // Same as the edit page: a refusal, named fields, or neither.
    const failure = classifySaveFailure(error)
    fieldErrors.value = failure.kind === 'fields' ? failure.fields : null
    errorMessage.value = t(
      failure.kind === 'forbidden'
        ? 'admin.lexicon.saveForbidden'
        : failure.kind === 'fields'
          ? 'admin.lexicon.saveFailedFields'
          : 'admin.lexicon.saveFailed')
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <AdminSectionNav />

    <NuxtLink
      to="/admin/lexicon"
      class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]"
    >← {{ t('admin.lexicon.backToList') }}</NuxtLink>

    <StateMessage
      v-if="isAdmin === null"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="isAdmin === false || !mayEdit"
      variant="unauthorized"
      :message="t('admin.adminRequired')"
      :hint="t('admin.adminRequiredHint')"
    />
    <template v-else>
      <p class="mt-4 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="mb-6 mt-2 font-serif text-3xl font-semibold tracking-[-0.015em]">{{ t('admin.lexicon.form.createTitle') }}</h1>

      <p
        v-if="errorMessage"
        class="mb-5 rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
      >{{ errorMessage }}</p>

      <LexiconEntryForm
        :submitting="submitting"
        :field-errors="fieldErrors"
        :submit-label="t('admin.lexicon.actions.create')"
        @submit="onSubmit"
        @cancel="router.push('/admin/lexicon')"
      />
    </template>
  </section>
</template>
