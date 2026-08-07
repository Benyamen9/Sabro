# -*- coding: utf-8 -*-
"""
Turns the 263 `_note` flags on scripts/shmo-figures.json into a worklist.

"263 figures carry unreviewed dating notes" is true but not actionable — it does
not say which of them could actually make the game give a wrong answer. Every
attribute is a hint, and `era` is a century, so the only notes that matter for
play are the ones whose uncertainty could move a figure ACROSS a century line.

This is a heuristic sort by phrasing, not scholarly review. It decides what to
read first; it does not decide anything.
"""
import json, io, os, re, collections

REPO = r"C:\Users\Benyamen Kaplan\Hito\Sabro"
SRC = os.path.join(REPO, "scripts", "shmo-figures.json")
OUT = os.path.join(REPO, "scripts", "shmo-dating-triage.md")

data = json.load(io.open(SRC, encoding="utf-8"))
figures = data["figures"]
noted = [f for f in figures if f.get("_note")]

# The ADVERSARY notes open with an identical paragraph about the roster having no
# alignment attribute. That is a modelling remark, not a dating one — but each
# note continues with figure-specific text that often IS about dating, so the
# boilerplate is stripped rather than the whole note dismissed.
BOILERPLATE = re.compile(
    r"^ADVERSARY\s*[\u2014-]\s*the roster has no alignment attribute.*?"
    r"read exactly as they do for anyone else\.\s*",
    re.S,
)

SIGNALS = [
    ("contested", r"\b(contested|disputed|dispute\b|debated|controvers|decline to date|should not silently settle)"),
    ("legendary", r"\b(legendary|tradition holds|traditional attribution|hagiograph|not a historical)"),
    ("floruit-only", r"\b(fl\.|floruit)"),
    ("chronology-system", r"\b(Ussher|Masoretic|Septuagint|chronolog)"),
    ("century-range", r"\b\d{1,2}(?:st|nd|rd|th)[\s/-]*(?:or|to|/|-)[\s/-]*\d{1,2}(?:st|nd|rd|th)\b"),
    ("approximate", r"(\bc\.\s*\d|\bcirca\b|\bapprox|\babout\s+\d)"),
]

# Ordered worst-first: a note hitting several is filed under the most severe.
SEVERITY = ["contested", "century-range", "legendary", "floruit-only", "chronology-system", "approximate"]

buckets = collections.defaultdict(list)
for fig in noted:
    body = BOILERPLATE.sub("", fig["_note"]).strip()
    found = {name for name, pat in SIGNALS if re.search(pat, body, re.I)}
    bucket = next((s for s in SEVERITY if s in found), "no-dating-language")
    buckets[bucket].append((fig, body))


def era_label(era):
    era = int(era)
    return f"{abs(era)}{'th c. BC' if era < 0 else 'th c. AD'}"


HEADINGS = {
    "contested": (
        "A. The note says something is contested — read these first",
        "Each of these calls something disputed. **Read what** — for some it is the date, for others "
        "the christological label or the role, which the sort cannot tell apart. Where it is the "
        "date, the game states a wrong century as fact.",
    ),
    "century-range": (
        "B. Spans more than one century",
        "The note offers a range that crosses a century boundary, so the stored `era` is a choice "
        "between them rather than a reading of the source.",
    ),
    "legendary": (
        "C. Legendary or traditional",
        "Dated by tradition rather than evidence. Defensible for a game built on Syriac tradition, "
        "but it should be a decision, not an accident.",
    ),
    "floruit-only": (
        "D. Floruit only",
        "No birth or death, only a period of activity. The century is usually safe; worth a glance.",
    ),
    "chronology-system": (
        "E. Depends on which chronology",
        "Primeval figures whose dates follow Ussher/Masoretic reckoning. A Septuagint chronology "
        "moves them by centuries — but they move TOGETHER, so relative ordering survives.",
    ),
    "approximate": (
        "F. Approximate but bounded",
        "A `c.` date well inside its century. Lowest risk: the century the game uses does not change.",
    ),
    "no-dating-language": (
        "G. Not about dating",
        "Modelling remarks — naming, category choice, the missing alignment attribute. No dating "
        "claim to review.",
    ),
}

lines = []
lines.append("# Shmo dating notes — triage\n")
lines.append(
    "`scripts/shmo-figures.json` carries a `_note` on **%d of its %d figures**, all published "
    "unreviewed. Every attribute in Shmo is a hint and `era` is a century, so the notes worth "
    "your time are the ones whose uncertainty could move a figure across a century line.\n"
    % (len(noted), len(figures))
)
lines.append(
    "> Sorted by the language each note uses, not by reading the history. This decides what to "
    "read first; it decides nothing else. Generated from the dataset — regenerate rather than "
    "hand-edit.\n"
)
SUMMARY = {
    "contested": "Something is called disputed — the date for some, a label or role for others",
    "century-range": "The range crosses a century line, so `era` is a choice between two",
    "legendary": "Dated by tradition rather than evidence",
    "floruit-only": "Only a period of activity; the century is usually safe",
    "chronology-system": "Ussher/Masoretic primeval dates — they move together, so order survives",
    "approximate": "A `c.` date well inside its century; the game's answer does not change",
    "no-dating-language": "Naming, category and modelling remarks — no dating claim at all",
}

lines.append("| Group | Figures | What it means |")
lines.append("|---|---:|---|")
for key in SEVERITY + ["no-dating-language"]:
    if not buckets[key]:
        continue
    letter = HEADINGS[key][0].split(".")[0]
    lines.append(f"| **{letter}** | {len(buckets[key])} | {SUMMARY[key]} |")
lines.append("")
lines.append(
    "**The short version:** **%d figures** are in groups A–C, where the stored century could "
    "actually be wrong. The other %d are approximations inside their own century, primeval dates "
    "that shift together, or remarks that are not about dating at all. The blocker is an "
    "afternoon, not a project.\n"
    % (
        sum(len(buckets[k]) for k in ("contested", "century-range", "legendary")),
        sum(len(buckets[k]) for k in ("floruit-only", "chronology-system", "approximate", "no-dating-language")),
    )
)

for key in SEVERITY + ["no-dating-language"]:
    rows = buckets[key]
    if not rows:
        continue
    title, blurb = HEADINGS[key]
    lines.append(f"\n## {title}  ({len(rows)})\n")
    lines.append(blurb + "\n")
    if key == "no-dating-language":
        lines.append("<details><summary>Show the %d names</summary>\n" % len(rows))
        lines.append(", ".join(sorted(f["name"] for f, _ in rows)))
        lines.append("\n</details>\n")
        continue
    for fig, body in sorted(rows, key=lambda r: int(r[0]["era"])):
        lines.append(f"### {fig['name']}  ·  era `{fig['era']}` ({era_label(fig['era'])})")
        lines.append(f"*{fig['category']} · {fig['role']} · {fig['region']} · {fig['period']}*\n")
        lines.append(f"> {body}\n")

io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
print("wrote", OUT)
for key in SEVERITY + ["no-dating-language"]:
    if buckets[key]:
        print(f"  {len(buckets[key]):4d}  {key}")
