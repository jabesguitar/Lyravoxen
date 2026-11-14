using System.Text.RegularExpressions;

namespace Lyravoxen;

internal static class YouTubeLink
{
    private static readonly Regex IdPattern =
        new(@"^[a-zA-Z0-9_-]{11}$", RegexOptions.Compiled);

    public static string? TryGetVideoId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim();
        if (IdPattern.IsMatch(input)) return input;

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.ToLowerInvariant();
        string? id = null;

        if (host == "youtu.be" || host == "www.youtu.be")
            id = uri.AbsolutePath.Trim('/');
        else if (host == "youtube.com" || host.EndsWith(".youtube.com"))
        {
            if (uri.AbsolutePath == "/watch")
            {
                var pair = uri.Query.TrimStart('?').Split('&')
                    .Select(part => part.Split('=', 2))
                    .FirstOrDefault(parts => parts.Length == 2 && parts[0] == "v");
                if (pair is not null) id = Uri.UnescapeDataString(pair[1]);
            }
            else if (uri.AbsolutePath.StartsWith("/embed/") || uri.AbsolutePath.StartsWith("/shorts/"))
                id = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        return id is not null && IdPattern.IsMatch(id) ? id : null;
    }
}
