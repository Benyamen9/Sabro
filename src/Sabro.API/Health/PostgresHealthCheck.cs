using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Sabro.API.Health;

/// <summary>
/// Proves the single shared PostgreSQL database actually answers a query, rather than
/// merely that this process is running.
/// </summary>
/// <remarks>
/// <para>
/// On 2026-07-31 the VPS disk filled, Postgres crash-looped, and every data endpoint
/// returned 500 for about fifteen minutes — while <c>/health</c> stayed 200 throughout,
/// because it checked nothing. UptimeRobot watches <c>/health</c>, so nothing alerted.
/// This check is what makes that monitor mean something.
/// </para>
/// <para>
/// Deliberately a connection + <c>SELECT 1</c> rather than <c>CanConnectAsync</c>: the
/// failure mode to catch is a server that accepts a socket but cannot serve queries
/// (crash recovery, out of disk), and a query is the only thing that distinguishes it.
/// </para>
/// <para>
/// The real exception is logged (it goes to Seq, where the operator can see it) but is
/// never written to the response — the caller is an anonymous monitor and connection
/// details are not its business.
/// </para>
/// </remarks>
public sealed class PostgresHealthCheck : IHealthCheck
{
    /// <summary>Name this check is registered and reported under.</summary>
    public const string Name = "postgres";

    /// <summary>
    /// Cap on the whole probe. UptimeRobot and the CD health gate both give up long
    /// before Npgsql's default timeouts would, so a hung database must surface as a
    /// fast, definite 503 rather than a request that never returns.
    /// </summary>
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(5);

    private readonly string connectionString;
    private readonly ILogger<PostgresHealthCheck> logger;

    public PostgresHealthCheck(IConfiguration configuration, ILogger<PostgresHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        connectionString = configuration.GetConnectionString("Sabro")
            ?? throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(Budget);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(budget.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = (int)Budget.TotalSeconds;
            await command.ExecuteScalarAsync(budget.Token);

            return HealthCheckResult.Healthy("PostgreSQL answered a query.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller went away (request aborted, host shutting down). That is not a
            // verdict on the database — let it propagate rather than report a false outage.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PostgreSQL health check failed.");
            return HealthCheckResult.Unhealthy("PostgreSQL did not answer a query.");
        }
    }
}
