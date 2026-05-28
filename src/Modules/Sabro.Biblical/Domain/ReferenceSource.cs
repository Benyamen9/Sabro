namespace Sabro.Biblical.Domain;

/// <summary>
/// Who originated a biblical cross-reference.
/// </summary>
/// <remarks>
/// Persisted as the enum member name (string conversion), so new values can be
/// added with an ordinary code change and migration. Renaming an existing value
/// is a breaking change for the <c>/api/v1/</c> contract.
/// </remarks>
public enum ReferenceSource
{
    /// <summary>
    /// The commentator (bar Ṣalibi) cites the passage within the source text
    /// itself, marked in the manuscript by a citation siglum.
    /// </summary>
    Author,

    /// <summary>
    /// A parallel added by the translator as apparatus (e.g. "cf. Ps 22:8"),
    /// not present in the commentator's text.
    /// </summary>
    Editorial,
}
