<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { SuggestedEditTargetType } from '~/types/api'

/**
 * A reviewer's way in: pick a field, propose a value, say why.
 *
 * One panel rather than a control beside every input. A reviewer works field by
 * field and usually has one thing to say about an entry, so a picker matches the
 * task and keeps the editor's form untouched — the alternative would thread
 * proposal state through every control on a page an editor uses differently.
 *
 * The field list comes from the server (see useProposals.proposableFields). It is
 * deliberately not hardcoded here: publication fields are absent from that list
 * server-side, and a local copy would eventually offer one and be refused.
 */

const props = defineProps<{
  targetType: SuggestedEditTargetType
  targetId: string
}>()

const { t, te } = useI18n()
const { proposableFields, proposeField } = useProposals()

const open = ref(false)
const fields = ref<string[]>([])
const fieldsState = ref<'idle' | 'loading' | 'ready' | 'failed'>('idle')
const field = ref('')
const proposedValue = ref('')
const rationale = ref('')
const submitting = ref(false)
const message = ref<{ kind: 'ok' | 'error', text: string } | null>(null)

async function openPanel() {
  open.value = true
  if (fieldsState.value === 'ready' || fieldsState.value === 'loading') return
  fieldsState.value = 'loading'
  try {
    fields.value = await proposableFields(props.targetType)
    field.value = fields.value[0] ?? ''
    fieldsState.value = 'ready'
  }
  catch {
    fieldsState.value = 'failed'
  }
}

/**
 * Friendly label for a server field name. Meaning/description fields arrive as
 * `meaning.fr`, so the language half is rendered through the locale names the
 * rest of the app already uses; anything without a translation falls back to the
 * raw name rather than showing a missing-key string.
 */
function labelFor(name: string): string {
  const parts = name.split('.')
  const head = parts[0] ?? name
  const language = parts[1]
  const key = `admin.propose.field.${head}`
  const base = te(key) ? t(key) : head
  return language ? `${base} (${language.toUpperCase()})` : base
}

async function submit() {
  if (!field.value || !proposedValue.value.trim()) return
  submitting.value = true
  message.value = null
  try {
    await proposeField({
      targetType: props.targetType,
      targetId: props.targetId,
      field: field.value,
      proposedValue: proposedValue.value.trim(),
      rationale: rationale.value.trim() || undefined,
    })
    message.value = { kind: 'ok', text: t('admin.propose.sent') }
    proposedValue.value = ''
    rationale.value = ''
  }
  catch (error) {
    message.value = {
      kind: 'error',
      text: ((error as FetchError).data?.detail as string | undefined) ?? t('admin.propose.failed'),
    }
  }
  finally {
    submitting.value = false
  }
}

const inputClass = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2 py-1.5 font-sans text-sm text-[var(--color-text)]'
</script>

<template>
  <section class="mt-8 rounded-md border border-[var(--color-border)] bg-[var(--color-bg-subtle)] p-4">
    <button
      v-if="!open"
      type="button"
      class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 font-sans text-sm"
      @click="openPanel"
    >
      {{ t('admin.propose.open') }}
    </button>

    <div v-else>
      <h2 class="font-serif text-lg font-semibold">{{ t('admin.propose.title') }}</h2>
      <p class="mt-1 max-w-prose font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('admin.propose.blurb') }}
      </p>

      <StateMessage
        v-if="fieldsState === 'loading'"
        variant="loading"
        :message="t('common.loading')"
      />
      <StateMessage
        v-else-if="fieldsState === 'failed'"
        variant="failed"
        :message="t('admin.propose.fieldsFailed')"
        @retry="openPanel"
      />

      <form v-else class="mt-4 grid max-w-xl gap-3" @submit.prevent="submit">
        <label class="font-sans text-sm">
          <span class="mb-1 block text-[var(--color-text-muted)]">{{ t('admin.propose.field.label') }}</span>
          <select v-model="field" :class="inputClass">
            <option v-for="name in fields" :key="name" :value="name">{{ labelFor(name) }}</option>
          </select>
        </label>

        <label class="font-sans text-sm">
          <span class="mb-1 block text-[var(--color-text-muted)]">{{ t('admin.propose.value') }}</span>
          <input v-model="proposedValue" type="text" :class="inputClass" required>
        </label>

        <label class="font-sans text-sm">
          <span class="mb-1 block text-[var(--color-text-muted)]">{{ t('admin.propose.rationale') }}</span>
          <textarea v-model="rationale" rows="2" :class="inputClass" />
          <span class="mt-1 block text-xs text-[var(--color-text-faint)]">
            {{ t('admin.propose.rationaleHint') }}
          </span>
        </label>

        <p
          v-if="message"
          :class="[
            'font-sans text-sm',
            message.kind === 'ok' ? 'text-[var(--color-text-muted)]' : 'text-[var(--color-accent)]',
          ]"
          role="status"
        >{{ message.text }}</p>

        <div class="flex gap-2">
          <button
            type="submit"
            :disabled="submitting || !proposedValue.trim()"
            class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 font-sans text-sm disabled:opacity-60"
          >
            {{ t('admin.propose.submit') }}
          </button>
          <button
            type="button"
            class="rounded-md px-3 py-1.5 font-sans text-sm text-[var(--color-text-muted)]"
            @click="open = false"
          >
            {{ t('common.cancel') }}
          </button>
        </div>
      </form>

      <p class="mt-3 max-w-prose font-sans text-xs text-[var(--color-text-faint)]">
        {{ t('admin.propose.ownerDecides') }}
      </p>
    </div>
  </section>
</template>
