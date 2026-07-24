namespace Sabro.Lexicon.Application.Search;

/// <summary>
/// Direction for a <see cref="LexiconAdminSort"/>. When unspecified the service applies the
/// field's natural default (recent → descending, syriac/status/length → ascending). Duplicated
/// from <c>Sabro.Play.Application.Meltho.SortDirection</c> rather than referenced across modules —
/// the modular-monolith rule forbids cross-module Application-layer references.
/// </summary>
public enum SortDirection
{
    Ascending = 0,

    Descending = 1,
}
