using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// An edit to a chant's fields, as the backoffice submits it.
/// </summary>
/// <remarks>
/// <c>ModeId</c> is omitted for a section that has no modes — the madroshe. See
/// <see cref="CreateChantRequest"/>.
/// </remarks>
public sealed record UpdateChantRequest(
    string SyriacIncipit,
    string Transliteration,
    Guid SectionId,
    Guid? ModeId = null,
    string? SyriacIncipitVocalized = null,
    ChantVariantKind VariantKind = ChantVariantKind.None,
    int? VariantNumber = null,
    Guid? InheritsMelodyFromId = null);
