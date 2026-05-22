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

    /// <summary>
    /// When <c>true</c>, write-side calls (<c>UpsertAsync</c>, <c>UpsertManyAsync</c>,
    /// <c>DeleteAsync</c>) await the underlying Meilisearch task before returning,
    /// giving read-after-write consistency at the cost of a few hundred ms per write.
    /// Defaults to <c>false</c> — fire-and-forget is the right trade-off for normal
    /// user writes since Meili is best-effort and Postgres is the source of truth.
    /// Enable for environments that want deterministic search visibility after a write
    /// (e.g. integration tests, single-user admin tooling).
    /// </summary>
    public bool WaitForTasks { get; set; }
}
