<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()
const config = useRuntimeConfig()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { load: loadProfile } = useProfile()

// The hub surfaces its launched app: a link out to Meltho (its own deployment).
const melthoUrl = config.public.melthoUrl
const mnoUrl = config.public.mnoUrl

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
    <!-- The 2px accent rail on top puts the app's colour on every page; the
         bottom hairline fades toward the edges so the bar sits on the page
         rather than cutting it. Both are pseudo-elements so the glass
         background stays a single layer. -->
    <header
      class="sticky top-0 z-40 bg-[color-mix(in_oklab,var(--color-bg)_85%,transparent)] backdrop-blur-md backdrop-saturate-150 before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-[var(--color-accent)] after:absolute after:inset-x-0 after:bottom-0 after:h-px after:bg-[linear-gradient(to_right,transparent,var(--color-border-strong)_14%,var(--color-border-strong)_86%,transparent)]"
    >
      <!-- On mobile this wraps into two balanced rows: brand + controls on top,
           nav + the Meltho link below. The basis-full spacer forces that break.
           From sm: everything sits on one row (brand · nav · ··· · Meltho · controls). -->
      <div class="mx-auto flex min-h-14 max-w-6xl flex-wrap items-center gap-x-3 gap-y-2.5 px-4 py-2.5 sm:flex-nowrap sm:gap-x-6 sm:gap-y-0 sm:px-6 sm:py-0">
        <NuxtLink
          to="/"
          class="group order-1 flex shrink-0 items-center gap-2 font-sans text-base font-semibold tracking-tight no-underline"
        >
          <span
            aria-hidden="true"
            class="flex size-6 items-center justify-center rounded-[7px] bg-[linear-gradient(180deg,color-mix(in_oklab,var(--color-accent)_86%,white)_0%,var(--color-accent)_55%)] font-syriac text-[0.95rem] leading-none text-white shadow-[var(--shadow-soft),inset_0_1px_0_rgb(255_255_255/0.28)]"
          >ܣ</span>
          {{ t('site.title') }}
          <!-- The word the name transliterates, set like a dictionary gloss. -->
          <span
            aria-hidden="true"
            dir="rtl"
            class="font-syriac text-lg font-normal leading-none text-[var(--color-text-faint)] transition-colors group-hover:text-[var(--color-accent)]"
          >ܣܒܪܐ</span>
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
          class="order-5 ml-auto inline-flex items-center gap-1.5 rounded-full bg-[var(--color-meltho-faint)] px-3 py-1 font-sans text-xs font-medium text-[var(--color-meltho)] no-underline transition-colors hover:bg-[color-mix(in_oklab,var(--color-meltho-faint)_70%,var(--color-meltho)_14%)] sm:order-3"
        >
          <!-- Meltho's own mini badge, so the pill shows where it goes. -->
          <span
            aria-hidden="true"
            class="flex size-4 items-center justify-center rounded-[5px] bg-[var(--color-meltho)] font-syriac text-[0.66rem] leading-none text-white"
          >ܡ</span>
          {{ t('nav.meltho') }}
        </a>

        <a
          :href="mnoUrl"
          class="order-6 inline-flex items-center gap-1.5 rounded-full bg-[var(--color-mno-faint)] px-3 py-1 font-sans text-xs font-medium text-[var(--color-mno)] no-underline transition-colors hover:bg-[color-mix(in_oklab,var(--color-mno-faint)_70%,var(--color-mno)_14%)] sm:order-4"
        >
          <!-- Mno's badge glyph: ܐ݈, the numeral 1000. -->
          <span
            aria-hidden="true"
            dir="rtl"
            class="flex size-4 items-center justify-center rounded-[5px] bg-[var(--color-mno)] font-syriac text-[0.66rem] leading-none text-white"
          >ܐ݈</span>
          {{ t('nav.mno') }}
        </a>

        <span aria-hidden="true" class="order-7 hidden h-5 w-px bg-[var(--color-border)] sm:order-5 sm:block" />

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
        <span class="font-serif italic text-[var(--color-text-muted)]">{{ t('site.promise') }}</span>
      </div>
      <div class="mx-auto mt-2 max-w-6xl px-6">
        <a
          href="https://sedra.bethmardutho.org"
          target="_blank"
          rel="noopener noreferrer"
          class="no-underline hover:text-[var(--color-text-muted)]"
        >{{ t('site.attribution') }}</a>
      </div>
    </footer>
  </div>
</template>
