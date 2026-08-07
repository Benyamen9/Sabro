<script setup lang="ts">
/**
 * The switcher that sits above every backoffice section.
 *
 * The hub at `/admin` is the way in, but landing in a section should not be a
 * dead end — this keeps every other section one click away, without spending a
 * permanent sidebar on it.
 *
 * It used to carry a "← Backoffice" link too, which put two back-links on every
 * detail page pointing at different places. Going back up now belongs to
 * `AdminBreadcrumb`; this owns sideways moves only, and the two devices no
 * longer overlap.
 *
 * The current section is marked with `aria-current="page"` rather than colour
 * alone, so the distinction survives for a screen reader and in high contrast.
 */
const { t } = useI18n()
const { visibleSections, isCurrent } = useAdminSections()
</script>

<template>
  <nav
    :aria-label="t('admin.sections.navLabel')"
    class="mb-8 flex flex-wrap items-center gap-x-1 gap-y-2 border-b border-[var(--color-border)] pb-3"
  >
    <NuxtLink
      v-for="section in visibleSections"
      :key="section.to"
      :to="section.to"
      :aria-current="isCurrent(section.to) ? 'page' : undefined"
      class="inline-flex items-center gap-2 rounded-md px-2.5 py-1.5 font-sans text-sm no-underline transition-colors"
      :class="isCurrent(section.to)
        ? 'bg-[var(--color-bg-subtle)] font-semibold text-[var(--color-text)]'
        : 'text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)] hover:text-[var(--color-text)]'"
    >
      <!-- The colour rail is decorative: it echoes the hub's doors so a section
           is recognisable at a glance, but never carries meaning on its own. -->
      <span
        class="h-3.5 w-[3px] rounded-full"
        :style="{ backgroundColor: `var(${section.accent})` }"
        aria-hidden="true"
      />
      {{ t(section.labelKey) }}
    </NuxtLink>
  </nav>
</template>
