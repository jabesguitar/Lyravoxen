using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace Lyravoxen.Services;

/// <summary>
/// Request-level domain filtering for the embedded WebView2 browser.
/// This is a lightweight tracker/ad domain filter, not a cosmetic filter
/// and not a guarantee against first-party/in-stream YouTube advertising.
/// </summary>
internal static class BrowserAdBlocker
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly ConcurrentDictionary<string, byte> BlockedHosts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> AllowedHosts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<CoreWebView2, byte> Attached = new();
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Lyravoxen", "browser-protection.json");
    private static readonly string HostsListUrl = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";
    private static readonly string[] ProtectedHosts =
    {
        "youtube.com", "youtube-nocookie.com", "googlevideo.com", "ytimg.com",
        "youtubei.googleapis.com", "googleapis.com", "gstatic.com"
    };
    private static readonly string[] BuiltInBlockedHosts =
    {
        "doubleclick.net", "googlesyndication.com", "googleadservices.com",
        "adservice.google.com", "adnxs.com", "adsrvr.org", "advertising.com",
        "adform.net", "taboola.com", "outbrain.com", "scorecardresearch.com",
        "quantserve.com", "criteo.com", "casalemedia.com", "popads.net",
        "popcash.net", "zedo.com", "trafficjunky.com"
    };
    private static int _enabled = 1;
    private static int _blockedCount;
    private static Task? _loadTask;

    public static bool Enabled
    {
        get => Volatile.Read(ref _enabled) == 1;
        set { Volatile.Write(ref _enabled, value ? 1 : 0); SaveSettings(); }
    }

    public static int BlockedCount => Volatile.Read(ref _blockedCount);
    public static int RuleCount => BlockedHosts.Count;

    public static async Task InitializeAsync()
    {
        LoadSettings();
        foreach (var host in BuiltInBlockedHosts) BlockedHosts.TryAdd(host, 0);
        _loadTask ??= LoadHostsListAsync();
        await _loadTask;
    }

    public static void Attach(CoreWebView2 core)
    {
        if (!Attached.TryAdd(core, 0)) return;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, args) =>
        {
            if (!Enabled || !Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var uri)) return;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;
            if (!ShouldBlock(uri.Host)) return;
            args.Response = core.Environment.CreateWebResourceResponse(
                null, 403, "Blocked by Lyravoxen", "Content-Type: text/plain; charset=utf-8");
            Interlocked.Increment(ref _blockedCount);
        };
    }

    public static void AllowCurrentSite(string? address)
    {
        if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            AllowedHosts.TryAdd(uri.Host.TrimEnd('.').ToLowerInvariant(), 0);
            SaveSettings();
        }
    }

    private static bool ShouldBlock(string host)
    {
        host = host.TrimEnd('.').ToLowerInvariant();
        if (ProtectedHosts.Any(domain => IsHostOrChild(host, domain))) return false;
        if (AllowedHosts.Keys.Any(domain => IsHostOrChild(host, domain))) return false;
        return BlockedHosts.Keys.Any(domain => IsHostOrChild(host, domain));
    }

    private static bool IsHostOrChild(string host, string domain)
        => host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
           host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    private static async Task LoadHostsListAsync()
    {
        try
        {
            using var response = await Http.GetAsync(HostsListUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            foreach (var raw in body.Split('\n'))
            {
                var line = raw.Split('#')[0].Trim();
                if (line.Length == 0) continue;
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || (parts[0] != "0.0.0.0" && parts[0] != "127.0.0.1" && parts[0] != "::")) continue;
                var host = parts[1].TrimEnd('.').ToLowerInvariant();
                if (host.Contains('.') && host.All(c => char.IsLetterOrDigit(c) || c is '.' or '-'))
                    if (!ProtectedHosts.Any(domain => IsHostOrChild(host, domain))) BlockedHosts.TryAdd(host, 0);
            }
        }
        catch
        {
            // Built-in rules remain active when the public list cannot be downloaded.
        }
    }

    private sealed record Settings(bool Enabled, string[] AllowedHosts);

    private static void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
            if (settings is null) return;
            Volatile.Write(ref _enabled, settings.Enabled ? 1 : 0);
            foreach (var host in settings.AllowedHosts ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(host)) AllowedHosts.TryAdd(host, 0);
        }
        catch { }
    }

    private static void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(new Settings(Enabled, AllowedHosts.Keys.ToArray()));
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
