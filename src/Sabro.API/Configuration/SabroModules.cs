using Sabro.Biblical.Public;
using Sabro.Historical.Public;
using Sabro.Identity.Public;
using Sabro.Lexicon.Public;
using Sabro.Play.Public;
using Sabro.Reviews.Public;
using Sabro.Shared.Abstractions;
using Sabro.Translations.Public;

namespace Sabro.API.Configuration;

/// <summary>
/// Every module the application is composed of.
/// </summary>
/// <remarks>
/// <para>
/// Lifted out of <c>Program.cs</c> so a test can read the same list the composition
/// root does. A module that is not here does not exist as far as the application is
/// concerned, which makes this the one place that has to be edited when a module is
/// added — and therefore the right place to hang the migration check off.
/// </para>
/// <para>
/// Deferred modules are listed too: they register their services and are simply not
/// migrated in production, which each one declares for itself through
/// <see cref="IModuleRegistration.ProductionDbContextName"/>.
/// </para>
/// </remarks>
public static class SabroModules
{
    public static IReadOnlyList<IModuleRegistration> All { get; } =
    [
        new LexiconModule(),
        new TranslationsModule(),
        new ReviewsModule(),
        new BiblicalModule(),
        new HistoricalModule(),
        new IdentityModule(),
        new PlayModule(),
    ];
}
