namespace Sabro.Play.Domain;

/// <summary>
/// Well-known <c>GameId</c> discriminators. The Play module is multi-game by
/// design — results and puzzles are keyed by a free-form game id — but the few
/// ids Sabro itself reasons about live here so they are not stringly-typed at
/// call sites. New client games add a constant here when (and only when) Sabro
/// needs to name them.
/// </summary>
public static class Games
{
    public const string Meltho = "meltho";

    public const string Mno = "mno";
}
