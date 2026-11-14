using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lyravoxen.Integration;

public sealed class HolyricsClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Lyravoxen", "holyrics.dat");

    public async Task ConfigureAndTestAsync(string host, int port, string token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Informe o endereço do Holyrics.");
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Informe o token do Holyrics.");
        var settings = new Settings(host.Trim(), port, token.Trim());
        await SendAsync(settings, "Teste de conexão Lyravoxen ↔ Holyrics");
        Save(settings);
    }

    public async Task ShowTextAsync(string text)
    {
        var settings = Load();
        if (settings is null || string.IsNullOrWhiteSpace(text)) return;
        await SendAsync(settings, text);
    }

    private static async Task SendAsync(Settings settings, string text)
    {
        var url = $"http://{settings.Host}:{settings.Port}/api/ShowQuickPresentation?token={Uri.EscapeDataString(settings.Token)}";
        using var response = await Http.PostAsJsonAsync(url, new { text });
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        if (root.TryGetProperty("status", out var status) && status.GetString() == "error")
        {
            var detail = root.TryGetProperty("error", out var error) ? error.ToString() : "A API recusou a solicitação.";
            throw new InvalidOperationException(detail);
        }
    }

    private void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var plain = JsonSerializer.SerializeToUtf8Bytes(settings);
        var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_settingsPath, protectedBytes);
        CryptographicOperations.ZeroMemory(plain);
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
