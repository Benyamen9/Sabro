<script setup lang="ts">
/**
 * The backoffice hub: one door per section, in that section's colour, echoing
 * the game doors on the home page. Replaces having a separate top-level nav
 * entry per area — the header now carries a single "Backoffice" link here.
 */

// Editorial surface — keep out of search indexes, like every other admin page.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { refresh: refreshAccess } = useMyAccess()
const { visibleSections } = useAdminSections()

await refreshAdmin()
await refreshAccess()
</script>

<template>
  <section>
    <header class="mb-8">
      <p class="mb-2 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="font-serif text-3xl font-semibold tracking-[-0.015em]">
        {{ t('admin.hub.title') }}
      </h1>
      <p class="mt-2 max-w-prose font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('admin.hub.subtitle') }}
      </p>
    </header>

    <StateMessage
      v-if="isAdmin === false"
      variant="unauthorized"
      :message="t('admin.adminRequired')"
      :hint="t('admin.adminRequiredHint')"
    />

    <!-- Staff, but granted nothing yet. Saying so is kinder than an empty page,
         which reads as a broken backoffice rather than as access still to come. -->
    <StateMessage
      v-else-if="isAdmin && visibleSections.length === 0"
      variant="unauthorized"
      :message="t('admin.hub.noAreas')"
      :hint="t('admin.hub.noAreasHint')"
    />

    <div v-else-if="isAdmin" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <NuxtLink
        v-for="section in visibleSections"
        :key="section.to"
        :to="section.to"
        class="group relative block overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-5 no-underline transition-shadow hover:shadow-[var(--shadow-soft)]"
      >
        <!-- Colour crown, the same device the home page's game cards use. -->
        <span
          class="absolute inset-x-0 top-0 h-[3px]"
          :style="{ backgroundColor: `var(${section.accent})` }"
          aria-hidden="true"
        />
        <h2 class="mt-1 font-serif text-xl font-semibold text-[var(--color-text)]">
          {{ t(section.labelKey) }}
        </h2>
        <p class="mt-1.5 font-sans text-sm text-[var(--color-text-muted)]">
          {{ t(section.blurbKey) }}
        </p>
      </NuxtLink>
    </div>
  </section>
</template>
