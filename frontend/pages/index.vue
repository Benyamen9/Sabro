<script setup lang="ts">
const { t } = useI18n()
</script>

<template>
  <section>
    <h1>{{ t('site.title') }}</h1>
    <p>{{ t('site.tagline') }}</p>
    <p class="sample">
      <SyriacText text="ܫܠܡܐ ܠܟ" />
    </p>
  </section>
</template>

<style scoped>
.sample {
  margin-top: 2rem;
  font-size: 1.5rem;
}
</style>
