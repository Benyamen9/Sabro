using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Shared.Abstractions;

namespace Sabro.Reviews.Public;

public sealed class ReviewsModule : IModuleRegistration
{
    public string ModuleName => "Reviews";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}
