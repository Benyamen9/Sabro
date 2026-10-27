namespace Sabro.Play.Application.Nahlo;

/// <summary>One Nahlo guess, as the client records it: melody, mode, and whether the chant is a shuḥlofo.</summary>
internal sealed record NahloGuess(string Melody, string? Mode, bool IsShuhlofo);
