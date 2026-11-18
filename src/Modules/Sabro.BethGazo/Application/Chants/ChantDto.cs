using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A chant as the backoffice sees it. Carries the section's and mode's names
/// alongside their ids so a list can be rendered without a second call — the editor
/// reads "Farde · Tlithoyo", not two GUIDs.
/// </summary>
/// <param name="Id">The chant's id.</param>
/// <param name="SyriacIncipit">The chant's opening words in Syriac; null until they are read from the book.</param>
/// <param name="SyriacIncipitVocalized">The same incipit with vowel points; optional, and set only with the plain one.</param>
/// <param name="Transliteration">The melody name, romanized. Its own datum: never derived from the incipit.</param>
/// <param name="SectionId">The section the chant belongs to.</param>
/// <param name="SectionName">That section's name.</param>
/// <param name="ModeId">
/// Null for a chant in a section that has no modes — the madroshe. Not "unfilled":
/// the domain refuses a chant whose mode disagrees with its section either way.
/// </param>
/// <param name="ModeName">Null exactly when <paramref name="ModeId"/> is.</param>
public sealed record ChantDto(
    Guid Id,
    string? SyriacIncipit,
    string? SyriacIncipitVocalized,
    string Transliteration,
    Guid SectionId,
    string SectionName,
    Guid? ModeId,
    string? ModeName,
    ChantVariantKind VariantKind,
    int? VariantNumber,
    Guid? InheritsMelodyFromId,
    string? InheritsMelodyFromTransliteration,
    string? AudioUrl,
    ChantStatus Status,
    bool PlayableInNahlo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
