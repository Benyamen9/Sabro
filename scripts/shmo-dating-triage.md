# Shmo dating notes — triage

`scripts/shmo-figures.json` carries a `_note` on **263 of its 289 figures**, all published unreviewed. Every attribute in Shmo is a hint and `era` is a century, so the notes worth your time are the ones whose uncertainty could move a figure across a century line.

> Sorted by the language each note uses, not by reading the history. This decides what to read first; it decides nothing else. Generated from the dataset — regenerate rather than hand-edit.

| Group | Figures | What it means |
|---|---:|---|
| **A** | 9 | Something is called disputed — the date for some, a label or role for others |
| **B** | 2 | The range crosses a century line, so `era` is a choice between two |
| **C** | 6 | Dated by tradition rather than evidence |
| **D** | 3 | Only a period of activity; the century is usually safe |
| **E** | 14 | Ussher/Masoretic primeval dates — they move together, so order survives |
| **F** | 39 | A `c.` date well inside its century; the game's answer does not change |
| **G** | 190 | Naming, category and modelling remarks — no dating claim at all |

**The short version:** **17 figures** are in groups A–C, where the stored century could actually be wrong. The other 246 are approximations inside their own century, primeval dates that shift together, or remarks that are not about dating at all. The blocker is an afternoon, not a project.


## A. The note says something is contested — read these first  (9)

Each of these calls something disputed. **Read what** — for some it is the date, for others the christological label or the role, which the sort cannot tell apart. Where it is the date, the game states a wrong century as fact.

### Abraham son of Terah  ·  era `-19` (19th c. BC)
*BiblicalOldTestament · Other · IsraelJudah · Patriarchal*

> Conventional Middle Bronze dating. Patriarchal chronology is debated and some scholars decline to date him at all.

### Pharaoh of the Exodus  ·  era `-13` (13th c. BC)
*BiblicalOldTestament · King · Egypt · ExodusAndConquest*

> Unnamed in scripture, so the descriptive title is the name. Dated with Moses on the late-date Exodus; identifying him with a specific pharaoh is exactly the contested question the roster should not silently settle.

### Arius  ·  era `4` (4th c. AD)
*Patristic · Other · Egypt · NiceneEra*

> c. 256-336; the controversy opens 318. Role is Other — he was a presbyter, and the enum has no Priest.

### Nestorius  ·  era `5` (5th c. AD)
*Patristic · Patriarch · AsiaMinor · NiceneEra*

> Patriarch of Constantinople 428-431, condemned at Ephesus. SENSITIVE: "Nestorian" as a label for the Church of the East is contested and rejected by that church; his presence here is a historical entry, not an endorsement of the label. Tradition is NotApplicable because he predates Chalcedon and none of the three enum values fits him.

### Ibas of Edessa  ·  era `5` (5th c. AD)
*Patristic · Bishop · Mesopotamia · NiceneEra*

> d. 457. Condemned then restored at Chalcedon; his letter was later anathematised in the Three Chapters. Tradition is genuinely contested here.

### Movses Khorenatsi  ·  era `5` (5th c. AD)
*Patristic · Commentator · Armenia · NiceneEra*

> The History of the Armenians; the traditional dating is disputed.

### Sergius of Reshaina  ·  era `6` (6th c. AD)
*Patristic · Translator · Mesopotamia · PostChalcedonian*

> d. 536. Translator of Galen and Pseudo-Dionysius. His own christological allegiance is disputed.

### John of Dalyatha  ·  era `8` (8th c. AD)
*Patristic · Monk · Mesopotamia · IslamicEra*

> Mystical writer, later contested within his own church.

### Nonnus of Nisibis  ·  era `9` (9th c. AD)
*Patristic · Other · Mesopotamia · IslamicEra*

> Apologist who debated both Chalcedonians and Muslims.


## B. Spans more than one century  (2)

