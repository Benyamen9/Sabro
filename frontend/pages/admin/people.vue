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
const ownerCount = computed(() => people.value.filter(p => p.role === 'Owner').length)

// Holding an area is what actually opens the backoffice, so it is the number
// worth showing beside the head count — not how many rows exist.
const grantedCount = computed(() =>
  people.value.filter(p => p.role === 'Owner' || (p.areas?.length ?? 0) > 0).length)

const needsFirstOwner = computed(() => people.value.length > 0 && !people.value.some(p => p.role === 'Owner'))

/** Your own row is locked once an Owner exists; during bootstrap it is the way out. */
function isLocked(person: PersonDto) {
  return person.isYou && !needsFirstOwner.value
}
/** Area colours, so a grant reads as belonging to its section at a glance. */
const areaAccent: Record<ContentArea, string> = {
  Lexicon: '--color-meltho',
  Shmo: '--color-shmo',
}

/** The three levels, in order of increasing power. */
const accessChoices = computed(() => accessOptions.map(option => ({
  value: option,
  label: option ? t(`admin.people.access.${option}`) : t('admin.people.access.none'),
})))

/**
 * First letter of whatever the person is called. Falls back to a dash rather than
 * a letter from the opaque id, which would look like information and be noise.
 */
function initialFor(person: PersonDto) {
  const label = labelFor(person)
  return label ? [...label][0]!.toUpperCase() : '—'
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

</script>

<template>
  <section>
    <AdminBreadcrumb section-key="admin.sections.people.label" section-to="/admin/people" />
    <AdminSectionNav />

    <AdminPageHeader :title="t('admin.people.title')" :subtitle="t('admin.people.subtitle')">
      <template v-if="viewState === 'ready' && people.length > 0" #stats>
        <AdminStat :value="people.length" :label="t('admin.people.statPeople', people.length)" />
        <AdminStat :value="ownerCount" :label="t('admin.people.statOwners', ownerCount)" />
        <AdminStat :value="grantedCount" :label="t('admin.people.statGranted', grantedCount)" />
      </template>
    </AdminPageHeader>

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

      <!-- One card per person rather than a table row. Access is a rare,
           consequential decision, and it was being made through a select nested in
           a table cell: the options hidden until opened, the whole thing scrolling
           sideways on a phone. Cards stack, and the segmented control shows every
           level at once — including the one you are not granting. -->
      <ul class="grid gap-3">
        <li
          v-for="person in people"
          :key="person.id"
          class="grid grid-cols-[2.5rem_1fr] gap-3 rounded-lg border p-4 sm:gap-4 sm:p-5"
          :class="isOwner(person)
            ? 'border-[color-mix(in_oklab,var(--color-accent)_35%,var(--color-border))] bg-[color-mix(in_oklab,var(--color-accent-faint)_45%,var(--color-bg-elevated))]'
            : 'border-[var(--color-border)] bg-[var(--color-bg-elevated)]'"
        >
          <!-- An initial, not an avatar: Sabro stores no picture and never will.
               It gives the eye something to land on down a list of addresses. -->
          <span
            class="grid size-10 place-items-center rounded-full font-serif text-base font-semibold"
            :class="isOwner(person)
              ? 'bg-[var(--color-accent)] text-white'
              : 'border border-[var(--color-border)] bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'"
            aria-hidden="true"
          >{{ initialFor(person) }}</span>

          <div class="min-w-0">
            <div class="flex flex-wrap items-baseline gap-x-2 gap-y-1">
              <span v-if="labelFor(person)" class="font-sans font-semibold text-[var(--color-text)] break-all">
                {{ labelFor(person) }}
              </span>
              <span v-else class="font-sans italic text-[var(--color-text-faint)]">
                {{ t('admin.people.unnamed') }}
              </span>

              <span
                v-if="person.isYou"
                class="rounded-full bg-[var(--color-bg-subtle)] px-2 py-0.5 font-sans text-[0.65rem] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]"
              >{{ t('admin.people.you') }}</span>
              <span
                v-if="isOwner(person)"
                class="rounded-full bg-[var(--color-accent)] px-2 py-0.5 font-sans text-[0.65rem] font-semibold uppercase tracking-wider text-white"
              >{{ t('admin.people.role.Owner') }}</span>
            </div>

            <p class="mt-0.5 font-sans text-xs text-[var(--color-text-muted)] tabular-nums">
              {{ t('admin.people.sinceDate', { date: new Date(person.createdAt).toLocaleDateString() }) }}
            </p>

            <!-- Owner is a yes/no, not a rung: it grants every area at once, so the
                 per-area controls are hidden rather than shown lying. -->
            <label class="mt-3 flex items-center gap-2 font-sans text-sm">
              <input
                type="checkbox"
                :checked="isOwner(person)"
                :disabled="isLocked(person) || savingId === person.id"
                class="size-4"
                @change="onRoleChange(person, ($event.target as HTMLInputElement).checked ? 'Owner' : 'Reader')"
              >
              <span>{{ t('admin.people.makeOwner') }}</span>
            </label>
            <p v-if="isLocked(person)" class="mt-1 font-sans text-xs text-[var(--color-text-faint)]">
              {{ t('admin.people.cannotChangeOwn') }}
            </p>

            <p v-if="isOwner(person)" class="mt-2 font-sans text-sm text-[var(--color-text-muted)]">
              {{ t('admin.people.ownerEverything') }}
            </p>

            <div v-else class="mt-3 grid gap-2">
              <div
                v-for="area in areas"
                :key="area"
                class="flex flex-wrap items-center justify-between gap-2"
              >
                <span class="inline-flex items-center gap-2 font-sans text-sm text-[var(--color-text-muted)]">
                  <span class="h-3.5 w-[3px] rounded-full" :style="{ backgroundColor: `var(${areaAccent[area]})` }" aria-hidden="true" />
                  {{ t(`admin.people.area.${area}`) }}
                </span>
                <SegmentedControl
                  :name="`access-${person.id}-${area}`"
                  :model-value="accessFor(person, area)"
                  :options="accessChoices"
                  :label="t('admin.people.accessLabel', { area: t(`admin.people.area.${area}`) })"
                  :accent="areaAccent[area]"
                  :disabled="savingId === person.id"
                  @update:model-value="onAreaChange(person, area, $event as AreaAccess | '')"
                />
              </div>
            </div>

            <!-- A role predating the area grants. Not assignable here, but it may
                 already be set, so say so rather than render it as "no access". -->
            <p v-if="person.role === 'ExpertReviewer'" class="mt-2 font-sans text-xs text-[var(--color-text-faint)]">
              {{ t('admin.people.role.ExpertReviewer') }}
            </p>
          </div>
        </li>
      </ul>

      <p class="mt-5 max-w-prose font-sans text-xs text-[var(--color-text-faint)]">
        {{ t('admin.people.identityNote') }}
      </p>
    </div>
  </section>
</template>
