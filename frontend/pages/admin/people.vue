<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { AreaAccess, ContentArea, PersonDto, Role } from '~/types/api'
import type { PersonTier } from '~/utils/people'
import {
  CONTENT_AREAS,
  EMPTY_PEOPLE_FILTERS,
  PERSON_TIERS,
  areaAccessFor,
  filterPeople,
  groupPeople,
  hasActivePeopleFilters,
  isUnidentified,
  personIdentity,
  personInitial,
} from '~/utils/people'

/**
 * Who may edit what. Owner-only: the API refuses this list to anyone else, and
 * the page shows the same refusal rather than an empty table.
 *
 * The list is grouped by what people actually hold — Owners, then editors, then
 * reviewers, then everyone who signed in and holds nothing. A flat list in signup
 * order put the one account that can change anything somewhere in the middle,
 * and gave no way to answer "who can edit the Lexicon" by looking.
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
// button, then one row per area. The old single select forced them together and
// could not express "reviewer for Shmo, editor for the Lexicon" at all.
const areas = CONTENT_AREAS

// '' is the wire's null — no grant. Kept as an empty string because that is what
// the segmented control gives back; it is translated at the boundary, never stored.
const accessOptions: (AreaAccess | '')[] = ['', 'Reviewer', 'Editor']

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

// Accounts Logto could tell us nothing about. Worth its own count: it is the one
// number on this page that means something is wrong rather than merely quiet.
const unidentifiedCount = computed(() => people.value.filter(isUnidentified).length)

const needsFirstOwner = computed(() => people.value.length > 0 && !people.value.some(p => p.role === 'Owner'))

/** Your own row is locked once an Owner exists; during bootstrap it is the way out. */
function isLocked(person: PersonDto) {
  return person.isYou && !needsFirstOwner.value
}

/** Area colours, so a grant reads as belonging to its section at a glance. */
const areaAccent: Record<ContentArea, string> = {
  Lexicon: '--color-meltho',
  Shmo: '--color-shmo',
  Nahlo: '--color-nahlo',
}

/** The three levels, in order of increasing power. */
const accessChoices = computed(() => accessOptions.map(option => ({
  value: option,
  label: option ? t(`admin.people.access.${option}`) : t('admin.people.access.none'),
})))

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

/** Swaps one person in place — the endpoints return the same shape as the list. */
function replacePerson(updated: PersonDto) {
  const index = people.value.findIndex(p => p.id === updated.id)
  if (index !== -1) people.value[index] = updated
}

