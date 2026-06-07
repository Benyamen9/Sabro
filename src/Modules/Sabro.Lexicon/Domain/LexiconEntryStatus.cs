namespace Sabro.Lexicon.Domain;

/// <summary>
/// Editorial lifecycle of a <see cref="LexiconEntry"/>. A <see cref="Draft"/> may
/// hold partial data; only a <see cref="Published"/> entry (all of en/fr/nl meanings
/// present) may be marked playable or served to clients.
/// </summary>
public enum LexiconEntryStatus
{
    Draft,
    Published,
}
