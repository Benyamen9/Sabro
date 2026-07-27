namespace Sabro.Play.Application.Shmo;

/// <summary>
/// Configuration for Shmo daily-puzzle selection, bound from the <c>Shmo</c>
/// section. The anti-repetition window must stay configurable: a small launch
/// roster starves under a large window, so it starts low and is raised toward
/// 365 as the roster grows. Never hardcode it.
/// </summary>
public sealed class ShmoOptions
{
    public const string SectionName = "Shmo";

    /// <summary>
    /// Number of days a figure is barred from reuse after being served. A figure
    /// served within the last this-many days is excluded from selection. Default
    /// is deliberately low so a small launch roster never runs dry.
    /// </summary>
    public int AntiRepetitionWindowDays { get; set; } = 30;
}
