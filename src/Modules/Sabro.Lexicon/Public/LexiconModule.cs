using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Shared.Abstractions;

namespace Sabro.Lexicon.Public;

public sealed class LexiconModule : IModuleRegistration
{
    public string ModuleName => "Lexicon";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Domain, Application, Infrastructure registrations land here as the module grows.
    }
}
