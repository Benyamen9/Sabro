<script setup lang="ts">
type ScriptVariant = 'estrangela' | 'serto' | 'madnhaya'

interface Props {
  text: string
  variant?: ScriptVariant
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'estrangela',
})

const fontFamily = computed(() => {
  switch (props.variant) {
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
    class="syriac-text"
    :style="{ fontFamily }"
  >{{ text }}</span>
</template>

<style scoped>
.syriac-text {
  font-size: 1.15em;
  line-height: 1.7;
  unicode-bidi: isolate;
}
</style>
