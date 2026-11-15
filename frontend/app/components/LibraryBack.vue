<script setup lang="ts">
/**
 * The way back from a book to the Library shelf (Owner, 2026-11-15): an arrow just before the
 * book's name at the top of its page. It takes the colour of the line it stands in.
 */
const { t } = useI18n()
</script>

<template>
  <NuxtLink
    to="/library"
    :aria-label="t('library.shelf.back')"
    :title="t('library.shelf.back')"
    class="-my-2 -ml-2 mr-0.5 inline-block rounded-md px-2 py-2 text-[15px] leading-none tracking-normal text-inherit no-underline opacity-70 transition-opacity hover:opacity-100 focus-visible:opacity-100"
  ><span aria-hidden="true">←</span></NuxtLink>
</template>
