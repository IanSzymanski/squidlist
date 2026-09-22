using System.Text.Json.Serialization;
using Squidlist.Core.Models;

namespace Squidlist.Storage.Contracts;

/// <summary>A single persistence unit for playback state and its ordered queue.</summary>
public sealed record PlaybackSession
{
    [JsonConstructor]
    public PlaybackSession(PlaybackState state, PlaybackQueue queue)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(queue);
        if (state.CurrentMediaId is { Value: var value } && value == Guid.Empty)
        {
            throw new ArgumentException("Current media ID must be nonempty when present.", nameof(state));
        }

        State = state;
        Queue = queue;
    }

    public PlaybackState State { get; }
    public PlaybackQueue Queue { get; }
}
