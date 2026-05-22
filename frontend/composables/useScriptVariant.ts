export type ScriptVariant = 'estrangela' | 'serto' | 'madnhaya'

export const scriptVariants: readonly ScriptVariant[] = ['estrangela', 'serto', 'madnhaya'] as const

const cookieKey = 'sabro_script_variant'

function isScriptVariant(value: unknown): value is ScriptVariant {
  return typeof value === 'string' && (scriptVariants as readonly string[]).includes(value)
}

/**
 * Reactive, cookie-backed user preference for which Syriac script variant to render.
 * Persists across reloads and is shared globally — components read this rather than
 * passing the variant down through props (per CLAUDE.md: "switcher available everywhere
 * Syriac text is displayed").
 */
export function useScriptVariant() {
  const cookie = useCookie<ScriptVariant>(cookieKey, {
    default: () => 'estrangela',
    maxAge: 60 * 60 * 24 * 365,
    sameSite: 'lax',
  })

  function set(value: ScriptVariant) {
    if (!isScriptVariant(value)) {
      return
    }
    cookie.value = value
  }

  return {
    variant: cookie,
    set,
    available: scriptVariants,
  }
}
