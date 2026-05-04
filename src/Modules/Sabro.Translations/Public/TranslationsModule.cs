using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Shared.Abstractions;
using Sabro.Translations.Application.Authors;
using Sabro.Translations.Application.Segments;
using Sabro.Translations.Application.Sources;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Public;

public sealed class TranslationsModule : IModuleRegistration
{
    public string ModuleName => "Translations";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TranslationsDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TranslationsDbContext.SchemaName));
        });

        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<ISourceService, SourceService>();
        services.AddScoped<ISegmentService, SegmentService>();
    }
}
