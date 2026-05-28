<script setup lang="ts">
const { t } = useI18n()
const { isConfigured, isSignedIn, user, signIn, signOut } = useAuth()

const displayName = computed(() => {
  const u = user.value
  if (!u) return ''
  return (u.name as string) || (u.username as string) || (u.sub as string)
})
</script>

<template>
  <div class="flex items-center gap-2">
    <span
      v-if="!isConfigured"
      class="font-sans text-xs italic text-[var(--color-text-faint)]"
      :title="t('auth.notConfigured')"
    >
      {{ t('auth.notConfigured') }}
    </span>
    <button
      v-else-if="!isSignedIn"
      type="button"
      class="cursor-pointer rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-1.5 font-sans text-xs font-medium transition-colors hover:bg-[var(--color-bg-subtle)]"
      @click="signIn"
    >
      {{ t('auth.signIn') }}
    </button>
    <template v-else>
      <span
        class="max-w-[12rem] truncate font-sans text-xs text-[var(--color-text-muted)]"
        :title="t('auth.signedInAs', { name: displayName })"
      >
        {{ displayName }}
      </span>
      <button
        type="button"
        class="cursor-pointer rounded-md border border-[var(--color-border)] px-3 py-1.5 font-sans text-xs font-medium text-[var(--color-text-muted)] transition-colors hover:border-[var(--color-border-strong)] hover:text-[var(--color-text)]"
        @click="signOut"
      >
        {{ t('auth.signOut') }}
      </button>
    </template>
  </div>
</template>
