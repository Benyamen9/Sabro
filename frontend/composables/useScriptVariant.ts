export type ScriptVariant = 'estrangela' | 'serto' | 'madnhaya'

export const scriptVariants: readonly ScriptVariant[] = ['estrangela', 'serto', 'madnhaya'] as const

const cookieKey = 'sabro_script_variant'
const stateKey = 'sabro:scriptVariant'

function isScriptVariant(value: unknown): value is ScriptVariant {
  return typeof value === 'string' && (scriptVariants as readonly string[]).includes(value)
}

/**
 * Reactive, cookie-backed user preference for which Syriac script variant to render.
 * Persists across reloads and is shared globally — useCookie alone returns a fresh
 * ref per call site, so we mirror it into useState to keep all consumers in sync
 * (per CLAUDE.md: "switcher available everywhere Syriac text is displayed").
 */
export function useScriptVariant() {
  const cookie = useCookie<ScriptVariant>(cookieKey, {
    default: () => 'estrangela',
    maxAge: 60 * 60 * 24 * 365,
    sameSite: 'lax',
  })

  const variant = useState<ScriptVariant>(stateKey, () => cookie.value)

  function set(value: ScriptVariant) {
    if (!isScriptVariant(value)) {
      return
    }
    variant.value = value
    cookie.value = value
  }

  return {
    variant,
    set,
    available: scriptVariants,
  }
}
