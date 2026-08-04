namespace Sabro.BethGazo.Domain;

/// <summary>
/// Editorial lifecycle of a <see cref="Chant"/>. A <see cref="Draft"/> may hold
/// partial data — a melody transcribed today, its recording made next week. Only
/// a <see cref="Published"/> chant may be marked playable or served to clients.
/// </summary>
public enum ChantStatus
{
    Draft,
    Published,
}
