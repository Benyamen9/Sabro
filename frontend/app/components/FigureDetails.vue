<script setup lang="ts">
import type { GalleryFigure } from '~/utils/gallery'
import { pickDescription } from '~/utils/gallery'
import { formatEra } from '~/utils/historicalFigures'

/**
 * What the player knows about a figure: the line on how they met it, the
 * attributes Shmo showed them, and, once found, the story. Shared by the
 * computer's side page and the back of the phone's lifted card.
 */
const props = withDefaults(defineProps<{ figure: GalleryFigure, showLink?: boolean }>(), { showLink: true })

const { t, locale } = useI18n()

const story = computed(() => pickDescription(props.figure.descriptions, locale.value))

const progress = computed(() => {
  const f = props.figure
  const met = f.timesMet === 1 ? t('bethSalme.detail.metOnce') : t('bethSalme.detail.metTimes', { count: f.timesMet })
  if (f.tier === 'found' && f.firstFoundOn) {
    const date = new Intl.DateTimeFormat(locale.value, { day: 'numeric', month: 'long', year: 'numeric' })
      .format(new Date(`${f.firstFoundOn}T00:00:00Z`))
    return `${t('bethSalme.detail.foundOn', { date, count: f.fewestAttempts ?? 0 })} · ${met}`
  }
  return `${met} · ${t('bethSalme.detail.notFoundYet')}`
})

const attributes = computed(() => {
  const f = props.figure
  return [
    { key: 'century', label: t('bethSalme.detail.century'), value: formatEra(f.era, t) },
    { key: 'period', label: t('admin.historicalFigures.form.period'), value: t(`admin.historicalFigures.period.${f.period}`) },
    { key: 'category', label: t('admin.historicalFigures.form.category'), value: t(`admin.historicalFigures.category.${f.category}`) },
    { key: 'role', label: t('admin.historicalFigures.form.role'), value: t(`admin.historicalFigures.role.${f.role}`) },
    { key: 'region', label: t('admin.historicalFigures.form.region'), value: t(`admin.historicalFigures.region.${f.region}`) },
    {
      key: 'tradition',
      label: t('admin.historicalFigures.form.tradition'),
      value: f.tradition ? t(`admin.historicalFigures.tradition.${f.tradition}`) : '—',
    },
  ]
})
</script>

<template>
  <div class="font-sans">
    <p class="text-[13px] text-[var(--color-text-muted)]">{{ progress }}</p>
    <dl class="mt-3 grid grid-cols-2 gap-x-4 gap-y-2.5 border-t border-[var(--color-border)] pt-3">
      <div v-for="item in attributes" :key="item.key" class="min-w-0">
        <dt class="text-[10px] font-semibold uppercase tracking-[0.1em] text-[var(--color-text-faint)]">{{ item.label }}</dt>
        <dd class="mt-0.5 text-[13.5px] text-[var(--color-text)]">{{ item.value }}</dd>
      </div>
    </dl>
    <div class="mt-4 border-t border-[var(--color-border)] pt-3 font-serif text-[15px] leading-relaxed">
      <template v-if="figure.tier === 'found'">
        <p v-if="story" class="text-[var(--color-text)]">{{ story }}</p>
        <p v-else class="italic text-[var(--color-text-faint)]">{{ t('bethSalme.detail.noStory') }}</p>
      </template>
      <p v-else class="text-[var(--color-text-muted)]">{{ t('bethSalme.detail.findToRead', { name: figure.name }) }}</p>
    </div>
    <a
      v-if="showLink && figure.tier === 'found' && figure.readMoreUrl"
      :href="figure.readMoreUrl"
      target="_blank"
      rel="noopener"
      class="mt-3 inline-block text-[13.5px] font-medium text-[var(--color-shmo)] no-underline hover:underline"
    >{{ t('bethSalme.detail.readMore') }} →</a>
  </div>
</template>
