<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'
import { fontForVariant } from '~/composables/useScriptVariant'

interface Props {
  text: string
  variant?: ScriptVariant
}

const props = defineProps<Props>()

const { variant: userVariant } = useScriptVariant()

const effectiveVariant = computed<ScriptVariant>(() => props.variant ?? userVariant.value)

// Resolve the variant's font from the shared map. An explicit `variant` prop
// overrides the user's global choice for this one instance.
const fontFamily = computed(() => fontForVariant(effectiveVariant.value))
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
