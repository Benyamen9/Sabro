using System.ComponentModel.DataAnnotations;

namespace Sabro.Shared.Infrastructure.Search;

/// <summary>
/// Bound from the <c>Meilisearch</c> configuration section. Validated eagerly
/// at startup so misconfiguration fails the host rather than the first request.
/// </summary>
public sealed class MeilisearchOptions
{
    public const string SectionName = "Meilisearch";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional in development (Meilisearch can run without a master key) but
    /// required in any environment where the engine is exposed beyond localhost.
    /// Empty/null is accepted here; environment-specific enforcement belongs in
    /// deployment, not in code.
    /// </summary>
    public string? MasterKey { get; set; }

    /// <summary>
    /// Per-request timeout for HTTP calls to Meilisearch. Defaults to 5s — long
    /// enough for healthy upserts, short enough that a hung engine doesn't
    /// stall user writes (which are best-effort anyway).
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00.500", "00:01:00")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
