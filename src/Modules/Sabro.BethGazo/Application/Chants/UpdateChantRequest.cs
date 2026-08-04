namespace Sabro.BethGazo.Application.Chants;

public sealed record UpdateChantRequest(
    string SyriacIncipit,
    string Transliteration,
    Guid ModeId,
    string? SyriacIncipitVocalized = null,
    string? Shuhlofo = null,
    Guid? InheritsMelodyFromId = null);