The note offers a range that crosses a century boundary, so the stored `era` is a choice between them rather than a reading of the source.

### Bardaisan of Edessa  ·  era `2` (2th c. AD)
*Patristic · Other · Mesopotamia · AnteNicene*

> d. 222; born 154, so spans the 2nd-3rd c. Philosopher and hymnographer of Edessa, later judged heterodox. Pre-Chalcedonian, hence NotApplicable.

### Timothy I  ·  era `8` (8th c. AD)
*Patristic · Patriarch · Mesopotamia · IslamicEra*

> Catholicos 780-823, so the office spans the 8th-9th c.; anchored to its start. -> 9 if you prefer the bulk of the reign.


## C. Legendary or traditional  (6)

Dated by tradition rather than evidence. Defensible for a game built on Syriac tradition, but it should be a decision, not an accident.

### Abgar V of Edessa  ·  era `1` (1th c. AD)
*Patristic · King · Mesopotamia · AnteNicene*

> LEGENDARY — the Doctrine of Addai has him corresponding with Christ. Historically Abgar V reigned in Edessa in the 1st c., but the correspondence is not historical. Included because the legend is foundational to Syriac self-understanding, not because the events are.

### Addai  ·  era `1` (1th c. AD)
*Patristic · Apostle · Mesopotamia · AnteNicene*

> LEGENDARY — apostle of Edessa, traditionally one of the seventy. The Church of the East's eucharistic liturgy bears his name.

### Mari  ·  era `1` (1th c. AD)
*Patristic · Apostle · Mesopotamia · AnteNicene*

> LEGENDARY — Addai's disciple; co-name of the Anaphora of Addai and Mari.

### Aggai  ·  era `2` (2th c. AD)
*Patristic · Bishop · Mesopotamia · AnteNicene*

> LEGENDARY — Addai's successor at Edessa.

### Mar Awgin  ·  era `4` (4th c. AD)
*Patristic · Monk · Mesopotamia · NiceneEra*

> Traditional founder of Mesopotamian monasticism; the tradition itself is legendary and the dating is not secure.

### Pelagia of Antioch  ·  era `5` (5th c. AD)
*Patristic · Monk · Syria · NiceneEra*

> The penitent of Antioch who lived as a recluse; Role is Monk for the ascetic life. A staple of Syriac hagiography.


## D. Floruit only  (3)

No birth or death, only a period of activity. The century is usually safe; worth a glance.

### Marcion of Sinope  ·  era `2` (2th c. AD)
*Patristic · Other · AsiaMinor · AnteNicene*

> Floruit c. 144. One of the three heresiarchs Ephrem writes against by name.

### John of Dara  ·  era `9` (9th c. AD)
*Patristic · Bishop · Mesopotamia · IslamicEra*

> Floruit first half of the 9th c.

### Ishodad of Merv  ·  era `9` (9th c. AD)
*Patristic · Commentator · Mesopotamia · IslamicEra*

> Floruit c. 850.


## E. Depends on which chronology  (14)

Primeval figures whose dates follow Ussher/Masoretic reckoning. A Septuagint chronology moves them by centuries — but they move TOGETHER, so relative ordering survives.

### Adam  ·  era `-41` (41th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — dated by chronology, not history. Masoretic/Ussher reckoning (creation c. 4004 BC). A Septuagint-based chronology would put him near -55 instead, and every primeval entry below would shift with him. Region follows Genesis 2:14, which names the Tigris and Euphrates among Eden's rivers.

### Eve  ·  era `-41` (41th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — dated with Adam; same chronology caveat.

### Cain son of Adam  ·  era `-41` (41th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — first generation after Adam; same chronology caveat.

### Seth son of Adam  ·  era `-39` (39th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3874 BC on the Ussher reckoning.

### Enosh son of Seth  ·  era `-38` (38th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3769 BC on the Ussher reckoning. Son of Seth; Genesis 4:26 dates the calling on the name of the Lord to his lifetime.

