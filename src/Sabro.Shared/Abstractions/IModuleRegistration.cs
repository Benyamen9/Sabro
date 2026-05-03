using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sabro.Shared.Abstractions;

/// <summary>
/// Each module exposes a single registration entry point. The composition root
/// (Sabro.API) discovers and invokes these to wire DI, EF Core, validators, and
/// Meilisearch sync — without ever touching module internals.
/// </summary>
public interface IModuleRegistration
{
    string ModuleName { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
