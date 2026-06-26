<script setup lang="ts">
const { t } = useI18n()
const { submitting, changePassword } = useChangePassword()

const current = ref('')
const next = ref('')
const confirm = ref('')

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
  = 'mt-1.5 block w-full max-w-sm rounded-lg border border-[var(--color-border-strong)] bg-[var(--color-bg)] px-3 py-2 font-sans text-sm text-[var(--color-text)] outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[var(--color-accent-faint)]'
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
        <input
          id="currentPassword"
          v-model="current"
          type="password"
          autocomplete="current-password"
          :class="inputClass"
        >
      </div>

      <div>
        <label for="newPassword" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.security.new') }}
        </label>
        <input
          id="newPassword"
          v-model="next"
          type="password"
          autocomplete="new-password"
          :class="inputClass"
        >
        <p class="mt-1 font-sans text-xs text-[var(--color-text-faint)]">{{ t('account.security.hint') }}</p>
      </div>

      <div>
        <label for="confirmPassword" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.security.confirm') }}
        </label>
        <input
          id="confirmPassword"
          v-model="confirm"
          type="password"
          autocomplete="new-password"
          :class="inputClass"
        >
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
