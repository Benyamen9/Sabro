<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

const { t, locale, locales, setLocale } = useI18n()
const { isConfigured, isSignedIn, displayName, email, username, avatarUrl, initial, signIn, signOut }
  = useAuth()
const { variant, set: setVariant, available: scriptOptions } = useScriptVariant()
const { profile, load, persist, saveAccount, deleteAccount } = useProfile()
const { load: loadLeaderboard } = useLeaderboard()
const { hasPassword, load: loadCapabilities } = useAccountCapabilities()

// The app titleTemplate appends "— Sabro"; personal pages stay out of search.
useHead({ title: () => t('account.title') })
useSeoMeta({ robots: 'noindex, nofollow' })

// The layout loads the profile on mount, but the page can be hit directly, so
// resolve it here too. Both calls are idempotent (loaded-once guard inside).
onMounted(() => {
  load()
  loadCapabilities()
})

// --- Leaderboard opt-in editing ---------------------------------------------
// Local draft of the editable account fields. Seeded once from the profile (or
// the Logto name as a sensible default for the display name) so typing isn't
// clobbered by a later profile refresh.
const displayNameDraft = ref('')
const optIn = ref(false)
const seeded = ref(false)
const saving = ref(false)
const saveState = ref<'idle' | 'saved' | 'error'>('idle')

watchEffect(() => {
  if (seeded.value || !profile.value) return
  displayNameDraft.value = profile.value.displayName ?? displayName.value ?? ''
  optIn.value = profile.value.showOnLeaderboard
  seeded.value = true
})

const trimmedName = computed(() => displayNameDraft.value.trim())
const canOptIn = computed(() => trimmedName.value.length > 0)

// Clearing the name while opted in turns the opt-in off — you can't appear nameless.
watch(canOptIn, (ok) => {
  if (!ok) optIn.value = false
})

const accountDirty = computed(() =>
  profile.value
    ? trimmedName.value !== (profile.value.displayName ?? '') || optIn.value !== profile.value.showOnLeaderboard
    : trimmedName.value.length > 0 || optIn.value,
)

async function saveLeaderboard() {
  saving.value = true
  saveState.value = 'idle'
  const ok = await saveAccount({
    displayName: trimmedName.value.length > 0 ? trimmedName.value : null,
    showOnLeaderboard: optIn.value,
  })
  saving.value = false
  saveState.value = ok ? 'saved' : 'error'
  if (ok) await loadLeaderboard(true) // refresh the board with the new name/opt-in
}

const localeOptions = computed(() =>
  (locales.value as Array<{ code: string, name: string }>).map(l => ({ code: l.code, name: l.name })),
)

// Brief "Saved" acknowledgement after a preference change, so language/script
// edits give the same feedback as the leaderboard save (which they lacked).
const prefsSaved = ref(false)
let prefsSavedTimer: ReturnType<typeof setTimeout> | null = null

function flagPrefsSaved() {
  prefsSaved.value = true
  if (prefsSavedTimer) clearTimeout(prefsSavedTimer)
  prefsSavedTimer = setTimeout(() => (prefsSaved.value = false), 2000)
}

// Account deletion — irreversible, so gated behind a type-to-confirm step.
const confirmingDelete = ref(false)
const deleteConfirmText = ref('')
const deleting = ref(false)
const deleteError = ref(false)

const deleteConfirmWord = computed(() => t('account.delete.confirmWord'))
const canConfirmDelete = computed(() =>
  deleteConfirmText.value.trim().toLowerCase() === deleteConfirmWord.value.toLowerCase(),
)

function cancelDelete() {
  confirmingDelete.value = false
  deleteConfirmText.value = ''
  deleteError.value = false
}

async function confirmDelete() {
  if (!canConfirmDelete.value || deleting.value) return
  deleting.value = true
  deleteError.value = false
  const ok = await deleteAccount()
  if (ok) {
    // Identity is gone; clear the Logto session and leave the app.
    await signOut()
    return
  }
  deleting.value = false
  deleteError.value = true
}