### Kenan son of Enosh  ·  era `-37` (37th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3679 BC on the Ussher reckoning.

### Mahalalel son of Kenan  ·  era `-37` (37th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3609 BC on the Ussher reckoning.

### Jared son of Mahalalel  ·  era `-36` (36th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3544 BC on the Ussher reckoning. Father of Enoch.

### Enoch son of Jared  ·  era `-34` (34th c. BC)
*BiblicalOldTestament · Prophet · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3382 BC on the Ussher reckoning. Role is Prophet on the strength of Jude 14 ('Enoch, the seventh from Adam, prophesied'); Other is defensible if you would rather reserve Prophet for the writing prophets.

### Methuselah son of Enoch  ·  era `-34` (34th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3317 BC on the Ussher reckoning.

### Lamech son of Methuselah  ·  era `-32` (32th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 3130 BC on the Ussher reckoning. Son of Methuselah and father of Noah. NOTE: Genesis 4:18 names a second Lamech in the line of Cain; only the Sethite one is in the roster, because two figures cannot share an answer name.

### Noah son of Lamech  ·  era `-30` (30th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 2948 BC on the Ussher reckoning; the flood falls c. 2348 BC (-24). Anchored to birth, like the other primeval entries. Region is his Mesopotamian setting, not Ararat.

### Shem son of Noah  ·  era `-25` (25th c. BC)
*BiblicalOldTestament · Other · Mesopotamia · Primeval*

> PRIMEVAL — born c. 2446 BC on the Ussher reckoning. The Semitic line runs through him to Abraham.

### Nimrod son of Cush  ·  era `-23` (23th c. BC)
*BiblicalOldTestament · King · Mesopotamia · Primeval*

> PRIMEVAL — post-flood; Babel falls c. 2242 BC on the Ussher reckoning. Genesis 10:10 makes Babel the beginning of his kingdom, so King rather than Other.


## F. Approximate but bounded  (39)

A `c.` date well inside its century. Lowest risk: the century the game uses does not change.

### Jeroboam son of Nebat  ·  era `-10` (10th c. BC)
*BiblicalOldTestament · King · IsraelJudah · DividedMonarchy*

> First king of the northern kingdom; reigned c. 931-910 BC.

### Ahab son of Omri  ·  era `-9` (9th c. BC)
*BiblicalOldTestament · King · IsraelJudah · DividedMonarchy*

> Reigned c. 874-853 BC; Elijah's royal opponent.

### Athaliah  ·  era `-9` (9th c. BC)
*BiblicalOldTestament · Other · IsraelJudah · DividedMonarchy*

> Reigned c. 841-835 BC — the one reigning queen of Judah, but Role is Other for the same reason as Jezebel. Worth revisiting if a Queen role is ever added.

### Hezekiah son of Ahaz  ·  era `-8` (8th c. BC)
*BiblicalOldTestament · King · IsraelJudah · DividedMonarchy*

> Reigned c. 715-686 BC; Isaiah's royal contemporary. Anchored to the start of the reign, so the 8th c.

### Manasseh of Judah  ·  era `-7` (7th c. BC)
*BiblicalOldTestament · King · IsraelJudah · DividedMonarchy*

> Reigned c. 697-642 BC. Qualified as "of Judah" to keep him clear of Manasseh son of Joseph, who is not in the roster.

### Jeremiah son of Hilkiah  ·  era `-6` (6th c. BC)
*BiblicalOldTestament · Prophet · IsraelJudah · ExileAndReturn*

> Spans the boundary: called c. 627 (7th c.), but anchored here to the fall of Jerusalem in 586 (6th c.). -7 is equally defensible.

### Clement of Rome  ·  era `1` (1th c. AD)
*Patristic · Bishop · Italy · AnteNicene*

> d. c. 99. The earliest of the apostolic fathers.

