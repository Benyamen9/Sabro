<script setup lang="ts">
/**
 * One trail, one way back.
 *
 * Detail pages used to carry two back-links pointing at different places: the
 * section switcher's "← Backoffice" and the page's own "← Back to list". Two
 * arrows side by side is a coin toss, not navigation. This replaces both — the
 * switcher keeps the sideways moves, this owns going back up.
 *
 * `current` is the leaf. It is plain text rather than a link because you are
 * already there, and it is optional: a section's index page ends its trail at
 * the section.
 */
const props = withDefaults(
  defineProps<{
    /** i18n key for the section this page belongs to. */
    sectionKey: string
    /** Route of that section's index page. */
    sectionTo: string
    /** The current item, when the page is inside a section rather than its index. */
    current?: string | null
    /** Render the leaf as Syriac: right-to-left, in the Syriac face. */
    currentIsSyriac?: boolean
  }>(),
  { current: null, currentIsSyriac: false },
)

const { t } = useI18n()

const linkClass
  = 'no-underline text-[var(--color-text-muted)] transition-colors hover:text-[var(--color-text)] focus-visible:text-[var(--color-text)]'
</script>

<template>
  <nav
    :aria-label="t('admin.breadcrumb.label')"
    class="mb-6 flex flex-wrap items-center gap-x-2 gap-y-1 font-sans text-sm text-[var(--color-text-muted)]"
  >
    <NuxtLink to="/admin" :class="linkClass">{{ t('admin.sections.hub') }}</NuxtLink>

    <span aria-hidden="true" class="text-[var(--color-text-faint)]">›</span>

    <!-- The section is a link even on its own index page: it costs nothing and
         keeps the trail the same shape everywhere. -->
    <NuxtLink
      :to="props.sectionTo"
      :class="props.current ? linkClass : 'no-underline font-medium text-[var(--color-text)]'"
      :aria-current="props.current ? undefined : 'page'"
    >{{ t(props.sectionKey) }}</NuxtLink>

    <template v-if="props.current">
      <span aria-hidden="true" class="text-[var(--color-text-faint)]">›</span>
      <SyriacText
        v-if="props.currentIsSyriac"
        :text="props.current"
        aria-current="page"
        class="font-medium text-[var(--color-text)]"
      />
      <span v-else aria-current="page" class="font-medium text-[var(--color-text)]">{{ props.current }}</span>
    </template>
  </nav>
</template>
