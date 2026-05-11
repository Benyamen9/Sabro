using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.UnitTests.Shared.Search;

public class MeilisearchServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSabroSearch_RegistersClientAndOpenGenericIndex()
    {
        var services = NewServices();
        services.AddSabroSearch(BuildConfig("http://localhost:7700", masterKey: null));
        services.AddSearchIndex<TestDocument, TestDescriptor>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetRequiredService<MeilisearchClient>().Should().NotBeNull();
        provider.GetRequiredService<ISearchIndex<TestDocument>>().Should().BeOfType<MeilisearchSearchIndex<TestDocument>>();
    }

    [Fact]
    public void AddSabroSearch_RegistersOpenGenericQuery()
    {
        var services = NewServices();
        services.AddSabroSearch(BuildConfig("http://localhost:7700", masterKey: null));
        services.AddSearchIndex<TestDocument, TestDescriptor>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetRequiredService<ISearchIndexQuery<TestDocument>>()
            .Should().BeOfType<MeilisearchSearchIndexQuery<TestDocument>>();
    }

    [Fact]
    public void AddSabroSearch_RegistersHostedServiceForIndexInitialization()
    {
        var services = NewServices();
        services.AddSabroSearch(BuildConfig("http://localhost:7700", masterKey: null));

        using var provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetServices<IHostedService>()
            .Should().Contain(s => s is SearchIndexInitializerHostedService);
    }

    [Fact]
    public void AddSearchIndex_RegistersDescriptorUnderBothInterfaces()
    {
        var services = NewServices();
        services.AddSabroSearch(BuildConfig("http://localhost:7700", masterKey: null));
        services.AddSearchIndex<TestDocument, TestDescriptor>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var typed = provider.GetRequiredService<ISearchIndexDescriptor<TestDocument>>();
        var nonGeneric = provider.GetServices<ISearchIndexDescriptor>().Single();

        typed.Should().BeSameAs(nonGeneric);
        typed.IndexName.Should().Be("test-index");
    }

    [Fact]
    public void AddSabroSearch_WithMissingUrl_FailsValidationWhenOptionsResolved()
    {
        var services = NewServices();
        services.AddSabroSearch(BuildConfig(url: string.Empty, masterKey: null));

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var act = () => provider.GetRequiredService<IOptions<MeilisearchOptions>>().Value;
        act.Should().Throw<OptionsValidationException>();
    }

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        return services;
    }

    private static IConfiguration BuildConfig(string url, string? masterKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Meilisearch:Url"] = url,
                ["Meilisearch:MasterKey"] = masterKey,
            })
            .Build();

    public sealed class TestDocument
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestDescriptor : ISearchIndexDescriptor<TestDocument>
    {
        public string IndexName => "test-index";

        public string PrimaryKey => "id";

        public IndexSettings Settings => IndexSettings.Empty;
    }

    private sealed class NullLoggerProvider : ILoggerProvider
    {
        public static readonly NullLoggerProvider Instance = new();

        public ILogger CreateLogger(string categoryName) => Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        public void Dispose()
        {
        }
    }
}
