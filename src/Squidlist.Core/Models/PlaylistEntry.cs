using System.Text.Json.Serialization;

namespace Squidlist.Core.Models;

/// <summary>
/// An ordered reference to media in a playlist.
/// </summary>
/// <remarks>
/// <see cref="PathHint"/> is non-authoritative and may be stale. The media ID
/// remains the identity of the entry when the referenced media is moved or
/// temporarily unavailable.
/// </remarks>
public sealed record PlaylistEntry
{
    [JsonConstructor]
    public PlaylistEntry(MediaId mediaId, string? pathHint = null)
    {
        if (mediaId.Value == Guid.Empty)
        {
            throw new ArgumentException("A playlist entry must reference a media ID.", nameof(mediaId));
        }

        MediaId = mediaId;
        PathHint = pathHint;
    }

    public MediaId MediaId { get; }

    public string? PathHint { get; }
}
