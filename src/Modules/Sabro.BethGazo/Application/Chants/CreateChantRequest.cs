namespace Sabro.BethGazo.Application.Chants;

/// <param name="ModeId">
/// Omitted for a section that has no modes — the madroshe. Supplying one there is
/// refused, as is omitting one for a section that does have modes: the section
/// decides, not the caller.
/// </param>
public sealed record CreateChantRequest(
    string SyriacIncipit,
    string Transliteration,
    Guid SectionId,
    Guid? ModeId = null,
    string? SyriacIncipitVocalized = null,
    string? Shuhlofo = null,
    Guid? InheritsMelodyFromId = null);
