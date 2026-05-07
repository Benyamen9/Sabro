using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sabro.Shared.Search;

public static class SearchServiceCollectionExtensions
{
    /// <summary>
    /// Registers a typed search index descriptor under both its generic and
    /// non-generic interface so that the open-generic <see cref="ISearchIndex{TDocument}"/>
    /// can resolve it and the startup initializer can iterate all of them.
    /// Modules call this from their <c>RegisterServices</c> entry point.
    /// </summary>
    public static IServiceCollection AddSearchIndex<TDocument, TDescriptor>(this IServiceCollection services)
        where TDocument : class
        where TDescriptor : class, ISearchIndexDescriptor<TDocument>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TDescriptor>();
        services.AddSingleton<ISearchIndexDescriptor<TDocument>>(sp => sp.GetRequiredService<TDescriptor>());
        services.AddSingleton<ISearchIndexDescriptor>(sp => sp.GetRequiredService<TDescriptor>());

        return services;
    }
}
