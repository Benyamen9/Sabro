namespace Sabro.Biblical.Domain;

/// <summary>
/// The nature of a biblical cross-reference.
/// </summary>
/// <remarks>
/// Persisted as the enum member name (string conversion), so new values can be
/// added with an ordinary code change and migration. Renaming an existing value
/// is a breaking change for the <c>/api/v1/</c> contract.
/// </remarks>
public enum ReferenceKind
{
    /// <summary>
    /// Explicit, verbatim or near-verbatim citation; typically siglum-marked.
    /// </summary>
    Quotation,

    /// <summary>
    /// An unmarked echo or substructure — the passage is in view but is not
    /// quoted or named.
    /// </summary>
    Allusion,
}
