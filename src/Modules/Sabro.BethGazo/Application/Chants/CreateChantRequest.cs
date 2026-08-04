namespace Sabro.BethGazo.Application.Chants;

public sealed record CreateChantRequest(
    string SyriacIncipit,
    string Transliteration,
    Guid ModeId,
    string? SyriacIncipitVocalized = null,
    string? Shuhlofo = null,
    Guid? InheritsMelodyFromId = null);