### Ignatius of Antioch  ·  era `2` (2th c. AD)
*Patristic · Bishop · Syria · AnteNicene*

> d. c. 108. Role is Bishop, the office, though he is venerated as a martyr; Syriac Orthodox patriarchs take Ignatius as a regnal name after him.

### Polycarp of Smyrna  ·  era `2` (2th c. AD)
*Patristic · Bishop · AsiaMinor · AnteNicene*

> d. c. 155; a disciple of John, by Irenaeus' account.

### Justin Martyr  ·  era `2` (2th c. AD)
*Patristic · Martyr · Italy · AnteNicene*

> d. c. 165. Born in Samaria, taught and died at Rome.

### Irenaeus of Lyons  ·  era `2` (2th c. AD)
*Patristic · Bishop · Other · AnteNicene*

> d. c. 202. From Smyrna but bishop in Gaul, which the Region enum cannot express.

### Clement of Alexandria  ·  era `2` (2th c. AD)
*Patristic · Commentator · Egypt · AnteNicene*

> d. c. 215.

### Mani  ·  era `3` (3th c. AD)
*Patristic · Other · Mesopotamia · AnteNicene*

> c. 216-274, born in Mesopotamia. Ephrem's Prose Refutations answer him directly.

### Tertullian  ·  era `3` (3th c. AD)
*Patristic · Commentator · Other · AnteNicene*

> d. c. 240, Carthage. Region is Other — North Africa beyond Egypt is not in the enum.

### Origen  ·  era `3` (3th c. AD)
*Patristic · Commentator · Egypt · AnteNicene*

> d. c. 253. The exegetical tradition behind both the Antiochene and Alexandrian schools, though later condemned.

### Hippolytus of Rome  ·  era `3` (3th c. AD)
*Patristic · Bishop · Italy · AnteNicene*

> d. c. 235.

### Gregory Thaumaturgus  ·  era `3` (3th c. AD)
*Patristic · Bishop · AsiaMinor · AnteNicene*

> d. c. 270; Origen's pupil.

### Simeon bar Sabbae  ·  era `4` (4th c. AD)
*Patristic · Patriarch · Persia · NiceneEra*

> Catholicos, martyred c. 341 in the Great Persecution under Shapur II — who is also in the roster. Role is Patriarch (the office) rather than Martyr (the death).

### Gregory of Nyssa  ·  era `4` (4th c. AD)
*Patristic · Bishop · AsiaMinor · NiceneEra*

> d. c. 395.

### Eusebius of Caesarea  ·  era `4` (4th c. AD)
*Patristic · Bishop · IsraelJudah · NiceneEra*

> d. c. 339; the Ecclesiastical History.

### Diodore of Tarsus  ·  era `4` (4th c. AD)
*Patristic · Bishop · AsiaMinor · NiceneEra*

> d. c. 390. Founder of the Antiochene exegetical school the East Syriac tradition inherits.

### Didymus the Blind  ·  era `4` (4th c. AD)
*Patristic · Commentator · Egypt · NiceneEra*

> d. c. 398.

### John Cassian  ·  era `4` (4th c. AD)
*Patristic · Monk · Italy · NiceneEra*

> d. c. 435. Carried Egyptian monastic practice to the Latin west.

### Gregory the Illuminator  ·  era `4` (4th c. AD)
*Patristic · Patriarch · Armenia · NiceneEra*

> d. c. 331. Converted Armenia and founded its church — pre-Chalcedonian, so the Armenian tradition proper begins after him.

### Shenoute of Atripe  ·  era `4` (4th c. AD)
*Patristic · Monk · Egypt · NiceneEra*

> d. c. 465, anchored to the long abbacy that begins in the 4th c. The major author of Coptic literature.

### Eutyches  ·  era `5` (5th c. AD)
*Patristic · Monk · AsiaMinor · NiceneEra*

> c. 380-456, condemned at Chalcedon in 451. Named here because the miaphysite tradition is routinely and wrongly conflated with his position.

