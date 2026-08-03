namespace Sabro.Shared.Abstractions;

/// <summary>
/// How a proposal target names itself in a list, so the review queue can say which
/// word or which figure a proposal is about.
/// </summary>
/// <remarks>
/// Two parts rather than one string because the primary half is often Syriac and has
/// to be rendered right-to-left in its own element; concatenating it with a Latin
/// transliteration produces a bidirectional mess in the markup.
/// </remarks>
/// <param name="Primary">The identifying text — a Syriac form, a person's name.</param>
/// <param name="Secondary">An optional Latin-script aid, e.g. a transliteration.</param>
public sealed record ProposalTargetLabel(string Primary, string? Secondary = null);
