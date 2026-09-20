namespace Squidlist.Core.Media;

/// <summary>
/// Describes the state of the configured Squidlist media root.
/// </summary>
public enum MediaRootState
{
    /// <summary>
    /// The root is configured with a valid normalized path.
    /// </summary>
    Configured,

    /// <summary>
    /// No usable root is currently available.
    /// </summary>
    Missing,

    /// <summary>
    /// A root path is known, but it has moved from its previously discovered location.
    /// </summary>
    Moved,

    /// <summary>
    /// The configured root could not be represented as a valid absolute path.
    /// </summary>
    Invalid
}
