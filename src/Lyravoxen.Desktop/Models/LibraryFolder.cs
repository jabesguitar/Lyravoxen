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
    public string TrackName { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Composer { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsOffline { get; set; }
    public string? LocalPath { get; set; }
    public string? Format { get; set; }
    // Full editable lyric saved independently from its optional timing cues.
    public string FullLyrics { get; set; } = "";
    public List<LyricCue> Cues { get; set; } = new();
    /// <summary>Quando a letra/sincronia foi salva pela última vez (para mostrar as músicas salvas mais recentes).</summary>
    public DateTime SavedAt { get; set; }
}
