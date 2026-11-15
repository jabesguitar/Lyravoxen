using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lyravoxen.Integration;

public sealed class HolyricsClient
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.None
    }) { Timeout = TimeSpan.FromSeconds(8) };

    public bool IsConfigured => File.Exists(_settingsPath);

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Lyravoxen", "holyrics.dat");

    public async Task ConfigureAndTestAsync(string host, int port, string token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Informe o endereço do Holyrics.");
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Informe o token do Holyrics.");

        var settings = new Settings(host.Trim(), port, token.Trim());
        await CheckPermissionAsync(settings, "ShowQuickPresentation,CloseCurrentQuickPresentation,CloseCurrentPresentation,SearchLyrics,GetLyrics");
        Save(settings);
    }

    public async Task TestConnectionAsync()
    {
        var settings = Load();
        if (settings is null)
            throw new InvalidOperationException("Holyrics ainda não foi configurado. Abra as configurações e teste a conexão primeiro.");
        await CheckPermissionAsync(settings, "ShowQuickPresentation,CloseCurrentQuickPresentation");
    }

    public async Task<IReadOnlyList<HolyricsSong>> SearchSongsAsync(string query)
    {
        var settings = Load() ?? throw new InvalidOperationException("Configure primeiro a conexão com o Holyrics.");
        await CheckPermissionAsync(settings, "SearchLyrics,GetLyrics");
        var body = JsonSerializer.SerializeToUtf8Bytes(new
        {
            text = query,
            title = true,
            artist = true,
            lyrics = false,
            fields = "id,title,artist,author"
        });
        using var response = await SendJsonAsync(BuildUrl(settings, "SearchLyrics"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {responseBody}");
        EnsureApiSuccess(responseBody);
        using var json = JsonDocument.Parse(responseBody);
        if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return Array.Empty<HolyricsSong>();
        return data.EnumerateArray().Select(item => new HolyricsSong(
            item.TryGetProperty("id", out var id) ? id.ToString() : "",
            item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            item.TryGetProperty("artist", out var artist) ? artist.GetString() ?? "" : "",
            item.TryGetProperty("author", out var author) ? author.GetString() ?? "" : ""))
            .Where(song => !string.IsNullOrWhiteSpace(song.Id))
            .Take(30).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetSongSlidesAsync(string songId)
    {
        var settings = Load() ?? throw new InvalidOperationException("Configure primeiro a conexão com o Holyrics.");
        var body = JsonSerializer.SerializeToUtf8Bytes(new { id = songId });
        using var response = await SendJsonAsync(BuildUrl(settings, "GetLyrics"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {responseBody}");
        EnsureApiSuccess(responseBody);
        using var json = JsonDocument.Parse(responseBody);
        if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("slides", out var slides) || slides.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        return slides.EnumerateArray()
            .Select(slide => slide.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "")
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
    }

    public async Task TakeOverProjectionAsync(string firstText)
    {
        var settings = Load() ?? throw new InvalidOperationException("Configure primeiro a conexão com o Holyrics.");
        await CheckPermissionAsync(settings, "CloseCurrentPresentation,ShowQuickPresentation,CloseCurrentQuickPresentation,MediaPlayerAction");
        // Stop the independent Holyrics media player before taking over the public screen.
        using (var stopPlayer = await SendJsonAsync(BuildUrl(settings, "MediaPlayerAction"),
            JsonSerializer.SerializeToUtf8Bytes(new { action = "stop" })))
        {
            var body = await stopPlayer.Content.ReadAsStringAsync();
            if (!stopPlayer.IsSuccessStatusCode) throw new HttpRequestException($"Holyrics respondeu HTTP {(int)stopPlayer.StatusCode} ao parar o player: {body}");
            if (!string.IsNullOrWhiteSpace(body)) EnsureApiSuccess(body);
        }
        using (var close = await SendJsonAsync(BuildUrl(settings, "CloseCurrentPresentation"), "{}"u8.ToArray()))
        {
            var body = await close.Content.ReadAsStringAsync();
            if (!close.IsSuccessStatusCode) throw new HttpRequestException($"Holyrics respondeu HTTP {(int)close.StatusCode}: {body}");
            if (!string.IsNullOrWhiteSpace(body)) EnsureApiSuccess(body);
        }
        if (!string.IsNullOrWhiteSpace(firstText))
            await ShowTextAsync(firstText);
    }

    public async Task ShowTextAsync(string text)
    {
        var settings = Load();
        if (settings is null) return;

        if (string.IsNullOrWhiteSpace(text))
        {
            await CloseQuickPresentationAsync(settings);
            return;
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(new { text });
        using var response = await SendJsonAsync(BuildUrl(settings, "ShowQuickPresentation"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {responseBody}");
        if (!string.IsNullOrWhiteSpace(responseBody))
            EnsureApiSuccess(responseBody);
    }

    private static async Task CloseQuickPresentationAsync(Settings settings)
    {
        using var response = await SendJsonAsync(BuildUrl(settings, "CloseCurrentQuickPresentation"), "{}"u8.ToArray());
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode} ao fechar a apresentação rápida: {responseBody}");
        if (!string.IsNullOrWhiteSpace(responseBody))
            EnsureApiSuccess(responseBody);
    }

    private static async Task CheckPermissionAsync(Settings settings, string action)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { actions = action });
        using var response = await SendJsonAsync(BuildUrl(settings, "CheckPermissions"), body);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {responseBody}");
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new InvalidOperationException("O Holyrics não retornou confirmação. Confira se o API Server Local está ativo.");

        EnsureApiSuccess(responseBody);
    }

    private static async Task<HttpResponseMessage> SendJsonAsync(string url, byte[] body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ByteArrayContent(body)
        };
        request.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        // ByteArrayContent informa Content-Length explicitamente; não usa Transfer-Encoding: chunked.
        request.Content.Headers.ContentLength = body.Length;
        return await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
    }

    private static string BuildUrl(Settings settings, string action) =>
        $"http://{settings.Host}:{settings.Port}/api/{action}?token={Uri.EscapeDataString(settings.Token)}";

    private static void EnsureApiSuccess(string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        if (root.TryGetProperty("status", out var status) &&
            string.Equals(status.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var detail = root.TryGetProperty("error", out var error) ? error.ToString() : "A API recusou a solicitação.";
            throw new InvalidOperationException("Holyrics: " + detail);
        }
        if (!root.TryGetProperty("status", out var ok) ||
            !string.Equals(ok.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resposta inesperada do Holyrics: " + body);
    }

    private void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var plain = JsonSerializer.SerializeToUtf8Bytes(settings);
        try
        {
            var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_settingsPath, protectedBytes);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private Settings? Load()
    {
        if (!File.Exists(_settingsPath)) return null;
        var encrypted = File.ReadAllBytes(_settingsPath);
        var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize<Settings>(plain); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private sealed record Settings(string Host, int Port, string Token);
}

public sealed record HolyricsSong(string Id, string Title, string Artist, string Author);
