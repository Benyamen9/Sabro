using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sabro.API.Health;

/// <summary>
/// Maps the two health endpoints and keeps the liveness/readiness split explicit.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Readiness: can this instance actually serve requests (database included).
    /// This is the URL UptimeRobot already watches, so the dependency check goes
    /// here rather than behind a new path nobody is monitoring.
    /// </summary>
    public const string ReadyPath = "/health";

    /// <summary>
    /// Liveness: is the process up. Runs no checks and depends on nothing.
    /// </summary>
    public const string LivePath = "/health/live";

    /// <summary>Matches the camelCase contract the rest of the API serializes with.</summary>
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Maps <see cref="ReadyPath"/> (all registered checks) and <see cref="LivePath"/>
    /// (no checks at all).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Never wire <see cref="ReadyPath"/> into a Docker <c>healthcheck:</c> or a
    /// <c>depends_on: service_healthy</c> gate.</b> It reports on dependencies by design,
    /// so doing that turns a brief database blip into a container restart loop and a
    /// stack that refuses to come up — which is exactly how the 2026-07-28 Meilisearch
    /// outage took the whole site down. <see cref="LivePath"/> exists for that job.
    /// </para>
    /// </remarks>
    public static void MapSabroHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            ResponseWriter = WriteResponseAsync,
        });

        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            // Matches no check, so this answers 200 as long as the process serves
            // requests — no database, no Meilisearch, nothing that can flap.
            Predicate = _ => false,
            ResponseWriter = WriteResponseAsync,
        });
    }

    /// <summary>
    /// Writes the overall status plus a per-check breakdown, so an operator who curls the
    /// endpoint during an incident learns which dependency is down without opening Seq.
    /// Descriptions are the fixed strings the checks return — never exception text.
    /// </summary>
    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = (long)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = (long)entry.Value.Duration.TotalMilliseconds,
                }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
