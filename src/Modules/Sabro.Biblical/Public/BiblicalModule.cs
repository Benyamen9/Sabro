using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Biblical.Application.Books;
using Sabro.Biblical.Application.Passages;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Search;

namespace Sabro.Biblical.Public;

public sealed class BiblicalModule : IModuleRegistration
{
    public string ModuleName => "Biblical";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BiblicalDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BiblicalDbContext.SchemaName));
        });

        services.AddScoped<IBiblicalBookService, BiblicalBookService>();
        services.AddScoped<IBiblicalPassageService, BiblicalPassageService>();
        services.AddScoped<IBiblicalPassageSearchService, BiblicalPassageSearchService>();
        services.AddScoped<ISearchRebuilder, BiblicalPassageSearchRebuilder>();

        services.AddSearchIndex<BiblicalPassageSearchDocument, BiblicalPassageIndexDescriptor>();
    }
}
