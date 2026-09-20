namespace Squidlist.Core.Media;

/// <summary>
/// Identifies how a media location is stored.
/// </summary>
public enum MediaLocationKind
{
    /// <summary>
    /// The path is relative to a Squidlist media root.
    /// </summary>
    Relative,

    /// <summary>
    /// The path is absolute because it is not below the Squidlist media root.
    /// </summary>
    Absolute
}
