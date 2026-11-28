using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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

        var sourceTitle = Normalize(source.TrackName.Length > 0 ? source.TrackName : source.Title);
        if (sourceTitle.Length == 0) return null;
        var sourceArtist = Normalize(source.Artist);
        var sameTitle = available
            .Where(item => Normalize(item.TrackName.Length > 0 ? item.TrackName : item.Title) == sourceTitle)
            .ToList();
        if (sameTitle.Count == 0) return null;

        if (sourceArtist.Length > 0)
        {
            var exactArtist = sameTitle
                .Where(item => Normalize(item.Artist) == sourceArtist)
                .OrderByDescending(item => item.SavedAt)
                .ToList();
            if (exactArtist.Count == 1)
                return new OfflineLibraryMatch(exactArtist[0], MatchedByVideoId: false);
            if (exactArtist.Count > 1) return null;

            // Arquivo importado pelo nome, sem intérprete preenchido, só é aceito quando único.
            var noArtist = sameTitle.Where(item => Normalize(item.Artist).Length == 0).ToList();
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

        var leftTitle = Normalize(left.TrackName.Length > 0 ? left.TrackName : left.Title);
        var rightTitle = Normalize(right.TrackName.Length > 0 ? right.TrackName : right.Title);
        if (leftTitle.Length == 0 || leftTitle != rightTitle) return false;

        var leftArtist = Normalize(left.Artist);
        var rightArtist = Normalize(right.Artist);
        return leftArtist.Length == 0 || rightArtist.Length == 0 || leftArtist == rightArtist;
    }

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
