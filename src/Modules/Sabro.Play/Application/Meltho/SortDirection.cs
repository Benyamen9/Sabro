namespace Sabro.Play.Application.Meltho;

/// <summary>Direction for a <see cref="LibrarySort"/>. When unspecified the service applies the
/// field's natural default (recent → descending, alphabetical and length → ascending).</summary>
public enum SortDirection
{
    Ascending = 0,

    Descending = 1,
}
