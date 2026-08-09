using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A new chant, as the backoffice submits it.
/// </summary>
/// <remarks>
/// <c>ModeId</c> is omitted for a section that has no modes — the madroshe.
/// Supplying one there is refused, as is omitting one for a section that does have
/// modes: the section decides, not the caller.
/// </remarks>
public sealed record CreateChantRequest(
    string SyriacIncipit,
    string Transliteration,
    Guid SectionId,
    Guid? ModeId = null,
    string? SyriacIncipitVocalized = null,
    ChantVariantKind VariantKind = ChantVariantKind.None,
    int? VariantNumber = null,
    Guid? InheritsMelodyFromId = null);
