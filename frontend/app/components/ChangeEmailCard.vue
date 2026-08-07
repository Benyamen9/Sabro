<script setup lang="ts">
const { t } = useI18n()
const { email: currentEmail } = useAuth()
const { submitting, requestCode, confirmEmail } = useChangeEmail()

const email = ref('')
const code = ref('')
const currentPassword = ref('')

const codeSent = ref(false)
const recordId = ref('')
const verificationId = ref('')

// `null` = idle, '' = success, otherwise an error reason code for i18n.
const result = ref<string | null>(null)

const canSendCode = computed(() => !submitting.value && isValidEmail(email.value))
const canConfirm = computed(() =>
  !submitting.value && code.value.trim().length > 0 && currentPassword.value.length > 0,
)

async function sendCode() {
  result.value = null
  const outcome = await requestCode(email.value)
  if (outcome.ok) {
    recordId.value = outcome.recordId!
    verificationId.value = outcome.verificationId!
    codeSent.value = true
  }
  else {
    result.value = outcome.reason ?? 'unavailable'
  }
}

async function confirm() {
  result.value = null
  const outcome = await confirmEmail({
    email: email.value,
    code: code.value,
    recordId: recordId.value,
    verificationId: verificationId.value,
    currentPassword: currentPassword.value,
  })
  if (outcome.ok) {
    result.value = ''
    // Reload so the server-rendered Logto session reflects the new email.
    setTimeout(() => window.location.reload(), 900)
  }
  else {
    result.value = outcome.reason ?? 'unavailable'
  }
}

function restart() {
  codeSent.value = false
  code.value = ''
  currentPassword.value = ''
  recordId.value = ''
  verificationId.value = ''
  result.value = null
}

const inputClass
  = 'mt-1.5 block w-full max-w-sm rounded-lg border border-[var(--color-border-strong)] bg-[var(--color-bg)] px-3 py-2 font-sans text-sm text-[var(--color-text)] outline-none focus:border-[var(--color-accent)] focus:ring-2 focus:ring-[var(--color-accent-faint)]'
</script>

<template>
  <section
    class="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
  >
    <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
      {{ t('account.email.heading') }}
    </h2>
    <p class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">
      {{ currentEmail ? t('account.email.bodyCurrent', { email: currentEmail }) : t('account.email.body') }}
    </p>

    <!-- Step 1 — enter the new address and request a code. -->
    <form v-if="!codeSent" class="mt-5" @submit.prevent="sendCode">
      <label for="newEmail" class="block font-sans text-sm font-medium text-[var(--color-text)]">
        {{ t('account.email.label') }}
      </label>
      <input
        id="newEmail"
        v-model="email"
        type="email"
        autocomplete="email"
        spellcheck="false"
        :class="inputClass"
      >

      <div class="mt-4 flex items-center gap-3">
        <button
          type="submit"
          :disabled="!canSendCode"
          class="inline-flex cursor-pointer items-center rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {{ submitting ? t('account.email.sending') : t('account.email.sendCode') }}
        </button>
        <span
          v-if="result"
          class="font-sans text-xs text-[var(--color-accent)]"
        >{{ t(`account.email.errors.${result}`) }}</span>
      </div>
    </form>

    <!-- Step 2 — enter the emailed code and the current password. -->
    <form v-else class="mt-5" @submit.prevent="confirm">
      <p class="font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('account.email.sentTo', { email }) }}
      </p>

      <div class="mt-4">
        <label for="emailCode" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.email.codeLabel') }}
        </label>
        <input
          id="emailCode"
          v-model="code"
          type="text"
          inputmode="numeric"
          autocomplete="one-time-code"
          :class="inputClass"
        >
      </div>

      <div class="mt-4">
        <label for="emailPassword" class="block font-sans text-sm font-medium text-[var(--color-text)]">
          {{ t('account.email.passwordLabel') }}
        </label>
        <input
          id="emailPassword"
          v-model="currentPassword"
          type="password"
          autocomplete="current-password"
          :class="inputClass"
        >
      </div>

      <div class="mt-4 flex flex-wrap items-center gap-3">
        <button
          type="submit"
          :disabled="!canConfirm"
          class="inline-flex cursor-pointer items-center rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {{ submitting ? t('common.saving') : t('account.email.confirm') }}
        </button>
        <button
          type="button"
          :disabled="submitting"
          class="inline-flex cursor-pointer items-center rounded-md px-3.5 py-2 font-sans text-sm font-medium text-[var(--color-text-muted)] transition-colors hover:text-[var(--color-text)] disabled:opacity-50"
          @click="restart"
        >
          {{ t('account.email.useDifferent') }}
        </button>
        <span
          v-if="result === ''"
          class="font-sans text-xs text-[var(--color-text-muted)]"
        >{{ t('account.email.success') }}</span>
        <span
          v-else-if="result"
          class="font-sans text-xs text-[var(--color-accent)]"
        >{{ t(`account.email.errors.${result}`) }}</span>
      </div>
    </form>
  </section>
</template>
