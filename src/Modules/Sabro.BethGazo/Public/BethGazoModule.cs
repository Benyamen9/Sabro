using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.BethGazo.Application.Chants;
using Sabro.BethGazo.Infrastructure;
using Sabro.Shared.Abstractions;

namespace Sabro.BethGazo.Public;

public sealed class BethGazoModule : IModuleRegistration
{
    public string ModuleName => "BethGazo";

    /// <inheritdoc />
    public string? ProductionDbContextName => "BethGazoDbContext";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BethGazoDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BethGazoDbContext.SchemaName));
        });

        services.AddScoped<IChantService, ChantService>();
        services.AddScoped<IChantPlayablePool, ChantPlayablePool>();
        services.AddValidatorsFromAssemblyContaining<CreateChantRequestValidator>();
    }
}
