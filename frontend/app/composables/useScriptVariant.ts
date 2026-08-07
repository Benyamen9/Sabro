export type ScriptVariant = 'estrangela' | 'serto' | 'madnhaya'

export const scriptVariants: readonly ScriptVariant[] = ['estrangela', 'serto', 'madnhaya'] as const

// Each variant maps to a distinct Google-hosted Noto Syriac family (loaded in
// nuxt.config head), falling back to the Estrangela family then a generic serif.
const FONTS: Record<ScriptVariant, string> = {
  estrangela: '"Noto Sans Syriac", serif',
  serto: '"Noto Sans Syriac Western", "Noto Sans Syriac", serif',
  madnhaya: '"Noto Sans Syriac Eastern", "Noto Sans Syriac", serif',
}

/** The font stack for a given script variant — shared by SyriacText and the global binding. */
export function fontForVariant(variant: ScriptVariant): string {
  return FONTS[variant]
}

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
  // Share the script-variant cookie across the ecosystem (sabro.be + *.sabro.be)
  // via a configurable domain, so the choice holds in Meltho and future apps.
  // Empty in dev → host-only; ".sabro.be" in prod. Secure only when a domain is
  // set, so http localhost dev still works.
  const cookieDomain = useRuntimeConfig().public.cookieDomain
  const cookie = useCookie<ScriptVariant>(cookieKey, {
    default: () => 'serto',
    maxAge: 60 * 60 * 24 * 365,
    sameSite: 'lax',
    domain: cookieDomain || undefined,
    secure: Boolean(cookieDomain),
  })

  const variant = useState<ScriptVariant>(stateKey, () => cookie.value)

  function set(value: ScriptVariant) {
    if (!isScriptVariant(value)) {
      return
    }
    variant.value = value
    cookie.value = value
  }

  // The active variant's font stack, for binding --font-syriac globally so every
  // Syriac surface (including chrome like the nav brand mark) follows the choice.
  const fontFamily = computed(() => fontForVariant(variant.value))

  return {
    variant,
    set,
    available: scriptVariants,
    fontFamily,
  }
}
