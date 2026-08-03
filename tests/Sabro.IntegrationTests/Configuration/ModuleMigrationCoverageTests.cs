using System.Text.RegularExpressions;
using Sabro.API.Configuration;
using Sabro.Shared.Abstractions;

namespace Sabro.IntegrationTests.Configuration;

/// <summary>
/// Keeps the modules and the deploy-time migration list in agreement.
/// </summary>
/// <remarks>
/// <para>
/// <c>scripts/apply-migrations.sh</c> is the only statement of which schemas exist in
/// production — CD runs it before swapping containers, and nothing else creates a
/// schema there. Nothing in the build could see it, so a module could be shipped,
/// reviewed and merged while being structurally impossible in production.
/// </para>
/// <para>
/// That is not hypothetical. The reviewer workflow shipped across three green PRs
/// and a deploy, and every proposal failed with
/// <c>42P01: relation "reviews.suggested_edits" does not exist</c> — the Reviews
/// module had been un-deferred in code and the migration list never followed. It
/// passed every test, because tests build their schema from the model and local dev
/// applies migrations by hand; production is the only place that depends on this
/// script.
/// </para>
/// <para>
/// These tests are the missing feedback. They read no database — the point is that
/// they fail on a laptop, in CI, before anything reaches a server.
/// </para>
/// </remarks>
public class ModuleMigrationCoverageTests
{
    /// <summary>Entries in the script's <c>contexts=( … )</c> array, in order.</summary>
    private static readonly (string Context, string Project)[] ScriptContexts = ReadScriptContexts();

    [Fact]
    public void EveryActiveModuleIsMigratedByTheDeployScript()
    {
        var declared = SabroModules.All
            .Where(module => module.ProductionDbContextName is not null)
            .ToArray();

        declared.Should().NotBeEmpty("the application would have no schema at all otherwise");

        const string because = "{0} declares it is migrated in production, so scripts/apply-migrations.sh"
            + " must apply {1} — without it the module works in every test and in local dev, and fails in"
            + " production at the first write with 42P01";

        foreach (var module in declared)
        {
            ScriptContexts.Select(entry => entry.Context)
                .Should()
                .Contain(module.ProductionDbContextName!, because, module.ModuleName, module.ProductionDbContextName);
        }
    }

    [Fact]
    public void TheDeployScriptMigratesNothingNoModuleClaims()
    {
        // The other direction: an entry left behind after a module is removed or
        // re-deferred would have CD building a schema nothing owns.
        var declaredContexts = SabroModules.All
            .Select(module => module.ProductionDbContextName)
            .Where(name => name is not null)
            .ToArray();

        const string because = "scripts/apply-migrations.sh migrates {0}, but no module declares it as its"
            + " production DbContext — either a module needs to claim it or the entry is stale";

        foreach (var (context, _) in ScriptContexts)
        {
            declaredContexts.Should().Contain(context, because, context);
        }
    }

    [Fact]
    public void DeferredModulesStayOutOfProduction()
    {
        // Deferred modules still register their DbContext in DI, which is exactly why
        // DI cannot be the signal. Pinned so "it is registered, so migrate it" never
        // looks like the obvious fix.
        var deferred = SabroModules.All
            .Where(module => module.ProductionDbContextName is null)
            .Select(module => module.ModuleName)
            .ToArray();

        const string because = "these are the modules whose specs are retained but not built; un-deferring"
            + " one means declaring its DbContext and adding it to scripts/apply-migrations.sh in the"
            + " same change";

        deferred.Should().BeEquivalentTo(["Translations", "Biblical"], because);
    }

    [Fact]
    public void EachMigratedContextPointsAtItsOwnModuleProject()
    {
        // The script needs a project path as well as a context name. Convention is
        // src/Modules/Sabro.{ModuleName}; a mismatch means dotnet-ef would look for
        // the migrations in the wrong assembly.
        foreach (var module in SabroModules.All.Where(m => m.ProductionDbContextName is not null))
        {
            // SingleOrDefault, not Single: when the entry is missing entirely the other
            // test already says so clearly, and an exception here would bury that
            // message under "sequence contains no matching element".
            var entry = ScriptContexts.SingleOrDefault(e => e.Context == module.ProductionDbContextName);
            if (entry.Context is null)
            {
                continue;
            }

            entry.Project.Should().Be(
                $"src/Modules/Sabro.{module.ModuleName}",
                "the {0} entry must point at that module's own project",
                module.ProductionDbContextName);
        }
    }

    private static (string Context, string Project)[] ReadScriptContexts()
    {
        var script = File.ReadAllText(RepositoryFile("scripts/apply-migrations.sh"));

        var array = Regex.Match(script, @"contexts=\((?<body>[^)]*)\)", RegexOptions.Singleline);
        if (!array.Success)
        {
            throw new InvalidOperationException(
                "Could not find the contexts=( … ) array in scripts/apply-migrations.sh. If its shape "
                + "changed, this parser has to change with it — silently reading zero entries would "
                + "turn this guard into a test that always passes.");
        }

        var entries = Regex.Matches(array.Groups["body"].Value, @"""(?<context>[^"":]+):(?<project>[^""]+)""")
            .Select(match => (match.Groups["context"].Value, match.Groups["project"].Value))
            .ToArray();

        if (entries.Length == 0)
        {
            throw new InvalidOperationException(
                "Parsed the contexts array in scripts/apply-migrations.sh but found no entries.");
        }

        return entries;
    }

    /// <summary>
    /// Resolves a repository-relative path by walking up from the test binaries until
    /// the file turns up, so this works from any build output layout.
    /// </summary>
    private static string RepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} above {AppContext.BaseDirectory}.");
    }
}
