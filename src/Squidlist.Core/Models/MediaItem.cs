using System.Text.Json.Serialization;
using Squidlist.Core.Media;

namespace Squidlist.Core.Models;

/// <summary>
/// A platform-neutral catalog item with stable identity and an optional current location.
/// </summary>
public sealed record MediaItem
{
    [JsonConstructor]
    public MediaItem(MediaId mediaId, MediaKind kind, MediaLocation? location = null)
    {
        if (mediaId.Value == Guid.Empty)
        {
            throw new ArgumentException("A media item must have a media ID.", nameof(mediaId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown media kind.");
        }

        MediaId = mediaId;
        Kind = kind;
        Location = location;
    }

    /// <summary>
    /// Gets the stable identity of this media item.
    /// </summary>
    public MediaId MediaId { get; }

    /// <summary>
    /// Gets whether this item is audio or video.
    /// </summary>
    public MediaKind Kind { get; }

    /// <summary>
    /// Gets the item's current location, or <see langword="null"/> when unresolved.
    /// </summary>
    public MediaLocation? Location { get; }
}
