using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A chant as the backoffice sees it. Carries the mode's name alongside its id so
/// a list can be rendered without a second call — the editor reads "Tlithoyo", not
/// a GUID.
/// </summary>
public sealed record ChantDto(
    Guid Id,
    string SyriacIncipit,
    string? SyriacIncipitVocalized,
    string Transliteration,
    Guid ModeId,
    string ModeName,
    string? Shuhlofo,
    Guid? InheritsMelodyFromId,
    string? InheritsMelodyFromTransliteration,
    string? AudioUrl,
    ChantStatus Status,
    bool PlayableInNahlo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
