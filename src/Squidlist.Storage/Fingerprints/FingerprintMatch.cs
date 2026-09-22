using System.Collections.ObjectModel;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Fingerprints;

public enum FingerprintMatchKind
{
    None,
    Unique,
    Ambiguous
}

/// <summary>All matching identities; even a unique content match does not authorize relinking.</summary>
public sealed class FingerprintMatch
{
    internal FingerprintMatch(IEnumerable<MediaId> mediaIds)
    {
        MediaIds = new ReadOnlyCollection<MediaId>(mediaIds.ToArray());
        Kind = MediaIds.Count switch
        {
            0 => FingerprintMatchKind.None,
            1 => FingerprintMatchKind.Unique,
            _ => FingerprintMatchKind.Ambiguous
        };
    }

    public FingerprintMatchKind Kind { get; }
    public IReadOnlyList<MediaId> MediaIds { get; }
}
