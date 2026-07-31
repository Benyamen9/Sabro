/**
 * The backoffice's sections, declared once.
 *
 * Both the `/admin` hub and the switcher that rides above every section read
 * from this list, so adding an area (Nahlo, Stories) is one entry here rather
 * than an edit in three places — the drift that left the hub and the header
 * disagreeing about which areas exist.
 *
 * `accent` is the CSS custom property carrying the area's colour: content
 * sections wear their game's colour, and anything that is not content wears the
 * house accent, so a glance at the rail says what kind of place you are in.
 */
export interface AdminSection {
  /** Route of the section's index page. */
  to: string
  /** i18n key for the section's short name, as shown in the switcher. */
  labelKey: string
  /** i18n key for the one-line description shown on the hub's door. */
  blurbKey: string
  /** CSS custom property holding this section's colour. */
  accent: string
}

export const adminSections: AdminSection[] = [
  {
    to: '/admin/lexicon',
    labelKey: 'admin.sections.lexicon.label',
    blurbKey: 'admin.sections.lexicon.blurb',
    accent: '--color-meltho',
  },
  {
    to: '/admin/historical-figures',
    labelKey: 'admin.sections.figures.label',
    blurbKey: 'admin.sections.figures.blurb',
    accent: '--color-shmo',
  },
]

export function useAdminSections() {
  const route = useRoute()

  /**
   * A section is current when the route is its index or anything beneath it, so
   * the switcher stays lit while editing a single entry.
   */
  const isCurrent = (to: string) => route.path === to || route.path.startsWith(`${to}/`)

  const current = computed(() => adminSections.find(section => isCurrent(section.to)) ?? null)

  return { sections: adminSections, isCurrent, current }
}
