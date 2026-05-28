<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

interface Props {
  text: string
  variant?: ScriptVariant
}

const props = defineProps<Props>()

const { variant: userVariant } = useScriptVariant()

const effectiveVariant = computed<ScriptVariant>(() => props.variant ?? userVariant.value)

const fontFamily = computed(() => {
  switch (effectiveVariant.value) {
    case 'serto':
      return '"Serto Jerusalem", "Noto Sans Syriac", serif'
    case 'madnhaya':
      return '"East Syriac Adiabene", "Noto Sans Syriac", serif'
    case 'estrangela':
    default:
      return '"Estrangelo Edessa", "Noto Sans Syriac", serif'
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
