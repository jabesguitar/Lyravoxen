using System.IO;
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
    private static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen");
    private static readonly string SettingsPath = Path.Combine(DataFolder, "browser-protection.json");
    private static readonly string CachedHostsPath = Path.Combine(DataFolder, "browser-hosts.txt");
    private static readonly string NativeRulesPath = Path.Combine(DataFolder, "adblock-rules.txt");
    private static readonly string[] FilterListUrls =
    {
        "https://easylist.to/easylist/easylist.txt",
        "https://easylist.to/easylist/easyprivacy.txt"
    };
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

    public static Task InitializeAsync()
    {
        LoadSettings();
        foreach (var host in BuiltInBlockedHosts) BlockedHosts.TryAdd(host, 0);
        LoadCachedHosts();
        // Never delay browser startup for a network download; refresh rules in the background.
        _loadTask ??= LoadProtectionListsAsync();
        return _loadTask;
    }

    public static void Attach(CoreWebView2 core)
    {
        if (!Attached.TryAdd(core, 0)) return;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
        core.WebResourceRequested += (_, args) =>
        {
            if (!Enabled || !Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var uri)) return;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;
            var sourceUrl = core.Source?.ToString() ?? uri.GetLeftPart(UriPartial.Authority);
            var requestType = GetRequestType(args.ResourceContext, uri, sourceUrl);
            var nativeBlocked = NativeAdblockEngine.ShouldBlock(args.Request.Uri, sourceUrl, requestType, args.Request.Method);
            if (!nativeBlocked && !Lyravoxen.Offline.BrowserRequestPolicy.ShouldBlockRequest(uri, ProtectedHosts, BlockedHosts.Keys, AllowedHosts.Keys)) return;
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
        => Lyravoxen.Offline.BrowserRequestPolicy.ShouldBlock(
            host, ProtectedHosts, BlockedHosts.Keys, AllowedHosts.Keys);

    private static bool IsHostOrChild(string host, string domain)
        => host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
           host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    private static string GetRequestType(CoreWebView2WebResourceContext context, Uri requestUri, string sourceUrl) => context switch
    {
        CoreWebView2WebResourceContext.Document => Uri.TryCreate(sourceUrl, UriKind.Absolute, out var pageUri) && requestUri == pageUri ? "document" : "sub_frame",
        CoreWebView2WebResourceContext.Stylesheet => "stylesheet",
        CoreWebView2WebResourceContext.Image => "image",
        CoreWebView2WebResourceContext.Media => "media",
        CoreWebView2WebResourceContext.Font => "font",
        CoreWebView2WebResourceContext.Script => "script",
        CoreWebView2WebResourceContext.XmlHttpRequest => "xmlhttprequest",
        CoreWebView2WebResourceContext.Fetch => "xmlhttprequest",
        _ => "other"
    };

    private static async Task LoadProtectionListsAsync()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            if (File.Exists(NativeRulesPath))
                NativeAdblockEngine.Initialize(await File.ReadAllTextAsync(NativeRulesPath));

            var tasks = FilterListUrls.Select(async url =>
            {
                try
                {
                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch { return string.Empty; }
            });
            var lists = await Task.WhenAll(tasks);
            var rules = string.Join(Environment.NewLine, lists.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrWhiteSpace(rules))
            {
                await File.WriteAllTextAsync(NativeRulesPath, rules);
                NativeAdblockEngine.Initialize(rules);
            }
        }
        catch
        {
            // Existing domain protection remains active if lists or native engine are unavailable.
        }
        _ = LoadHostsListAsync();
    }

    private static async Task LoadHostsListAsync()
    {
        try
        {
            using var response = await Http.GetAsync(HostsListUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            try
            {
                Directory.CreateDirectory(DataFolder);
                File.WriteAllText(CachedHostsPath, body);
            }
            catch { }
            ParseHosts(body);
        }
        catch
        {
            // Built-in and cached rules remain active when the public list cannot be downloaded.
        }
    }

    private static void LoadCachedHosts()
    {
        try
        {
            if (File.Exists(CachedHostsPath)) ParseHosts(File.ReadAllText(CachedHostsPath));
        }
        catch { }
    }

    private static void ParseHosts(string body)
    {
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
