using Squidlist.Core.Models;

namespace Squidlist.Storage.Fingerprints;

/// <summary>Compares usable fingerprint evidence without modifying media or catalog identities.</summary>
public static class FingerprintMatcher
{
    public static FingerprintMatch Match(MediaFingerprint fingerprint, IEnumerable<FingerprintCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(candidates);
        var known = new Dictionary<MediaId, MediaFingerprint>();
        var matches = new List<MediaId>();
        foreach (var candidate in candidates)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            if (known.TryGetValue(candidate.MediaId, out var previous))
            {
                if (previous != candidate.Fingerprint)
                {
                    throw new ArgumentException("Candidates contain conflicting fingerprints for the same media ID.", nameof(candidates));
                }
                continue;
            }
            known.Add(candidate.MediaId, candidate.Fingerprint);
            if (fingerprint == candidate.Fingerprint)
            {
                matches.Add(candidate.MediaId);
            }
        }
        return new FingerprintMatch(matches);
    }
}
