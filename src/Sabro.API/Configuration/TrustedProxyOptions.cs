namespace Sabro.API.Configuration;

/// <summary>
/// The networks whose <c>X-Forwarded-For</c> header the API will believe, bound
/// from the <c>TrustedProxies</c> section.
/// </summary>
/// <remarks>
/// <para>
/// This has to be an allowlist. <c>X-Forwarded-For</c> is client-supplied text,
/// so trusting it from anywhere would let a caller pick their own rate-limit
/// partition — trading one broken limiter for a spoofable one.
/// </para>
/// <para>
/// The defaults are the loopback and RFC 1918 ranges, which covers the compose
/// bridge without pinning a subnet the network does not declare (<c>internal</c>
/// in <c>docker-compose.prod.yml</c> lets Docker assign it, so hardcoding one
/// would break the day that pool changes). This is safe here because the API
/// publishes no host port — <c>caddy</c> is the only service with a <c>ports:</c>
/// mapping, so nothing off-box can reach Kestrel to present a forged header in
/// the first place. Any deployment that does expose the API directly must narrow
/// this to the proxy's own address.
/// </para>
/// </remarks>
public sealed class TrustedProxyOptions
{
    public const string SectionName = "TrustedProxies";

    public string[] Networks { get; set; } =
    [
        "127.0.0.0/8",
        "::1/128",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
    ];
}
