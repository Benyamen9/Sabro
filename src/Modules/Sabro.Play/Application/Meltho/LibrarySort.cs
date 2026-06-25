namespace Sabro.Play.Application.Meltho;

/// <summary>
/// Ordering for the Meltho word library list. The pool is small (the past daily words), so the
/// service materialises the whole set and sorts in memory — alphabetical and length sort on
/// Lexicon fields, which the Play module only sees through the projection.
/// </summary>
public enum LibrarySort
{
    /// <summary>Most recently served first (default) — the order the archive grew in.</summary>
    Recent = 0,

    /// <summary>Syriac alphabetical order of the unvocalized form (the Syriac block is in abjad order).</summary>
    Alphabetical = 1,

    /// <summary>Shortest word first, by playable length; alphabetical within a length.</summary>
    Length = 2,
}
