using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Shared.Abstractions;

namespace Sabro.Biblical.Public;

public sealed class BiblicalModule : IModuleRegistration
{
    public string ModuleName => "Biblical";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}
