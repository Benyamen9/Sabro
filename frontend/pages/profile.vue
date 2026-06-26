<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

const { t, locale, locales, setLocale } = useI18n()
const { isConfigured, isSignedIn, displayName, email, username, avatarUrl, initial, signIn, signOut }
  = useAuth()
const { variant, set: setVariant, available: scriptOptions } = useScriptVariant()
const { profile, load, persist, saveAccount } = useProfile()
const { load: loadLeaderboard } = useLeaderboard()

useHead({ title: () => `${t('account.title')} · ${t('site.title')}` })

// The layout loads the profile on mount, but the page can be hit directly, so
// resolve it here too. Both calls are idempotent (loaded-once guard inside).
onMounted(load)

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

async function chooseLocale(code: string) {
  if (code === locale.value) return
  await setLocale(code as typeof locale.value)
  await persist()
}

function chooseScript(value: ScriptVariant) {
  if (value === variant.value) return
  setVariant(value)
  persist()
}

const role = computed(() => profile.value?.role ?? 'Reader')

// "Member since" from the profile's creation date, formatted in the active locale.
const memberSince = computed(() => {
  const created = profile.value?.createdAt
  if (!created) return ''
  return new Intl.DateTimeFormat(locale.value, { year: 'numeric', month: 'long', day: 'numeric' })
    .format(new Date(created))
})
</script>

<template>
  <div class="mx-auto max-w-2xl">
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
      <header class="mb-8">
        <p class="font-sans text-xs font-medium uppercase tracking-[0.18em] text-[var(--color-accent)]">
          {{ t('account.eyebrow') }}
        </p>
        <h1 class="mt-1.5 font-serif text-3xl font-semibold tracking-tight text-[var(--color-text)]">
          {{ t('account.title') }}
        </h1>
      </header>

      <!-- Identity — read-only, sourced from the sign-in provider (Logto). -->
      <section
        class="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
      >
        <div class="flex items-center gap-4">
          <UserAvatar :initial="initial" :src="avatarUrl" :label="displayName" size="lg" />
          <div class="min-w-0">
            <p class="truncate font-sans text-lg font-semibold text-[var(--color-text)]">{{ displayName }}</p>
            <p
              v-if="email"
              class="truncate font-sans text-sm text-[var(--color-text-muted)]"
            >{{ email }}</p>
            <span
              class="mt-1.5 inline-flex items-center rounded-full bg-[var(--color-accent-faint)] px-2.5 py-0.5 font-sans text-xs font-medium text-[var(--color-accent)]"
            >{{ t(`account.role.${role}`) }}</span>
          </div>
        </div>

        <dl class="mt-6 grid gap-px overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-border)] sm:grid-cols-2">
          <div v-if="username" class="bg-[var(--color-bg-elevated)] px-4 py-3">
            <dt class="font-sans text-xs text-[var(--color-text-muted)]">{{ t('account.fields.username') }}</dt>
            <dd class="mt-0.5 font-sans text-sm text-[var(--color-text)]">{{ username }}</dd>
          </div>
          <div v-if="email" class="bg-[var(--color-bg-elevated)] px-4 py-3">
            <dt class="font-sans text-xs text-[var(--color-text-muted)]">{{ t('account.fields.email') }}</dt>
            <dd class="mt-0.5 truncate font-sans text-sm text-[var(--color-text)]">{{ email }}</dd>
          </div>
          <div v-if="memberSince" class="bg-[var(--color-bg-elevated)] px-4 py-3">
            <dt class="font-sans text-xs text-[var(--color-text-muted)]">{{ t('account.fields.memberSince') }}</dt>
            <dd class="mt-0.5 font-sans text-sm text-[var(--color-text)]">{{ memberSince }}</dd>
          </div>
        </dl>

        <p class="mt-4 font-sans text-xs text-[var(--color-text-faint)]">
          {{ t('account.identityNote') }}
        </p>
      </section>

      <!-- Player stats — derived from the user's own Meltho results. -->
      <div class="mt-6">
        <MelthoStatsCard />
      </div>

      <!-- Preferences — editable, persisted to the profile + shared cookies. -->
      <section
        class="mt-6 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
      >
        <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
          {{ t('account.preferences.heading') }}
        </h2>
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

      <!-- Leaderboard: opt-in controls + the board itself. -->
      <section
        class="mt-6 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
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
            class="font-sans text-sm font-medium text-[var(--color-text)]"
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

      <!-- Account actions. -->
      <section class="mt-6 flex items-center justify-between rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-6 py-4 shadow-[var(--shadow-soft)]">
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
    </template>
  </div>
</template>
