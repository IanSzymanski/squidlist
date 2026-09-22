using System.Text.Json.Serialization;
using Squidlist.Core.Media;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Catalog;

/// <summary>Last observed metadata for a stable media identity in one root's catalog.</summary>
public sealed record CatalogEntry
{
    [JsonConstructor]
    public CatalogEntry(MediaId mediaId, MediaKind kind, MediaLocation? location,
        CatalogMediaState state, long? sizeBytes, DateTimeOffset? modifiedAt,
        TimeSpan? duration, FingerprintStatus fingerprintStatus)
    {
        // Reuse Core's identity and media-kind validation.
        _ = new MediaItem(mediaId, kind, location);
        if (!Enum.IsDefined(state))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (!Enum.IsDefined(fingerprintStatus))
            throw new ArgumentOutOfRangeException(nameof(fingerprintStatus));
        if (state != CatalogMediaState.Unresolved && location is null)
            throw new ArgumentException("Available and missing entries require a current or last-known location.", nameof(location));
        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        MediaId = mediaId;
        Kind = kind;
        Location = location;
        State = state;
        SizeBytes = sizeBytes;
        ModifiedAt = modifiedAt;
        Duration = duration;
        FingerprintStatus = fingerprintStatus;
    }

    public MediaId MediaId { get; }
    public MediaKind Kind { get; }
    public MediaLocation? Location { get; }
    public CatalogMediaState State { get; }
    public long? SizeBytes { get; }
    public DateTimeOffset? ModifiedAt { get; }
    public TimeSpan? Duration { get; }
    public FingerprintStatus FingerprintStatus { get; }
}
