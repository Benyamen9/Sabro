<script setup lang="ts">
import type { CreateChantRequest } from '~/types/api'

// Editorial surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { canEdit, refresh: refreshAccess } = useMyAccess()
const { create, listModes, listAll } = useChantsAdmin()
const router = useRouter()

await refreshAdmin()
await refreshAccess()

// Adding a chant is an editor action. A reviewer reads the treasury; they do not
// add to it.
const mayEdit = computed(() => canEdit('Nahlo'))

const { data: modes } = await useAsyncData(
  'admin-chant-modes',
  () => listModes(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

// The whole treasury, for the solqin picker — a chant that borrows a melody
// points at one specific other chant, so any missing from the list cannot be
// chosen at all.
const { data: melodySources } = await useAsyncData(
  'admin-chant-melody-sources',
  () => listAll(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

const submitting = ref(false)
const errorMessage = ref<string | null>(null)
const fieldErrors = ref<Record<string, string[]> | null>(null)

async function onSubmit(payload: CreateChantRequest) {
  submitting.value = true
  errorMessage.value = null
  fieldErrors.value = null
  try {
    const chant = await create(payload)
    await router.push(`/admin/chants/${chant.id}`)
  }
  catch (error) {
    errorMessage.value = t(chantSaveErrorKey(error))
    const failure = classifySaveFailure(error)
    fieldErrors.value = failure.kind === 'fields' ? failure.fields : null
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <AdminBreadcrumb
      section-key="admin.sections.chants.label"
      section-to="/admin/chants"
      :current="t('admin.chants.newChant')"
    />
    <AdminSectionNav />

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
      <h1 class="mb-6 mt-2 font-serif text-3xl font-semibold tracking-[-0.015em]">
        {{ t('admin.chants.form.createTitle') }}
      </h1>

      <p
        v-if="errorMessage"
        class="mb-5 rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
        role="alert"
      >{{ errorMessage }}</p>

      <!-- The recording is uploaded once the chant exists, and publishing waits
           on it. Said here so a new chant staying Draft is expected rather than
           puzzling. -->
      <p class="mb-5 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] px-4 py-3 font-sans text-sm text-[var(--color-text)]">
        {{ t('admin.chants.form.recordingComesLater') }}
      </p>

      <ChantForm
        :modes="modes"
        :melody-sources="melodySources"
        :submitting="submitting"
        :field-errors="fieldErrors"
        :submit-label="t('admin.chants.actions.create')"
        @submit="onSubmit"
        @cancel="router.push('/admin/chants')"
      />
    </template>
  </section>
</template>
