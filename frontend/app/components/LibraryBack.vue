<script setup lang="ts">
/**
 * The way back from a book to the Library shelf (Owner, 2026-11-15): a chevron like a back button,
 * just before the book's name at the top of its page (size 2 of the three he was shown). It takes
 * the colour of the line it stands in, the book's own. The negative margins keep the line's height:
 * the chevron is larger than the small capitals beside it.
 */
const { t } = useI18n()
</script>

<template>
  <NuxtLink
    to="/library"
    :aria-label="t('library.shelf.back')"
    :title="t('library.shelf.back')"
    class="library-back"
  ><span aria-hidden="true">‹</span></NuxtLink>
</template>

<style scoped>
.library-back {
  display: inline-grid;
  place-items: center;
  width: 1.9rem;
  height: 2.25rem;
  margin-block: -0.75rem;
  margin-left: -0.5rem;
  margin-right: 0.1rem;
  padding-bottom: 0.25rem;
  box-sizing: border-box;
  vertical-align: middle;
  border-radius: 0.5rem;
  font-family: var(--font-serif);
  font-size: 2.1rem;
  font-weight: 400;
  line-height: 1;
  letter-spacing: 0;
  color: inherit;
  text-decoration: none;
  transition: background-color 0.15s ease;
}

.library-back:hover,
.library-back:focus-visible {
  background: color-mix(in oklab, currentColor 10%, transparent);
}
</style>
