using System.Text.Json.Serialization;

namespace Squidlist.Storage.Fingerprints;

/// <summary>Versioned full-content evidence; never a replacement for MediaId.</summary>
public sealed record MediaFingerprint
{
    public const int CurrentVersion = 1;
    public const string CurrentAlgorithm = "sha256";

    [JsonConstructor]
    public MediaFingerprint(int version, string algorithm, long sizeBytes, string digest)
    {
        if (version != CurrentVersion || algorithm != CurrentAlgorithm)
        {
            throw new NotSupportedException("Unsupported media fingerprint version or algorithm.");
        }
        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }
        ArgumentNullException.ThrowIfNull(digest);
        if (digest.Length != 64 || digest.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("A SHA-256 digest must contain exactly 64 hexadecimal characters.", nameof(digest));
        }

        Version = version;
        Algorithm = algorithm;
        SizeBytes = sizeBytes;
        Digest = digest.ToLowerInvariant();
    }

    public int Version { get; }
    public string Algorithm { get; }
    public long SizeBytes { get; }
    public string Digest { get; }
}
