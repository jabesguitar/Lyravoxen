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
