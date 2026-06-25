<script setup lang="ts">
// The chosen Syriac script drives --font-syriac at the root, so the override
// cascades to every Syriac surface — including chrome like the nav brand mark
// and the script-switcher icon — not just <SyriacText>.
const { fontFamily } = useScriptVariant()
</script>

<template>
  <div :style="{ '--font-syriac': fontFamily }">
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </div>
</template>
