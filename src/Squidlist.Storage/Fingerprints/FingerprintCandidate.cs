using System.Text.Json.Serialization;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Fingerprints;

/// <summary>A known catalog identity and its last usable content fingerprint.</summary>
public sealed record FingerprintCandidate
{
    [JsonConstructor]
    public FingerprintCandidate(MediaId mediaId, MediaFingerprint fingerprint)
    {
        if (mediaId.Value == Guid.Empty)
        {
            throw new ArgumentException("A fingerprint candidate must have a media ID.", nameof(mediaId));
        }
        ArgumentNullException.ThrowIfNull(fingerprint);
        MediaId = mediaId;
        Fingerprint = fingerprint;
    }

    public MediaId MediaId { get; }
    public MediaFingerprint Fingerprint { get; }
}
