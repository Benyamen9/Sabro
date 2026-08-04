namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// Points a chant at its recording. Separate from <see cref="UpdateChantRequest"/>
/// because attaching audio is an upload, not a field edit — the same split the
/// Lexicon's pronunciation clips use.
/// </summary>
public sealed record SetChantAudioRequest(string? AudioUrl);
