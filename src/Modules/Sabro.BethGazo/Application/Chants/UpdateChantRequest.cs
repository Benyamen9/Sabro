namespace Sabro.BethGazo.Application.Chants;

/// <param name="ModeId">
/// Omitted for a section that has no modes — the madroshe. See
/// <see cref="CreateChantRequest"/>.
/// </param>
public sealed record UpdateChantRequest(
    string SyriacIncipit,
    string Transliteration,
    Guid SectionId,
    Guid? ModeId = null,
    string? SyriacIncipitVocalized = null,
    string? Shuhlofo = null,
    Guid? InheritsMelodyFromId = null);
