<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()
const config = useRuntimeConfig()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { load: loadProfile } = useProfile()

// The hub surfaces its launched app: a link out to Meltho (its own deployment).
const melthoUrl = config.public.melthoUrl

// Resolve admin status once on mount so the backoffice link only appears for
// editors. The API enforces the admin scope regardless of what we show here.
// Also adopt the signed-in user's saved language + script-variant preferences
// so they follow the account across devices.
onMounted(() => {
  refreshAdmin()
  loadProfile()
})

const navItems = computed(() => {
  // The Meltho word library is public. Translations are deferred post-launch; the
  // lexicon CRUD link is gated on the admin scope from Logto.
  const items: { to: string, labelKey: string }[] = [
    { to: '/library', labelKey: 'nav.library' },
  ]
  if (isAdmin.value) {
    items.push({ to: '/admin/lexicon', labelKey: 'nav.admin' })
  }
  return items
})

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
      <!-- On mobile this wraps into two balanced rows: brand + controls on top,
           nav + the Meltho link below. The basis-full spacer forces that break.
           From sm: everything sits on one row (brand · nav · ··· · Meltho · controls). -->
      <div class="mx-auto flex min-h-14 max-w-6xl flex-wrap items-center gap-x-3 gap-y-2.5 px-4 py-2.5 sm:flex-nowrap sm:gap-x-6 sm:gap-y-0 sm:px-6 sm:py-0">
        <NuxtLink
          to="/"
          class="order-1 flex shrink-0 items-center gap-2 font-sans text-base font-semibold tracking-tight no-underline"
        >
          <span
            aria-hidden="true"
            class="flex size-6 items-center justify-center rounded-md bg-[var(--color-accent)] font-syriac text-[0.95rem] leading-none text-white"
          >ܣ</span>
          {{ t('site.title') }}
        </NuxtLink>

        <nav class="order-4 flex items-center gap-1 sm:order-2" :aria-label="t('nav.primary')">
          <NuxtLink
            v-for="item in navItems"
            :key="item.to"
            :to="item.to"
            class="relative px-3 py-1.5 font-sans text-sm no-underline transition-colors"
            :class="
              isActive(item.to)
                ? 'text-[var(--color-text)] after:absolute after:inset-x-3 after:-bottom-px after:h-0.5 after:rounded-full after:bg-[var(--color-accent)]'
                : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
            "
          >{{ t(item.labelKey) }}</NuxtLink>
        </nav>

        <a
          :href="melthoUrl"
          class="order-5 ml-auto inline-flex items-center gap-1 rounded-full bg-[var(--color-meltho-faint)] px-3 py-1 font-sans text-xs font-medium text-[var(--color-meltho)] no-underline transition-colors hover:bg-[color-mix(in_oklab,var(--color-meltho-faint)_70%,var(--color-meltho)_14%)] sm:order-3"
        >
          {{ t('nav.meltho') }}
          <span aria-hidden="true">→</span>
        </a>

        <span aria-hidden="true" class="order-6 hidden h-5 w-px bg-[var(--color-border)] sm:order-4 sm:block" />

        <div class="order-2 ml-auto flex items-center gap-1.5 sm:order-5 sm:ml-0 sm:gap-2">
          <ScriptVariantSwitcher />
          <LanguageSwitcher />
          <UserMenu />
        </div>

        <!-- Mobile-only line break: pushes nav + Meltho onto a second row. -->
        <div aria-hidden="true" class="order-3 basis-full sm:hidden" />
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
