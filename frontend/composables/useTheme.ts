export type Theme = 'light' | 'dark' | 'system'

export const themes: readonly Theme[] = ['light', 'dark', 'system'] as const

const cookieKey = 'sabro_theme'
const stateKey = 'sabro:theme'

function isTheme(value: unknown): value is Theme {
  return typeof value === 'string' && (themes as readonly string[]).includes(value)
}

/**
 * Reactive, cookie-backed user preference for light/dark/system appearance.
 * Cookie-only (no profile sync) — unlike locale/script variant this doesn't
 * need to follow the signed-in user across devices, and per-browser display
 * preference is the more natural default anyway.
 *
 * `htmlAttr` is what app.vue binds to <html data-theme>: 'light' or 'dark'
 * force the palette in main.css regardless of OS setting; 'system' omits the
 * attribute so the existing prefers-color-scheme media query decides.
 */
export function useTheme() {
  const cookieDomain = useRuntimeConfig().public.cookieDomain
  const cookie = useCookie<Theme>(cookieKey, {
    default: () => 'system',
    maxAge: 60 * 60 * 24 * 365,
    sameSite: 'lax',
    domain: cookieDomain || undefined,
    secure: Boolean(cookieDomain),
  })

  const theme = useState<Theme>(stateKey, () => cookie.value)

  function set(value: Theme) {
    if (!isTheme(value)) {
      return
    }
    theme.value = value
    cookie.value = value
  }

  const htmlAttr = computed(() => (theme.value === 'system' ? undefined : theme.value))

  return {
    theme,
    set,
    available: themes,
    htmlAttr,
  }
}
