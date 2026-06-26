namespace Sabro.Play.Application.Meltho;

/// <summary>A player's Meltho streaks: their longest-ever run and the run still live today.</summary>
public readonly record struct MelthoStreak(int Longest, int Current);