### Narsai of Nisibis  ·  era `5` (5th c. AD)
*Patristic · Commentator · Mesopotamia · PostChalcedonian*

> d. c. 502; anchored to his School of Nisibis career, which begins in the 5th c.

### Theodoret of Cyrrhus  ·  era `5` (5th c. AD)
*Patristic · Bishop · Syria · NiceneEra*

> d. c. 460. Accepted Chalcedon; his writings were later condemned in the Three Chapters.

### Barsauma of Nisibis  ·  era `5` (5th c. AD)
*Patristic · Bishop · Mesopotamia · PostChalcedonian*

> d. c. 491. Consolidated the School of Nisibis and the Persian church's independence.

### Jacob of Serugh  ·  era `6` (6th c. AD)
*Patristic · Bishop · Mesopotamia · PostChalcedonian*

> d. 521, bishop of Batnan. Born c. 451, so most of his life is 5th c.; anchored to his death and episcopate.

### Simeon of Beth Arsham  ·  era `6` (6th c. AD)
*Patristic · Bishop · Persia · PostChalcedonian*

> d. c. 540.

### John of Ephesus  ·  era `6` (6th c. AD)
*Patristic · Bishop · Syria · PostChalcedonian*

> d. c. 589. Historian of the miaphysite church.

### Paul of Tella  ·  era `7` (7th c. AD)
*Patristic · Translator · Mesopotamia · PostChalcedonian*

> The Syro-Hexapla, c. 617.

### John of Damascus  ·  era `8` (8th c. AD)
*Patristic · Monk · Syria · IslamicEra*

> d. c. 749. Wrote under Umayyad rule from the monastery of Mar Saba — the roster's clearest Chalcedonian voice.

### Thomas of Marga  ·  era `9` (9th c. AD)
*Patristic · Monk · Mesopotamia · IslamicEra*

> Author of the Book of Governors, c. 840.

### Photios of Constantinople  ·  era `9` (9th c. AD)
*Patristic · Patriarch · AsiaMinor · IslamicEra*

> d. c. 893.

### Gregory of Narek  ·  era `10` (10th c. AD)
*Patristic · Monk · Armenia · IslamicEra*

> d. c. 1003. The Book of Lamentations is the summit of Armenian devotional writing.

### Severus ibn al-Muqaffa  ·  era `10` (10th c. AD)
*Patristic · Bishop · Egypt · IslamicEra*

> d. c. 987. Wrote in Arabic; the History of the Patriarchs of Alexandria is attributed to him.

### Tekle Haymanot  ·  era `13` (13th c. AD)
*Patristic · Monk · Ethiopia · SyriacRenaissance*

> d. c. 1313. The most widely venerated Ethiopian saint.


## G. Not about dating  (190)

Modelling remarks — naming, category choice, the missing alignment attribute. No dating claim to review.

<details><summary>Show the 190 names</summary>

