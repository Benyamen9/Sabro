namespace Sabro.Play.Application.Mno;

/// <summary>
/// Today's Mno puzzle as served to clients. The full solution ships with the
/// puzzle — like Meltho's word, the client evaluates guesses locally and needs
/// the exact board form (<paramref name="TileForm"/>) for per-tile feedback.
/// <paramref name="Difficulty"/> is the ladder level this puzzle belongs to,
/// in the lowercase form the query parameter uses (beginner … extreme).
/// <paramref name="TileCount"/> is the board width the tile form occupies
/// (letters and operators; combining marks don't count).
/// </summary>
public sealed record MnoPuzzleDto(
    DateOnly Date,
    string Difficulty,
    int Target,
    int TileCount,
    string Expression,
    string TileForm);
