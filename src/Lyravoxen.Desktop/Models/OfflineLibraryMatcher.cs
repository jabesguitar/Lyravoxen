using System.IO;
using Lyravoxen.Services;

namespace Lyravoxen.Models;

public sealed record OfflineLibraryMatch(LibraryVideo Video, bool MatchedByVideoId);

public static class OfflineLibraryMatcher
{
    public static OfflineLibraryMatch? FindMatchingBackup(
        IEnumerable<LibraryVideo>? libraryItems,
        LibraryVideo? source,
        DateTime now)
    {
        if (source is null) return null;
        var available = (libraryItems ?? Enumerable.Empty<LibraryVideo>())
            .Where(item => item is not null && item.IsOffline &&
                !string.IsNullOrWhiteSpace(item.LocalPath) &&
                File.Exists(item.LocalPath) &&
                (!item.OfflineExpiresAt.HasValue || item.OfflineExpiresAt.Value > now))
            .ToList();

        var sourceId = NormalizeVideoId(source.VideoId);
        if (sourceId.Length > 0)
        {
            var exactId = available
                .Where(item => NormalizeVideoId(item.VideoId) == sourceId)
                .OrderByDescending(item => item.SavedAt)
                .FirstOrDefault();
            if (exactId is not null)
                return new OfflineLibraryMatch(exactId, MatchedByVideoId: true);
        }

        var sourceIdentity = GetIdentity(source);
        if (sourceIdentity.Track.Length == 0) return null;

        var sameTitle = available
            .Where(item => GetIdentity(item).Track == sourceIdentity.Track)
            .ToList();
        if (sameTitle.Count == 0) return null;

        if (sourceIdentity.Artist.Length > 0)
        {
            var exactArtist = sameTitle
                .Where(item => GetIdentity(item).Artist == sourceIdentity.Artist)
                .OrderByDescending(item => item.SavedAt)
                .ToList();
            if (exactArtist.Count == 1)
                return new OfflineLibraryMatch(exactArtist[0], MatchedByVideoId: false);
            if (exactArtist.Count > 1) return null;

            // Arquivo importado pelo nome, sem intérprete reconhecido, só é aceito quando único.
            var noArtist = sameTitle.Where(item => GetIdentity(item).Artist.Length == 0).ToList();
            return sameTitle.Count == 1 && noArtist.Count == 1
                ? new OfflineLibraryMatch(noArtist[0], MatchedByVideoId: false)
                : null;
        }

        return sameTitle.Count == 1
            ? new OfflineLibraryMatch(sameTitle[0], MatchedByVideoId: false)
            : null;
    }

    public static bool AreSameRecording(LibraryVideo? left, LibraryVideo? right)
    {
        if (left is null || right is null) return false;
        var leftId = NormalizeVideoId(left.VideoId);
        var rightId = NormalizeVideoId(right.VideoId);
        if (leftId.Length > 0 && rightId.Length > 0 && leftId == rightId) return true;

        var leftIdentity = GetIdentity(left);
        var rightIdentity = GetIdentity(right);
        if (leftIdentity.Track.Length == 0 || leftIdentity.Track != rightIdentity.Track) return false;
        return leftIdentity.Artist.Length == 0 || rightIdentity.Artist.Length == 0 ||
            leftIdentity.Artist == rightIdentity.Artist;
    }

    private static (string Track, string Artist) GetIdentity(LibraryVideo video)
    {
        var rawTrack = !string.IsNullOrWhiteSpace(video.TrackName) ? video.TrackName : video.Title;
        var parsed = TrackTitleParser.Parse(rawTrack, video.Artist);
        var track = string.IsNullOrWhiteSpace(parsed.Track)
            ? LyricsPreviewService.CleanTitle(rawTrack)
            : parsed.Track;
        var artist = !string.IsNullOrWhiteSpace(video.Artist) ? video.Artist : parsed.Artist;
        return (LyricsPreviewService.Normalize(track), LyricsPreviewService.Normalize(artist));
    }

    private static string NormalizeVideoId(string? value) => (value ?? "").Trim();

    public static double ResolveResumePosition(double? requestedSeconds, double savedSeconds)
    {
        var value = requestedSeconds ?? savedSeconds;
        return double.IsFinite(value) && value > 0 ? value : 0;
    }

    private static string NormalizeVideoId(string? value) => (value ?? "").Trim();

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }
}
