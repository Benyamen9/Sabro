namespace Sabro.Play.Application.Meltho;

/// <summary>
/// Configuration for Meltho daily-puzzle selection, bound from the <c>Meltho</c>
/// section. The anti-repetition window must stay configurable: a small launch
/// pool (~30–50 words) starves under a large window, so it starts low and is
/// raised toward 365 as the pool grows. Never hardcode it.
/// </summary>
public sealed class MelthoOptions
{
    public const string SectionName = "Meltho";

    /// <summary>
    /// Number of days a word is barred from reuse after being served. A word
    /// served within the last this-many days is excluded from selection. Default
    /// is deliberately low so a small launch pool never runs dry.
    /// </summary>
    public int AntiRepetitionWindowDays { get; set; } = 30;
}
