<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { SuggestedEditDto } from '~/types/api'

/**
 * The Owner's review queue. Each row is one reviewer's proposed value for one
 * field, shown as before → after so the decision can be made from this page
 * rather than by going and looking the entry up.
 *
 * Accepting records a decision only; the entry is edited on its own page. The
 * link appears once a proposal is accepted, which is the cue to go apply it.
 */

useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { list, accept, reject, editLinkFor } = useProposals()

await refreshAdmin()
// Not for this page's own gate — the API decides that — but so the section
// switcher above it knows which other sections to offer.
await useMyAccess().refresh()

const proposals = ref<SuggestedEditDto[]>([])
const viewState = ref<'loading' | 'ready' | 'unauthorized' | 'failed'>('loading')
const busyId = ref<string | null>(null)
const errorMessage = ref<string | null>(null)

/**
 * Proposals the server refused because the field changed since it was filed.
 * Holding them here is what turns the second click into a deliberate override
 * rather than a repeat of the first.
 */
const conflicted = ref<Set<string>>(new Set())

const showDecided = ref(false)

async function load() {
  viewState.value = 'loading'
  errorMessage.value = null
  conflicted.value = new Set()
  try {
    const result = await list(showDecided.value ? 'Accepted' : 'Pending')
    proposals.value = result.items
    viewState.value = 'ready'
  }
  catch (error) {
    const status = (error as FetchError).statusCode
    viewState.value = status === 401 || status === 403 ? 'unauthorized' : 'failed'
  }
}

async function onAccept(proposal: SuggestedEditDto) {
  const override = conflicted.value.has(proposal.id)
  busyId.value = proposal.id
  errorMessage.value = null
  try {
    const updated = await accept(proposal.id, undefined, override)
    replace(updated)
    conflicted.value.delete(proposal.id)
  }
  catch (error) {
    const fetchError = error as FetchError
    // 409 means the field moved while this waited. Do not retry automatically:
    // the whole point is that taking an older correction over a newer edit is a
    // decision somebody makes, not one a client makes on their behalf.
    if (fetchError.statusCode === 409) {
      conflicted.value = new Set(conflicted.value).add(proposal.id)
      errorMessage.value = (fetchError.data?.detail as string | undefined)
        ?? t('admin.proposals.changedUnderneath')
    }
    else {
      errorMessage.value = (fetchError.data?.detail as string | undefined)
        ?? t('admin.proposals.decisionFailed')
    }
  }
  finally {
    busyId.value = null
  }
}

async function onReject(proposal: SuggestedEditDto) {
  busyId.value = proposal.id
  errorMessage.value = null
  try {
    replace(await reject(proposal.id))
  }
  catch (error) {
    errorMessage.value = ((error as FetchError).data?.detail as string | undefined)
      ?? t('admin.proposals.decisionFailed')
  }
  finally {
    busyId.value = null
  }
}

/** A decided proposal leaves the pending list; keep it visible when browsing decided ones. */
function replace(updated: SuggestedEditDto) {
  if (!showDecided.value && updated.status !== 'Pending') {
    proposals.value = proposals.value.filter(p => p.id !== updated.id)
    return
  }
  const index = proposals.value.findIndex(p => p.id === updated.id)
  if (index !== -1) proposals.value[index] = updated
}

function toggleDecided() {
  showDecided.value = !showDecided.value
  return load()
}

/** An empty original is a real state — the proposal fills a gap rather than corrects one. */
function originalOf(proposal: SuggestedEditDto) {
  return proposal.originalValue || null
}

onMounted(load)

const cellClass = 'px-3 py-3 align-top'
</script>

