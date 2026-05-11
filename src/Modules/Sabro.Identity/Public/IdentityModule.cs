using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Infrastructure;
using Sabro.Shared.Abstractions;

namespace Sabro.Identity.Public;

public sealed class IdentityModule : IModuleRegistration
{
    public string ModuleName => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.SchemaName));
        });

        services.AddScoped<IUserProfileService, UserProfileService>();
    }
}
