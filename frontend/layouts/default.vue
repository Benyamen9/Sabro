<script setup lang="ts">
const { t } = useI18n()

const navItems = computed(() => [
  { to: '/', labelKey: 'nav.home' },
  { to: '/translations', labelKey: 'nav.translations' },
  { to: '/lexicon', labelKey: 'nav.lexicon' },
])
</script>

<template>
  <div class="app">
    <header class="app-header">
      <div class="app-header__inner">
        <NuxtLink to="/" class="app-header__brand">{{ t('site.title') }}</NuxtLink>
        <nav class="app-header__nav" :aria-label="t('nav.primary')">
          <NuxtLink
            v-for="item in navItems"
            :key="item.to"
            :to="item.to"
            class="app-header__link"
          >{{ t(item.labelKey) }}</NuxtLink>
        </nav>
        <div class="app-header__switchers">
          <ScriptVariantSwitcher />
          <LanguageSwitcher />
          <UserMenu />
        </div>
      </div>
    </header>
    <main class="app-main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  border-bottom: 1px solid #e5e5e5;
  padding: 0.75rem 1rem;
}

.app-header__inner {
  max-width: 1100px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.app-header__brand {
  font-size: 1.25rem;
  font-weight: 600;
  text-decoration: none;
  color: inherit;
}

.app-header__nav {
  display: flex;
  gap: 1rem;
  flex: 1;
}

.app-header__link {
  text-decoration: none;
  color: inherit;
}

.app-header__link:hover {
  text-decoration: underline;
}

.app-header__switchers {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.app-main {
  flex: 1;
  max-width: 1100px;
  margin: 0 auto;
  padding: 1.5rem 1rem;
  width: 100%;
  box-sizing: border-box;
}
</style>
