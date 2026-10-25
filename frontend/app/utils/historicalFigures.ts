import type {
  HistoricalFigureCategory,
  HistoricalFigureEmblem,
  HistoricalFigureGender,
  HistoricalFigureRegion,
  HistoricalFigureRole,
  HistoricalFigureStatus,
  HistoricalFigureTradition,
  HistoricalPeriod,
} from '~/types/api'

/**
 * Option lists for the Shmo roster backoffice, in the order the enums are
 * declared in src/Modules/Sabro.Historical/Domain — not alphabetically, so the
 * dropdowns read in the order an editor thinks in (biblical roles before
 * ecclesiastical ones, and so on). Keep in sync with the domain enums; the API
 * rejects unknown values, so drift shows up as a failed save rather than bad data.
 */
export const HISTORICAL_FIGURE_CATEGORIES: readonly HistoricalFigureCategory[] = [
  'BiblicalOldTestament',
  'BiblicalNewTestament',
  'Patristic',
] as const

/**
 * Chronological, not alphabetical: the dropdown reads as a timeline, which is
 * how an editor placing a figure actually thinks.
 */
export const HISTORICAL_PERIODS: readonly HistoricalPeriod[] = [
  'Primeval',
  'Patriarchal',
  'ExodusAndConquest',
  'Judges',
  'UnitedMonarchy',
  'DividedMonarchy',
  'ExileAndReturn',
  'SecondTemple',
  'Apostolic',
  'AnteNicene',
  'NiceneEra',
  'PostChalcedonian',
  'IslamicEra',
  'SyriacRenaissance',
  'LateMedieval',
  'ModernEra',
] as const

export const HISTORICAL_FIGURE_ROLES: readonly HistoricalFigureRole[] = [
  'Prophet',
  'King',
  'Judge',
  'Apostle',
  'Evangelist',
  'Patriarch',
  'Bishop',
  'Translator',
  'Commentator',
  'Monk',
  'Martyr',
  'Other',
] as const

// Ordered outward from the Syriac heartland rather than alphabetically.
export const HISTORICAL_FIGURE_REGIONS: readonly HistoricalFigureRegion[] = [
  'IsraelJudah',
  'Mesopotamia',
  'Syria',
  'Persia',
  'Arabia',
  'Egypt',
  'Ethiopia',
  'AsiaMinor',
  'Greece',
  'Italy',
  'Armenia',
  'India',
  'Other',
] as const

// Undivided church first, then the Syriac traditions, then the sister churches
// of the Oriental Orthodox communion, then the rest.
export const HISTORICAL_FIGURE_TRADITIONS: readonly HistoricalFigureTradition[] = [
  'PreChalcedonian',
  'WestSyriac',
  'EastSyriac',
  'Coptic',
  'Armenian',
  'Ethiopian',
  'Malankara',
  'ByzantineChalcedonian',
  'Latin',
  'NotApplicable',
] as const

export const HISTORICAL_FIGURE_GENDERS: readonly HistoricalFigureGender[] = ['Male', 'Female'] as const

export const HISTORICAL_FIGURE_STATUSES: readonly HistoricalFigureStatus[] = ['Draft', 'Published'] as const

/**
 * Widest centuries the API accepts — mirrors HistoricalFigure.MinEra/MaxEra.
 * Signed: negative is BC, positive is AD, and there is no century zero. The
 * lower bound reaches back to the primeval genealogies of Genesis 1–11, whose
 * dating depends on which traditional chronology you follow.
 */
export const MIN_ERA = -60
export const MAX_ERA = 21

/**
 * Renders a signed century as a human-readable era, e.g. -10 -> "10th c. BC".
 * The ordinal suffix comes from i18n so it can be localised; the era itself is
 * a bare number in the data because Shmo compares it numerically for its
 * higher/lower hint.
 */
export function formatEra(era: number, t: (key: string, named?: Record<string, unknown>) => string): string {
  const century = Math.abs(era)
  return t(era < 0 ? 'admin.historicalFigures.eraBc' : 'admin.historicalFigures.eraAd', { century })
}

/**
 * Every emblem a figure card can carry, as a record so that a value added to
 * HistoricalFigureEmblem on the backend fails this file's typecheck until it is
 * listed here (the generated union is the source of truth). Shown sorted by
 * translated name, so the order here does not matter.
 */
const EMBLEMS: Record<HistoricalFigureEmblem, true> = {
  Alphabet: true,
  Anchor: true,
  Ark: true,
  Arrows: true,
  Basin: true,
  BasketOfLoaves: true,
  Beehive: true,
  Birds: true,
  Book: true,
  Bow: true,
  BowlOfLentils: true,
  BronzeHelmet: true,
  Brush: true,
  BuildersSquare: true,
  BurningCoal: true,
  BurningHeart: true,
  Candle: true,
  CelestialSphere: true,
  Censer: true,
  Chains: true,
  ChariotOfFire: true,
  ChariotWheel: true,
  CityWall: true,
  Coat: true,
  Coins: true,
  Crook: true,
  Crown: true,
  Donkey: true,
  Dove: true,
  Eagle: true,
  Flames: true,
  Fleece: true,
  FlintKnife: true,
  FloweringRod: true,
  FloweringStaff: true,
  Flute: true,
  Fruit: true,
  FullersClub: true,
  Gallows: true,
  GoldenCalf: true,
  GospelBook: true,
  GrapeCluster: true,
  GreatFish: true,
  Hand: true,
  Harp: true,
  HolyFaceCloth: true,
  HornOfOil: true,
  Horse: true,
  JarOfNard: true,
  Jawbone: true,
  Keys: true,
  Knife: true,
  Ladder: true,
  Ladle: true,
  Lamb: true,
  LambWithCross: true,
  Lamp: true,
  Lily: true,
  LinenShroud: true,
  Lion: true,
  Lions: true,
  Loaves: true,
  Mandrakes: true,
  Mantle: true,
  MoneyBag: true,
  MyrrhJar: true,
  NightLamp: true,
  Oak: true,
  Ox: true,
  PalmTree: true,
  PilgrimStaff: true,
  Pillar: true,
  PillarOfSalt: true,
  Platter: true,
  Plough: true,
  PlumbLine: true,
  PurpleCloth: true,
  Ram: true,
  ReedPen: true,
  RootOfJesse: true,
  SaddleCloth: true,
  SaltireCross: true,
  Sandal: true,
  ScallopShell: true,
  Sceptre: true,
  Scroll: true,
  SealAndCord: true,
  Serpent: true,
  SheafOfBarley: true,
  Shears: true,
  Ship: true,
  Shofar: true,
  SilverCup: true,
  Spear: true,
  Star: true,
  Stars: true,
  Statue: true,
  Stones: true,
  Sun: true,
  Sundial: true,
  Sword: true,
  Tablets: true,
  Tambourine: true,
  TauCross: true,
  Temple: true,
  Tent: true,
  Tomb: true,
  TorahScroll: true,
  TornRobe: true,
  Tower: true,
  TreeOfKnowledge: true,
  Vineyard: true,
  WaterJar: true,
  WaterSkin: true,
  Wheels: true,
  Window: true,
  WingedLion: true,
  WingedMan: true,
  Wings: true,
  WritingHand: true,
  Yoke: true,
}

export const HISTORICAL_FIGURE_EMBLEMS = Object.keys(EMBLEMS) as HistoricalFigureEmblem[]
