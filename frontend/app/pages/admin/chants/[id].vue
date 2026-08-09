<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { CreateChantRequest } from '~/types/api'

// Editorial surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const {
  getById,
  listModes,
  listSections,
  listAll,
  update,
  remove,
  publish,
  unpublish,
  setPlayable,
  uploadAudio,
  removeAudio,
} = useChantsAdmin()
const mediaUrl = useMediaUrl()
const route = useRoute()
const router = useRouter()

const id = computed(() => route.params.id as string)

await refreshAdmin()

const { canEdit, canViewBackoffice, refresh: refreshAccess } = useMyAccess()
await refreshAccess()

// A reviewer may read this page but not save it. Purely about what to offer —
// the API applies the same rules and refuses on its own, so a wrong answer here
// hides a control rather than granting one.
const mayEdit = computed(() => canEdit('Nahlo'))
const mayView = computed(() => canViewBackoffice('Nahlo'))

const { data: chant, pending, error, refresh } = await useAsyncData(
  () => `admin-chant-${id.value}`,
  () => getById(id.value),
  { lazy: true, default: () => null, immediate: isAdmin.value === true },
)

const { data: modes } = await useAsyncData(
  'admin-chant-modes',
  () => listModes(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

// The sections, and with them which modes each admits — the form needs this to
// know whether to ask for a mode at all.
const { data: sections } = await useAsyncData(
  'admin-chant-sections-detail',
  () => listSections(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

const { data: melodySources } = await useAsyncData(
  'admin-chant-melody-sources',
  () => listAll(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

const submitting = ref(false)
const acting = ref(false)
const errorMessage = ref<string | null>(null)
const fieldErrors = ref<Record<string, string[]> | null>(null)

const status = computed<'loading' | 'unauthorized' | 'failed' | 'notFound' | 'ready'>(() => {
  // Both locks, in the order they are checked server-side: the admin scope says
  // whether this person is staff at all, the Nahlo grant says whether this is
  // one of their areas.
  if (isAdmin.value === false || !mayView.value) return 'unauthorized'
  if (isAdmin.value === null || pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    if (fetchError.statusCode === 404) return 'notFound'
    return 'failed'
  }
  if (!chant.value) return 'notFound'
  return 'ready'
})

const isPublished = computed(() => chant.value?.status === 'Published')

// A chant with no recording is not a puzzle, so the audio is the publish gate —
// what a gloss in every language is to the Lexicon. Surfaced here so the editor
// sees why publish is blocked rather than discovering it on refusal.
const hasRecording = computed(() => Boolean(chant.value?.audioUrl))
const canPublish = computed(() => !isPublished.value && hasRecording.value)

async function onSubmit(payload: CreateChantRequest) {
  if (!chant.value) return
  submitting.value = true
  errorMessage.value = null
  fieldErrors.value = null
  try {
    await update(chant.value.id, payload)
    await refresh()
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
  if (!chant.value) return
  runAction(() => publish(chant.value!.id), 'admin.chants.actionFailed')
}

function onUnpublish() {
  if (!chant.value) return
  runAction(() => unpublish(chant.value!.id), 'admin.chants.actionFailed')
}

function onTogglePlayable() {
  if (!chant.value) return
  const next = !chant.value.playableInNahlo
  runAction(() => setPlayable(chant.value!.id, next), 'admin.chants.actionFailed')
}

const audioInput = ref<HTMLInputElement | null>(null)
const audioUploading = ref(false)
const audioError = ref<string | null>(null)

function triggerAudioSelect() {
  audioInput.value?.click()
}

async function onAudioSelected(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!chant.value || !file) return

  audioUploading.value = true
  audioError.value = null
  try {
    await uploadAudio(chant.value.id, file)
    await refresh()
  }
  catch (error) {
    // The 15 MB cap is the one refusal worth naming: a chant is a sung phrase
    // rather than a single word, so an over-long take is a real mistake to make.
    const statusCode = (error as FetchError | undefined)?.statusCode
    audioError.value = t(
      statusCode === 413 || statusCode === 400
        ? 'admin.chants.recording.uploadRejected'
        : 'admin.chants.recording.uploadFailed')
  }
  finally {
    audioUploading.value = false
    if (audioInput.value) audioInput.value.value = ''
  }
}

async function onRemoveAudio() {
  if (!chant.value) return
  audioUploading.value = true
  audioError.value = null
  try {
    await removeAudio(chant.value.id)
    await refresh()
  }
  catch (error) {
    // Refused while published, by the domain: a published chant without audio
    // would sit in the pool as an unplayable puzzle.
    const statusCode = (error as FetchError | undefined)?.statusCode
    audioError.value = t(
      statusCode === 400 && isPublished.value
        ? 'admin.chants.recording.removeWhilePublished'
        : 'admin.chants.recording.removeFailed')
  }
  finally {
    audioUploading.value = false
  }
}

async function onDelete() {
  if (!chant.value) return
  if (!window.confirm(t('admin.chants.deleteConfirm'))) return
  acting.value = true
  errorMessage.value = null
  try {
    await remove(chant.value.id)
    await router.push('/admin/chants')
  }
  catch (error) {
    // A solqin points at the melody it borrows, so a parent cannot simply go.
    errorMessage.value = t(
      (error as FetchError | undefined)?.statusCode === 409
        ? 'admin.chants.deleteConflict'
        : 'admin.chants.deleteFailed')
    acting.value = false
  }
}

const actionButtonClass
  = 'inline-flex items-center rounded-md border border-[var(--color-border-strong)] px-4 py-2 font-sans text-sm font-medium transition-colors hover:bg-[var(--color-bg-subtle)] disabled:cursor-not-allowed disabled:opacity-50'
</script>

<template>
  <section class="mx-auto max-w-3xl">
    <AdminBreadcrumb
      section-key="admin.sections.chants.label"
      section-to="/admin/chants"
      :current="chant?.transliteration ?? null"
    />
    <AdminSectionNav />

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
      :message="t('admin.chants.notFound')"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('admin.chants.loadChantFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <template v-else-if="chant">
      <p class="mt-4 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="mb-1 mt-2 font-serif text-3xl font-semibold tracking-[-0.015em]">
        {{ chant.transliteration }}
      </h1>
      <p class="mb-2 font-sans text-sm text-[var(--color-text-muted)]">
        {{ chant.modeName }}<span v-if="chant.variantNumber"> · {{ t(`admin.chants.form.variantKind.${chant.variantKind}`) }} {{ chant.variantNumber }}</span>
      </p>
      <p class="mb-6">
        <SyriacText :text="chant.syriacIncipit" class="!text-2xl" />
      </p>

      <p
        v-if="errorMessage"
        class="mb-5 rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
        role="alert"
      >{{ errorMessage }}</p>

      <!-- A reviewer sees the chant but cannot save it. Saying so up front beats
           a form that looks editable until the server refuses it. -->
      <p
        v-if="!mayEdit"
        class="mb-5 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] px-4 py-3 font-sans text-sm text-[var(--color-text)]"
      >{{ t('admin.chants.readOnlyNotice') }}</p>

      <ChantForm
        :key="chant.updatedAt"
        :chant="chant"
        :modes="modes"
        :sections="sections"
        :melody-sources="melodySources"
        :submitting="submitting"
        :readonly="!mayEdit"
        :field-errors="fieldErrors"
        :submit-label="t('common.save')"
        @submit="onSubmit"
        @cancel="router.push('/admin/chants')"
      />

      <!-- The recording, then the lifecycle it gates. Editor actions only — a
           reviewer has nothing to change here. -->
      <div v-if="mayEdit" class="mt-10 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-subtle)] p-6">
        <h2 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.chants.recording.heading') }}
        </h2>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ t('admin.chants.recording.hint') }}
        </p>

        <audio
          v-if="chant.audioUrl"
          class="mt-3 w-full max-w-sm"
          controls
          :src="mediaUrl(chant.audioUrl)"
        />
        <p v-else class="mt-3 font-sans text-sm text-[var(--color-text-muted)]">
          {{ t('admin.chants.recording.none') }}
        </p>

        <p
          v-if="audioError"
          role="alert"
          class="mt-2 font-sans text-sm text-[var(--color-accent)]"
        >{{ audioError }}</p>

        <div class="mt-3 flex flex-wrap items-center gap-3">
          <input
            ref="audioInput"
            type="file"
            accept="audio/mpeg,audio/mp3,audio/wav,audio/x-wav,audio/ogg,audio/webm,audio/mp4,audio/x-m4a"
            class="hidden"
            @change="onAudioSelected"
          >
          <button
            type="button"
            :disabled="audioUploading"
            :class="actionButtonClass"
            @click="triggerAudioSelect"
          >
            {{ audioUploading
              ? t('admin.chants.recording.uploading')
              : chant.audioUrl
                ? t('admin.chants.recording.replace')
                : t('admin.chants.recording.upload') }}
          </button>
          <button
            v-if="chant.audioUrl"
            type="button"
            :disabled="audioUploading || isPublished"
            :class="actionButtonClass"
            @click="onRemoveAudio"
          >{{ t('admin.chants.recording.remove') }}</button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ chant.audioUrl && isPublished
            ? t('admin.chants.recording.removeWhilePublished')
            : t('admin.chants.recording.formatHint') }}
        </p>

        <hr class="my-5 border-t border-[var(--color-border)]">

        <h3 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.chants.lifecycle.heading') }}
        </h3>

        <div class="mt-4 flex flex-wrap items-center gap-3">
          <span
            class="inline-flex items-center rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
            :class="
              isPublished
                ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                : 'bg-[var(--color-bg-elevated)] text-[var(--color-text-muted)] ring-1 ring-[var(--color-border-strong)]'
            "
          >{{ t(`admin.chants.status.${chant.status}`) }}</span>

          <button
            v-if="!isPublished"
            type="button"
            :disabled="acting || !canPublish"
            :class="actionButtonClass"
            @click="onPublish"
          >{{ t('admin.chants.actions.publish') }}</button>
          <button
            v-else
            type="button"
            :disabled="acting"
            :class="actionButtonClass"
            @click="onUnpublish"
          >{{ t('admin.chants.actions.unpublish') }}</button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ !isPublished && !hasRecording
            ? t('admin.chants.lifecycle.recordingRequired')
            : t('admin.chants.lifecycle.publishHint') }}
        </p>

        <hr class="my-5 border-t border-[var(--color-border)]">

        <h3 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('admin.chants.lifecycle.poolHeading') }}
        </h3>
        <div class="mt-3 flex flex-wrap items-center gap-3">
          <span class="font-sans text-sm text-[var(--color-text-muted)]">
            {{ chant.playableInNahlo
              ? t('admin.chants.lifecycle.inPool')
              : t('admin.chants.lifecycle.notInPool') }}
          </span>
          <button
            type="button"
            :disabled="acting || !isPublished"
            :class="actionButtonClass"
            @click="onTogglePlayable"
          >
            {{ chant.playableInNahlo
              ? t('admin.chants.actions.removeFromPool')
              : t('admin.chants.actions.addToPool') }}
          </button>
        </div>
        <p class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
          {{ t('admin.chants.lifecycle.poolHint') }}
        </p>
        <!-- A solqin borrows another chant's melody, so what a listener
             recognises is the parent's. Left out of the launch pool by the
             owner's decision, not by a rule the domain enforces. -->
        <p
          v-if="chant.inheritsMelodyFromTransliteration"
          class="mt-2 font-sans text-xs text-[var(--color-text-faint)]"
        >{{ t('admin.chants.lifecycle.solqinPoolHint', { melody: chant.inheritsMelodyFromTransliteration }) }}</p>

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
