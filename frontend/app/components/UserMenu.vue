<script setup lang="ts">
const { t } = useI18n()
const localePath = useLocalePath()
const { isConfigured, isSignedIn, displayName, email, avatarUrl, initial, signIn, signOut }
  = useAuth()

// Dropdown open state, with close-on-outside-click and close-on-Escape. The
// avatar button mirrors the icon-button look of the script/language switchers.
const open = ref(false)
const root = ref<HTMLElement | null>(null)

function close() {
  open.value = false
}

function onPointerDown(event: PointerEvent) {
  if (root.value && !root.value.contains(event.target as Node)) close()
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') close()
}

onMounted(() => {
  document.addEventListener('pointerdown', onPointerDown)
  document.addEventListener('keydown', onKeydown)
})
onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onPointerDown)
  document.removeEventListener('keydown', onKeydown)
})

// Close the menu after navigating to the profile page.
const route = useRoute()
watch(() => route.fullPath, close)
</script>

<template>
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

  <div v-else ref="root" class="relative">
    <button
      type="button"
      class="flex cursor-pointer items-center rounded-full ring-offset-2 ring-offset-[var(--color-bg)] transition-shadow hover:ring-2 hover:ring-[var(--color-accent-faint)] focus-visible:ring-2 focus-visible:ring-[var(--color-accent-faint)] focus-visible:outline-none"
      :class="open ? 'ring-2 ring-[var(--color-accent-faint)]' : ''"
      :aria-label="t('account.menu')"
      :title="displayName"
      aria-haspopup="menu"
      :aria-expanded="open"
      @click="open = !open"
    >
      <UserAvatar :initial="initial" :src="avatarUrl" :label="displayName" size="sm" />
    </button>

    <Transition
      enter-active-class="transition duration-100 ease-out"
      enter-from-class="opacity-0 -translate-y-1"
      leave-active-class="transition duration-75 ease-in"
      leave-to-class="opacity-0 -translate-y-1"
    >
      <div
        v-if="open"
        role="menu"
        class="absolute right-0 top-full z-50 mt-2 w-60 overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] shadow-[var(--shadow-soft)]"
      >
        <div class="flex items-center gap-3 border-b border-[var(--color-border)] px-4 py-3">
          <UserAvatar :initial="initial" :src="avatarUrl" :label="displayName" size="sm" />
          <div class="min-w-0">
            <p class="truncate font-sans text-sm font-medium text-[var(--color-text)]">{{ displayName }}</p>
            <p
              v-if="email"
              class="truncate font-sans text-xs text-[var(--color-text-muted)]"
            >{{ email }}</p>
          </div>
        </div>

        <NuxtLink
          :to="localePath('/profile')"
          role="menuitem"
          class="flex items-center gap-2.5 px-4 py-2.5 font-sans text-sm text-[var(--color-text)] no-underline transition-colors hover:bg-[var(--color-bg-subtle)]"
          @click="close"
        >
          <svg
            class="size-4 text-[var(--color-text-muted)]"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.75"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
          >
            <circle cx="12" cy="8" r="3.5" />
            <path d="M5 20a7 7 0 0 1 14 0" />
          </svg>
          {{ t('account.profileSettings') }}
        </NuxtLink>

        <button
          type="button"
          role="menuitem"
          class="flex w-full cursor-pointer items-center gap-2.5 border-t border-[var(--color-border)] px-4 py-2.5 text-left font-sans text-sm text-[var(--color-text-muted)] transition-colors hover:bg-[var(--color-bg-subtle)] hover:text-[var(--color-text)]"
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
      </div>
    </Transition>
  </div>
</template>
