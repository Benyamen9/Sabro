<script setup lang="ts">
// The app launcher (owner, 2026-10-13): a 3×3 grid button that opens a panel
// with Sabro and every game, Sabro marked as here. It replaces the row of game
// pills, so the header no longer wraps them onto a second line. The same
// component lives in all five game clients; keep the copies alike.
//
// The hub's own header also has page links (Library, and Backoffice for
// editors). On phones there is no room for them beside the tools (owner,
// 2026-10-15), so they move here, as lines under the apps — `links`, shown
// below `sm` only; from `sm` up they stay in the header.
defineProps<{ links?: { to: string, label: string }[] }>()

const { t } = useI18n()
const config = useRuntimeConfig().public

const apps = computed(() =>
  ECOSYSTEM_APPS.map(app => ({
    ...app,
    href: app.urlKey ? (config[app.urlKey] as string) : null,
    what: t(`launcher.what.${app.id}`),
  })),
)

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
</script>

<template>
  <!-- Not `relative`: the panel is placed against the header row (which is),
       so it opens flush with the header's right edge and never runs off a
       phone's left edge, wherever this button sits in the row. -->
  <div ref="root" class="inline-flex">
    <button
      type="button"
      class="flex size-8 cursor-pointer items-center justify-center rounded-md border border-[var(--color-border)] bg-[var(--color-bg-elevated)] text-[var(--color-text-muted)] transition-colors hover:text-[var(--color-text)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-accent-faint)]"
      :class="{ '!text-[var(--color-text)]': open }"
      :aria-label="t('launcher.label')"
      :aria-expanded="open"
      aria-controls="app-launcher"
      @click="open = !open"
    >
      <svg class="size-4" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <rect x="3" y="3" width="4.5" height="4.5" rx="1.2" /><rect x="9.75" y="3" width="4.5" height="4.5" rx="1.2" /><rect x="16.5" y="3" width="4.5" height="4.5" rx="1.2" />
        <rect x="3" y="9.75" width="4.5" height="4.5" rx="1.2" /><rect x="9.75" y="9.75" width="4.5" height="4.5" rx="1.2" /><rect x="16.5" y="9.75" width="4.5" height="4.5" rx="1.2" />
        <rect x="3" y="16.5" width="4.5" height="4.5" rx="1.2" /><rect x="9.75" y="16.5" width="4.5" height="4.5" rx="1.2" /><rect x="16.5" y="16.5" width="4.5" height="4.5" rx="1.2" />
      </svg>
    </button>

    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0 -translate-y-1"
      leave-active-class="transition duration-100 ease-in"
      leave-to-class="opacity-0 -translate-y-1"
    >
      <nav
        v-if="open"
        id="app-launcher"
        :aria-label="t('launcher.label')"
        class="absolute right-4 top-full z-50 mt-1.5 grid w-[min(17rem,calc(100%-2rem))] grid-cols-3 gap-1 rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-2 shadow-[0_12px_32px_-12px_rgba(28,25,23,0.28),0_2px_6px_-2px_rgba(28,25,23,0.1)] lg:right-6"
      >
        <template v-for="app in apps" :key="app.id">
          <span
            v-if="!app.href"
            aria-current="page"
            class="grid justify-items-center gap-1 rounded-xl border border-[var(--color-border)] bg-[var(--color-accent-faint)] px-1 pb-2 pt-3 font-sans"
          >
            <span class="launcher-badge font-syriac" :style="{ background: app.color }" aria-hidden="true">{{ app.badge }}</span>
            <span class="text-[0.84rem] font-semibold">{{ app.name }}</span>
            <span class="-mt-0.5 text-[0.7rem] text-[var(--color-text-muted)]">{{ app.what }}</span>
          </span>
          <a
            v-else
            :href="app.href"
            class="grid justify-items-center gap-1 rounded-xl border border-transparent px-1 pb-2 pt-3 font-sans text-[var(--color-text)] no-underline transition-colors hover:bg-[var(--color-accent-faint)]"
          >
            <span class="launcher-badge font-syriac" :style="{ background: app.color }" aria-hidden="true">{{ app.badge }}</span>
            <span class="text-[0.84rem] font-semibold">{{ app.name }}</span>
            <span class="-mt-0.5 text-[0.7rem] text-[var(--color-text-muted)]">{{ app.what }}</span>
          </a>
        </template>
        <div v-if="links?.length" class="col-span-3 mt-1 grid border-t border-[var(--color-border)] pt-1 sm:hidden">
          <NuxtLink
            v-for="link in links"
            :key="link.to"
            :to="link.to"
            class="flex items-center justify-between rounded-lg px-2.5 py-2 font-sans text-[0.84rem] font-semibold text-[var(--color-accent)] no-underline hover:bg-[var(--color-accent-faint)]"
            @click="close"
          >{{ link.label }}<span aria-hidden="true">→</span></NuxtLink>
        </div>
      </nav>
    </Transition>
  </div>
</template>

<style scoped>
.launcher-badge {
  display: grid;
  place-items: center;
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 11px;
  color: #fff;
  font-size: 1.3rem;
  line-height: 1;
  white-space: nowrap;
  box-shadow: var(--shadow-soft), inset 0 1px 0 rgb(255 255 255 / 0.25);
}
</style>
