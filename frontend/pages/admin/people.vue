<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { AreaAccess, ContentArea, PersonDto, Role } from '~/types/api'

/**
 * Who may edit what. Owner-only: the API refuses this list to anyone else, and
 * the page shows the same refusal rather than an empty table.
 */

// Editorial surface — keep out of search indexes, like every other admin page.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { list, assignRole, setAreaAccess } = usePeopleAdmin()

await refreshAdmin()
// Not for this page's own gate — the API decides that — but so the section
// switcher above it knows which other sections to offer.
await useMyAccess().refresh()

// Area and level are two questions, so the page asks them separately: an Owner
// toggle, then one row per area. The old single select forced them together and
// could not express "reviewer for Shmo, editor for the Lexicon" at all.
const areas: ContentArea[] = ['Lexicon', 'Shmo']

// '' is the wire's null — no grant. Kept as an empty string because that is what
// a <select> gives back; it is translated at the boundary, never stored.
const accessOptions: (AreaAccess | '')[] = ['', 'Reviewer', 'Editor']

function accessFor(person: PersonDto, area: ContentArea): AreaAccess | '' {
  return person.areas.find(a => a.area === area)?.access ?? ''
}

/** The Owner reaches every area regardless of grants, so the rows would lie. */
function isOwner(person: PersonDto) {
  return person.role === 'Owner'
}

const people = ref<PersonDto[]>([])
const viewState = ref<'loading' | 'ready' | 'unauthorized' | 'failed'>('loading')
const savingId = ref<string | null>(null)

// Nobody is Owner yet — the state a fresh installation starts in. The server
// lets an admin appoint the first Owner, including themselves, so the page must
// not disable the only row that can end it.
const needsFirstOwner = computed(() => people.value.length > 0 && !people.value.some(p => p.role === 'Owner'))

/** Your own row is locked once an Owner exists; during bootstrap it is the way out. */
function isLocked(person: PersonDto) {
  return person.isYou && !needsFirstOwner.value
}
const errorMessage = ref<string | null>(null)

async function load() {
  viewState.value = 'loading'
  errorMessage.value = null
  try {
    people.value = await list()
    viewState.value = 'ready'
  }
  catch (error) {
    const status = (error as FetchError).statusCode
    viewState.value = status === 401 || status === 403 ? 'unauthorized' : 'failed'
  }
}

async function onAreaChange(person: PersonDto, area: ContentArea, value: AreaAccess | '') {
  savingId.value = person.id
  errorMessage.value = null
  try {
    const updated = await setAreaAccess(person.id, area, value === '' ? null : value)
    const index = people.value.findIndex(p => p.id === person.id)
    if (index !== -1) people.value[index] = updated
  }
  catch (error) {
    const detail = (error as FetchError).data?.detail as string | undefined
    errorMessage.value = detail || t('admin.people.saveFailed')
    await load()
  }
  finally {
    savingId.value = null
  }
}

async function onRoleChange(person: PersonDto, role: Role) {
  if (role === person.role) return
  savingId.value = person.id
  errorMessage.value = null
  try {
    const updated = await assignRole(person.id, role)
    const index = people.value.findIndex(p => p.id === person.id)
    if (index !== -1) people.value[index] = updated
  }
  catch (error) {
    // Surface the server's reason — "you cannot change your own role" is the
    // common one and is far more use than a generic failure.
    const detail = (error as FetchError).data?.detail as string | undefined
    errorMessage.value = detail || t('admin.people.saveFailed')
    await load()
  }
  finally {
    savingId.value = null
  }
}

onMounted(load)

/** Best available label: the Logto name, else a self-set display name, else nothing. */
function labelFor(person: PersonDto) {
  return person.name || person.displayName || null
}

const cellClass = 'px-3 py-3 align-middle'
</script>

