namespace Sabro.Play.Application.Meltho;

/// <summary>
/// The fields <see cref="MelthoLibraryService"/>'s in-memory sort/search shares across its two
/// list DTOs (<see cref="MelthoLibraryEntryDto"/> and <see cref="PlayedLibraryEntryDto"/>) — lets
/// <c>Order</c>/<c>MatchesSearch</c> run once instead of being duplicated per DTO. Purely an
/// implementation-sharing detail; both records already expose these properties by name, so
/// implementing this interface adds no new public surface.
/// </summary>
internal interface ISortableLibraryWord
{
    string SyriacUnvocalized { get; }

    string? SblTransliteration { get; }

    int PlayableLength { get; }

    DateOnly LastPlayedOn { get; }

    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings { get; }
}