async function chooseLocale(code: string) {
  if (code === locale.value) return
  await setLocale(code as typeof locale.value)
  await persist()
  flagPrefsSaved()
}

async function chooseScript(value: ScriptVariant) {
  if (value === variant.value) return
  setVariant(value)
  await persist()
  flagPrefsSaved()
}

// "Member since" from the profile's creation date — month + year is enough
// for the header's meta line.
const memberSince = computed(() => {
  const created = profile.value?.createdAt
  if (!created) return ''
  return new Intl.DateTimeFormat(locale.value, { year: 'numeric', month: 'long' })
    .format(new Date(created))
})

// --- Documentation-style side navigation -----------------------------------
// Two groups in the sidebar; the content sections below carry the matching ids
// in this same order, so the page reads top-to-bottom as the nav lists them.
const navGroups = computed(() => [
  {
    label: t('account.nav.account'),
    items: [
      { id: 'preferences', label: t('account.nav.preferences') },
    ],
  },
  {
    label: t('account.nav.meltho'),
    items: [
      { id: 'meltho', label: t('account.nav.stats') },
      { id: 'leaderboard', label: t('account.nav.leaderboard') },
    ],
  },
  {
    label: t('account.nav.security'),
    items: [
      { id: 'username', label: t('account.nav.username') },
      // Email + password changes need the current password as identity proof,
      // so they only apply to accounts that have one.
      ...(hasPassword.value
        ? [
            { id: 'email', label: t('account.nav.email') },
            { id: 'password', label: t('account.nav.password') },
          ]
        : []),
      { id: 'session', label: t('account.nav.session') },
      { id: 'delete', label: t('account.nav.delete') },
    ],
  },
])

// Flattened for the mobile nav, which drops the group headers for a single
// horizontally-scrollable pill row.
const flatNavItems = computed(() => navGroups.value.flatMap(group => group.items))

const sectionIds = ['preferences', 'meltho', 'leaderboard', 'username', 'email', 'password', 'session', 'delete']
const activeSection = ref('preferences')

function goToSection(id: string) {
  const el = document.getElementById(id)
  if (!el) return
  el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  activeSection.value = id
  history.replaceState(null, '', `#${id}`)
}

// The horizontal mobile nav scrolls to keep the active pill centred as the
// active section changes while scrolling the page — the bar tracks along.
const mobileNav = ref<HTMLElement | null>(null)

watch(activeSection, (id) => {
  const nav = mobileNav.value
  if (!nav) return
  const pill = nav.querySelector<HTMLElement>(`[data-section="${id}"]`)
  if (!pill) return
  const navRect = nav.getBoundingClientRect()
  const pillRect = pill.getBoundingClientRect()
  const delta = (pillRect.left + pillRect.width / 2) - (navRect.left + navRect.width / 2)
  nav.scrollBy({ left: delta, behavior: 'smooth' })
})

// Scroll-spy: highlight the nav item for the section currently being read.
// The reference line sits ~30% down the viewport, so a section becomes active
// once its heading reaches the upper-third reading area — this tracks timely
// while scrolling down rather than only when a heading reaches the very top.
const SPY_RATIO = 0.3
let ticking = false

function updateActiveSection() {
  if (typeof window === 'undefined') return

  const base = window.innerHeight * SPY_RATIO

  // The short trailing sections (password, session) can never scroll their tops
  // up to the base reference line — there isn't enough page below them. So as the
  // page bottoms out, let the line drift down toward the viewport bottom. This
  // gives each trailing section its own window (password, then session) instead
  // of a hard bottom-guard that would jump straight to the last one and skip
  // password.
  const distanceToBottom = document.documentElement.scrollHeight - (window.scrollY + window.innerHeight)
  const referenceLine = base + Math.max(0, window.innerHeight - base - distanceToBottom)

  // Active = the last section whose top has passed the (possibly drifted) line.
  let current = sectionIds[0] ?? 'profile'
  for (const id of sectionIds) {
    const el = document.getElementById(id)
    if (!el) continue
    if (el.getBoundingClientRect().top <= referenceLine) current = id
    else break
  }
  activeSection.value = current
}

