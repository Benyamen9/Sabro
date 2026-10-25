<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { load: loadProfile } = useProfile()


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
    { to: '/beth-salme', labelKey: 'nav.bethSalme' },
  ]
  // One entry, not one per area: the hub at /admin is the way in, and the
  // switcher above each section carries you between them. Adding a module used
  // to mean adding another top-level link here.
  if (isAdmin.value) {
    items.push({ to: '/admin', labelKey: 'nav.admin' })
  }
  return items
})

// The same page links, for the launcher panel on phones.
const launcherLinks = computed(() => navItems.value.map(item => ({ to: item.to, label: t(item.labelKey) })))

function isActive(path: string) {
  if (path === '/') return route.path === '/'
  return route.path === path || route.path.startsWith(`${path}/`)
}
</script>

<template>
  <div class="flex min-h-screen flex-col bg-[var(--color-bg)] text-[var(--color-text)]">
    <!-- The 2px accent rail on top puts the app's colour on every page; the
         bottom hairline fades toward the edges so the bar sits on the page
         rather than cutting it. Both are pseudo-elements so the glass
         background stays a single layer. -->
    <header
      class="sticky top-0 z-40 bg-[color-mix(in_oklab,var(--color-bg)_85%,transparent)] backdrop-blur-md backdrop-saturate-150 before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-[var(--color-accent)] after:absolute after:inset-x-0 after:bottom-0 after:h-px after:bg-[linear-gradient(to_right,transparent,var(--color-border-strong)_14%,var(--color-border-strong)_86%,transparent)]"
    >
      <!-- One line at every width (owner, 2026-10-13): the brand and the nav on
           the left; on the right the app launcher — which replaced the row of
           game pills that used to wrap onto a second line — then script,
           language, theme and the account. `relative` anchors the launcher's
           panel to this row's right edge. -->
      <div class="relative mx-auto flex min-h-14 max-w-6xl flex-nowrap items-center gap-x-2 px-3 py-2.5 sm:gap-x-6 sm:px-4 lg:px-6 lg:py-0">
        <NuxtLink
          to="/"
          :aria-label="t('site.title')"
          class="group flex shrink-0 items-center gap-2 font-sans text-base font-semibold tracking-tight no-underline"
        >
          <span
            aria-hidden="true"
            class="badge-swim flex size-6 items-center justify-center rounded-[7px] bg-[linear-gradient(180deg,color-mix(in_oklab,var(--color-accent)_86%,white)_0%,var(--color-accent)_55%)] font-syriac text-[0.95rem] leading-none text-white shadow-[var(--shadow-soft),inset_0_1px_0_rgb(255_255_255/0.28)] [--swim-rgb1:75,58,48] [--swim-rgb2:212,175,55] [--swim-a1:0.55] [--swim-a2:0.22]"
          ><span>ܣ</span></span>
          <!-- On the very narrowest phones the badge alone carries the brand. -->
          <span class="hidden min-[340px]:inline">{{ t('site.title') }}</span>
          <!-- The word the name transliterates, set like a dictionary gloss. -->
          <span
            aria-hidden="true"
            dir="rtl"
            class="hidden font-syriac text-lg font-normal leading-none text-[var(--color-text-faint)] min-[400px]:inline transition-colors group-hover:text-[var(--color-accent)]"
          >ܣܒܪܐ</span>
        </NuxtLink>
        <!-- Below sm these links live in the launcher panel instead (see AppLauncher). -->
        <nav class="hidden min-w-0 items-center gap-1 sm:flex" :aria-label="t('nav.primary')">
          <NuxtLink
            v-for="item in navItems"
            :key="item.to"
            :to="item.to"
            class="relative whitespace-nowrap px-3 py-1.5 font-sans text-sm no-underline transition-colors"
            :class="
              isActive(item.to)
                ? 'text-[var(--color-text)] after:absolute after:inset-x-3 after:-bottom-px after:h-0.5 after:rounded-full after:bg-[var(--color-accent)]'
                : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
            "
          >{{ t(item.labelKey) }}</NuxtLink>
        </nav>
        <div class="ml-auto flex shrink-0 items-center gap-1 sm:gap-1.5 lg:gap-2">
          <AppLauncher :links="launcherLinks" />
          <ScriptVariantSwitcher />
          <LanguageSwitcher />
          <ThemeSwitcher />
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
        <span class="font-serif italic text-[var(--color-text-muted)]">{{ t('site.promise') }}</span>
      </div>
    </footer>
  </div>
</template>
