using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Application.Roots;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Search;

namespace Sabro.Lexicon.Public;

public sealed class LexiconModule : IModuleRegistration
{
    public string ModuleName => "Lexicon";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LexiconDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LexiconDbContext.SchemaName));
        });

        services.AddScoped<ILexiconRootService, LexiconRootService>();
        services.AddScoped<ILexiconEntryService, LexiconEntryService>();
        services.AddScoped<ILexiconSearchService, LexiconSearchService>();

        services.AddSearchIndex<LexiconEntrySearchDocument, LexiconEntryIndexDescriptor>();
    }
}
