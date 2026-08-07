import type { AreaAccess, ContentArea, PersonDto } from '~/types/api'

/**
 * How the People page reads a list of accounts: what each person is, what to
 * call them, and in what order they belong.
 *
 * Pure functions, deliberately. The page renders one card per person and the
 * only interesting decisions — which bucket somebody falls in, what to show when
 * Logto tells us nothing about them — are exactly the ones worth testing without
 * mounting a component.
 */

/** The areas the backoffice can grant, in the order the page lists them. */
export const CONTENT_AREAS: ContentArea[] = ['Lexicon', 'Shmo', 'Nahlo']

/**
 * What somebody effectively is, taken from the strongest thing they hold.
 *
 * Not a stored field: a person is a row of per-area grants, and the page needs a
 * single answer to sort and group by. Owner outranks any grant because it does
 * not go through the area table at all.
 */
export type PersonTier = 'Owner' | 'Editor' | 'Reviewer' | 'None'

/** Groups top to bottom: the Owner first, then descending power. */
export const PERSON_TIERS: PersonTier[] = ['Owner', 'Editor', 'Reviewer', 'None']

export function personTier(person: PersonDto): PersonTier {
  if (person.role === 'Owner') return 'Owner'
  const areas = person.areas ?? []
  if (areas.some(grant => grant.access === 'Editor')) return 'Editor'
  if (areas.some(grant => grant.access === 'Reviewer')) return 'Reviewer'
  return 'None'
}

/**
 * One person's level in one area, as the segmented control wants it: `''` for no
 * grant, because that is what a radio group round-trips.
 *
 * Named for the grant, not for access in general — `accessFor` is already taken
 * by `useMyAccess`, which answers the same question about the signed-in user and
 * returns null rather than an empty string. Two auto-imported functions of the
 * same name silently shadow each other.
 */
export function areaAccessFor(person: PersonDto, area: ContentArea): AreaAccess | '' {
  return person.areas?.find(grant => grant.area === area)?.access ?? ''
}

/**
 * Where a person's name came from, so the page can be honest about it.
 *
 * `none` is not "this person has no name" — it is "the sign-in service told us
 * nothing", which is a different fact and deserves different words. Sabro stores
 * neither name nor email, so a blank is always a failed or empty Logto lookup,
 * never missing data on our side.
 */
export type IdentitySource = 'name' | 'email' | 'none'

export interface PersonIdentity {
  /** The headline for the card. Null only when nothing at all is known. */
  label: string | null
  source: IdentitySource
  /** The secondary line — null when the headline already is the address. */
  secondary: string | null
  /** The last handle when Logto knows nothing: the head of the profile id. */
  shortId: string
}

function trimmed(value: string | null | undefined): string | null {
  const text = value?.trim()
  return text ? text : null
}

/**
 * The best thing to call somebody: their Logto name (or username), else the
 * display name they set themselves, else their email address, else nothing.
 *
 * The email fallback is the point. Logto's `name` is optional and stays null for
 * anyone who signed up with an address and never filled a profile in, so the page
 * used to render "Name unavailable" over an account it could in fact identify
 * perfectly well — the address was already on the wire, just never shown.
 */
export function personIdentity(person: PersonDto): PersonIdentity {
  const name = trimmed(person.name) ?? trimmed(person.displayName)
  const email = trimmed(person.email)
  const shortId = person.id.replace(/-/g, '').slice(0, 8)

  if (name) return { label: name, source: 'name', secondary: email, shortId }
  if (email) return { label: email, source: 'email', secondary: null, shortId }
  return { label: null, source: 'none', secondary: null, shortId }
}

/** Nothing at all is known — the page says so rather than showing a blank. */
export function isUnidentified(person: PersonDto): boolean {
  return personIdentity(person).source === 'none'
}

/**
 * First letter for the avatar circle. An address gives its first letter too, so
 * an account known only by email still gets a real initial instead of a dash.
 */
export function personInitial(person: PersonDto): string {
  const { label } = personIdentity(person)
  return label ? [...label][0]!.toUpperCase() : '?'
}

/**
 * Matches the search box against everything that identifies somebody — including
 * the parts the card does not show. Searching an id you copied out of a log or a
 * database row should find its person.
 */
export function matchesPersonSearch(person: PersonDto, query: string): boolean {
  const needle = query.trim().toLowerCase()
  if (!needle) return true

  return [person.name, person.displayName, person.email, person.id]
    .some(field => field?.toLowerCase().includes(needle))
}

export interface PeopleFilters {
  search: string
  tier: PersonTier | ''
  area: ContentArea | ''
  /** Only the accounts the sign-in service could not name. */
  unidentifiedOnly: boolean
}

export const EMPTY_PEOPLE_FILTERS: PeopleFilters = {
  search: '',
  tier: '',
  area: '',
  unidentifiedOnly: false,
}

export function hasActivePeopleFilters(filters: PeopleFilters): boolean {
  return Boolean(
    filters.search.trim() || filters.tier || filters.area || filters.unidentifiedOnly,
  )
}

export function filterPeople(people: PersonDto[], filters: PeopleFilters): PersonDto[] {
  return people.filter((person) => {
    if (!matchesPersonSearch(person, filters.search)) return false
    if (filters.tier && personTier(person) !== filters.tier) return false
    if (filters.unidentifiedOnly && !isUnidentified(person)) return false

    // The Owner holds every area without a grant to show for it, so filtering by
    // area has to keep them or the filter would claim nobody can edit the Lexicon.
    if (filters.area && person.role !== 'Owner' && areaAccessFor(person, filters.area) === '') return false

    return true
  })
}

/**
 * Yourself first, then by name, then the unnamed. Alphabetical alone buried the
 * one row you always want — your own — somewhere in the middle of the list.
 */
export function comparePeople(a: PersonDto, b: PersonDto): number {
  if (a.isYou !== b.isYou) return a.isYou ? -1 : 1

  const left = personIdentity(a)
  const right = personIdentity(b)
  if (!left.label) return right.label ? 1 : left.shortId.localeCompare(right.shortId)
  if (!right.label) return -1

  return left.label.localeCompare(right.label, undefined, { sensitivity: 'base' })
}

export interface PeopleGroup {
  tier: PersonTier
  people: PersonDto[]
}

/**
 * The list as the page draws it: Owners first, then editors, reviewers, and the
 * accounts holding nothing. Empty groups are dropped — a heading over no rows
 * reads as a loading failure.
 */
export function groupPeople(people: PersonDto[]): PeopleGroup[] {
  return PERSON_TIERS
    .map(tier => ({
      tier,
      people: people.filter(person => personTier(person) === tier).sort(comparePeople),
    }))
    .filter(group => group.people.length > 0)
}
