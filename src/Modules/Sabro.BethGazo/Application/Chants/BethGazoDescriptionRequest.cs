namespace Sabro.BethGazo.Application.Chants;

/// <summary>One language's line about a section or a mode, as the backoffice sends it.</summary>
public sealed record BethGazoDescriptionRequest(string Language, string Text);
