using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Microsoft.Web.WebView2.Core;

namespace Lyravoxen.Services;

/// <summary>
/// Lightweight request-level ad/tracker filtering for the embedded WebView2 browser.
/// It intentionally avoids blocking YouTube media/CDN endpoints.
/// </summary>
internal static class BrowserAdBlocker
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
    private static readonly ConcurrentDictionary<string, byte> BlockedHosts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> AllowedHosts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> AttachedProfiles = new(StringComparer.Ordinal);
    private static readonly string[] Lists =
    {
        "https://easylist-downloads.adblockplus.org/easylist.txt",
        "https://easylist-downloads.adblockplus.org/easyprivacy.txt"
    };
    private static readonly string[] BuiltInHosts =
    {
        "doubleclick.net", "googlesyndication.com", "googleadservices.com",
        "adservice.google.com", "pagead2.googlesyndication.com",
        "adnxs.com", "adsrvr.org", "advertising.com", "adform.net",
        "taboola.com", "outbrain.com", "scorecardresearch.com",
        "quantserve.com", " casalemedia.com", "criteo.com"
    };
    private static readonly string[] ProtectedMediaHosts =
    {
        "youtube.com", "youtube-nocookie.com", "googlevideo.com",
        "ytimg.com", "youtubei.googleapis.com", "googleapis.com",
        "gstatic.com", "google.com"
    };
    private static Task? _loadTask;
    private static int _blockedCount;
    private static int _enabled = 1;

    public static bool Enabled
    {
        get => Volatile.Read(ref _enabled) != 0;
        set => Volatile.Write(ref _enabled, value ? 1 : 0);
    }

    public static int BlockedCount => Volatile.Read(ref _blockedCount);

    public static Task InitializeAsync()
    {
        _loadTask ??= LoadListsAsync();
        return _loadTask;
    }

    public static void Attach(CoreWebView2 core)
    {
        var key = core.BrowserProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                  core.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!AttachedProfiles.TryAdd(key, 0)) return;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, args) =>
        {
            if (!Enabled || !Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var uri)) return;
            if (uri.Scheme is not ("http" or "https")) return;
            if (!ShouldBlock(uri.Host)) return;
            args.Response = core.Environment.CreateWebResourceResponse(
                null, 403, "Blocked by Lyravoxen protection",
                "Content-Type: text/plain; charset=utf-8");
            Interlocked.Increment(ref _blockedCount);
        };
    }

    private static bool ShouldBlock(string host)
    {
        host = host.TrimEnd('.').ToLowerInvariant();
        if (ProtectedMediaHosts.Any(domain => IsDomainOrSubdomain(host, domain))) return false;
        if (AllowedHosts.Keys.Any(domain => IsDomainOrSubdomain(host, domain))) return false;
        return BlockedHosts.Keys.Any(domain => IsDomainOrSubdomain(host, domain));
    }

    private static bool IsDomainOrSubdomain(string host, string domain)
        => host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
           host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    private static async Task LoadListsAsync()
    {
        foreach (var host in BuiltInHosts)
            if (!string.IsNullOrWhiteSpace(host)) BlockedHosts.TryAdd(host.Trim(), 0);

        foreach (var url in Lists)
        {
            try
            {
                using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var text = await response.Content.ReadAsStringAsync();
                ParseList(text);
            }
            catch
            {
                // Keep the built-in protection if a list is unavailable.
            }
        }
    }

    private static void ParseList(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('!') || line.StartsWith('[') ||
                line.StartsWith("@@") || line.Contains('$') || line.Contains('*') ||
                line.Contains('/') && !line.StartsWith("||", StringComparison.Ordinal)) continue;

            string? host = null;
            if (line.StartsWith("||", StringComparison.Ordinal) && line.EndsWith('^'))
                host = line[2..^1];
            else
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && (parts[0] == "0.0.0.0" || parts[0] == "127.0.0.1"))
                    host = parts[1];
            }

            if (host is null || host.StartsWith('.') || !host.Contains('.') ||
                host.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '-'))) continue;
            host = host.TrimEnd('.').ToLowerInvariant();
            if (!ProtectedMediaHosts.Any(domain => IsDomainOrSubdomain(host, domain)))
                BlockedHosts.TryAdd(host, 0);
        }
    }

    public static void AllowSite(string host)
    {
        if (!string.IsNullOrWhiteSpace(host)) AllowedHosts.TryAdd(host.Trim().ToLowerInvariant(), 0);
    }

    public static void ResetAllowedSites() => AllowedHosts.Clear();
}
