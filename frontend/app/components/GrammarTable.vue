<script setup lang="ts">
import { startsPlural, type GrammarTable } from '~/data/grammar'

/**
 * One conjugation table as the grammar book sets it out: read right to left, the persons down the
 * right-hand side, the tense dots in red (see GrammarWord).
 */
defineProps<{ table: GrammarTable, caption: string }>()

const { t } = useI18n()
</script>

<template>
  <div class="overflow-x-auto">
    <table class="grammar-table w-full border-collapse" dir="rtl">
      <caption class="sr-only">{{ caption }}</caption>
      <thead v-if="table.columns.length > 1">
        <tr>
          <td class="w-[30%] sm:w-[24%]" />
          <th v-for="column in table.columns" :key="column" scope="col" dir="ltr" class="font-sans text-[11.5px] font-semibold uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
            {{ t(`grammar.columns.${column}`) }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in table.rows" :key="row.person" :class="{ 'plural-start': startsPlural(row.person) }">
          <th scope="row" class="w-[30%] bg-[var(--color-bg-subtle)] sm:w-[24%]">
            <GrammarWord :text="row.pronoun" class="block text-[20px] leading-snug" />
            <span dir="ltr" class="block font-sans text-[11.5px] font-normal text-[var(--color-text-muted)]">{{ t(`grammar.persons.${row.person}`) }}</span>
          </th>
          <td v-for="(forms, index) in row.cells" :key="index">
            <span v-if="!forms.length" class="text-[var(--color-text-faint)]">—</span>
            <template v-for="(form, n) in forms" :key="form">
              <span v-if="n > 0" aria-hidden="true" class="mx-2 text-[var(--color-text-faint)]">/</span>
              <GrammarWord :text="form" :tense="table.tense" class="form" />
            </template>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.grammar-table th,
.grammar-table td {
  padding: 9px 8px;
  border-bottom: 1px solid var(--color-border);
  text-align: center;
  vertical-align: middle;
}

.grammar-table thead th,
.grammar-table thead td { padding-block: 8px; }
.grammar-table tbody tr:last-child th,
.grammar-table tbody tr:last-child td { border-bottom: 0; }
.grammar-table tbody th { border-left: 1px solid var(--color-border); }
.grammar-table td + td { border-right: 1px solid var(--color-border); }

/* The book's heavier line where the plural begins. */
.grammar-table tr.plural-start th,
.grammar-table tr.plural-start td { border-top: 2px solid var(--color-border-strong); }

.grammar-table .form {
  font-size: 27px;
  line-height: 1.7;
}

@media (max-width: 480px) {
  .grammar-table .form { font-size: 24px; }
}
</style>
