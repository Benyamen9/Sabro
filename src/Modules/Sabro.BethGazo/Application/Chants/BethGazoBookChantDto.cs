using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// One recorded chant: its mode (null for a section without modes), its variant,
/// its incipit when it has been read, and the recording to play.
/// </summary>
public sealed record BethGazoBookChantDto(
    Guid Id,
    Guid? ModeId,
    ChantVariantKind VariantKind,
    int? VariantNumber,
    string? SyriacIncipit,
    string? SyriacIncipitVocalized,
    string AudioUrl);
