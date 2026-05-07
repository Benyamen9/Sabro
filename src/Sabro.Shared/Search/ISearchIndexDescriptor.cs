namespace Sabro.Shared.Search;

/// <summary>
/// Declares an index's name, primary key, and desired settings.
/// One descriptor exists per logical index; modules implement and register them.
/// The infrastructure layer reads all descriptors at startup to ensure indexes
/// exist with the expected configuration.
/// </summary>
public interface ISearchIndexDescriptor
{
    string IndexName { get; }

    string PrimaryKey { get; }

    IndexSettings Settings { get; }
}

/// <summary>
/// Strongly-typed descriptor binding a document type to its index. The generic
/// implementation of <see cref="ISearchIndex{TDocument}"/> resolves this to
/// know which index to write to.
/// </summary>
/// <typeparam name="TDocument">The document type stored in this index.</typeparam>
public interface ISearchIndexDescriptor<TDocument> : ISearchIndexDescriptor
    where TDocument : class
{
}
