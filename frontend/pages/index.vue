<script setup lang="ts">
const { t } = useI18n()
</script>

<template>
  <section class="mx-auto max-w-3xl py-12 sm:py-20">
    <p class="mb-6 font-sans text-xs font-medium uppercase tracking-[0.18em] text-[var(--color-accent)]">
      {{ t('home.eyebrow') }}
    </p>

    <h1 class="font-serif text-4xl leading-[1.1] sm:text-5xl md:text-6xl">
      <span class="block italic">{{ t('home.titleLine1') }}</span>
      <span class="block">{{ t('home.titleLine2') }}</span>
    </h1>

    <p class="mt-8 max-w-2xl font-serif text-lg leading-relaxed text-[var(--color-text-muted)] sm:text-xl">
      {{ t('home.lede') }}
    </p>

    <figure class="my-14 border-l-2 border-[var(--color-accent)] pl-6">
      <blockquote class="font-serif">
        <SyriacText text="ܫܠܡܐ ܠܟ" class="!text-4xl sm:!text-5xl" />
      </blockquote>
      <figcaption class="mt-3 font-sans text-xs uppercase tracking-wider text-[var(--color-text-faint)]">
        {{ t('home.greetingCaption') }}
      </figcaption>
    </figure>

    <div class="flex flex-wrap gap-3">
      <NuxtLink
        to="/translations"
        class="inline-flex items-center gap-2 rounded-md bg-[var(--color-accent)] px-5 py-2.5 font-sans text-sm font-medium text-white no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-accent-hover)]"
      >
        {{ t('home.ctaTranslations') }}
        <span aria-hidden="true">→</span>
      </NuxtLink>
      <NuxtLink
        to="/lexicon"
        class="inline-flex items-center gap-2 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-5 py-2.5 font-sans text-sm font-medium no-underline transition-colors hover:bg-[var(--color-bg-subtle)]"
      >
        {{ t('home.ctaLexicon') }}
      </NuxtLink>
    </div>

    <hr class="my-16 border-t border-[var(--color-border)]">

    <div class="grid gap-10 sm:grid-cols-2">
      <article>
        <h2 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('home.aboutHeading') }}
        </h2>
        <p class="mt-3 font-serif text-base leading-relaxed">
          {{ t('home.aboutBody') }}
        </p>
      </article>
      <article>
        <h2 class="font-sans text-sm font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t('home.statusHeading') }}
        </h2>
        <p class="mt-3 font-serif text-base leading-relaxed">
          {{ t('home.statusBody') }}
        </p>
      </article>
    </div>
  </section>
</template>
