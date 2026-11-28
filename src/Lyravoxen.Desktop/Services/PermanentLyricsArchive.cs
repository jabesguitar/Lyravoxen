using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lyravoxen.Models;

namespace Lyravoxen.Services;

/// <summary>Arquivo independente de letras e sincronizações; não depende da existência do áudio.</summary>
public sealed class PermanentLyricsArchive
{
    private readonly string _file;
    private readonly object _sync = new();
    private Dictionary<string, LibraryVideo> _items = new(StringComparer.OrdinalIgnoreCase);

    public PermanentLyricsArchive(string file)
    {
        _file = file;
        Load();
    }

    public void Save(LibraryVideo video)
    {
        var key = GetKey(video);
        if (string.IsNullOrWhiteSpace(key) || (string.IsNullOrWhiteSpace(video.FullLyrics) && video.Cues.Count == 0 && video.ExecutionOrder.Count == 0)) return;
        lock (_sync)
        {
            _items[key] = Clone(video);
            Persist();
        }
    }

    public LibraryVideo? Find(string videoId, string url)
    {
        var key = !string.IsNullOrWhiteSpace(videoId) ? "youtube:" + videoId :
            (!string.IsNullOrWhiteSpace(url) ? "url:" + url.Trim() : "");
        if (key.Length == 0) return null;
        lock (_sync) return _items.TryGetValue(key, out var item) ? Clone(item) : null;
    }

    private static string GetKey(LibraryVideo video)
    {
        if (!string.IsNullOrWhiteSpace(video.VideoId)) return "youtube:" + video.VideoId.Trim();
        if (!string.IsNullOrWhiteSpace(video.Url)) return "url:" + video.Url.Trim();
        var identity = (video.Artist + "|" + video.TrackName + "|" + video.Title).Trim('|', ' ');
        return identity.Length == 0 ? "" : "metadata:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToUpperInvariant())));
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_file)) return;
            _items = JsonSerializer.Deserialize<Dictionary<string, LibraryVideo>>(File.ReadAllText(_file)) ??
                new Dictionary<string, LibraryVideo>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            var backup = _file + ".bak";
            try { if (File.Exists(backup)) _items = JsonSerializer.Deserialize<Dictionary<string, LibraryVideo>>(File.ReadAllText(backup)) ?? new(); }
            catch { _items = new(StringComparer.OrdinalIgnoreCase); }
        }
    }

    private void Persist()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        var temp = _file + ".tmp";
        var backup = _file + ".bak";
        File.WriteAllText(temp, JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(_file)) File.Copy(_file, backup, true);
        File.Move(temp, _file, true);
    }

    private static LibraryVideo Clone(LibraryVideo v) => new()
    {
        VideoId = v.VideoId, Title = v.Title, TrackName = v.TrackName, Artist = v.Artist,
        Composer = v.Composer, Url = v.Url, IsOffline = false, LocalPath = null, Format = null,
        FullLyrics = v.FullLyrics, SavedAt = v.SavedAt,
        Cues = v.Cues.Select(c => new LyricCue
        {
            Seconds = c.Seconds, Text = c.Text, SectionId = c.SectionId,
            OccurrenceIndex = c.OccurrenceIndex, SourceLineIndex = c.SourceLineIndex,
            Confidence = c.Confidence, NeedsReview = c.NeedsReview, IsManual = c.IsManual
        }).ToList(),
        Sections = v.Sections.Select(section => new LyricsSectionBlock
        {
            SectionId = section.SectionId, Label = section.Label, Kind = section.Kind,
            SourceIndex = section.SourceIndex, Lines = new List<string>(section.Lines),
            RepeatCount = section.RepeatCount, OccurrenceCount = section.OccurrenceCount,
            ExplicitlyLabeled = section.ExplicitlyLabeled, NeedsReview = section.NeedsReview
        }).ToList(),
        ExecutionOrder = v.ExecutionOrder.Select(item => new LyricsExecutionOccurrence
        {
            SectionId = item.SectionId, Label = item.Label, Kind = item.Kind,
            OccurrenceIndex = item.OccurrenceIndex, SourceIndex = item.SourceIndex
        }).ToList()
    };
}