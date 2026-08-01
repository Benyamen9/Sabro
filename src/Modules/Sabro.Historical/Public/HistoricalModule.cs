using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Application.Proposals;
using Sabro.Historical.Infrastructure;
using Sabro.Shared.Abstractions;

namespace Sabro.Historical.Public;

public sealed class HistoricalModule : IModuleRegistration
{
    public string ModuleName => "Historical";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<HistoricalDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", HistoricalDbContext.SchemaName));
        });

        services.AddScoped<IHistoricalFigureService, HistoricalFigureService>();
        services.AddScoped<IHistoricalFigurePlayablePool, HistoricalFigurePlayablePool>();

        // Lets Reviews resolve figures as proposal targets without referencing this
        // module. Registered against the shared interface, not the concrete type —
        // Reviews picks the source whose TargetTypeName matches.
        services.AddScoped<IProposalTargetSource, HistoricalFigureProposalTargetSource>();
    }
}