<template>
  <section>
    <AdminSectionNav />

    <header class="mb-8">
      <p class="mb-2 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="font-serif text-3xl font-semibold tracking-[-0.015em]">{{ t('admin.proposals.title') }}</h1>
      <p class="mt-2 max-w-prose font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('admin.proposals.subtitle') }}
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
      :message="t('admin.proposals.ownerRequired')"
      :hint="t('admin.proposals.ownerRequiredHint')"
    />
    <StateMessage
      v-else-if="viewState === 'failed'"
      variant="failed"
      :message="t('admin.proposals.loadFailed')"
      @retry="load"
    />

    <div v-else>
      <div class="mb-4 flex items-center justify-between gap-4">
        <button
          type="button"
          class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 font-sans text-sm text-[var(--color-text)]"
          @click="toggleDecided"
        >
          {{ showDecided ? t('admin.proposals.showPending') : t('admin.proposals.showAccepted') }}
        </button>
      </div>

      <p
        v-if="errorMessage"
        class="mb-4 rounded-md border border-[var(--color-accent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
        role="alert"
      >{{ errorMessage }}</p>

      <p
        v-if="proposals.length === 0"
        class="rounded-md border border-[var(--color-border)] bg-[var(--color-bg-subtle)] px-4 py-6 text-center font-sans text-sm text-[var(--color-text-muted)]"
      >{{ showDecided ? t('admin.proposals.noneAccepted') : t('admin.proposals.nonePending') }}</p>

      <div v-else class="overflow-x-auto">
        <table class="w-full min-w-[44rem] border-collapse font-sans text-sm">
          <thead>
            <tr class="border-b border-[var(--color-border)] text-left">
              <th :class="[cellClass, 'text-xs font-semibold uppercase tracking-wider text-[var(--color-text-faint)]']">
                {{ t('admin.proposals.what') }}
              </th>
              <th :class="[cellClass, 'text-xs font-semibold uppercase tracking-wider text-[var(--color-text-faint)]']">
                {{ t('admin.proposals.change') }}
              </th>
              <th :class="[cellClass, 'text-xs font-semibold uppercase tracking-wider text-[var(--color-text-faint)]']">
                {{ t('admin.proposals.decide') }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="proposal in proposals"
              :key="proposal.id"
              class="border-b border-[var(--color-border)]"
            >
              <td :class="cellClass">
                <span class="font-medium text-[var(--color-text)]">
                  {{ t(`admin.proposals.target.${proposal.targetType}`) }}
                </span>
                <code class="mt-0.5 block text-xs text-[var(--color-text-muted)]">{{ proposal.field }}</code>
                <span class="mt-1 block text-xs text-[var(--color-text-faint)]">
                  {{ new Date(proposal.createdAt).toLocaleDateString() }}
                </span>
              </td>

              <td :class="cellClass">
                <!-- before → after. An absent "before" is a real state: the
                     proposal fills a gap rather than correcting a value. -->
                <div class="max-w-prose">
                  <p v-if="originalOf(proposal)" class="text-[var(--color-text-muted)] line-through">
                    {{ originalOf(proposal) }}
                  </p>
                  <p v-else class="italic text-[var(--color-text-faint)]">
                    {{ t('admin.proposals.wasEmpty') }}
                  </p>
                  <p class="mt-1 font-medium text-[var(--color-text)]">{{ proposal.proposedContent }}</p>
                  <p v-if="proposal.rationale" class="mt-2 text-xs text-[var(--color-text-muted)]">
                    {{ proposal.rationale }}
                  </p>
                  <p
                    v-if="proposal.acceptedDespiteChange"
                    class="mt-2 text-xs text-[var(--color-accent)]"
                  >{{ t('admin.proposals.acceptedDespiteChange') }}</p>
                </div>
              </td>

              <td :class="cellClass">
                <div v-if="proposal.status === 'Pending'" class="flex flex-wrap items-center gap-2">
                  <button
                    type="button"
                    :disabled="busyId === proposal.id"
                    class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 text-sm disabled:opacity-60"
                    @click="onAccept(proposal)"
                  >
                    <!-- The label changes after a refusal, so the second click is
                         visibly a different act from the first. -->
                    {{ conflicted.has(proposal.id) ? t('admin.proposals.acceptAnyway') : t('admin.proposals.accept') }}
                  </button>
                  <button
                    type="button"
                    :disabled="busyId === proposal.id"
                    class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 text-sm disabled:opacity-60"
                    @click="onReject(proposal)"
                  >
                    {{ t('admin.proposals.reject') }}
                  </button>
                  <p
                    v-if="conflicted.has(proposal.id)"
                    class="w-full text-xs text-[var(--color-accent)]"
                  >{{ t('admin.proposals.changedUnderneathHint') }}</p>
                </div>

                <div v-else>
                  <p class="text-[var(--color-text-muted)]">
                    {{ t(`admin.proposals.status.${proposal.status}`) }}
                  </p>
                  <!-- Accepting decides; applying is a separate act on the entry
                       itself. This link is the cue to go and do it. -->
                  <NuxtLink
                    v-if="proposal.status === 'Accepted' && editLinkFor(proposal.targetType, proposal.targetId)"
                    :to="`${editLinkFor(proposal.targetType, proposal.targetId)}?proposal=${proposal.id}`"
                    class="mt-1 inline-block text-xs underline"
                  >{{ t('admin.proposals.applyOnEntry') }}</NuxtLink>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <p class="mt-5 max-w-prose font-sans text-xs text-[var(--color-text-faint)]">
        {{ t('admin.proposals.acceptRecordsOnly') }}
      </p>
    </div>
  </section>
</template>