function onScroll() {
  if (ticking) return
  ticking = true
  requestAnimationFrame(() => {
    updateActiveSection()
    ticking = false
  })
}

function setupScrollSpy() {
  if (typeof window === 'undefined') return
  window.addEventListener('scroll', onScroll, { passive: true })
  window.addEventListener('resize', onScroll, { passive: true })
  updateActiveSection()
}

function teardownScrollSpy() {
  if (typeof window === 'undefined') return
  window.removeEventListener('scroll', onScroll)
  window.removeEventListener('resize', onScroll)
}

// Sections only exist once signed in; wire the listeners when that flips.
watch(
  isSignedIn,
  async (signedIn) => {
    if (!signedIn) {
      teardownScrollSpy()
      return
    }
    await nextTick()
    setupScrollSpy()
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  teardownScrollSpy()
  if (prefsSavedTimer) clearTimeout(prefsSavedTimer)
})
</script>

<template>
  <div class="mx-auto max-w-4xl">
    <StateMessage
      v-if="!isConfigured"
      variant="failed"
      :message="t('auth.notConfigured')"
    />

    <StateMessage
      v-else-if="!isSignedIn"
      variant="unauthorized"
      :message="t('account.signInRequired')"
      :hint="t('auth.signInHint')"
      :action-label="t('auth.signIn')"
      @action="signIn"
    />

    <template v-else>
      <!-- Identity IS the page header — who you are shouldn't cost a card. -->
      <header class="mb-9 flex items-center gap-5">
        <UserAvatar :initial="initial" :src="avatarUrl" :label="displayName" size="lg" class="!size-[72px] !text-[28px]" />
        <div class="min-w-0">
          <p class="font-sans text-[11px] font-medium uppercase tracking-[0.18em] text-[var(--color-accent)]">
            {{ t('account.eyebrow') }}
          </p>
          <h1 class="mt-0.5 truncate font-serif text-[2.1rem] font-semibold leading-tight tracking-[-0.02em] text-[var(--color-text)]">
            {{ displayName || t('account.title') }}
          </h1>
          <p class="mt-1 flex flex-wrap items-baseline gap-x-3.5 gap-y-0.5 font-sans text-[13px] text-[var(--color-text-muted)]">
            <span v-if="email" class="truncate">{{ email }}</span>
            <span v-if="username">{{ username }}</span>
            <span v-if="memberSince">{{ t('account.fields.memberSince').toLowerCase() }} {{ memberSince }}</span>
          </p>
          <p class="mt-1.5 font-sans text-xs text-[var(--color-text-faint)]">
            {{ t('account.identityNote') }}
          </p>
        </div>
      </header>

      <div class="lg:grid lg:grid-cols-[190px_minmax(0,1fr)] lg:gap-x-10">
        <!-- Mobile in-page nav: a scrollable pill row (the sidebar is hidden < lg).
             Sticky below the header. The header wraps to two rows on mobile
             (~6rem tall), so it pins at top-24, not top-14 — otherwise the
             header overlapped it. -->
        <nav
          ref="mobileNav"
          class="no-scrollbar sticky top-24 z-30 mb-6 -mx-6 overflow-x-auto border-b border-[var(--color-border)] bg-[color-mix(in_oklab,var(--color-bg)_90%,transparent)] px-6 backdrop-blur lg:hidden"
          :aria-label="t('account.title')"
        >
          <div class="flex min-w-max gap-1.5 py-2.5">
            <button
              v-for="item in flatNavItems"
              :key="item.id"
              :data-section="item.id"
              type="button"
              class="whitespace-nowrap rounded-full px-3.5 py-1.5 font-sans text-sm transition-colors"
              :class="
                activeSection === item.id
                  ? 'bg-[var(--color-accent-faint)] font-medium text-[var(--color-accent)]'
                  : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
              "
              :aria-current="activeSection === item.id ? 'true' : undefined"
              @click="goToSection(item.id)"
            >{{ item.label }}</button>
          </div>
        </nav>

        <!-- Documentation-style side navigation. -->
        <aside class="hidden lg:block">
          <nav class="sticky top-20 space-y-6" :aria-label="t('account.title')">
            <div v-for="group in navGroups" :key="group.label">
              <p class="px-3 font-sans text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-text-faint)]">
                {{ group.label }}
              </p>
              <ul class="mt-2 space-y-0.5">
                <li v-for="item in group.items" :key="item.id">
                  <a
                    :href="`#${item.id}`"
                    class="block rounded-lg px-3 py-1.5 font-sans text-sm transition-colors"
                    :class="
                      activeSection === item.id
                        ? 'bg-[var(--color-accent-faint)] font-medium text-[var(--color-accent)]'
                        : 'text-[var(--color-text-muted)] hover:bg-[var(--color-bg-elevated)] hover:text-[var(--color-text)]'
                    "
                    :aria-current="activeSection === item.id ? 'true' : undefined"
                    @click.prevent="goToSection(item.id)"
                  >{{ item.label }}</a>
                </li>
              </ul>
            </div>
          </nav>
        </aside>

        <!-- Content column — sections in nav order, each an anchor target.
             Identity lives in the page header above, not in a card. -->
        <div class="min-w-0 space-y-6">
          <!-- Preferences — editable, persisted to the profile + shared cookies. -->
          <section
            id="preferences"
            class="scroll-mt-24 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
          >
            <div class="flex items-center gap-3">
              <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
                {{ t('account.preferences.heading') }}
              </h2>
              <Transition
                enter-active-class="transition-opacity duration-200"
                leave-active-class="transition-opacity duration-500"
                enter-from-class="opacity-0"
                leave-to-class="opacity-0"
              >
                <span
                  v-if="prefsSaved"
                  class="inline-flex items-center gap-1 font-sans text-xs text-[var(--color-text-muted)]"
                >
                  <svg
                    class="size-3.5 text-[var(--color-accent)]"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2.5"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    aria-hidden="true"
                  >
                    <path d="M20 6 9 17l-5-5" />
                  </svg>
                  {{ t('account.preferences.saved') }}
                </span>
              </Transition>
            </div>
            <p class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">
              {{ t('account.preferences.body') }}
            </p>

            <div class="mt-5">
              <p class="font-sans text-sm font-medium text-[var(--color-text)]">{{ t('switcher.languageLabel') }}</p>
              <div class="mt-2 flex flex-wrap gap-2">
                <button
                  v-for="option in localeOptions"
                  :key="option.code"
                  type="button"
                  class="cursor-pointer rounded-lg border px-3.5 py-1.5 font-sans text-sm transition-colors"
                  :class="
                    option.code === locale
                      ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                      : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
                  "
                  @click="chooseLocale(option.code)"
                >{{ option.name }}</button>
              </div>
            </div>

            <div class="mt-6">
              <p class="font-sans text-sm font-medium text-[var(--color-text)]">{{ t('switcher.scriptLabel') }}</p>
              <div class="mt-2 grid gap-2 sm:grid-cols-3">
                <button
                  v-for="option in scriptOptions"
                  :key="option"
                  type="button"
                  class="flex cursor-pointer flex-col items-center gap-1.5 rounded-xl border px-3 py-3 transition-colors"
                  :class="
                    option === variant
                      ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)]'
                      : 'border-[var(--color-border-strong)] hover:border-[var(--color-text-faint)]'
                  "
                  @click="chooseScript(option)"
                >
                  <SyriacText
                    text="ܫܠܡܐ"
                    :variant="option"
                    class="text-2xl leading-none text-[var(--color-text)]"
                  />
                  <span
                    class="font-sans text-xs"
                    :class="option === variant ? 'font-medium text-[var(--color-accent)]' : 'text-[var(--color-text-muted)]'"
                  >{{ t(`switcher.script.${option}`) }}</span>
                </button>
              </div>
            </div>
          </section>

          <!-- Meltho — player stats derived from the user's own results. -->
          <div id="meltho" class="scroll-mt-24">
            <MelthoStatsCard />
          </div>

          <!-- Leaderboard — opt-in controls + the board itself. -->
          <section
            id="leaderboard"
            class="scroll-mt-24 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
          >
            <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
              {{ t('account.leaderboard.optInHeading') }}
            </h2>
            <p class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">
              {{ t('account.leaderboard.optInBody') }}
            </p>

            <div class="mt-4">
              <label
                for="displayName"
                class="block font-sans text-sm font-medium text-[var(--color-text)]"
              >{{ t('account.leaderboard.displayName') }}</label>
              <input
                id="displayName"
                v-model="displayNameDraft"
                type="text"
                :maxlength="40"
                :placeholder="t('account.leaderboard.displayNamePlaceholder')"
                class="mt-1.5 w-full max-w-sm rounded-lg border border-[var(--color-border-strong)] bg-[var(--color-bg)] px-3 py-2 font-sans text-sm text-[var(--color-text)] outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[var(--color-accent-faint)]"
              >
            </div>

            <label class="mt-4 flex cursor-pointer items-start gap-3">
              <input
                v-model="optIn"
                type="checkbox"
                :disabled="!canOptIn"
                class="mt-0.5 size-4 cursor-pointer accent-[var(--color-accent)] disabled:cursor-not-allowed disabled:opacity-50"
              >
              <span class="font-sans text-sm">
                <span class="font-medium text-[var(--color-text)]">{{ t('account.leaderboard.optInToggle') }}</span>
                <span class="mt-0.5 block text-xs text-[var(--color-text-muted)]">{{ t('account.leaderboard.optInHint') }}</span>
              </span>
            </label>

            <div class="mt-4 flex items-center gap-3">
              <button
                type="button"
                :disabled="saving || !accountDirty"
                class="inline-flex cursor-pointer items-center rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                @click="saveLeaderboard"
              >
                {{ saving ? t('common.saving') : t('common.save') }}
              </button>
              <span
                v-if="saveState === 'saved'"
                class="font-sans text-xs text-[var(--color-text-muted)]"
              >{{ t('account.leaderboard.saved') }}</span>
              <span
                v-else-if="saveState === 'error'"
                class="font-sans text-xs text-[var(--color-accent)]"
              >{{ t('account.leaderboard.saveError') }}</span>
            </div>

            <div class="mt-6 border-t border-[var(--color-border)] pt-6">
              <MelthoLeaderboardCard />
            </div>
          </section>

          <!-- Username — change the Logto username via the Account API. -->
          <div id="username" class="scroll-mt-24">
            <ChangeUsernameCard />
          </div>

          <!-- Email + password both prove identity with the current password, so
               they're hidden for social-only accounts that have none. -->
          <template v-if="hasPassword">
            <!-- Email — change the primary email (code + identity) via the Account API. -->
            <div id="email" class="scroll-mt-24">
              <ChangeEmailCard />
            </div>

            <!-- Password — change the sign-in password via Logto's Account API. -->
            <div id="password" class="scroll-mt-24">
              <ChangePasswordCard />
            </div>
          </template>

          <!-- Session — account actions. -->
          <section
            id="session"
            class="scroll-mt-24 flex flex-wrap items-center justify-between gap-x-4 gap-y-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-6 py-4 shadow-[var(--shadow-soft)]"
          >
            <div>
              <p class="font-sans text-sm font-medium text-[var(--color-text)]">{{ t('account.session.heading') }}</p>
              <p class="font-sans text-xs text-[var(--color-text-muted)]">{{ t('account.session.body') }}</p>
            </div>
            <button
              type="button"
              class="inline-flex cursor-pointer items-center gap-2 rounded-md border border-[var(--color-border-strong)] px-3.5 py-2 font-sans text-sm font-medium text-[var(--color-text-muted)] transition-colors hover:border-[var(--color-text-faint)] hover:text-[var(--color-text)]"
              @click="signOut"
            >
              <svg
                class="size-4"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="1.75"
                stroke-linecap="round"
                stroke-linejoin="round"
                aria-hidden="true"
              >
                <path d="M15 17l5-5-5-5" />
                <path d="M20 12H9" />
                <path d="M9 5H6a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h3" />
              </svg>
              {{ t('auth.signOut') }}
            </button>
          </section>

          <!-- Delete account — danger zone, irreversible, gated by type-to-confirm. -->
          <section
            id="delete"
            class="scroll-mt-24 rounded-2xl border border-[color-mix(in_oklab,var(--color-accent)_35%,var(--color-border))] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
          >
            <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-accent)]">
              {{ t('account.delete.heading') }}
            </h2>
            <p class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">
              {{ t('account.delete.body') }}
            </p>

            <button
              v-if="!confirmingDelete"
              type="button"
              class="mt-4 inline-flex cursor-pointer items-center rounded-md border border-[var(--color-accent)] px-3.5 py-2 font-sans text-sm font-medium text-[var(--color-accent)] transition-colors hover:bg-[var(--color-accent-faint)]"
              @click="confirmingDelete = true"
            >
              {{ t('account.delete.start') }}
            </button>

            <div v-else class="mt-4">
              <label for="deleteConfirm" class="block font-sans text-sm text-[var(--color-text)]">
                {{ t('account.delete.confirmPrompt', { word: deleteConfirmWord }) }}
              </label>
              <input
                id="deleteConfirm"
                v-model="deleteConfirmText"
                type="text"
                autocomplete="off"
                spellcheck="false"
                class="mt-1.5 block w-full max-w-xs rounded-lg border border-[var(--color-border-strong)] bg-[var(--color-bg)] px-3 py-2 font-sans text-sm text-[var(--color-text)] outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[var(--color-accent-faint)]"
              >
              <div class="mt-4 flex flex-wrap items-center gap-3">
                <button
                  type="button"
                  :disabled="!canConfirmDelete || deleting"
                  class="inline-flex cursor-pointer items-center rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                  @click="confirmDelete"
                >
                  {{ deleting ? t('account.delete.deleting') : t('account.delete.confirmButton') }}
                </button>
                <button
                  type="button"
                  :disabled="deleting"
                  class="inline-flex cursor-pointer items-center rounded-md px-3.5 py-2 font-sans text-sm font-medium text-[var(--color-text-muted)] transition-colors hover:text-[var(--color-text)] disabled:opacity-50"
                  @click="cancelDelete"
                >
                  {{ t('common.cancel') }}
                </button>
                <span
                  v-if="deleteError"
                  class="font-sans text-xs text-[var(--color-accent)]"
                >{{ t('account.delete.error') }}</span>
              </div>
            </div>
          </section>

          <!-- The data-handling reference for the account surface above. -->
          <p class="font-sans text-xs text-[var(--color-text-faint)]">
            <NuxtLink
              to="/privacy"
              class="underline decoration-[var(--color-border-strong)] underline-offset-2 transition-colors hover:text-[var(--color-text-muted)]"
            >{{ t('privacy.title') }}</NuxtLink>
          </p>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
/* Keep the mobile nav horizontally scrollable but hide the scrollbar chrome. */
.no-scrollbar {
  scrollbar-width: none; /* Firefox */
  -ms-overflow-style: none; /* legacy Edge */
}
.no-scrollbar::-webkit-scrollbar {
  display: none; /* Chrome, Safari */
}
</style>