<template>
  <section>
    <AdminBreadcrumb section-key="admin.sections.people.label" section-to="/admin/people" />
    <AdminSectionNav />

    <header class="mb-8">
      <p class="mb-2 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="font-serif text-3xl font-semibold tracking-[-0.015em]">{{ t('admin.people.title') }}</h1>
      <p class="mt-2 max-w-prose font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('admin.people.subtitle') }}
      </p>
    </header>

    <StateMessage
      v-if="viewState === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="viewState === 'unauthorized' || isAdmin === false"
      variant="unauthorized"
      :message="t('admin.people.ownerRequired')"
      :hint="t('admin.people.ownerRequiredHint')"
    />
    <StateMessage
      v-else-if="viewState === 'failed'"
      variant="failed"
      :message="t('admin.people.loadFailed')"
      @retry="load"
    />

    <div v-else>
      <p
        v-if="needsFirstOwner"
        class="mb-4 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] px-4 py-3 font-sans text-sm text-[var(--color-text)]"
      >{{ t('admin.people.noOwnerYet') }}</p>

      <p
        v-if="errorMessage"
        class="mb-4 rounded-md border border-[var(--color-accent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
        role="alert"
      >{{ errorMessage }}</p>

      <div class="overflow-x-auto">
        <table class="w-full min-w-[36rem] border-collapse font-sans text-sm">
          <thead>
            <tr class="border-b border-[var(--color-border)] text-left">
              <th :class="[cellClass, 'text-xs font-semibold uppercase tracking-wider text-[var(--color-text-faint)]']">
                {{ t('admin.people.person') }}
              </th>
              <th :class="[cellClass, 'text-xs font-semibold uppercase tracking-wider text-[var(--color-text-faint)]']">
                {{ t('admin.people.mayEdit') }}
              </th>
              <th :class="[cellClass, 'text-xs font-semibold uppercase tracking-wider text-[var(--color-text-faint)]']">
                {{ t('admin.people.since') }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="person in people"
              :key="person.id"
              class="border-b border-[var(--color-border)]"
            >
              <td :class="cellClass">
                <span v-if="labelFor(person)" class="font-medium text-[var(--color-text)]">
                  {{ labelFor(person) }}
                </span>
                <!-- Sabro stores no name; when Logto cannot be reached there is
                     genuinely nothing to show, so say that rather than print an id. -->
                <span v-else class="italic text-[var(--color-text-faint)]">
                  {{ t('admin.people.unnamed') }}
                </span>
                <span v-if="person.isYou" class="ml-2 text-xs text-[var(--color-text-faint)]">
                  {{ t('admin.people.you') }}
                </span>
                <span v-if="person.email" class="mt-0.5 block text-xs text-[var(--color-text-muted)]">
                  {{ person.email }}
                </span>
              </td>
              <td :class="cellClass">
                <!-- Owner is a yes/no, not a rung: it grants every area at once, so
                     the per-area rows below are hidden rather than shown lying. -->
                <label class="flex items-center gap-2">
                  <input
                    type="checkbox"
                    :checked="isOwner(person)"
                    :disabled="isLocked(person) || savingId === person.id"
                    class="size-4"
                    @change="onRoleChange(person, ($event.target as HTMLInputElement).checked ? 'Owner' : 'Reader')"
                  >
                  <span>{{ t('admin.people.role.Owner') }}</span>
                </label>
                <span v-if="isLocked(person)" class="mt-1 block text-xs text-[var(--color-text-faint)]">
                  {{ t('admin.people.cannotChangeOwn') }}
                </span>

                <p v-if="isOwner(person)" class="mt-2 text-xs text-[var(--color-text-muted)]">
                  {{ t('admin.people.ownerEverything') }}
                </p>

                <div v-else class="mt-2 grid gap-2">
                  <label
                    v-for="area in areas"
                    :key="area"
                    class="flex items-center justify-between gap-3"
                  >
                    <span class="text-[var(--color-text-muted)]">{{ t(`admin.people.area.${area}`) }}</span>
                    <select
                      :value="accessFor(person, area)"
                      :disabled="savingId === person.id"
                      class="w-40 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2 py-1 font-sans text-sm text-[var(--color-text)] disabled:cursor-not-allowed disabled:opacity-60"
                      @change="onAreaChange(person, area, ($event.target as HTMLSelectElement).value as AreaAccess | '')"
                    >
                      <option v-for="option in accessOptions" :key="option || 'none'" :value="option">
                        {{ option ? t(`admin.people.access.${option}`) : t('admin.people.access.none') }}
                      </option>
                    </select>
                  </label>
                </div>

                <!-- A role predating the area grants. Not assignable here, but it may
                     already be set, so say so rather than render it as "no access". -->
                <p v-if="person.role === 'ExpertReviewer'" class="mt-2 text-xs text-[var(--color-text-faint)]">
                  {{ t('admin.people.role.ExpertReviewer') }}
                </p>
              </td>
              <td :class="[cellClass, 'whitespace-nowrap text-[var(--color-text-muted)] tabular-nums']">
                {{ new Date(person.createdAt).toLocaleDateString() }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <p class="mt-5 max-w-prose font-sans text-xs text-[var(--color-text-faint)]">
        {{ t('admin.people.identityNote') }}
      </p>
    </div>
  </section>
</template>
