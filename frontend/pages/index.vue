<script setup lang="ts">
const { t } = useI18n()
</script>

<template>
  <main>
    <h1>{{ t('site.title') }}</h1>
    <p>{{ t('site.tagline') }}</p>
  </main>
</template>
