<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { PersonDto, Role } from '~/types/api'

/**
 * Who may edit what. Owner-only: the API refuses this list to anyone else, and
 * the page shows the same refusal rather than an empty table.
 */

// Editorial surface — keep out of search indexes, like every other admin page.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { list, assignRole } = usePeopleAdmin()

await refreshAdmin()

// Ordered from least to most access, so the select reads as a ladder rather
// than as an arbitrary list. ExpertReviewer is omitted: it belongs to the
// deferred Reviews module and is not an area role.
const assignableRoles: Role[] = [
  'Reader',
  'ShmoReviewer',
  'ShmoEditor',
  'LexiconReviewer',
  'LexiconEditor',
  'Owner',
]

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
                <select
                  :value="person.role"
                  :disabled="isLocked(person) || savingId === person.id"
                  :aria-label="t('admin.people.mayEdit')"
                  class="w-full max-w-[14rem] rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2 py-1.5 font-sans text-sm text-[var(--color-text)] disabled:cursor-not-allowed disabled:opacity-60"
                  @change="onRoleChange(person, ($event.target as HTMLSelectElement).value as Role)"
                >
                  <!-- ExpertReviewer is not assignable here but may already be set,
                       so keep the current value selectable rather than silently
                       showing the wrong one. -->
                  <option v-if="!assignableRoles.includes(person.role)" :value="person.role">
                    {{ t(`admin.people.role.${person.role}`) }}
                  </option>
                  <option v-for="role in assignableRoles" :key="role" :value="role">
                    {{ t(`admin.people.role.${role}`) }}
                  </option>
                </select>
                <span v-if="isLocked(person)" class="mt-1 block text-xs text-[var(--color-text-faint)]">
                  {{ t('admin.people.cannotChangeOwn') }}
                </span>
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
