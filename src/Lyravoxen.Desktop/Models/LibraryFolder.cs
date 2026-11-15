using Lyravoxen.Models;

namespace Lyravoxen.Models;

public sealed class LibraryFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<LibraryVideo> Videos { get; set; } = new();
}

public sealed class LibraryVideo
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string SongTitle { get; set; } = "";
    public string Performer { get; set; } = "";
    public string Composer { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsOffline { get; set; }
    public bool IsLyricsOnly { get; set; }
    public string? LocalPath { get; set; }
    public string? Format { get; set; }
    public List<LyricCue> Cues { get; set; } = new();
}
