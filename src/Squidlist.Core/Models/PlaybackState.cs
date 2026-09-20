using System.Text.Json.Serialization;

namespace Squidlist.Core.Models;

/// <summary>
/// Persisted state for resuming playback without performing playback itself.
/// </summary>
public sealed record PlaybackState
{
    [JsonConstructor]
    public PlaybackState(
        MediaId? currentMediaId,
        TimeSpan position,
        double volume,
        DateTimeOffset? lastPlayedAt)
    {
        if (position < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Playback position cannot be negative.");
        }

        if (double.IsNaN(volume) || double.IsInfinity(volume) || volume is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be a finite value between 0 and 1.");
        }

        CurrentMediaId = currentMediaId;
        Position = position;
        Volume = volume;
        LastPlayedAt = lastPlayedAt;
    }

    public MediaId? CurrentMediaId { get; }

    public TimeSpan Position { get; }

    /// <summary>
    /// Volume normalized to the inclusive range 0..1.
    /// </summary>
    public double Volume { get; }

    public DateTimeOffset? LastPlayedAt { get; }

    public static PlaybackState Default => new(null, TimeSpan.Zero, 1, null);
}
