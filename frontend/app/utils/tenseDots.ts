/**
 * The tense dots of the Grammar book (Owner, 2026-10-30 to 2026-11-02).
 *
 * A verb form carries up to three dots that tell tenses and persons apart: a dot below or above the
 * first letter, a dot on the final ܬ of "I" (past), the feminine dot of "she" (past). The font
 * (Serto Urhoy) places qushoyo, rukkokho and vowels itself; the tense dots are taken out of the text
 * and drawn in red where the Owner's book has them, which is not where the font puts them.
 *
 * `prepareWord` is the pure half: what to show and what to measure. `drawTenseDots` is the browser
 * half: it draws the word on a hidden canvas with and without each mark, by the same font, and takes
 * the dot's place and size from the difference. Every offset below was set by eye with the Owner;
 * do not "tidy" them.
 */
export type Tense = 'past' | 'present' | 'future'

const BELOW = '̣'
const ABOVE = '̇'
const FEMININE = '݀'
const QUSHOYO = '݁'
const ZQOFO = 'ܳ'
/** A base character's combining marks: the Syriac points and the generic diacritics. */
const MARKS = '[̀-ͯܰ-݊]'
const FIRST_CLUSTER = new RegExp(`^.${MARKS}*`, 'u')
const VOWELS = /[ܰ-ܿ]/g

export interface TenseMark {
  /** below/above the first letter; `first` = "I" in the past; `fem` = "she" in the past. */
  kind: 'below' | 'above' | 'first' | 'fem'
  /** The word with this one mark where the font would draw it. */
  marked: string
  /** How many characters the first letter and its marks take in `word`. */
  firstLength: number
  /** above only: the word without the first letter's vowel. */
  bare?: string
  /** above, future only: the word with a zqofo on the first letter, where the present's vowel stands. */
  ghost?: string
}

export interface PreparedWord {
  /** The text shown: the form without its tense dots. */
  word: string
  marks: TenseMark[]
  /** future "I" and "we": the word without the first letter's vowel, which is redrawn a little higher. */
  raise: string | null
}

/** A base character followed by its combining marks. */
export function clusters(text: string): string[] {
  const out: string[] = []
  const mark = new RegExp(`^${MARKS}$`, 'u')
  for (const ch of text) {
    if (mark.test(ch) && out.length) out[out.length - 1] += ch
    else out.push(ch)
  }
  return out
}

/** One word of a form (no spaces), split into the text to show and the dots to draw. */
export function prepareWord(text: string, tense: Tense): PreparedWord {
  if (tense === 'past') {
    if (![BELOW, ABOVE, FEMININE].some(mark => text.includes(mark))) return { word: text, marks: [], raise: null }
    const plain = clusters(text).map(c => [...c].filter(ch => ch !== BELOW && ch !== ABOVE && ch !== FEMININE).join(''))
    const first = plain[0] ?? ''
    const last = plain[plain.length - 1] ?? ''
    const head = plain.slice(0, -1).join('')
    const marks: TenseMark[] = []
    if (text.includes(BELOW)) marks.push({ kind: 'below', marked: first + BELOW + plain.slice(1).join(''), firstLength: first.length })
    // "I": a qushoyo on the final letter, exactly where the font puts one
    if (text.endsWith(ABOVE)) marks.push({ kind: 'first', marked: head + last + QUSHOYO, firstLength: 0 })
    // "she": the font's own feminine dot, left of the final letter
    if (text.includes(FEMININE)) marks.push({ kind: 'fem', marked: head + last + FEMININE, firstLength: 0 })
    return { word: plain.join(''), marks, raise: null }
  }

  if (text.includes(ABOVE)) {
    const word = text.replaceAll(ABOVE, '')
    const first = FIRST_CLUSTER.exec(word)?.[0] ?? ''
    const rest = word.slice(first.length)
    const letter = first.replace(VOWELS, '')
    const mark: TenseMark = { kind: 'above', marked: first + ABOVE + rest, firstLength: first.length, bare: letter + rest }
    let raise: string | null = null
    if (tense === 'future') {
      // The future's first vowel is not where the present's is, so the dot is measured against a
      // ghost zqofo; on ܢ (we) and ܐ (I) the vowel itself also sits a bit higher.
      mark.ghost = letter + ZQOFO + rest
      if (letter[0] === 'ܢ' || letter[0] === 'ܐ') raise = letter + rest
    }
    return { word, marks: [mark], raise }
  }

  if (text.includes(BELOW)) {
    const word = text.replaceAll(BELOW, '')
    const first = FIRST_CLUSTER.exec(word)?.[0] ?? ''
    return { word, marks: [{ kind: 'below', marked: first + BELOW + word.slice(first.length), firstLength: first.length }], raise: null }
  }

  return { word: text, marks: [], raise: null }
}

