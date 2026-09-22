using Squidlist.Core.Media;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Contracts;

/// <summary>A scanner observation. Consumers count observations for progress; no total is promised.</summary>
public abstract record MediaScanEvent
{
    private MediaScanEvent() { }

    /// <summary>A supported file observed under the root; identity is assigned during reconciliation.</summary>
    public sealed record Discovered : MediaScanEvent
    {
        public Discovered(MediaLocation location, MediaKind kind)
        {
            ArgumentNullException.ThrowIfNull(location);
            if (!location.IsRelative)
            {
                throw new ArgumentException("Scan locations must be relative to the root.", nameof(location));
            }
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            Location = location;
            Kind = kind;
        }

        public MediaLocation Location { get; }
        public MediaKind Kind { get; }
    }

    /// <summary>A failed file or subtree beneath the root. Root-wide failures throw StorageException.</summary>
    public sealed record Failed : MediaScanEvent
    {
        public Failed(MediaLocation location, StorageFailure failure)
        {
            ArgumentNullException.ThrowIfNull(location);
            if (!location.IsRelative)
            {
                throw new ArgumentException("Scan locations must be relative to the root.", nameof(location));
            }
            if (!Enum.IsDefined(failure))
            {
                throw new ArgumentOutOfRangeException(nameof(failure));
            }
            Location = location;
            Failure = failure;
        }

        public MediaLocation Location { get; }
        public StorageFailure Failure { get; }
    }
}
