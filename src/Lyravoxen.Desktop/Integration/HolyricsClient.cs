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

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Lyravoxen", "holyrics.dat");

    public async Task ConfigureAndTestAsync(string host, int port, string token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Informe o endereço do Holyrics.");
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Informe o token do Holyrics.");

        var settings = new Settings(host.Trim(), port, token.Trim());
        await CheckPermissionAsync(settings, "ShowQuickPresentation,CloseCurrentQuickPresentation");
        Save(settings);
    }

    public async Task TestConnectionAsync()
    {
        var settings = Load();
        if (settings is null)
            throw new InvalidOperationException("Holyrics ainda não foi configurado. Abra as configurações e teste a conexão primeiro.");
        await CheckPermissionAsync(settings, "ShowQuickPresentation,CloseCurrentQuickPresentation");
    }

    public async Task ShowTextAsync(string text)
    {
        var settings = Load();
        if (settings is null || string.IsNullOrWhiteSpace(text)) return;

        var body = JsonSerializer.SerializeToUtf8Bytes(new { text });
        using var response = await SendJsonAsync(BuildUrl(settings, "ShowQuickPresentation"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {responseBody}");
        if (!string.IsNullOrWhiteSpace(responseBody))
            EnsureApiSuccess(responseBody);
    }

    public async Task CloseQuickPresentationAsync()
    {
        var settings = Load();
        if (settings is null) return;

        var body = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>());
        using var response = await SendJsonAsync(BuildUrl(settings, "CloseCurrentQuickPresentation"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {responseBody}");
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
