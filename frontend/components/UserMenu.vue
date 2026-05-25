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
  <div class="user-menu">
    <span v-if="!isConfigured" class="user-menu__notice">
      {{ t('auth.notConfigured') }}
    </span>
    <button
      v-else-if="!isSignedIn"
      type="button"
      class="user-menu__button"
      @click="signIn"
    >
      {{ t('auth.signIn') }}
    </button>
    <template v-else>
      <span class="user-menu__name" :title="t('auth.signedInAs', { name: displayName })">
        {{ displayName }}
      </span>
      <button
        type="button"
        class="user-menu__button"
        @click="signOut"
      >
        {{ t('auth.signOut') }}
      </button>
    </template>
  </div>
</template>

<style scoped>
.user-menu {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.user-menu__notice {
  font-size: 0.75rem;
  color: #888;
  font-style: italic;
}

.user-menu__name {
  font-size: 0.875rem;
  color: #333;
  max-width: 12rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.user-menu__button {
  padding: 0.3rem 0.7rem;
  font-size: 0.875rem;
  border: 1px solid #d0d0d0;
  border-radius: 4px;
  background: white;
  cursor: pointer;
}

.user-menu__button:hover {
  background: #f6f6f6;
}
</style>
