using System.Text.Json;
using Squidlist.Core.Media;
using Xunit;

namespace Squidlist.Core.Tests;

public sealed class MediaRootTests
{
    [Fact]
    public void Root_normalizes_separators_and_dot_segments()
    {
        var root = MediaRoot.Configured(@"C:\\Music\\.\\Library\\");

        Assert.Equal(MediaRootState.Configured, root.State);
        Assert.Equal("C:/Music/Library", root.Path);
    }

    [Fact]
    public void Root_preserves_each_supported_state()
    {
        Assert.Equal(MediaRootState.Configured, MediaRoot.Configured("/media").State);
        Assert.Equal(MediaRootState.Missing, MediaRoot.Missing().State);
        Assert.Null(MediaRoot.Missing().Path);
        Assert.Equal(MediaRootState.Moved, MediaRoot.Moved("/old-media").State);
        Assert.Equal(MediaRootState.Invalid, MediaRoot.Invalid("not-absolute").State);
    }

    [Fact]
    public void Locate_makes_descendant_absolute_paths_relative()
    {
        var root = MediaRoot.Configured("/media/library");

        var location = root.Locate(@"/media/library\\Albums\\../track.mp3");

        Assert.Equal(MediaLocationKind.Relative, location.Kind);
        Assert.Equal("track.mp3", location.Path);
    }

    [Fact]
    public void Locate_keeps_paths_outside_root_absolute()
    {
        var location = MediaRoot.Configured("/media/library").Locate("/media/library-backup/track.mp3");

        Assert.Equal(MediaLocationKind.Absolute, location.Kind);
        Assert.Equal("/media/library-backup/track.mp3", location.Path);
    }

    [Fact]
    public void Relative_paths_cannot_escape_the_root()
    {
        Assert.Throws<ArgumentException>(() => MediaLocation.Relative("../track.mp3"));
        Assert.Throws<ArgumentException>(() => MediaLocation.Relative("album/../../track.mp3"));
    }

    [Fact]
    public void Models_round_trip_through_json()
    {
        var root = MediaRoot.Configured("C:/Music");
        var location = root.Locate("C:/Music/track.mp3");

        var rootJson = JsonSerializer.Serialize(root);
        var locationJson = JsonSerializer.Serialize(location);

        Assert.Equal(root, JsonSerializer.Deserialize<MediaRoot>(rootJson));
        Assert.Equal(location, JsonSerializer.Deserialize<MediaLocation>(locationJson));
    }

    [Fact]
    public void Root_containment_is_case_sensitive_and_segment_aware()
    {
        var root = MediaRoot.Configured("C:/Music");

        Assert.Equal(MediaLocationKind.Absolute, root.Locate("c:/Music/track.mp3").Kind);
        Assert.Equal(MediaLocationKind.Absolute, root.Locate("C:/Music-old/track.mp3").Kind);
    }
}
