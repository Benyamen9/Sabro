namespace Sabro.Historical.Domain;

/// <summary>
/// The named era a figure belongs to — an exact-match hint, next to
/// <see cref="HistoricalFigure.Era"/>'s numeric higher/lower one.
/// </summary>
/// <remarks>
/// <para>
/// Not derivable from century plus category, which is why it is stored rather
/// than computed: the 5th century holds both <see cref="NiceneEra"/> and
/// <see cref="PostChalcedonian"/> figures, and the 10th century BC holds both
/// <see cref="UnitedMonarchy"/> and <see cref="DividedMonarchy"/> ones.
/// </para>
/// <para>
/// Persisted as the enum member name (string conversion), so adding a period is
/// an ordinary code change and migration. Renaming one is a breaking change for
/// the <c>/api/v1/</c> contract.
/// </para>
/// </remarks>
public enum HistoricalPeriod
{
    /// <summary>Genesis 1–11, from Adam to the tower of Babel.</summary>
    Primeval,

    /// <summary>Genesis 12–50: Abraham, Isaac, Jacob and their households.</summary>
    Patriarchal,

    /// <summary>Exodus through Joshua — Egypt, the wilderness, the settlement.</summary>
    ExodusAndConquest,

    /// <summary>The book of Judges and the Ruth narrative.</summary>
    Judges,

    /// <summary>Samuel, Saul, David and Solomon, before the kingdom splits.</summary>
    UnitedMonarchy,

    /// <summary>The divided kingdoms and the prophets who address them, to the fall of Judah.</summary>
    DividedMonarchy,

    /// <summary>The Babylonian exile and the return under Persia.</summary>
    ExileAndReturn,

    /// <summary>
    /// Between the last prophets and the New Testament. Currently unpopulated —
    /// it exists so Maccabean and intertestamental figures have a home when added.
    /// </summary>
    SecondTemple,

    /// <summary>The New Testament and the generation of the apostles.</summary>
    Apostolic,

    /// <summary>Christian antiquity before Nicaea (325), including its heresiarchs.</summary>
    AnteNicene,

    /// <summary>Nicaea to Chalcedon (325–451) — Ephrem's century and the christological controversies.</summary>
    NiceneEra,

    /// <summary>After Chalcedon (451): the hierarchies divide and the Syriac traditions take their distinct shape.</summary>
    PostChalcedonian,

    /// <summary>Syriac Christianity under Islamic rule, through the Abbasid period.</summary>
    IslamicEra,

    /// <summary>The Syriac Renaissance of the 11th–13th centuries — bar Salibi, Michael the Syrian, Bar Hebraeus.</summary>
    SyriacRenaissance,
}
