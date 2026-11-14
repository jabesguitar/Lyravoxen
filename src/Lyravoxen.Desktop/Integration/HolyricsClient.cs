using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Lyravoxen.Integration;

public sealed class HolyricsClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Lyravoxen", "holyrics.dat");

    public async Task ConfigureAndTestAsync(string host, int port, string token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Informe o endereço do Holyrics.");
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Informe o token do Holyrics.");

        var settings = new Settings(host.Trim(), port, token.Trim());
        await CheckPermissionAsync(settings, "ShowQuickPresentation");
        Save(settings);
    }

    public async Task ShowTextAsync(string text)
    {
        var settings = Load();
        if (settings is null || string.IsNullOrWhiteSpace(text)) return;

        var url = BuildUrl(settings, "ShowQuickPresentation");
        using var response = await Http.PostAsJsonAsync(url, new { text });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {body}");
        // ShowQuickPresentation é documentado pelo Holyrics como método sem retorno.
        // Portanto, não tentar interpretar o corpo como JSON.
        if (!string.IsNullOrWhiteSpace(body))
            EnsureApiSuccess(body);
    }

    private static async Task CheckPermissionAsync(Settings settings, string action)
    {
        var url = BuildUrl(settings, "CheckPermissions");
        using var response = await Http.PostAsJsonAsync(url, new { actions = action });
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Holyrics respondeu HTTP {(int)response.StatusCode}: {body}");
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("O Holyrics não retornou confirmação. Confira se o API Server Local está ativo na porta informada.");

        EnsureApiSuccess(body);
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
