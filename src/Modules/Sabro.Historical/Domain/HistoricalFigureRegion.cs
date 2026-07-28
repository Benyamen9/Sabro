namespace Sabro.Historical.Domain;

/// <summary>
/// The cultural-geographic sphere a figure is chiefly associated with — where
/// their work belongs, not necessarily where they were born.
/// </summary>
/// <remarks>
/// Roughly ordered from the Syriac heartland outward. Persisted as the enum
/// member name (string conversion), so adding a region needs no migration.
/// </remarks>
public enum HistoricalFigureRegion
{
    IsraelJudah,
    Mesopotamia,
    Syria,
    Persia,

    /// <summary>Arabia and the northern desert, including Midian.</summary>
    Arabia,

    Egypt,

    /// <summary>Ethiopia and Eritrea — the Aksumite sphere.</summary>
    Ethiopia,

    /// <summary>Anatolia, including Cappadocia, Ephesus and Constantinople.</summary>
    AsiaMinor,

    /// <summary>The Greek mainland and Aegean.</summary>
    Greece,

    /// <summary>Italy and the Latin West.</summary>
    Italy,

    Armenia,

    /// <summary>The Malabar coast and the Indian church.</summary>
    India,

    Other,
}
