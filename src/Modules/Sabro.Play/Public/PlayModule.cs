using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sabro.Play.Application.GameResults;
using Sabro.Play.Application.Meltho;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Abstractions;

namespace Sabro.Play.Public;

public sealed class PlayModule : IModuleRegistration
{
    public string ModuleName => "Play";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlayDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PlayDbContext.SchemaName));
        });

        services.Configure<MelthoOptions>(configuration.GetSection(MelthoOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IGameResultService, GameResultService>();
        services.AddScoped<IMelthoPuzzleService, MelthoPuzzleService>();
        services.AddScoped<IMelthoLibraryService, MelthoLibraryService>();
        services.AddScoped<IMelthoLeaderboardService, MelthoLeaderboardService>();
    }
}
