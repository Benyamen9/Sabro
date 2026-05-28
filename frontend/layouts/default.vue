<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()

const navItems = computed(() => [
  { to: '/translations', labelKey: 'nav.translations' },
  { to: '/lexicon', labelKey: 'nav.lexicon' },
])

function isActive(path: string) {
  if (path === '/') return route.path === '/'
  return route.path === path || route.path.startsWith(`${path}/`)
}
</script>

<template>
  <div class="flex min-h-screen flex-col bg-[var(--color-bg)] text-[var(--color-text)]">
    <header
      class="sticky top-0 z-40 border-b border-[var(--color-border)] bg-[color-mix(in_oklab,var(--color-bg)_85%,transparent)] backdrop-blur-md backdrop-saturate-150"
    >
      <div class="mx-auto flex h-14 max-w-6xl items-center gap-6 px-6">
        <NuxtLink
          to="/"
          class="font-sans text-base font-semibold tracking-tight no-underline"
        >
          {{ t('site.title') }}
        </NuxtLink>

        <nav class="flex items-center gap-1" :aria-label="t('nav.primary')">
          <NuxtLink
            v-for="item in navItems"
            :key="item.to"
            :to="item.to"
            class="rounded-md px-3 py-1.5 font-sans text-sm no-underline transition-colors"
            :class="
              isActive(item.to)
                ? 'bg-[var(--color-bg-subtle)] text-[var(--color-text)]'
                : 'text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)] hover:text-[var(--color-text)]'
            "
          >{{ t(item.labelKey) }}</NuxtLink>
        </nav>

        <div class="ml-auto flex items-center gap-2">
          <ScriptVariantSwitcher />
          <LanguageSwitcher />
          <UserMenu />
        </div>
      </div>
    </header>

    <main class="mx-auto w-full max-w-6xl flex-1 px-6 py-12">
      <slot />
    </main>

    <footer class="border-t border-[var(--color-border)] py-6 font-sans text-xs text-[var(--color-text-faint)]">
      <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-2 px-6">
        <span>{{ t('site.title') }} — {{ t('site.tagline') }}</span>
        <span>{{ t('site.statusMvp') }}</span>
      </div>
    </footer>
  </div>
</template>
