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
    /// <summary>Two single letters (units and tens), + and −, on a 3-tile board.</summary>
    Beginner,

    /// <summary>A two-letter compound meets a single letter (operands ≤ 99), + and −, on a 4-tile board.</summary>
    Easy,

    /// <summary>Full unmarked range (operands ≤ 999), all four operators with a × or ÷ guaranteed, 5-tile board, target ≤ 9,999.</summary>
    Normal,

    /// <summary>Thousands via alfayo (at least one operand ≥ 1,000), always two operators with a × or ÷, 6-tile board, target ≤ 99,999.</summary>
    Hard,

    /// <summary>The whole system: compact marked spellings, at least one operand ≥ 10,000, always two operators with a × or ÷, 6-tile board, up to 999,999.</summary>
    Extreme,
}
