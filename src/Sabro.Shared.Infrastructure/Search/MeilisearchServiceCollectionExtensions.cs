using System;
using System.Net.Http;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sabro.Shared.Search;

namespace Sabro.Shared.Infrastructure.Search;

public static class MeilisearchServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Meilisearch client (singleton, with master-key auth if
    /// provided), the options binding (validated eagerly at startup), the
    /// open-generic <see cref="ISearchIndex{TDocument}"/> implementation, and
    /// the hosted service that ensures every registered descriptor's index
    /// exists with the expected settings.
    /// </summary>
    public static IServiceCollection AddSabroSearch(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<MeilisearchOptions>()
            .Bind(configuration.GetSection(MeilisearchOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MeilisearchOptions>>().Value;
            var apiKey = string.IsNullOrWhiteSpace(options.MasterKey) ? null : options.MasterKey;
            var http = new HttpClient
            {
                BaseAddress = new Uri(options.Url),
                Timeout = options.RequestTimeout,
            };
            return new MeilisearchClient(http, apiKey);
        });

        services.AddSingleton(typeof(ISearchIndex<>), typeof(MeilisearchSearchIndex<>));
        services.AddHostedService<SearchIndexInitializerHostedService>();

        return services;
    }
}
