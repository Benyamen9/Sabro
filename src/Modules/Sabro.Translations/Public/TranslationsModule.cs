using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Shared.Abstractions;

namespace Sabro.Translations.Public;

public sealed class TranslationsModule : IModuleRegistration
{
    public string ModuleName => "Translations";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}
