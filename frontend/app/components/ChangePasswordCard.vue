<script setup lang="ts">
const { t } = useI18n()
const { submitting, changePassword } = useChangePassword()

const current = ref('')
const next = ref('')
const confirm = ref('')

// Per-field reveal state for the show/hide toggles.
const visible = reactive({ current: false, next: false, confirm: false })

// `null` = idle, '' = success, otherwise an error reason code for i18n.
const result = ref<string | null>(null)

const canSubmit = computed(() =>
  !submitting.value && current.value.length > 0 && next.value.length > 0 && confirm.value.length > 0,
)

async function submit() {
  result.value = null
  const outcome = await changePassword({ current: current.value, next: next.value, confirm: confirm.value })
  if (outcome.ok) {
    result.value = ''
    current.value = ''
    next.value = ''
    confirm.value = ''
  }
  else {
    result.value = outcome.reason ?? 'unavailable'
  }
}

const inputClass
  = 'block w-full rounded-lg border border-[var(--color-border-strong)] bg-[var(--color-bg)] py-2 pl-3 pr-10 font-sans text-sm text-[var(--color-text)] outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[var(--color-accent-faint)]'

const toggleClass
  = 'absolute inset-y-0 right-0 flex cursor-pointer items-center px-3 text-[var(--color-text-faint)] transition-colors hover:text-[var(--color-text)]'
</script>

<template>
  <section
    class="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
  >
    <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
      {{ t('account.security.heading') }}
    </h2>
    <p class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">
      {{ t('account.security.body') }}
    </p>

    <form class="mt-5 space-y-4" @submit.prevent="submit">
      <div>
        <label for="currentPassword" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.security.current') }}
        </label>
        <div class="relative mt-1.5 max-w-sm">
          <input
            id="currentPassword"
            v-model="current"
            :type="visible.current ? 'text' : 'password'"
            autocomplete="current-password"
            :class="inputClass"
          >
          <button
            type="button"
            :aria-label="visible.current ? t('account.security.hide') : t('account.security.show')"
            :aria-pressed="visible.current"
            :class="toggleClass"
            @click="visible.current = !visible.current"
          >
            <PasswordVisibilityIcon :visible="visible.current" />
          </button>
        </div>
      </div>

      <div>
        <label for="newPassword" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.security.new') }}
        </label>
        <div class="relative mt-1.5 max-w-sm">
          <input
            id="newPassword"
            v-model="next"
            :type="visible.next ? 'text' : 'password'"
            autocomplete="new-password"
            :class="inputClass"
          >
          <button
            type="button"
            :aria-label="visible.next ? t('account.security.hide') : t('account.security.show')"
            :aria-pressed="visible.next"
            :class="toggleClass"
            @click="visible.next = !visible.next"
          >
            <PasswordVisibilityIcon :visible="visible.next" />
          </button>
        </div>
        <p class="mt-1 font-sans text-xs text-[var(--color-text-faint)]">{{ t('account.security.hint') }}</p>
      </div>

      <div>
        <label for="confirmPassword" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.security.confirm') }}
        </label>
        <div class="relative mt-1.5 max-w-sm">
          <input
            id="confirmPassword"
            v-model="confirm"
            :type="visible.confirm ? 'text' : 'password'"
            autocomplete="new-password"
            :class="inputClass"
          >
          <button
            type="button"
            :aria-label="visible.confirm ? t('account.security.hide') : t('account.security.show')"
            :aria-pressed="visible.confirm"
            :class="toggleClass"
            @click="visible.confirm = !visible.confirm"
          >
            <PasswordVisibilityIcon :visible="visible.confirm" />
          </button>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <button
          type="submit"
          :disabled="!canSubmit"
          class="inline-flex cursor-pointer items-center rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {{ submitting ? t('common.saving') : t('account.security.save') }}
        </button>
        <span
          v-if="result === ''"
          class="font-sans text-xs text-[var(--color-text-muted)]"
        >{{ t('account.security.success') }}</span>
        <span
          v-else-if="result"
          class="font-sans text-xs text-[var(--color-accent)]"
        >{{ t(`account.security.errors.${result}`) }}</span>
      </div>
    </form>
  </section>
</template>
