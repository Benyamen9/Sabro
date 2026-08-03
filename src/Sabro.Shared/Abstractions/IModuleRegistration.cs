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

    /// <summary>
    /// The EF Core DbContext this module's schema is migrated under in production,
    /// or <see langword="null"/> when the module must not create its schema there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every module registers its DbContext in DI, deferred ones included</b>, so
    /// DI is no signal of what production actually has. This property is the signal,
    /// and it is deliberately on the module rather than in a list somewhere else:
    /// the decision belongs next to the module making it.
    /// </para>
    /// <para>
    /// It has to agree with <c>scripts/apply-migrations.sh</c>, which is what CD runs.
    /// <c>ModuleMigrationCoverageTests</c> fails the build when they disagree — that
    /// gap is not theoretical: the reviewer workflow shipped green and could not work
    /// in production, because Reviews was un-deferred in code and nothing brought the
    /// migration list along. It failed at the first write with
    /// <c>42P01: relation "reviews.suggested_edits" does not exist</c>.
    /// </para>
    /// </remarks>
    string? ProductionDbContextName { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
