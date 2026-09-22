using Squidlist.Core.Media;
using Squidlist.Storage.Catalog;

namespace Squidlist.Storage.Contracts;

/// <summary>Loads and replaces complete catalog snapshots for an explicitly supplied root.</summary>
public interface ICatalogStore
{
    /// <summary>Returns null only when no catalog exists; corrupt or unsupported data is an error.</summary>
    Task<MediaCatalog?> LoadAsync(MediaRoot root, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically replaces the catalog snapshot. Does not scan, assign IDs, remove media,
    /// or repair references. The caller serializes read/modify/write operations per root.
    /// </summary>
    Task SaveAsync(MediaRoot root, MediaCatalog catalog, CancellationToken cancellationToken = default);
}
