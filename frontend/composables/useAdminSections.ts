import type { ContentArea } from '~/types/api'

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
  /** The content area this section governs, or null when it is not content. */
  area: ContentArea | null
  /**
   * Owner-only, and not implied by any area grant: deciding whose correction
   * stands and deciding who else gets in are both the Owner's alone.
   */
  ownerOnly: boolean
}

// Order is deliberate and drives both surfaces: People leads, then content
// areas. Access is the decision you make before touching anything, and putting
// it first keeps it from being the item you scroll past.
export const adminSections: AdminSection[] = [
  {
    to: '/admin/people',
    labelKey: 'admin.sections.people.label',
    blurbKey: 'admin.sections.people.blurb',
    accent: '--color-accent',
    area: null,
    ownerOnly: true,
  },
  // Proposals sits second: it is work waiting on the Owner specifically, and it
  // is not content, so it wears the house accent rather than an area's colour.
  {
    to: '/admin/proposals',
    labelKey: 'admin.sections.proposals.label',
    blurbKey: 'admin.sections.proposals.blurb',
    accent: '--color-accent',
    area: null,
    ownerOnly: true,
  },
  {
    to: '/admin/lexicon',
    labelKey: 'admin.sections.lexicon.label',
    blurbKey: 'admin.sections.lexicon.blurb',
    accent: '--color-meltho',
    area: 'Lexicon',
    ownerOnly: false,
  },
  {
    to: '/admin/historical-figures',
    labelKey: 'admin.sections.figures.label',
    blurbKey: 'admin.sections.figures.blurb',
    accent: '--color-shmo',
    area: 'Shmo',
    ownerOnly: false,
  },
]

export function useAdminSections() {
  const route = useRoute()
  const { isOwner, canViewBackoffice } = useMyAccess()

  /**
   * A section is current when the route is its index or anything beneath it, so
   * the switcher stays lit while editing a single entry.
   */
  const isCurrent = (to: string) => route.path === to || route.path.startsWith(`${to}/`)

  const current = computed(() => adminSections.find(section => isCurrent(section.to)) ?? null)

  /**
   * The sections this person may actually open. A door that leads to a 403 is
   * worse than no door: it reads as a broken backoffice rather than as access
   * they were never given. Reads the shared access state, so a page that has
   * refreshed it gets the filtered list without asking again.
   */
  const visibleSections = computed(() =>
    adminSections.filter(section =>
      section.ownerOnly
        ? isOwner.value
        : section.area
          ? canViewBackoffice(section.area)
          : true))

  return { sections: adminSections, visibleSections, isCurrent, current }
}
