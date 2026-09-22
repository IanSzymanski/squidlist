using System.Buffers;
using System.Security.Cryptography;

namespace Squidlist.Storage.Fingerprints;

/// <summary>Computes fingerprints with a fixed-size read buffer and no filesystem dependencies.</summary>
public static class MediaFingerprinter
{
    private const int BufferSize = 64 * 1024;

    /// <summary>
    /// Reads from the beginning to EOF, leaving the caller-owned stream open at its final
    /// position. Nonseekable streams must be supplied at their beginning. A canceled or
    /// failed read returns no fingerprint and may leave the stream partially consumed.
    /// </summary>
    public static async Task<MediaFingerprint> ComputeAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        if (!content.CanRead)
        {
            throw new ArgumentException("The content stream must be readable.", nameof(content));
        }
        if (content.CanSeek && content.Position != 0)
        {
            throw new ArgumentException("Fingerprinting must begin at the start of the content.", nameof(content));
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long size = 0;
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = await content.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0) break;
                hash.AppendData(buffer, 0, count);
                size = checked(size + count);
            }

            return new MediaFingerprint(MediaFingerprint.CurrentVersion, MediaFingerprint.CurrentAlgorithm,
                size, Convert.ToHexString(hash.GetHashAndReset()));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
