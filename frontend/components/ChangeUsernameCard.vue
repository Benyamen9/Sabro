<script setup lang="ts">
const { t } = useI18n()
const { username } = useAuth()
const { submitting, changeUsername } = useChangeUsername()

// Seed the field from the current username so the user edits in place.
const value = ref(username.value)
watch(username, (next) => {
  if (!touched.value) value.value = next
})
const touched = ref(false)

// `null` = idle, '' = success, otherwise an error reason code for i18n.
const result = ref<string | null>(null)

const localError = computed(() => validateUsername(value.value, username.value))
const canSubmit = computed(() => !submitting.value && localError.value === null)

async function submit() {
  result.value = null
  if (localError.value) {
    result.value = localError.value
    return
  }
  const outcome = await changeUsername(value.value)
  if (outcome.ok) {
    result.value = ''
    // Reload so the server-rendered Logto session reflects the new username.
    setTimeout(() => window.location.reload(), 900)
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
      {{ t('account.username.heading') }}
    </h2>
    <p class="mt-1 font-sans text-sm text-[var(--color-text-muted)]">
      {{ t('account.username.body') }}
    </p>

    <form class="mt-5" @submit.prevent="submit">
      <label for="newUsername" class="block font-sans text-sm font-medium text-[var(--color-text)]">
        {{ t('account.username.label') }}
      </label>
      <input
        id="newUsername"
        v-model="value"
        type="text"
        autocomplete="username"
        spellcheck="false"
        :class="inputClass"
        @input="touched = true"
      >
      <p class="mt-1 font-sans text-xs text-[var(--color-text-faint)]">{{ t('account.username.hint') }}</p>

      <div class="mt-4 flex items-center gap-3">
        <button
          type="submit"
          :disabled="!canSubmit"
          class="inline-flex cursor-pointer items-center rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {{ submitting ? t('common.saving') : t('account.username.save') }}
        </button>
        <span
          v-if="result === ''"
          class="font-sans text-xs text-[var(--color-text-muted)]"
        >{{ t('account.username.success') }}</span>
        <span
          v-else-if="result"
          class="font-sans text-xs text-[var(--color-accent)]"
        >{{ t(`account.username.errors.${result}`) }}</span>
      </div>
    </form>
  </section>
</template>
