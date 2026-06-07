namespace Sabro.Lexicon.Application.Entries;

/// <summary>Body for the playable-toggle endpoint. Marking playable requires a published entry.</summary>
public sealed record SetPlayableLexiconEntryRequest(bool Playable);