Aaron the Priest, Abba Aregawi, Abdisho bar Brikha, Abel son of Adam, Abraham of Kashkar, Absalom son of David, Abu al-Barakat ibn Kabar, Ambrose of Milan, Anna the Prophetess, Annas, Antony the Great, Aphrahat the Persian Sage, Aquila of Pontus, Athanasius Gammolo, Athanasius of Alexandria, Athanasius of Balad, Augustine of Hippo, Babai the Great, Balaam son of Beor, Bar Hebraeus, Barabbas, Barnabas the Apostle, Barsamya of Edessa, Baruch son of Neriah, Baselios Yeldo, Basil of Caesarea, Bathsheba daughter of Eliam, Belshazzar, Benjamin I of Alexandria, Boaz of Bethlehem, Caiaphas, Caleb son of Jephunneh, Cyprian of Carthage, Cyriacus of Tagrit, Cyril of Alexandria, Cyril of Jerusalem, Dadisho Qatraya, Daniel the Prophet, Daniel the Stylite, Delilah, Dionysius bar Salibi, Dionysius of Tell Mahre, Dionysius the Great of Malankara, Dioscorus of Alexandria, Eleazar son of Aaron, Elias of Nisibis, Elizabeth wife of Zechariah, Ephrem the Syrian, Epiphanius of Salamis, Esau son of Isaac, Esther the Queen, Evagrius Ponticus, Ewostatewos, Ezekiel son of Buzi, Ezra the Scribe, Febronia of Nisibis, Frumentius, George, Bishop of the Arabs, Goliath of Gath, Gregorios of Parumala, Gregory Palamas, Gregory of Nazianzus, Gregory the Great, Gurya of Edessa, Habib the Deacon, Hagar the Egyptian, Ham son of Noah, Haman the Agagite, Hannah wife of Elkanah, Herod Antipas, Herod the Great, Herodias, Hilary of Poitiers, Hunayn ibn Ishaq, Ibn al-Tayyib, Isaac of Nineveh, Isaac son of Abraham, Isaiah son of Amoz, Ishmael son of Abraham, Ishoyahb III, Jacob Baradaeus, Jacob of Edessa, Jacob of Nisibis, Jacob son of Isaac, James son of Zebedee, James the Brother of the Lord, Japheth son of Noah, Jerome, Jesse of Bethlehem, Jethro the Midianite, Jezebel daughter of Ethbaal, Joab son of Zeruiah, Job of Edessa, John Chrysostom, John of Sedre, John of Sinai, John of Tella, John son of Zebedee, Jonah son of Amittai, Jonathan son of Saul, Joseph Hazzaya, Joseph of Nazareth, Joseph son of Jacob, Joshua son of Nun, Josiah son of Amon, Judah son of Jacob, Judas Iscariot, Julian Saba, Julian the Apostate, Korah son of Izhar, Laban the Aramean, Leo the Great, Levi son of Jacob, Lot son of Haran, Luke the Evangelist, Lydia of Thyatira, Macarius the Great, Mar Aba I, Mar Thoma I, Mark the Evangelist, Martha daughter of Pusai, Martha of Bethany, Marutha of Tikrit, Mary of Bethany, Mary the Mother of Jesus, Matthew the Apostle, Maximus the Confessor, Melito of Sardis, Mesrop Mashtots, Michael the Syrian, Michal daughter of Saul, Miriam the Prophetess, Mordecai son of Jair, Moses son of Amram, Moshe bar Kepha, Naomi wife of Elimelech, Nathan the Prophet, Nebuchadnezzar, Nehemiah son of Hacaliah, Nerses Shnorhali, Nerses of Lambron, Pachomius, Papa bar Aggai, Paul of Tarsus, Peter of Callinicum, Philoxenus of Mabbug, Phinehas son of Eleazar, Pontius Pilate, Priscilla of Corinth, Pusai, Rabban Bar Sauma, Rabban Hormizd, Rabbula of Edessa, Rebecca daughter of Bethuel, Ruth the Moabite, Sahak Partev, Sahdona, Salome the Myrrh-bearer, Samuel son of Elkanah, Samuel the Confessor, Sarah wife of Abraham, Sennacherib, Severus of Antioch, Shapur II, Sharbel of Edessa, Shmona of Edessa, Silas the Prophet, Simeon the Righteous, Simeon the Stylite the Elder, Simeon the Stylite the Younger, Simon Magus, Symeon the New Theologian, Tamar wife of Er, Tatian, Thecla of Iconium, Theodore of Mopsuestia, Theophilus of Edessa, Thomas Aquinas, Thomas of Harkel, Thomas the Apostle, Timothy Aelurus, Timothy of Ephesus, Titus of Crete, Yahballaha III, Yared, Yeghishe, Zacharias Rhetor, Zara Yaqob, Zechariah the Priest, Zipporah daughter of Jethro

</details>

