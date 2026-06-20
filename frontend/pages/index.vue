<script setup lang="ts">
const { t } = useI18n()
const melthoUrl = useRuntimeConfig().public.melthoUrl
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

    <!-- Introduces Meltho, the first ecosystem app, with a direct link to play. -->
    <div
      class="mt-10 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-7 shadow-[var(--shadow-soft)] sm:flex sm:items-center sm:justify-between sm:gap-6"
    >
      <div class="flex items-center gap-5">
        <SyriacText text="ܡܠܬܐ" class="!text-4xl text-[var(--color-accent)]" />
        <div>
          <h2 class="font-serif text-2xl">{{ t('home.meltho.heading') }}</h2>
          <p class="mt-1.5 max-w-md font-serif text-[var(--color-text-muted)]">
            {{ t('home.meltho.body') }}
          </p>
        </div>
      </div>
      <a
        :href="melthoUrl"
        class="mt-5 inline-block flex-none rounded-lg bg-[var(--color-accent)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline transition-colors hover:bg-[var(--color-accent-hover)] sm:mt-0"
      >{{ t('home.meltho.cta') }} →</a>
    </div>

    <figure class="my-14 border-l-2 border-[var(--color-accent)] pl-6">
      <blockquote class="font-serif">
        <SyriacText text="ܫܠܡܐ ܠܟ" class="!text-4xl sm:!text-5xl" />
      </blockquote>
      <figcaption class="mt-3 font-sans text-xs uppercase tracking-wider text-[var(--color-text-faint)]">
        {{ t('home.greetingCaption') }}
      </figcaption>
    </figure>

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
