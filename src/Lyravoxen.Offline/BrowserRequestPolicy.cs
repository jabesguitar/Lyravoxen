namespace Lyravoxen.Offline;

public static class BrowserRequestPolicy
{
    public static bool ShouldBlock(string host, IEnumerable<string> protectedHosts, IEnumerable<string> blockedHosts, IEnumerable<string> allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        host = host.TrimEnd('.').ToLowerInvariant();
        if (Matches(host, protectedHosts)) return false;
        if (Matches(host, allowedHosts)) return false;
        return Matches(host, blockedHosts);
    }


    /// <summary>
    /// Blocks a small, conservative set of known YouTube ad endpoints while preserving
    /// the main YouTube, iframe API, googlevideo media, thumbnail and playback services.
    /// These path rules are based on public filter implementations; they are not a
    /// guarantee against server-side or in-stream ads.
    /// </summary>
    public static bool ShouldBlockRequest(Uri uri, IEnumerable<string> protectedHosts, IEnumerable<string> blockedHosts, IEnumerable<string> allowedHosts)
    {
        if (uri is null || string.IsNullOrWhiteSpace(uri.Host)) return false;
        var host = uri.Host.TrimEnd('.').ToLowerInvariant();
        var path = uri.AbsolutePath;

        if (IsYouTubeHost(host))
        {
            if (MatchesAllowed(host, allowedHosts)) return false;
            if (path.StartsWith("/pagead/", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/api/stats/ads", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/stats/ads/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return ShouldBlock(host, protectedHosts, blockedHosts, allowedHosts);
    }

    private static bool IsYouTubeHost(string host)
        => host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
           host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesAllowed(string host, IEnumerable<string> allowedHosts)
        => Matches(host, allowedHosts);

    private static bool Matches(string host, IEnumerable<string> domains)
    {
        foreach (var raw in domains)
        {
            var domain = raw.Trim().TrimEnd('.').ToLowerInvariant();
            if (domain.Length > 0 && (host == domain || host.EndsWith("." + domain, StringComparison.Ordinal)))
                return true;
        }
        return false;
    }
}
