<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { BethGazoSectionDto } from '~/types/api'

/**
 * The section editor.
 *
 * Exists because the sections were modelled as a reference table on purpose — "a
 * row an editor adds, not a deploy" — and then shipped read-only, so every
 * correction was a migration. On 2026-08-08 that cost four deploys in one day.
 *
 * Two things this page deliberately does not offer:
 *  - Typing a position. It is uniquely indexed, so an editor picking a slot
 *    already in use would just be handed a constraint violation. New sections
 *    append; order is changed by moving a section past its neighbour.
 *  - Deleting a section that still has chants, or dropping a mode those chants
 *    use. The API refuses both; the page says why rather than showing a raw error.
 */

// Editorial surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { canEdit, canViewBackoffice, refresh: refreshAccess } = useMyAccess()
const {
  listModes,
  listSections,
  createSection,
  updateSection,
  deleteSection,
  moveSection,
  createMode,
  updateMode,
  deleteMode,
  moveMode,
} = useChantsAdmin()

await refreshAdmin()
await refreshAccess()

// A reviewer reads the treasury but changes nothing. Offering only — the API
// refuses independently.
const mayEdit = computed(() => canEdit('Nahlo'))
const mayView = computed(() => canViewBackoffice('Nahlo'))

const { data: modes, refresh: refreshModes } = await useAsyncData(
  'admin-section-editor-modes',
  () => listModes(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

const { data: sections, refresh: refreshSections } = await useAsyncData(
  'admin-section-editor-sections',
  () => listSections(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

const ordered = computed(() => [...(sections.value ?? [])].sort((a, b) => Number(a.position) - Number(b.position)))

/** Which section's form is open — a section id, 'new', or null for none. */
const editing = ref<string | null>(null)
const draftName = ref('')
const draftModeIds = ref<string[]>([])
const busy = ref(false)
const error = ref('')

function startNew() {
  editing.value = 'new'
  draftName.value = ''
  draftModeIds.value = []
  error.value = ''
}

function startEdit(section: BethGazoSectionDto) {
  editing.value = section.id
  draftName.value = section.name
  draftModeIds.value = [...section.allowedModeIds]
  error.value = ''
}

function cancel() {
  editing.value = null
  error.value = ''
}

function toggleMode(id: string) {
  draftModeIds.value = draftModeIds.value.includes(id)
    ? draftModeIds.value.filter(m => m !== id)
    : [...draftModeIds.value, id]
}

/** All or nothing, since a section is almost always one or the other. */
function setAllModes(on: boolean) {
  draftModeIds.value = on ? (modes.value ?? []).map(m => m.id) : []
}

const canSave = computed(() => draftName.value.trim().length > 0 && !busy.value)

/**
 * The API answers a refused edit with a plain sentence — "3 chant(s) in this
 * section still use a mode you are removing" — so show that rather than a
 * generic failure. An invisible reason is a reason the editor cannot act on.
 */
function messageFor(e: unknown): string {
  const detail = (e as FetchError<{ detail?: string, title?: string }>)?.data
  return detail?.detail || detail?.title || t('admin.chantSections.saveFailed')
}

async function save() {
  if (!canSave.value) return
  busy.value = true
  error.value = ''
  try {
    const body = { name: draftName.value.trim(), allowedModeIds: draftModeIds.value }
    if (editing.value === 'new') await createSection(body)
    else if (editing.value) await updateSection(editing.value, body)
    await refreshSections()
    editing.value = null
  }
  catch (e) {
    error.value = messageFor(e)
  }
  finally {
    busy.value = false
  }
}

const confirming = ref<string | null>(null)

async function remove(id: string) {
  busy.value = true
  error.value = ''
  try {
    await deleteSection(id)
    await refreshSections()
    confirming.value = null
  }
  catch (e) {
    error.value = messageFor(e)
  }
  finally {
    busy.value = false
  }
}

async function move(id: string, up: boolean) {
  busy.value = true
  error.value = ''
  try {
    await moveSection(id, up)
    await refreshSections()
  }
  catch (e) {
    error.value = messageFor(e)
  }
  finally {
    busy.value = false
  }
}

function modeNames(ids: readonly string[]): string {
  const byId = new Map((modes.value ?? []).map(m => [m.id, m]))
  return ids
    .map(id => byId.get(id))
    .filter(Boolean)
    .sort((a, b) => Number(a!.position) - Number(b!.position))
    .map(m => m!.name)
    .join(' · ')
}

const orderedModes = computed(() => [...(modes.value ?? [])].sort((a, b) => Number(a.position) - Number(b.position)))

/**
 * The modes, edited on the same page as the sections because they are edited
 * together: a section's whole definition is which of these it admits.
 *
 * Deleting one is guarded from two directions — a chant carrying it, and a
 * section admitting it. The second matters more than it looks: that link is
 * Cascade, so without the guard the row would vanish without a word and the
 * section would quietly stop offering a mode it is supposed to have.
 */
const editingMode = ref<string | null>(null)
const modeName = ref('')

function startNewMode() {
  editingMode.value = 'new'
  modeName.value = ''
  error.value = ''
}

function startEditMode(id: string, name: string) {
  editingMode.value = id
  modeName.value = name
  error.value = ''
}

async function saveMode() {
  if (!modeName.value.trim() || busy.value) return
  busy.value = true
  error.value = ''
  try {
    const body = { name: modeName.value.trim() }
    if (editingMode.value === 'new') await createMode(body)
    else if (editingMode.value) await updateMode(editingMode.value, body)
    await refreshModes()
    editingMode.value = null
  }
  catch (e) {
    error.value = messageFor(e)
  }
  finally {
    busy.value = false
  }
}

const confirmingMode = ref<string | null>(null)

async function removeMode(id: string) {
  busy.value = true
  error.value = ''
  try {
    await deleteMode(id)
    await refreshModes()
    confirmingMode.value = null
  }
  catch (e) {
    error.value = messageFor(e)
  }
  finally {
    busy.value = false
  }
}

async function shiftMode(id: string, up: boolean) {
  busy.value = true
  error.value = ''
  try {
    await moveMode(id, up)
    await refreshModes()
  }
  catch (e) {
    error.value = messageFor(e)
  }
  finally {
    busy.value = false
  }
}

const fieldClass
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none'
</script>

<template>
  <div class="flex flex-col gap-6">
    <AdminBreadcrumb
      section-key="admin.sections.chants.label"
      section-to="/admin/chants"
      :current="t('admin.chantSections.title')"
    />

    <AdminPageHeader :title="t('admin.chantSections.title')" :description="t('admin.chantSections.description')">
      <template #actions>
        <button v-if="mayEdit && editing !== 'new'" class="btn btn-primary" @click="startNew">
          {{ t('admin.chantSections.add') }}
        </button>
      </template>
    </AdminPageHeader>

    <p v-if="!mayView" class="font-sans text-sm text-[var(--color-text-muted)]">
      {{ t('admin.adminRequiredHint') }}
    </p>

    <template v-else>
      <p v-if="error" role="alert" class="rounded-md bg-[var(--color-accent-faint)] px-3 py-2 font-sans text-sm text-[var(--color-accent)]">
        {{ error }}
      </p>

      <!-- New section form, above the list so it is where the button pointed. -->
      <section v-if="editing === 'new'" class="rounded-lg border border-[var(--color-border)] p-4">
        <h2 class="font-sans text-sm font-semibold">{{ t('admin.chantSections.add') }}</h2>
        <div class="mt-3 flex flex-col gap-3">
          <input v-model="draftName" :placeholder="t('admin.chantSections.namePlaceholder')" :class="fieldClass">
          <div>
            <div class="flex items-center gap-3">
              <span class="font-sans text-xs font-medium text-[var(--color-text-faint)]">
                {{ t('admin.chantSections.modes') }}
              </span>
              <button class="font-sans text-xs text-[var(--color-accent)]" @click="setAllModes(true)">
                {{ t('admin.chantSections.allModes') }}
              </button>
              <button class="font-sans text-xs text-[var(--color-accent)]" @click="setAllModes(false)">
                {{ t('admin.chantSections.noModes') }}
              </button>
            </div>
            <div class="mt-2 flex flex-wrap gap-2">
              <label
                v-for="mode in modes"
                :key="mode.id"
                class="inline-flex cursor-pointer items-center gap-2 rounded-md border border-[var(--color-border-strong)] px-2.5 py-1.5 font-sans text-xs"
                :class="draftModeIds.includes(mode.id)
                  ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                  : 'bg-[var(--color-bg-elevated)]'"
              >
                <input
                  type="checkbox"
                  :checked="draftModeIds.includes(mode.id)"
                  class="accent-[var(--color-accent)]"
                  @change="toggleMode(mode.id)"
                >
                {{ mode.position }} · {{ mode.name }}
              </label>
            </div>
            <p class="mt-1 font-sans text-xs text-[var(--color-text-faint)]">
              {{ t('admin.chantSections.modesHint') }}
            </p>
          </div>
          <div class="flex gap-2">
            <button class="btn btn-primary" :disabled="!canSave" @click="save">
              {{ t('admin.chantSections.save') }}
            </button>
            <button class="btn btn-ghost" @click="cancel">{{ t('admin.chantSections.cancel') }}</button>
          </div>
        </div>
      </section>

      <ul class="flex flex-col gap-2">
        <li
          v-for="(section, index) in ordered"
          :key="section.id"
          class="rounded-lg border border-[var(--color-border)] p-4"
        >
          <!-- Editing this one -->
          <div v-if="editing === section.id" class="flex flex-col gap-3">
            <input v-model="draftName" :class="fieldClass">
            <div>
              <div class="flex items-center gap-3">
                <span class="font-sans text-xs font-medium text-[var(--color-text-faint)]">
                  {{ t('admin.chantSections.modes') }}
                </span>
                <button class="font-sans text-xs text-[var(--color-accent)]" @click="setAllModes(true)">
                  {{ t('admin.chantSections.allModes') }}
                </button>
                <button class="font-sans text-xs text-[var(--color-accent)]" @click="setAllModes(false)">
                  {{ t('admin.chantSections.noModes') }}
                </button>
              </div>
              <div class="mt-2 flex flex-wrap gap-2">
                <label
                  v-for="mode in modes"
                  :key="mode.id"
                  class="inline-flex cursor-pointer items-center gap-2 rounded-md border border-[var(--color-border-strong)] px-2.5 py-1.5 font-sans text-xs"
                  :class="draftModeIds.includes(mode.id)
                    ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                    : 'bg-[var(--color-bg-elevated)]'"
                >
                  <input
                    type="checkbox"
                    :checked="draftModeIds.includes(mode.id)"
                    class="accent-[var(--color-accent)]"
                    @change="toggleMode(mode.id)"
                  >
                  {{ mode.position }} · {{ mode.name }}
                </label>
              </div>
            </div>
            <div class="flex gap-2">
              <button class="btn btn-primary" :disabled="!canSave" @click="save">
                {{ t('admin.chantSections.save') }}
              </button>
              <button class="btn btn-ghost" @click="cancel">{{ t('admin.chantSections.cancel') }}</button>
            </div>
          </div>

          <!-- Reading it -->
          <div v-else class="flex flex-wrap items-start justify-between gap-3">
            <div class="min-w-0">
              <p class="font-sans text-sm font-semibold">{{ section.name }}</p>
              <p class="mt-0.5 font-sans text-xs text-[var(--color-text-muted)]">
                <template v-if="section.allowedModeIds.length">
                  {{ modeNames(section.allowedModeIds) }}
                </template>
                <template v-else>
                  {{ t('admin.chantSections.hasNoModes') }}
                </template>
              </p>
            </div>

            <div v-if="mayEdit" class="flex shrink-0 items-center gap-1">
              <button
                class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)] disabled:opacity-40"
                :disabled="index === 0 || busy"
                :aria-label="t('admin.chantSections.moveUp')"
                @click="move(section.id, true)"
              >↑</button>
              <button
                class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)] disabled:opacity-40"
                :disabled="index === ordered.length - 1 || busy"
                :aria-label="t('admin.chantSections.moveDown')"
                @click="move(section.id, false)"
              >↓</button>
              <button class="rounded px-2 py-1 font-sans text-xs text-[var(--color-accent)]" @click="startEdit(section)">
                {{ t('admin.chantSections.edit') }}
              </button>
              <button
                v-if="confirming !== section.id"
                class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)]"
                @click="confirming = section.id"
              >
                {{ t('admin.chantSections.remove') }}
              </button>
              <template v-else>
                <button class="rounded px-2 py-1 font-sans text-xs font-semibold text-[var(--color-accent)]" :disabled="busy" @click="remove(section.id)">
                  {{ t('admin.chantSections.confirmRemove') }}
                </button>
                <button class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)]" @click="confirming = null">
                  {{ t('admin.chantSections.cancel') }}
                </button>
              </template>
            </div>
          </div>
        </li>
      </ul>

      <!-- The modes, on the same page because a section's whole definition is
           which of these it admits. -->
      <section class="mt-4 border-t border-[var(--color-border)] pt-6">
        <div class="flex flex-wrap items-baseline justify-between gap-3">
          <div>
            <h2 class="font-sans text-base font-semibold">{{ t('admin.chantModes.title') }}</h2>
            <p class="mt-0.5 max-w-prose font-sans text-sm text-[var(--color-text-muted)]">
              {{ t('admin.chantModes.description') }}
            </p>
          </div>
          <button v-if="mayEdit && editingMode !== 'new'" class="btn btn-ghost" @click="startNewMode">
            {{ t('admin.chantModes.add') }}
          </button>
        </div>

        <div v-if="editingMode === 'new'" class="mt-3 flex flex-wrap gap-2">
          <input v-model="modeName" :placeholder="t('admin.chantModes.namePlaceholder')" :class="[fieldClass, 'max-w-xs']">
          <button class="btn btn-primary" :disabled="!modeName.trim() || busy" @click="saveMode">
            {{ t('admin.chantSections.save') }}
          </button>
          <button class="btn btn-ghost" @click="editingMode = null">{{ t('admin.chantSections.cancel') }}</button>
        </div>

        <ul class="mt-3 flex flex-col gap-1.5">
          <li
            v-for="(mode, index) in orderedModes"
            :key="mode.id"
            class="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--color-border)] px-3 py-2"
          >
            <div v-if="editingMode === mode.id" class="flex flex-wrap items-center gap-2">
              <input v-model="modeName" :class="[fieldClass, 'max-w-xs']">
              <button class="btn btn-primary" :disabled="!modeName.trim() || busy" @click="saveMode">
                {{ t('admin.chantSections.save') }}
              </button>
              <button class="btn btn-ghost" @click="editingMode = null">{{ t('admin.chantSections.cancel') }}</button>
            </div>

            <template v-else>
              <span class="font-sans text-sm">
                <span class="text-[var(--color-text-faint)]">{{ mode.position }}</span> · {{ mode.name }}
              </span>
              <div v-if="mayEdit" class="flex shrink-0 items-center gap-1">
                <button
                  class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)] disabled:opacity-40"
                  :disabled="index === 0 || busy"
                  :aria-label="t('admin.chantSections.moveUp')"
                  @click="shiftMode(mode.id, true)"
                >↑</button>
                <button
                  class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)] disabled:opacity-40"
                  :disabled="index === orderedModes.length - 1 || busy"
                  :aria-label="t('admin.chantSections.moveDown')"
                  @click="shiftMode(mode.id, false)"
                >↓</button>
                <button class="rounded px-2 py-1 font-sans text-xs text-[var(--color-accent)]" @click="startEditMode(mode.id, mode.name)">
                  {{ t('admin.chantSections.edit') }}
                </button>
                <button
                  v-if="confirmingMode !== mode.id"
                  class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)]"
                  @click="confirmingMode = mode.id"
                >
                  {{ t('admin.chantSections.remove') }}
                </button>
                <template v-else>
                  <button class="rounded px-2 py-1 font-sans text-xs font-semibold text-[var(--color-accent)]" :disabled="busy" @click="removeMode(mode.id)">
                    {{ t('admin.chantSections.confirmRemove') }}
                  </button>
                  <button class="rounded px-2 py-1 font-sans text-xs text-[var(--color-text-muted)]" @click="confirmingMode = null">
                    {{ t('admin.chantSections.cancel') }}
                  </button>
                </template>
              </div>
            </template>
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>
