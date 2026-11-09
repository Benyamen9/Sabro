using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sabro.Library.Application.Reading;
using Sabro.Library.Infrastructure;
using Sabro.Shared.Abstractions;

namespace Sabro.Library.Public;

/// <summary>
/// The reader's side of the hub's Library: the red ribbon (where they were in each book) and their
/// ܀ marks. Not Identity (who the user is) and not Play (what they played): what they read.
/// </summary>
public sealed class LibraryModule : IModuleRegistration
{
    public string ModuleName => "Library";

    /// <inheritdoc />
    public string? ProductionDbContextName => "LibraryDbContext";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LibraryDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LibraryDbContext.SchemaName));
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IReaderService, ReaderService>();
    }
}
