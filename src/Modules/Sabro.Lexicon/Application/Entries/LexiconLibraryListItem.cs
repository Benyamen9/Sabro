namespace Sabro.Lexicon.Application.Entries;

/// <summary>Minimal projection of a lexicon entry for the Meltho library list: the word and its glosses.</summary>
public sealed record LexiconLibraryListItem(
    Guid Id,
    string SyriacUnvocalized,
    IReadOnlyList<LexiconMeaningDto> Meanings);
