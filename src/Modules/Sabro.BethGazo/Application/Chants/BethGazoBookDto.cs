namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// The whole Beth Gazo as the hub's book page shows it (Owner, 2026-10-25): every
/// section in the book's order with the melodies and chants recorded in it, and
/// every mode with its text.
/// </summary>
/// <remarks>
/// <b>This pairs every melody with its mode, on purpose.</b> The Owner ruled the
/// treasury fully open, to anyone who wants to learn or listen, knowing that a
/// player can then look up today's Nahlo. That ruling is what lets this endpoint
/// make the join <see cref="ChantAnswerOptionsDto"/> refuses; the answer options
/// stay unjoined because Nahlo's own form still should not hand over the mode.
/// </remarks>
public sealed record BethGazoBookDto(
    IReadOnlyList<BethGazoBookSectionDto> Sections,
    IReadOnlyList<BethGazoModeDto> Modes);