/** Offsets in em, set by eye with the Owner. */
const BELOW_UP: Record<string, number> = { ܓ: 0.12, ܟ: 0.05 }
const BELOW_BACK: Record<string, number> = { ܦ: 0.12, ܥ: 0.23 }
const BELOW_BACK_DEFAULT = 0.07
const FUTURE_BELOW_BACK = 0.05
const FIRST_UP = 0.07
const ABOVE_GAP = 0.04
const ABOVE_LOWER = 0.06
const FUTURE_ABOVE_RIGHT: Record<string, number> = { ܢ: 0.12, ܐ: 0.12 }
const RAISE = 0.07

interface Pixels { data: Uint8ClampedArray, width: number, height: number }

/**
 * Draws one word's tense dots. `host` is the positioned wrapper, `textEl` the element holding only
 * the word's text node. Safe to call again (on resize, after the font loads): it clears what it drew.
 */
export function drawTenseDots(host: HTMLElement, textEl: HTMLElement, prepared: PreparedWord, tense: Tense) {
  host.querySelectorAll('.tense-dot, .tense-overlay').forEach(el => el.remove())
  textEl.classList.remove('is-raised')
  const node = textEl.firstChild
  if (!node || node.nodeType !== Node.TEXT_NODE || (!prepared.marks.length && !prepared.raise)) return

  const word = prepared.word
  const cs = getComputedStyle(textEl)
  const em = Number.parseFloat(cs.fontSize)
  const font = `${cs.fontStyle} ${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`
  const box = textEl.getBoundingClientRect()
  const hostBox = host.getBoundingClientRect()
  if (!box.width || !em) return
  const dpr = window.devicePixelRatio || 1
  const pad = Math.ceil(0.6 * em)
  const width = Math.ceil(box.width) + 2 * pad
  const height = Math.ceil(box.height) + 2 * pad
  // The hidden canvas's origin sits `pad` px up and left of the word; the host may start elsewhere.
  const originX = box.left - hostBox.left - pad
  const originY = box.top - hostBox.top - pad

  function render(text: string): Pixels | null {
    const canvas = document.createElement('canvas')
    canvas.width = width * dpr
    canvas.height = height * dpr
    const ctx = canvas.getContext('2d', { willReadFrequently: true })
    if (!ctx) return null
    ctx.scale(dpr, dpr)
    ctx.font = font
    ctx.direction = 'rtl'
    ctx.textAlign = 'right'
    ctx.textBaseline = 'alphabetic'
    ctx.fillText(text, pad + box.width, pad + ctx.measureText(text).fontBoundingBoxAscent)
    return ctx.getImageData(0, 0, canvas.width, canvas.height)
  }

  const plain = render(word)
  if (!plain) return
  const alpha = (pixels: Pixels, x: number, y: number) => pixels.data[(y * pixels.width + x) * 4 + 3] ?? 0
  const firstLetter = word.replace(new RegExp(MARKS, 'gu'), '')[0] ?? ''

  if (prepared.raise) {
    const bare = render(prepared.raise)
    if (bare) {
      let x0 = Infinity, x1 = -1, y0 = Infinity, y1 = -1
      for (let y = 0; y < plain.height; y++) {
        for (let x = 0; x < plain.width; x++) {
          if (alpha(plain, x, y) - alpha(bare, x, y) >= 8) {
            if (x < x0) x0 = x
            if (x > x1) x1 = x
            if (y < y0) y0 = y
            if (y > y1) y1 = y
          }
        }
      }
      if (x1 >= 0) {
        // The word's own text is hidden; a copy without the vowel is laid over it, and only the
        // vowel's own pixels are lifted (a clipped text copy also caught the olaf's tip).
        textEl.classList.add('is-raised')
        const copy = document.createElement('span')
        copy.className = 'tense-overlay tense-copy'
        copy.setAttribute('aria-hidden', 'true')
        copy.textContent = prepared.raise
        copy.style.left = `${box.left - hostBox.left}px`
        copy.style.top = `${box.top - hostBox.top}px`
        copy.style.lineHeight = `${box.height}px`
        host.appendChild(copy)

        const cw = x1 - x0 + 1
        const ch = y1 - y0 + 1
        const canvas = document.createElement('canvas')
        canvas.width = cw
        canvas.height = ch
        const ctx = canvas.getContext('2d')
        if (ctx) {
          const out = ctx.createImageData(cw, ch)
          for (let y = 0; y < ch; y++) {
            for (let x = 0; x < cw; x++) {
              out.data[(y * cw + x) * 4 + 3] = Math.max(0, alpha(plain, x + x0, y + y0) - alpha(bare, x + x0, y + y0))
            }
          }
          ctx.putImageData(out, 0, 0)
          // Painted through a mask in the text's own colour, so it follows the theme.
          const vowel = document.createElement('span')
          vowel.className = 'tense-overlay tense-vowel'
          vowel.setAttribute('aria-hidden', 'true')
          const url = `url(${canvas.toDataURL()})`
          vowel.style.maskImage = url
          vowel.style.setProperty('-webkit-mask-image', url)
          vowel.style.width = `${cw / dpr}px`
          vowel.style.height = `${ch / dpr}px`
          vowel.style.left = `${originX + x0 / dpr}px`
          vowel.style.top = `${originY + y0 / dpr - RAISE * em}px`
          host.appendChild(vowel)
        }
      }
    }
  }

  const dots: { x: number, y: number, d: number }[] = []
  for (const mark of prepared.marks) {
    const marked = render(mark.marked)
    if (!marked) continue

    if (mark.kind === 'above') {
      // Its bottom level with the bottom of the first letter's vowel, a little to the vowel's left.
      // The vowel is found by drawing the word without it; the dot's size is the font's own dot's.
      const ghost = mark.ghost ? render(mark.ghost) : plain
      const bare = mark.bare ? render(mark.bare) : null
      if (!ghost || !bare) continue
      let vx = Infinity, vy = -1, fx = Infinity, fX = -1, fy = Infinity, fY = -1
      for (let y = 0; y < plain.height; y++) {
        for (let x = 0; x < plain.width; x++) {
          if (alpha(ghost, x, y) - alpha(bare, x, y) >= 64) {
            if (x < vx) vx = x
            if (y > vy) vy = y
          }
          if (alpha(marked, x, y) - alpha(plain, x, y) >= 64) {
            if (x < fx) fx = x
            if (x > fX) fX = x
            if (y < fy) fy = y
            if (y > fY) fY = y
          }
        }
      }
      if (fX < 0) continue
      if (vy < 0) {
        // No vowel: the font's own dot.
        vx = fx + (fX - fx) / 2 + ABOVE_GAP * em * dpr + (fX - fx + 1) / 2
        vy = fY - ABOVE_LOWER * em * dpr
      }
      const d = Math.max(3, Math.round(Math.max(Math.max(fX - fx + 1, fY - fy + 1) / dpr, 0.08 * em)))
      const right = tense === 'future' ? (FUTURE_ABOVE_RIGHT[firstLetter] ?? 0) : 0
      dots.push({ x: vx / dpr - ABOVE_GAP * em - d / 2 + right * em, y: (vy + 1) / dpr - d / 2 + ABOVE_LOWER * em, d })
      continue
    }

    let dx = 0
    let dy = 0
    // "I": just above where the final letter's qushoyo would sit
    if (mark.kind === 'first') dy = -Math.round(FIRST_UP * em * dpr)
    if (mark.kind === 'below') {
      dy = -Math.round((BELOW_UP[firstLetter] ?? 0) * em * dpr)
      // From under the first letter to just before its end (its left edge: the script runs right to left).
      const range = document.createRange()
      range.setStart(node, 0)
      range.setEnd(node, mark.firstLength)
      const r = range.getBoundingClientRect()
      const back = ((BELOW_BACK[firstLetter] ?? BELOW_BACK_DEFAULT) + (tense === 'future' ? FUTURE_BELOW_BACK : 0)) * em
      dx = Math.round((r.left - (r.left + r.right) / 2 + back) * dpr)
    }
    let minX = Infinity, minY = Infinity, maxX = -1, maxY = -1, weight = 0, sumX = 0, sumY = 0
    for (let y = 0; y < marked.height; y++) {
      for (let x = 0; x < marked.width; x++) {
        const a = alpha(marked, x, y) - alpha(plain, x, y)
        if (a < 64) continue
        if (x < minX) minX = x
        if (x > maxX) maxX = x
        if (y < minY) minY = y
        if (y > maxY) maxY = y
        weight += a
        sumX += a * x
        sumY += a * y
      }
    }
    if (!weight) continue
    dots.push({ x: (sumX / weight + dx) / dpr, y: (sumY / weight + dy) / dpr, d: Math.max(maxX - minX + 1, maxY - minY + 1) / dpr })
  }

  for (const dot of dots) {
    // A real round element, not painted pixels: the browser draws its edge cleanly. The size of the
    // font's own dot, in whole pixels, on the pixel grid.
    const size = Math.max(3, Math.round(Math.max(dot.d, 0.08 * em)))
    const el = document.createElement('i')
    el.className = 'tense-dot'
    el.setAttribute('aria-hidden', 'true')
    el.style.width = el.style.height = `${size}px`
    el.style.left = `${Math.round(hostBox.left + originX + dot.x - size / 2) - hostBox.left}px`
    el.style.top = `${Math.round(hostBox.top + originY + dot.y - size / 2) - hostBox.top}px`
    host.appendChild(el)
  }
}

let fontReady: Promise<unknown> | null = null

/** Resolves once Serto Urhoy can be measured (drawing before that measures the fallback font). */
export function grammarFontReady(): Promise<unknown> {
  if (!fontReady) {
    fontReady = document.fonts
      ? document.fonts.load('25px "Serto Urhoy"').then(() => document.fonts.ready).catch(() => undefined)
      : Promise.resolve()
  }
  return fontReady
}
