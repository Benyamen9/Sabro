import type { HistoricalFigureEmblem, HistoricalFigureRole } from '~/types/api'

/**
 * Line drawings for the cards of Beth Ṣalme, each one SVG path in a 120×120 box,
 * centred on (60, 62) so it sits inside the card's medallion. Drawn with a round
 * stroke; closed shapes also take a light fill.
 *
 * Both maps are records over the generated API unions, so an emblem or role
 * added on the backend fails the typecheck until it is drawn here.
 */

/** A full circle as path data. */
function ring(x: number, y: number, r: number): string {
  return `M${x - r} ${y}a${r} ${r} 0 1 0 ${2 * r} 0a${r} ${r} 0 1 0 ${-2 * r} 0`
}

/** An ellipse as path data. */
function oval(x: number, y: number, rx: number, ry: number): string {
  return `M${x - rx} ${y}a${rx} ${ry} 0 1 0 ${2 * rx} 0a${rx} ${ry} 0 1 0 ${-2 * rx} 0`
}

/** A five-pointed star as path data. */
function star(x: number, y: number, r: number): string {
  let d = ''
  for (let k = 0; k < 10; k++) {
    const a = -Math.PI / 2 + (k * Math.PI) / 5
    const rr = k % 2 ? r * 0.45 : r
    d += `${k ? 'L' : 'M'}${(x + rr * Math.cos(a)).toFixed(1)} ${(y + rr * Math.sin(a)).toFixed(1)}`
  }
  return `${d}z`
}

const crown = 'M40 78 36 48l13 13 11-19 11 19 13-13-4 30zM40 86h40'
const openScroll = 'M44 48h32v38H44zM38 46h44M38 88h44M51 58h18M51 66h18M51 74h12'
const oilLamp = 'M38 72h38q-2 12-18 12T38 72zM76 72l10-6M60 66q-7-8 0-18 7 10 0 18z'
const openBook = 'M60 50q-12-6-24-4v36q12-2 24 4 12-6 24-4V46q-12-2-24 4zM60 50v36'
const lambBody = 'M42 66q-6-8 2-14 2-8 12-6 6-6 14 0 10-2 12 8 6 8-2 14H44q-6 0-2-2zM78 58q6-6 10 0-2 8-8 6M48 72v12M56 72v12M66 72v12M74 72v12'
const lionFace = (x: number, y: number, r: number) =>
  `${ring(x, y, r)}${ring(x, y + 2, r * 0.55)}M${x - 4} ${y}h.1M${x + 4} ${y}h.1M${x - 3} ${y + 6}q3 3 6 0`
const robe = 'M48 36l-14 10 6 10 6-4v36h28V52l6 4 6-10-14-10q-6 8-12 0z'

export const ROLE_EMBLEM_PATHS: Record<HistoricalFigureRole, string> = {
  King: crown,
  Prophet: openScroll,
  Bishop: 'M44 88V58l16-20 16 20v30zM60 60v18M53 68h14',
  Patriarch: 'M60 36v54M52 48h16M46 60h28',
  Monk: oilLamp,
  Apostle: 'M38 63q20-20 40 0-20 20-40 0zM78 63l10-10M78 63l10 10M48 61h.1',
  Evangelist: `${openBook}M60 32v12M55 37h10`,
  Commentator: `${openBook}M42 56h12M42 64h12M66 56h12M66 64h12`,
  Translator: 'M78 38Q52 46 46 78l-4 10M46 78q24-10 32-40M58 56l8 4',
  Judge: 'M60 40v46M48 86h24M38 50h44M38 50l-6 14h12zM82 50l-6 14h12zM32 64q6 6 12 0M76 64q6 6 12 0',
  Martyr: 'M60 90q0-28 6-50M61 78q-11-4-15-12M61 78q11-6 15-14M63 64q-11-4-13-12M63 64q9-6 11-14M65 50q-7-4-8-10M65 50q6-4 7-10',
  Other: 'M44 88q14-22 34-48M52 76q-12 0-14-10 10-2 14 10zM58 68q2-12 12-12 0 10-12 12zM62 62q-12-2-12-12 10 0 12 12zM68 54q2-10 10-10 0 8-10 10z',
}

