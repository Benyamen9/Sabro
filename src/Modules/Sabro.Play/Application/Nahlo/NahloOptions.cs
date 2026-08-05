namespace Sabro.Play.Application.Nahlo;

/// <summary>
/// Configuration for Nahlo daily-puzzle selection, bound from the <c>Nahlo</c>
/// section. The anti-repetition window must stay configurable: a small pool
/// starves under a large window, so it starts low and is raised as the pool
/// grows. Never hardcode it.
/// </summary>
public sealed class NahloOptions
{
    public const string SectionName = "Nahlo";

    /// <summary>
    /// Number of days a chant is barred from reuse after being served. A chant
    /// served within the last this-many days is excluded from selection.
    /// </summary>
    /// <remarks>
    /// Lower than Meltho's and Shmo's 30 on purpose. Those launched against pools
    /// of 42 words and 289 figures; Nahlo's pool is recordings the owner sings
    /// himself, so it starts at zero and grows at the pace of a studio rather than
    /// a spreadsheet. A window wider than the pool leaves the selector with no
    /// candidate and the game answers 409 — raise this as the treasury fills, not
    /// before.
    /// </remarks>
    public int AntiRepetitionWindowDays { get; set; } = 7;
}
