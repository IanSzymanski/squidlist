using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Squidlist.Core.Models;
using Squidlist.Storage.Fingerprints;
using Xunit;

namespace Squidlist.Storage.Tests;

public sealed class MediaFingerprintTests
{
    private static async Task<MediaFingerprint> Hash(string value)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
        return await MediaFingerprinter.ComputeAsync(stream);
    }

    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public async Task Uses_standard_sha256_vectors(string content, string digest)
    {
        var fingerprint = await Hash(content);
        Assert.Equal(digest, fingerprint.Digest);
        Assert.Equal(Encoding.UTF8.GetByteCount(content), fingerprint.SizeBytes);
        Assert.Equal("sha256", fingerprint.Algorithm);
        Assert.Equal(1, fingerprint.Version);
    }

    [Fact]
    public async Task Unchanged_and_renamed_files_keep_content_evidence_while_equal_metadata_does_not()
    {
        var directory = Path.Combine(Path.GetTempPath(), "squidlist-fingerprint-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var first = Path.Combine(directory, "first.mp3");
            var renamed = Path.Combine(directory, "renamed.mp3");
            var different = Path.Combine(directory, "different.mp3");
            await File.WriteAllTextAsync(first, "abc");
            await File.WriteAllTextAsync(different, "abd");
            var timestamp = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(first, timestamp);
            File.SetLastWriteTimeUtc(different, timestamp);
            Assert.Equal(new FileInfo(first).Length, new FileInfo(different).Length);
            Assert.Equal(File.GetLastWriteTimeUtc(first), File.GetLastWriteTimeUtc(different));
            var original = await HashFile(first);
            Assert.Equal(original, await HashFile(first));
            Assert.NotEqual(original, await HashFile(different));
            File.Move(first, renamed);
            Assert.Equal(original, await HashFile(renamed));
        }
        finally
        {
            // Only this uniquely generated fixture directory is owned by the test.
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<MediaFingerprint> HashFile(string path)
    {
        await using var stream = File.OpenRead(path);
        return await MediaFingerprinter.ComputeAsync(stream);
    }

    [Fact]
    public async Task Chunked_nonseekable_stream_uses_bounded_reads_and_stays_open()
    {
        var data = Enumerable.Range(0, 200_000).Select(index => (byte)(index % 251)).ToArray();
        using var stream = new ChunkedStream(data);
        var fingerprint = await MediaFingerprinter.ComputeAsync(stream);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(), fingerprint.Digest);
        Assert.Equal(data.Length, fingerprint.SizeBytes);
        Assert.InRange(stream.MaxReadRequest, 1, 64 * 1024);
        Assert.True(stream.Reads > 2);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Cancelled_reads_return_no_result_and_leave_stream_owned_by_caller()
    {
        using var cancellation = new CancellationTokenSource();
        using var stream = new ChunkedStream(new byte[20_000], count =>
        {
            if (count == 2) cancellation.Cancel();
        });
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            MediaFingerprinter.ComputeAsync(stream, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(2, stream.Reads);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Pre_cancelled_operation_does_not_read()
    {
        using var stream = new ChunkedStream([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MediaFingerprinter.ComputeAsync(stream, cancellation.Token));
        Assert.Equal(0, stream.Reads);
    }

    [Fact]
    public async Task Io_failure_propagates_without_disposing_caller_stream()
    {
        using var stream = new ChunkedStream(new byte[20_000], count =>
        {
            if (count == 2) throw new IOException("Simulated read failure");
        });
        await Assert.ThrowsAsync<IOException>(() => MediaFingerprinter.ComputeAsync(stream));
        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task Seekable_stream_must_begin_at_zero()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        stream.Position = 1;
        await Assert.ThrowsAsync<ArgumentException>(() => MediaFingerprinter.ComputeAsync(stream));
        Assert.Equal(1, stream.Position);
    }

    [Fact]
    public async Task Candidate_json_round_trip_preserves_identity_and_value_equality()
    {
        var candidate = new FingerprintCandidate(MediaId.New(), await Hash("abc"));
        Assert.Equal(candidate, JsonSerializer.Deserialize<FingerprintCandidate>(JsonSerializer.Serialize(candidate)));
        Assert.Equal(candidate.Fingerprint, new MediaFingerprint(1, "sha256", 3, candidate.Fingerprint.Digest.ToUpperInvariant()));
        Assert.Throws<ArgumentException>(() => new FingerprintCandidate(default, candidate.Fingerprint));
    }

    [Fact]
    public void Malformed_or_unsupported_fingerprints_are_rejected()
    {
        var digest = new string('a', 64);
        Assert.Throws<NotSupportedException>(() => new MediaFingerprint(2, "sha256", 0, digest));
        Assert.Throws<NotSupportedException>(() => new MediaFingerprint(1, "md5", 0, digest));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MediaFingerprint(1, "sha256", -1, digest));
        Assert.Throws<ArgumentException>(() => new MediaFingerprint(1, "sha256", 0, "abcd"));
        Assert.Throws<ArgumentException>(() => new MediaFingerprint(1, "sha256", 0, new string('z', 64)));
        Assert.Throws<ArgumentNullException>(() => new MediaFingerprint(1, "sha256", 0, null!));
    }

    [Fact]
    public async Task Matching_distinguishes_none_unique_and_duplicate_content()
    {
        var fingerprint = await Hash("same content");
        var first = new FingerprintCandidate(MediaId.New(), fingerprint);
        var second = new FingerprintCandidate(MediaId.New(), fingerprint);
        var other = new FingerprintCandidate(MediaId.New(), await Hash("other content"));
        Assert.Equal(FingerprintMatchKind.None, FingerprintMatcher.Match(fingerprint, [other]).Kind);
        var unique = FingerprintMatcher.Match(fingerprint, [other, first]);
        Assert.Equal(FingerprintMatchKind.Unique, unique.Kind);
        Assert.Equal(first.MediaId, Assert.Single(unique.MediaIds));
        var ambiguous = FingerprintMatcher.Match(fingerprint, [first, other, second]);
        Assert.Equal(FingerprintMatchKind.Ambiguous, ambiguous.Kind);
        Assert.Equal(new[] { first.MediaId, second.MediaId }, ambiguous.MediaIds);
    }

    [Fact]
    public async Task Repeated_candidate_ids_are_deduplicated_but_conflicting_evidence_is_rejected()
    {
        var fingerprint = await Hash("one");
        var candidate = new FingerprintCandidate(MediaId.New(), fingerprint);
        var result = FingerprintMatcher.Match(fingerprint, [candidate, candidate]);
        Assert.Equal(FingerprintMatchKind.Unique, result.Kind);
        Assert.Single(result.MediaIds);
        var conflicting = new FingerprintCandidate(candidate.MediaId, await Hash("two"));
        Assert.Throws<ArgumentException>(() => FingerprintMatcher.Match(fingerprint, [candidate, conflicting]));
    }

    [Fact]
    public async Task Matching_includes_size_and_result_is_detached_from_input()
    {
        var fingerprint = await Hash("abc");
        var wrongSize = new MediaFingerprint(1, "sha256", 4, fingerprint.Digest);
        Assert.Equal(FingerprintMatchKind.None,
            FingerprintMatcher.Match(fingerprint, [new FingerprintCandidate(MediaId.New(), wrongSize)]).Kind);
        var candidates = new List<FingerprintCandidate> { new(MediaId.New(), fingerprint) };
        var result = FingerprintMatcher.Match(fingerprint, candidates);
        candidates.Clear();
        Assert.Single(result.MediaIds);
    }

    private sealed class ChunkedStream(byte[] data, Action<int>? onRead = null) : Stream
    {
        private int offset;
        private bool disposed;
        public int Reads { get; private set; }
        public int MaxReadRequest { get; private set; }
        public override bool CanRead => !disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            MaxReadRequest = Math.Max(MaxReadRequest, buffer.Length);
            onRead?.Invoke(Reads);
            var count = Math.Min(Math.Min(4093, buffer.Length), data.Length - offset);
            data.AsMemory(offset, count).CopyTo(buffer);
            offset += count;
            return ValueTask.FromResult(count);
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { disposed = true; base.Dispose(disposing); }
    }
}
