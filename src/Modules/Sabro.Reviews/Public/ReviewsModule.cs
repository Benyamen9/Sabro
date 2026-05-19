using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Reviews.Application.Approvals;
using Sabro.Reviews.Application.SuggestedEdits;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Abstractions;

namespace Sabro.Reviews.Public;

public sealed class ReviewsModule : IModuleRegistration
{
    public string ModuleName => "Reviews";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReviewsDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Sabro");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Sabro is not configured.");
            }

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ReviewsDbContext.SchemaName));
        });

        services.AddScoped<ISuggestedEditService, SuggestedEditService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IAnnotationApprovalRepublisher, AnnotationApprovalRepublisher>();
    }
}
