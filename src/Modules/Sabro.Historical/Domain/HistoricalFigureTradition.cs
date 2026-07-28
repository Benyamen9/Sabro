namespace Sabro.Historical.Domain;

/// <summary>
/// The ecclesial tradition a figure belongs to.
/// </summary>
/// <remarks>
/// <para>
/// Sabro is a West Syriac project, but the roster is not a West Syriac roster:
/// it holds the undivided church that precedes the divisions, the sister
/// churches of the Oriental Orthodox communion, and the traditions Syriac
/// Christianity has since come to terms with. A hint that only distinguished
/// "ours" from "not ours" would be both a poor hint and a poor description.
/// </para>
/// <para>
/// Persisted as the enum member name (string conversion), so adding a tradition
/// is a code and data change with no migration at all — the column is a plain
/// varchar. Renaming an existing value is a breaking change for the
/// <c>/api/v1/</c> contract.
/// </para>
/// </remarks>
public enum HistoricalFigureTradition
{
    /// <summary>
    /// The undivided church, before Chalcedon (451). Ephrem, Aphrahat, Athanasius,
    /// the Cappadocians and Cyril all sit here: they precede the divisions and
    /// belong to every tradition that follows, so assigning them a side would be
    /// an anachronism.
    /// </summary>
    PreChalcedonian,

    /// <summary>Syriac Orthodox — the tradition of bar Ṣalibi and this project.</summary>
    WestSyriac,

    /// <summary>The Church of the East.</summary>
    EastSyriac,

    /// <summary>Coptic Orthodox — Alexandria and the Egyptian monastic tradition.</summary>
    Coptic,

    /// <summary>Armenian Apostolic.</summary>
    Armenian,

    /// <summary>Ethiopian and Eritrean Orthodox Tewahedo.</summary>
    Ethiopian,

    /// <summary>The Malankara tradition of India, tied to the Syriac Orthodox patriarchate.</summary>
    Malankara,

    /// <summary>Eastern Orthodox — the Chalcedonian Greek tradition.</summary>
    ByzantineChalcedonian,

    /// <summary>The Latin West.</summary>
    Latin,

    /// <summary>
    /// The axis does not apply — biblical figures, who precede the church itself.
    /// An informative answer in its own right, not a missing value.
    /// </summary>
    NotApplicable,
}
