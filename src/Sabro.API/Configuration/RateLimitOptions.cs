namespace Sabro.API.Configuration;

/// <summary>
/// The public rate limit, bound from the <c>RateLimit</c> section. Config rather
/// than constants for the same reason the anti-repetition windows are: the right
/// ceiling depends on live traffic, and finding it should not need a code change
/// and a deploy.
/// </summary>
/// <remarks>
/// The defaults reproduce the limit that was already in force (100 requests a
/// minute), so correcting the partitioning does not also quietly change policy.
/// Note what partitioning does change: until now every request in the ecosystem
/// shared one window, so each bucket after the fix is strictly more permissive
/// than what it replaced — there is no bucket that gets less room than it had.
/// <para>
/// One bucket is worth watching as traffic grows. All five frontends reach the
/// API at its public URL (<c>NUXT_PUBLIC_API_BASE_URL</c>), so their server-side
/// render fetches arrive through Caddy from the host's address rather than from
/// each visitor's — meaning SSR traffic across the hub and all four games shares
/// a single bucket, while browser traffic partitions per visitor. That is still
/// better than today's everything-in-one-window, but it is the bucket that will
/// hit the ceiling first. Raise <see cref="PermitLimit"/> if SSR starts seeing
/// 429s.
/// </para>
/// </remarks>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>Requests allowed per <see cref="WindowSeconds"/> per partition.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Length of the fixed window, in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}