async function onAreaChange(person: PersonDto, area: ContentArea, value: AreaAccess | '') {
  savingId.value = person.id
  errorMessage.value = null
  try {
    replacePerson(await setAreaAccess(person.id, area, value === '' ? null : value))
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

// --- Owner, behind a confirmation -------------------------------------------
// Granting Owner hands over every area at once and the right to change everyone
// else's access. It used to be a checkbox in the middle of a card, one click away
// from a mis-aimed cursor and silent once done.

const pendingOwnerChange = ref<PersonDto | null>(null)

const pendingIsPromotion = computed(() =>
  pendingOwnerChange.value ? !isOwner(pendingOwnerChange.value) : true)

const pendingName = computed(() => {
  const person = pendingOwnerChange.value
  if (!person) return ''
  return personIdentity(person).label ?? t('admin.people.unidentifiedShort', { id: personIdentity(person).shortId })
})

function askOwnerChange(person: PersonDto) {
  if (isLocked(person) || savingId.value === person.id) return
  pendingOwnerChange.value = person
}

async function confirmOwnerChange() {
  const person = pendingOwnerChange.value
  if (!person) return

  const role: Role = isOwner(person) ? 'Reader' : 'Owner'
  savingId.value = person.id
  errorMessage.value = null
  try {
    replacePerson(await assignRole(person.id, role))
    pendingOwnerChange.value = null
  }
  catch (error) {
    // Surface the server's reason — "you cannot change your own role" is the
    // common one and is far more use than a generic failure.
    const detail = (error as FetchError).data?.detail as string | undefined
    errorMessage.value = detail || t('admin.people.saveFailed')
    pendingOwnerChange.value = null
    await load()
  }
  finally {
    savingId.value = null
  }
}

// --- Search and filters ------------------------------------------------------
// The whole list is already in memory — it is one request with no paging — so
// every filter is applied here rather than round-tripping to the API.

const filters = reactive({ ...EMPTY_PEOPLE_FILTERS })

const tierOptions: PersonTier[] = PERSON_TIERS

const hasFilters = computed(() => hasActivePeopleFilters(filters))

const visiblePeople = computed(() => filterPeople(people.value, filters))

const groups = computed(() => groupPeople(visiblePeople.value))

function clearFilters() {
  Object.assign(filters, EMPTY_PEOPLE_FILTERS)
}

/** The unidentified count in the header doubles as the way to see only those. */
function showUnidentifiedOnly() {
  clearFilters()
  filters.unidentifiedOnly = true
}

onMounted(load)

const selectClass
  = 'rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]'
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
      :action-label="t('common.retry')"
      @action="load"
    />

    <div v-else>
      <p
        v-if="needsFirstOwner"
        class="mb-4 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] px-4 py-3 font-sans text-sm text-[var(--color-text)]"
      >{{ t('admin.people.noOwnerYet') }}</p>

      <!-- Accounts the sign-in service could not name. Called out rather than
           left scattered through the list as blanks: it is a fixable state, and
           the first thing you need is to see which ones they are. -->
      <p
        v-if="unidentifiedCount > 0 && !filters.unidentifiedOnly"
        class="mb-4 flex flex-wrap items-center gap-x-3 gap-y-1 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] px-4 py-3 font-sans text-sm text-[var(--color-text)]"
      >
        <span>{{ t('admin.people.unidentifiedNotice', unidentifiedCount) }}</span>
        <button
          type="button"
          class="font-sans text-sm font-medium text-[var(--color-accent)] underline"
          @click="showUnidentifiedOnly"
        >{{ t('admin.people.showUnidentified') }}</button>
      </p>

      <p
        v-if="errorMessage"
        class="mb-4 rounded-md border border-[var(--color-accent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
        role="alert"
      >{{ errorMessage }}</p>

      <div class="mb-6 flex flex-wrap items-center gap-3">
        <div class="flex min-w-[240px] flex-1 items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-4 py-2.5 focus-within:border-[var(--color-accent)]">
          <svg class="size-4 shrink-0 text-[var(--color-text-faint)]" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
            <circle cx="9" cy="9" r="6" />
            <path d="m17 17-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input
            v-model="filters.search"
            type="search"
            :placeholder="t('admin.people.filters.searchPlaceholder')"
            :aria-label="t('admin.people.filters.searchPlaceholder')"
            class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none placeholder:text-[var(--color-text-faint)]"
          >
          <button
            v-if="filters.search"
            type="button"
            class="shrink-0 font-sans text-sm text-[var(--color-text-faint)] hover:text-[var(--color-text)]"
            :aria-label="t('admin.people.filters.searchClear')"
            @click="filters.search = ''"
          >✕</button>
        </div>

        <div>
          <label for="filter-tier" class="sr-only">{{ t('admin.people.filters.tierLabel') }}</label>
          <select id="filter-tier" v-model="filters.tier" :class="selectClass">
            <option value="">{{ t('admin.people.filters.tierAll') }}</option>
            <option v-for="tier in tierOptions" :key="tier" :value="tier">
              {{ t(`admin.people.tier.${tier}`) }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-area" class="sr-only">{{ t('admin.people.filters.areaLabel') }}</label>
          <select id="filter-area" v-model="filters.area" :class="selectClass">
            <option value="">{{ t('admin.people.filters.areaAll') }}</option>
            <option v-for="area in areas" :key="area" :value="area">
              {{ t(`admin.people.area.${area}`) }}
            </option>
          </select>
        </div>

        <label class="flex items-center gap-2 font-sans text-sm text-[var(--color-text-muted)]">
          <input v-model="filters.unidentifiedOnly" type="checkbox" class="size-4">
          {{ t('admin.people.filters.unidentifiedOnly') }}
        </label>

        <button
          v-if="hasFilters"
          type="button"
          class="font-sans text-sm text-[var(--color-text-muted)] underline hover:text-[var(--color-text)]"
          @click="clearFilters"
        >{{ t('admin.people.filters.clear') }}</button>
      </div>

      <StateMessage
        v-if="people.length === 0"
        variant="empty"
        :message="t('admin.people.empty')"
      />
      <StateMessage
        v-else-if="visiblePeople.length === 0"
        variant="noMatches"
        :message="t('admin.people.filters.noResults')"
        :action-label="t('admin.people.filters.clear')"
        @action="clearFilters"
      />

      <!-- One card per person rather than a table row. Access is a rare,
           consequential decision, and it was being made through a select nested in
           a table cell: the options hidden until opened, the whole thing scrolling
           sideways on a phone. Cards stack, and the segmented control shows every
           level at once — including the one you are not granting. -->
      <div v-else class="grid gap-8">
        <section v-for="group in groups" :key="group.tier">
          <h2 class="mb-3 flex flex-wrap items-baseline gap-x-3 gap-y-1 border-b border-[var(--color-border)] pb-2">
            <span class="font-serif text-lg font-semibold tracking-[-0.01em] text-[var(--color-text)]">
              {{ t(`admin.people.tier.${group.tier}`) }}
            </span>
            <span class="font-sans text-xs tabular-nums text-[var(--color-text-faint)]">
              {{ group.people.length }}
            </span>
            <span class="font-sans text-sm text-[var(--color-text-muted)]">
              {{ t(`admin.people.tierHint.${group.tier}`) }}
            </span>
          </h2>

          <ul class="grid gap-3">
            <li
              v-for="person in group.people"
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
              >{{ personInitial(person) }}</span>

              <div class="min-w-0">
                <div class="flex flex-wrap items-baseline gap-x-2 gap-y-1">
                  <span
                    v-if="personIdentity(person).label"
                    class="font-sans font-semibold break-all text-[var(--color-text)]"
                  >{{ personIdentity(person).label }}</span>
                  <!-- No name and no address: the account is real, our knowledge of
                       it is not. Says which account it is, so it can be chased. -->
                  <span v-else class="font-sans font-semibold text-[var(--color-text-muted)]">
                    {{ t('admin.people.unidentifiedShort', { id: personIdentity(person).shortId }) }}
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

                <p
                  v-if="personIdentity(person).secondary"
                  class="mt-0.5 font-sans text-sm break-all text-[var(--color-text-muted)]"
                >{{ personIdentity(person).secondary }}</p>

                <p class="mt-0.5 font-sans text-xs tabular-nums text-[var(--color-text-muted)]">
                  {{ t('admin.people.sinceDate', { date: new Date(person.createdAt).toLocaleDateString() }) }}
                </p>

                <p
                  v-if="isUnidentified(person)"
                  class="mt-2 rounded-md bg-[var(--color-bg-subtle)] px-3 py-2 font-sans text-xs text-[var(--color-text-muted)]"
                >{{ t('admin.people.unidentifiedHint') }}</p>

                <!-- Owner is a yes/no, not a rung: it grants every area at once, so the
                     per-area controls are hidden rather than shown lying. -->
                <div class="mt-3 flex flex-wrap items-center gap-x-3 gap-y-2">
                  <button
                    type="button"
                    class="rounded-md border px-3 py-1.5 font-sans text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50"
                    :class="isOwner(person)
                      ? 'border-[var(--color-accent)] text-[var(--color-accent)] hover:bg-[var(--color-accent-faint)]'
                      : 'border-[var(--color-border-strong)] text-[var(--color-text)] hover:bg-[var(--color-bg-subtle)]'"
                    :disabled="isLocked(person) || savingId === person.id"
                    @click="askOwnerChange(person)"
                  >
                    {{ isOwner(person) ? t('admin.people.removeOwner') : t('admin.people.makeOwner') }}
                  </button>
                  <span v-if="isLocked(person)" class="font-sans text-xs text-[var(--color-text-faint)]">
                    {{ t('admin.people.cannotChangeOwn') }}
                  </span>
                </div>

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
                      :model-value="areaAccessFor(person, area)"
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
        </section>
      </div>

      <p class="mt-5 max-w-prose font-sans text-xs text-[var(--color-text-faint)]">
        {{ t('admin.people.identityNote') }}
      </p>
    </div>

    <ConfirmDialog
      :open="pendingOwnerChange !== null"
      :title="pendingIsPromotion ? t('admin.people.confirmOwner.promoteTitle') : t('admin.people.confirmOwner.demoteTitle')"
      :body="pendingIsPromotion
        ? t('admin.people.confirmOwner.promoteBody', { name: pendingName })
        : t('admin.people.confirmOwner.demoteBody', { name: pendingName })"
      :confirm-label="pendingIsPromotion ? t('admin.people.confirmOwner.promoteConfirm') : t('admin.people.confirmOwner.demoteConfirm')"
      :cancel-label="t('common.cancel')"
      :tone="pendingIsPromotion ? 'accent' : 'danger'"
      :busy="savingId !== null"
      @confirm="confirmOwnerChange"
      @cancel="pendingOwnerChange = null"
    />
  </section>
</template>
