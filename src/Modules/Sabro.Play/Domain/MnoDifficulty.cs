namespace Sabro.Play.Domain;

/// <summary>
/// The Mno difficulty ladder (owner-defined, 2026-07-15). Each level is its own
/// daily puzzle — one shared equation per (date, difficulty), so every player
/// on a level sees the same board. The ladder is pedagogical: each step
/// introduces the next piece of the numeral system.
/// Stored as a string-converted enum (house rule): new levels are a code
/// change + ordinary migration, and renaming an existing one is a breaking
/// API change — the names below are part of the /api/v1 contract.
/// </summary>
public enum MnoDifficulty
{
    /// <summary>Units and tens only (operands 1–99), + and −, target ≤ 199, no marks.</summary>
    Beginner,

    /// <summary>Hundreds letters join (operands 1–499), + − ×, target ≤ 999, no marks.</summary>
    Easy,

    /// <summary>Full unmarked range (operands 1–999, compound hundreds), all four operators, target ≤ 9,999.</summary>
    Normal,

    /// <summary>Thousands via alfayo (operands 1–9,999, at least one ≥ 1,000), all operators, target ≤ 99,999.</summary>
    Hard,

    /// <summary>The whole system: all four marks in compact spellings, operands and target up to 999,999.</summary>
    Extreme,
}
