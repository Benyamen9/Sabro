using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Shared.Abstractions;

namespace Sabro.Identity.Public;

public sealed class IdentityModule : IModuleRegistration
{
    public string ModuleName => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}
