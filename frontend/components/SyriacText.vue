<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

interface Props {
  text: string
  variant?: ScriptVariant
}

const props = defineProps<Props>()

const { variant: userVariant } = useScriptVariant()

const effectiveVariant = computed<ScriptVariant>(() => props.variant ?? userVariant.value)

// Each variant maps to a distinct Google-hosted Noto Syriac family (loaded in
// nuxt.config head). The earlier Beth Mardutho names (Serto Jerusalem, East
// Syriac Adiabene) were never loaded, so every variant silently fell back to
// one font and the switcher had no visible effect.
const fontFamily = computed(() => {
  switch (effectiveVariant.value) {
    case 'serto':
      return '"Noto Sans Syriac Western", "Noto Sans Syriac", serif'
    case 'madnhaya':
      return '"Noto Sans Syriac Eastern", "Noto Sans Syriac", serif'
    case 'estrangela':
    default:
      return '"Noto Sans Syriac", serif'
  }
})
</script>

<template>
  <span
    dir="rtl"
    lang="syc"
    class="syriac-text inline-block align-baseline"
    :style="{ fontFamily }"
  >{{ text }}</span>
</template>

<style scoped>
.syriac-text {
  font-size: 1.15em;
  line-height: 1.6;
  unicode-bidi: isolate;
  font-feature-settings: "kern", "liga";
}
</style>
