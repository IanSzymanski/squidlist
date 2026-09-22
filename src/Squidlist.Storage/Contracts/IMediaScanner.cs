using Squidlist.Core.Media;

namespace Squidlist.Storage.Contracts;

/// <summary>Discovers local media without assigning IDs or modifying the catalog or filesystem.</summary>
public interface IMediaScanner
{
    /// <summary>
    /// Streams discoveries and recoverable per-item failures under the supplied root.
    /// Normal exhaustion means traversal completed; failures mean coverage was partial.
    /// Cancellation or early disposal stops work and releases enumeration resources.
    /// Root-wide failure throws StorageException. Events observed before failure are partial.
    /// </summary>
    IAsyncEnumerable<MediaScanEvent> ScanAsync(MediaRoot root, CancellationToken cancellationToken = default);
}