export const EMBLEM_PATHS: Record<HistoricalFigureEmblem, string> = {
  Alphabet: 'M38 40h44v44H38zM44 74l8-22 8 22M47 66h10M68 52h8M68 60h8M68 68h8',
  Anchor: `${ring(60, 36, 4)}M60 40v46M50 50h20M38 72q2 14 22 14 20 0 22-14M38 72l-4 4M82 72l4 4`,
  Ark: 'M34 70h52l-8 14H42zM46 70V58h28v12M43 58l17-11 17 11',
  Arrows: 'M36 84l46-46M82 38H70M82 38v12M84 84L38 38M38 38h12M38 38v12',
  Basin: 'M34 64h52q-4 18-26 18T34 64zM44 70h32M60 40q-5 7 0 10 5-3 0-10z',
  BasketOfLoaves: 'M36 66h48l-6 20H42zM44 72h32M46 78h28M40 66q0-12 10-12t10 12M60 66q0-12 10-12t10 12',
  Beehive: 'M42 84q-6-30 18-42 24 12 18 42zM40 74h40M43 64h34M48 54h24M56 84v-6h8v6',
  Birds: 'M36 56q8-8 14 0 6-8 14 0M58 72q8-8 14 0 6-8 14 0M42 84q5-5 9 0 4-5 9 0',
  Book: 'M42 38h34v48H42zM48 38v48M54 50h16',
  Bow: 'M48 34q34 28 0 56M48 34v56M40 62h44M84 62l-6-4M84 62l-6 4',
  BowlOfLentils: 'M34 64h52q-4 18-26 18T34 64zM50 56q-4-6 0-12M60 56q-4-6 0-12M70 56q-4-6 0-12',
  BronzeHelmet: 'M38 82V72q0-32 22-32t22 32v10H70V70H50v12zM60 40v-6M50 58h20',
  Brush: 'M70 36l12 12-28 28-8-4-4-8zM46 72q-8 2-10 14 12-2 14-10',
  BuildersSquare: 'M40 36v48h44V74H50V36zM40 46h4M40 56h4M40 66h4M60 84v-4M70 84v-4',
  BurningCoal: `M36 40l30 26M44 36l26 26${oval(72, 70, 9, 6)}M72 60q-6-6 0-14 6 8 0 14z`,
  BurningHeart: 'M60 88q-24-16-24-32 0-10 10-10 8 0 14 8 6-8 14-8 10 0 10 10 0 16-24 32zM60 46q-6-8 0-16 6 8 0 16z',
  Candle: 'M52 52h16v36H52zM60 52v-6M60 46q-6-6 0-14 6 8 0 14zM46 88h28',
  CelestialSphere: `${ring(60, 60, 24)}M36 60q24 12 48 0M60 36q-10 24 0 48M60 84v6M50 90h20`,
  Censer: 'M44 62h32q-2 14-16 14T44 62zM46 62q14-14 28 0M48 58l12-24 12 24M54 84h12M60 76v8',
  Chains: `${ring(44, 46, 7)}${ring(54, 56, 7)}${ring(64, 66, 7)}${ring(74, 76, 7)}`,
  ChariotOfFire: `${ring(46, 76, 11)}M46 65v22M35 76h22M40 64h30l8 12H57M58 62q-4-14 6-22 0 8 6 10 2-10 12-12-4 12 2 18q-8 10-26 6z`,
  ChariotWheel: `${ring(60, 62, 24)}${ring(60, 62, 5)}M60 38v48M36 62h48M43 45l34 34M77 45L43 79`,
  CityWall: 'M34 88V56h8v-6h8v6h8v-6h8v6h8v-6h8v6h8v32zM54 88V74q6-6 12 0v14',
  Coat: `${robe}M46 62h28M46 72h28M46 82h28`,
  Coins: `${oval(52, 70, 12, 5)}M40 70v8q12 6 24 0v-8${oval(68, 54, 12, 5)}M56 54v8q12 6 24 0v-8`,
  Crook: 'M58 90V50q0-14 12-14t12 12q0 6-6 6',
  Crown: `M40 76l-4-22 12 10 12-16 12 16 12-10-4 22zM40 84h40${ring(48, 70, 2)}${ring(60, 70, 2)}${ring(72, 70, 2)}`,
  Donkey: 'M50 32l4 22M70 32l-4 22M50 54q-4 18 4 30h12q8-12 4-30zM56 78h.1M64 78h.1M54 62h.1M66 62h.1',
  Dove: 'M34 64q14-14 30-6l20-8-8 14q-4 14-22 16l-8 10-2-10q-8-4-10-16zM70 56h.1',
  Eagle: 'M60 44l-26-6 8 14-8 4 12 6-4 8 14-6 4 14 4-14 14 6-4-8 12-6-8-4 8-14z',
  Flames: 'M60 88q-20-6-16-26 4 10 10 10-6-18 6-36 2 16 12 20 10 10 4 22 6-4 6-12 8 18-22 22z',
  Fleece: 'M40 70q-6-10 4-14 0-10 10-8 6-8 14-2 10-4 14 6 10 4 4 14 4 8-6 12H42q-6 0-2-8zM48 84v4M60 84v4M72 84v4',
  FlintKnife: 'M38 76l34-34 8 8-34 34zM46 68l8 8M38 76l-4 10 10-4',
  FloweringRod: `M58 90V44M58 54q-10-4-12-12 10 2 12 12zM58 62q10-4 12-12-10 2-12 12z${ring(58, 40, 4)}`,
  FloweringStaff: 'M60 90V50M60 50q-8-6-8-16 6 4 8 10 2-6 8-10 0 10-8 16zM60 62q-8-2-10-8M60 70q8-2 10-8',
  Flute: `M38 84l44-44 4 4-44 44z${ring(54, 70, 1.5)}${ring(62, 62, 1.5)}${ring(70, 54, 1.5)}`,
  Fruit: 'M60 50q-20-8-22 12 0 20 22 22 22-2 22-22-2-20-22-12zM60 50q0-8 6-12M62 44q8-4 12 0-6 6-12 0z',
  FullersClub: 'M40 86l30-30M66 60q-2-14 10-20 8 8 0 20-6 4-10 0z',
  Gallows: 'M40 88V38h34M40 50l12-12M74 38v12M70 50h8v8h-8z',
  GoldenCalf: 'M38 70q0-12 14-12h18q12 0 14 8l6-6-2 12h-6v20h-6V76H50v16h-6V74q-6-2-6-4zM78 58l4-8M84 60l6-6',
  GospelBook: 'M42 38h36v48H42zM60 46v24M52 54h16',
  GrapeCluster: `${ring(50, 52, 5)}${ring(60, 52, 5)}${ring(70, 52, 5)}${ring(55, 61, 5)}${ring(65, 61, 5)}${ring(60, 70, 5)}M60 47v-9q6-2 10 2`,
  GreatFish: `M32 62q24-26 48-4l10-10v32l-10-10q-24 22-48-8z${ring(44, 58, 2)}`,
  Hand: 'M48 88V60q0-4 4-4h20q4 0 4 4v28zM52 56V36M58 56V30M64 56V32M70 56V40M48 66l-8-8',
  Harp: 'M42 88V36q20 6 36 36l-36 16zM48 44v40M54 50v32M60 56v28M66 62v22M72 68v14',
  HolyFaceCloth: `M34 40h52v40q-26 8-52 0z${ring(52, 54, 2)}${ring(68, 54, 2)}M60 58v8M54 70q6 4 12 0`,
  HornOfOil: 'M38 86q4-30 40-44l4 6q-30 18-36 40zM78 42q2-6 8-6',
  Horse: 'M44 88V70q-4-18 10-30l-2-8 8 6q16 2 22 18l-6 6-8-4-6 6v24zM62 48h.1',
  JarOfNard: 'M56 34h8v12q10 6 10 20 0 16-14 20-14-4-14-20 0-14 10-20z',
  Jawbone: 'M36 50q4 30 30 34 16 2 20-6-14 0-22-6-12-8-14-24zM46 66l4-4M52 72l4-4M60 76l3-5',
  Keys: `M44 80l26-26${ring(75, 49, 7)}M44 80l6 6M48 76l6 6M76 80L50 54${ring(45, 49, 7)}M76 80l-6 6M72 76l-6 6`,
  Knife: 'M40 80l34-34q6 0 6 6L50 82zM40 80l-6 6 4 4 6-6',
  Ladder: 'M48 34v56M72 34v56M48 44h24M48 56h24M48 68h24M48 80h24',
  Ladle: `M70 36L56 62${oval(52, 72, 14, 10)}`,
  Lamb: lambBody,
  LambWithCross: `${lambBody}M84 56V32M84 34H72v8h12`,
  Lamp: oilLamp,
  Lily: 'M60 90V62M60 62q-7-14 0-26 7 12 0 26zM60 62q-15-2-19-14 13-2 19 14zM60 62q15-2 19-14-13-2-19 14zM60 80q-8-8-14-6M60 80q8-8 14-6',
  LinenShroud: 'M36 44q24 8 48 0v40q-24 8-48 0zM44 52v28M52 54v28M60 54v28M68 54v28M76 52v28',
  Lion: `${lionFace(60, 62, 22)}M36 62l-6-4M84 62l6-4M42 44l-4-6M78 44l4-6M60 40v-6`,
  Lions: `${lionFace(48, 54, 14)}${lionFace(74, 72, 14)}`,
  Loaves: 'M38 76q0-14 14-14t14 14zM54 76q0-14 14-14t14 14zM46 64q0-14 14-14t14 14M34 80h52',
  Mandrakes: 'M60 54q-14-6-18-18 12 2 18 18zM60 54q14-6 18-18-12 2-18 18zM60 54V40M60 54q-8 10-4 22l-6 12M58 76l8 12M56 66l-8 6',
  Mantle: 'M46 36h28l12 52H34zM60 36v52M46 36q14 10 28 0',
  MoneyBag: 'M50 44h20l-4 8q18 8 14 26-2 10-20 10t-20-10q-4-18 14-26zM50 44q10-8 20 0M60 62v14M54 66h10',
  MyrrhJar: 'M55 38h10v8H55zM56 46q-10 6-10 20 0 14 14 22 14-8 14-22 0-14-10-20z',
  NightLamp: 'M34 76h38q-2 12-18 12T34 76zM72 76l10-6M56 70q-7-8 0-18 7 10 0 18zM82 34a10 10 0 1 0 6 14 8 8 0 1 1-6-14z',
  Oak: 'M60 90V64M60 74l-10-6M60 70l10-8M38 54q-4-14 10-16 4-10 14-6 10-6 18 4 10 2 8 14 2 12-12 10H46q-10-4-8-14z',
  Ox: 'M46 50q-12-2-12-14 8 6 16 6M74 50q12-2 12-14-8 6-16 6M46 46q14-6 28 0 2 22-6 34H52q-8-12-6-34zM54 74h.1M66 74h.1',
  PalmTree: 'M58 90q-2-24 4-40M62 50q-14-8-24-2M62 50q-6-12-18-14M62 50q8-12 20-12M62 50q14-4 22 6M62 50q2-14-4-20',
  PilgrimStaff: `M56 90V36q0-6 6-6t6 6${oval(64, 56, 5, 7)}M56 46l8 4`,
  Pillar: `${ring(60, 30, 4)}M44 36h32M48 40h24M50 40v44M70 40v44M56 44v40M64 44v40M44 84h32`,
  PillarOfSalt: `M52 88V50q0-8 8-8t8 8v38z${ring(60, 36, 6)}M48 88h24`,
  Platter: `${oval(60, 68, 28, 8)}${oval(60, 68, 18, 4)}`,
  Plough: 'M36 56l30 20h18M66 76l-6 12M48 64l-12 18M84 76q4-6 8-6',
  PlumbLine: 'M60 34v34M60 68l-6 8 6 10 6-10zM36 34h48M80 34v54',
  PurpleCloth: 'M36 52h48v10H36zM40 62h40v10H40zM36 72h48v12H36z',
  Ram: 'M52 46h16q2 16-8 34-10-18-8-34zM52 48q-6-12-16-6-6 6 0 12 6 4 8-2M68 48q6-12 16-6 6 6 0 12-6 4-8-2M56 56h.1M64 56h.1',
  ReedPen: 'M76 36L46 76l-4 12 10-6 30-40zM46 76l6 6',
  RootOfJesse: 'M44 88V70h32v18M44 70q4-4 8 0 4-4 8 0 4-4 8 0 4-4 8 0M60 70V44M60 54q10-6 12-14M60 60q-8-4-12-12M60 44q-4-6 0-10 4 4 0 10zM44 88l-6 2M76 88l6 2',
  SaddleCloth: 'M38 46h44v32H38zM38 46l22 10 22-10M42 78v8M50 78v8M58 78v8M66 78v8M74 78v8',
  SaltireCross: 'M38 44l6-6 38 40-6 6zM82 44l-6-6-38 40 6 6z',
  Sandal: 'M48 88q-10-20-4-40 4-12 14-12t12 14q4 20-6 38zM48 56h22M50 66h18',
  ScallopShell: 'M60 84L36 58q2-18 24-22 22 4 24 22zM60 84V40M60 84L46 44M60 84l14-40M54 84h12',
  Sceptre: `M60 90V46M52 46h16${ring(60, 37, 7)}M60 30v-6M57 27h6`,
  Scroll: 'M44 44h32v36H44zM44 44q-6 0-6 6t6 6M76 80q6 0 6-6t-6-6M50 54h20M50 62h20M50 70h14',
  SealAndCord: `${ring(52, 56, 12)}${ring(52, 56, 5)}M62 64q10 6 8 14t10 10`,
  Serpent: 'M44 86q-14-8 0-18t16-14q14-6 4-18-12-6-18 2M46 38q4-6 10-2-2 6-10 2z',
  SheafOfBarley: 'M60 88V46M60 88l-10-36M60 88l10-36M52 64h16M50 52q-4-10 0-16M60 46q-2-10 2-14M70 52q4-10 0-16',
  Shears: `M46 74l30-36M74 74L44 38${ring(42, 80, 6)}${ring(78, 80, 6)}`,
  Ship: 'M32 72h56l-8 14H40zM60 72V36M60 38l20 30H60',
  Shofar: 'M36 82q32-4 46-40l5 3q-11 32-50 43z',
  SilverCup: 'M44 40h32q0 20-16 24-16-4-16-24zM60 64v16M48 86h24M52 80h16',
  Spear: 'M60 90V44M60 30l6 14H54z',
  Star: star(60, 62, 24),
  Stars: star(60, 50, 13) + star(43, 74, 8) + star(77, 74, 8),
  Statue: `${ring(60, 40, 6)}M50 48h20l-2 28H52zM46 76h28v8H46zM40 84h40v6H40z`,
  Stones: 'M36 84q2-12 14-12 10 0 12 12zM60 84q0-10 12-10t12 10zM46 70q2-12 14-12 12 2 12 12z',
  Sun: `${ring(60, 62, 14)}M60 38v-6M60 86v6M36 62h-6M84 62h6M43 45l-4-4M77 45l4-4M43 79l-4 4M77 79l4 4`,
  Sundial: 'M34 74h52M34 74a26 26 0 0 1 52 0M60 74l14-24M60 74V50M60 74L46 50M40 74v10h40V74',
  Sword: 'M56 74V40l4-8 4 8v34zM48 74h24M60 74v14M56 90h8',
  Tablets: 'M38 86V46q0-10 10-10t10 10v40zM62 86V46q0-10 10-10t10 10v40zM44 54h8M44 62h8M44 70h8M68 54h8M68 62h8M68 70h8',
  Tambourine: `${ring(60, 62, 22)}${ring(60, 62, 16)}${ring(60, 40, 3)}${ring(82, 62, 3)}${ring(60, 84, 3)}${ring(38, 62, 3)}`,
  TauCross: 'M38 40h44v10H66v38H54V50H38z',
  Temple: 'M34 88h52M38 88V58M50 88V58M70 88V58M82 88V58M34 58h52L60 38z',
  Tent: 'M34 86 60 42l26 44zM52 86l8-14 8 14',
  Tomb: 'M34 88V58q0-22 26-22t26 22v30zM48 88V70a10 10 0 0 1 20 0v18',
  TorahScroll: 'M40 40v48M80 40v48M40 46h40v36H40zM40 34v6M40 88v4M80 34v6M80 88v4M48 54h24M48 62h24M48 70h24',
  TornRobe: `${robe}M60 52l-4 8 6 6-4 8 4 8`,
  Tower: 'M38 88h44l-4-12H42zM44 76h32l-4-12H48zM50 64h20l-4-12H54zM56 52h8l-2-12h-4z',
  TreeOfKnowledge: `M60 90V64M60 76l-10-7M60 72l10-7${oval(60, 50, 20, 16)}${ring(52, 48, 3)}${ring(66, 44, 3)}${ring(64, 56, 3)}`,
  Vineyard: `M36 88V40M84 88V40M36 50h48M36 50q12 14 24 0t24 0${ring(48, 64, 4)}${ring(44, 72, 4)}${ring(52, 72, 4)}${ring(72, 64, 4)}${ring(68, 72, 4)}${ring(76, 72, 4)}`,
  WaterJar: 'M52 36h16M54 36v8q-12 6-12 20 0 16 18 24 18-8 18-24 0-14-12-20v-8M44 52q-8 0-8 8t8 8M76 52q8 0 8 8t-8 8',
  WaterSkin: 'M50 40q-14 10-12 30 2 18 22 18t22-18q2-20-12-30zM50 40h20M54 40l-2-6h16l-2 6',
  Wheels: `${ring(60, 62, 26)}${ring(60, 62, 14)}M60 36v52M34 62h52${ring(60, 36, 2)}${ring(86, 62, 2)}${ring(60, 88, 2)}${ring(34, 62, 2)}`,
  Window: 'M42 88V52q0-18 18-18t18 18v36zM60 34v54M42 64h36',
  WingedLion: `${lionFace(60, 66, 14)}M46 60q-14-18-14 0 0 8 6 12M74 60q14-18 14 0 0 8-6 12`,
  WingedMan: `${ring(60, 44, 7)}M52 56h16l4 32H48zM52 58q-20-14-18 10M68 58q20-14 18 10`,
  Wings: 'M58 74q-24-4-26-34 18 6 26 20zM62 74q24-4 26-34-18 6-26 20zM44 58l8 4M76 58l-8 4',
  WritingHand: 'M44 88V64q0-4 4-4h18q4 0 4 4v24zM48 60V44M54 60V40M60 60V42M66 60l8-14M70 34h14M74 42h10',
  Yoke: 'M34 52q26 10 52 0M40 54v18q0 8 8 8M80 54v18q0 8-8 8M60 57v6',
}

/** Closed shapes take a light fill; these are open strokes that a fill would blot. */
export const OPEN_EMBLEMS: ReadonlySet<string> = new Set([
  'Other',
  'Patriarch',
  'Martyr',
  'Translator',
  'Arrows',
  'Birds',
  'Bow',
  'BowlOfLentils',
  'Chains',
  'CelestialSphere',
  'ChariotWheel',
  'Crook',
  'FullersClub',
  'Gallows',
  'Hand',
  'Ladder',
  'Mandrakes',
  'Oak',
  'PalmTree',
  'PilgrimStaff',
  'Plough',
  'PlumbLine',
  'RootOfJesse',
  'Serpent',
  'SheafOfBarley',
  'Shears',
  'Sun',
  'Sundial',
  'TreeOfKnowledge',
  'Vineyard',
  'Wheels',
  'Yoke',
  'Spear',
  'FloweringStaff',
  'Lily',
  'Sceptre',
  'SealAndCord',
])
