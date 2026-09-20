namespace Squidlist.Core.Models;

/// <summary>
/// An ordered reference to media in a playlist.
/// </summary>
/// <remarks>
/// <see cref="PathHint"/> is non-authoritative and may be stale. The media ID
/// remains the identity of the entry when the referenced media is moved or
/// temporarily unavailable.
/// </remarks>
public sealed record PlaylistEntry(MediaId MediaId, string? PathHint = null);
