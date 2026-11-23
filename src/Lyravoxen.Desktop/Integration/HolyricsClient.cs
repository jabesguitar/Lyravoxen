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

    public bool IsConfigured => File.Exists(_settingsPath);

    /// <summary>Salva a conexão. Devolve um aviso (ou null) quando o token não tem alguma permissão necessária.</summary>
    public async Task<string?> ConfigureAndTestAsync(string host, int port, string token)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Informe o endereço do Holyrics.");
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Informe o token do Holyrics.");

        var settings = new Settings(host.Trim(), port, token.Trim());
        var denied = await CheckPermissionAsync(settings, string.Join(",", RequiredActions));
        Save(settings);
        return denied.Count == 0
            ? null
            : "Conexão salva, mas o token não permite: " + string.Join(", ", denied) +
              ". Marque essas permissões no token do Holyrics (API Server Local) para usar Bíblia e projeção.";
    }

    public async Task TestConnectionAsync()
    {
        var settings = Load();
        if (settings is null)
            throw new InvalidOperationException("Holyrics ainda não foi configurado. Abra as configurações e teste a conexão primeiro.");
        var denied = await CheckPermissionAsync(settings, string.Join(",", RequiredActions));
        if (denied.Count > 0)
            throw new InvalidOperationException("O token do Holyrics não permite: " + string.Join(", ", denied) + ".");
    }

    public async Task<IReadOnlyList<BibleVersionInfo>> GetBibleVersionsAsync()
    {
        var settings = Load();
        if (settings is null) throw new InvalidOperationException("Holyrics ainda não foi configurado.");
        using var response = await SendJsonAsync(BuildUrl(settings, "GetBibleVersionsV2"), JsonSerializer.SerializeToUtf8Bytes(new { }));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw HttpFailure("GetBibleVersionsV2", response.StatusCode, body);
        EnsureApiSuccess(body);
        using var json = JsonDocument.Parse(body);
        var result = new List<BibleVersionInfo>();
        foreach (var item in json.RootElement.GetProperty("data").EnumerateArray())
        {
            var version = item.TryGetProperty("version", out var v) ? v.GetString() : null;
            var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
            var language = item.TryGetProperty("language", out var l) && l.TryGetProperty("iso", out var iso) ? iso.GetString() : null;
            if (!string.IsNullOrWhiteSpace(version) && !string.IsNullOrWhiteSpace(title))
                result.Add(new BibleVersionInfo(version, title, language ?? ""));
        }
        return result;
    }

    public async Task ShowBibleVerseAsync(string reference, string version)
    {
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Informe uma referência bíblica.");
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Selecione uma versão bíblica.");
        var settings = Load();
        if (settings is null) throw new InvalidOperationException("Holyrics ainda não foi configurado.");
        var body = JsonSerializer.SerializeToUtf8Bytes(new
        {
            references = reference.Trim(),
            version = version.Trim(),
            quick_presentation = true
        });
        using var response = await SendJsonAsync(BuildUrl(settings, "ShowVerse"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw HttpFailure("ShowVerse", response.StatusCode, responseBody);
        if (!string.IsNullOrWhiteSpace(responseBody)) EnsureApiSuccess(responseBody);
    }

    public sealed record BibleVersionInfo(string Version, string Title, string Language);

    public async Task ShowTextAsync(string text)
    {
        var settings = Load();
        if (settings is null || string.IsNullOrWhiteSpace(text)) return;

        var body = JsonSerializer.SerializeToUtf8Bytes(new { text });
        using var response = await SendJsonAsync(BuildUrl(settings, "ShowQuickPresentation"), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw HttpFailure("ShowQuickPresentation", response.StatusCode, responseBody);
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
            throw HttpFailure("CloseCurrentQuickPresentation", response.StatusCode, responseBody);
        if (!string.IsNullOrWhiteSpace(responseBody))
            EnsureApiSuccess(responseBody);
    }

    public async Task CloseProjectionAsync()
    {
        var settings = Load();
        if (settings is null)
            throw new InvalidOperationException("Holyrics ainda não foi configurado. Conecte o aplicativo antes de encerrar a projeção.");

        // A API do Holyrics separa a apresentação rápida da apresentação convencional.
        // Tenta ambas mesmo que uma delas não esteja ativa ou não esteja disponível na versão instalada.
        var errors = new List<Exception>();
        foreach (var action in new[] { "CloseCurrentQuickPresentation", "CloseCurrentPresentation" })
        {
            try { await CloseActionAsync(settings, action); }
            catch (Exception ex) { errors.Add(new InvalidOperationException(action + ": " + ex.Message, ex)); }
        }
        if (errors.Count > 0)
            throw new AggregateException("O Holyrics não confirmou o encerramento de todas as apresentações. Verifique as permissões do token e a versão do Holyrics.", errors);
    }

    private static async Task CloseActionAsync(Settings settings, string action)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>());
        using var response = await SendJsonAsync(BuildUrl(settings, action), body);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw HttpFailure(action, response.StatusCode, responseBody);
        if (!string.IsNullOrWhiteSpace(responseBody))
            EnsureApiSuccess(responseBody);
    }

    private static async Task<List<string>> CheckPermissionAsync(Settings settings, string action)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { actions = action });
        using var response = await SendJsonAsync(BuildUrl(settings, "CheckPermissions"), body);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw HttpFailure("CheckPermissions", response.StatusCode, responseBody);
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new InvalidOperationException("O Holyrics não retornou confirmação. Confira se o API Server Local está ativo.");

        EnsureApiSuccess(responseBody);
        return DeniedActions(responseBody);
    }

    /// <summary>Confere uma a uma as permissões do token e devolve um texto legível.</summary>
    public async Task<string> DiagnoseAsync()
    {
        var settings = Load();
        if (settings is null) return "Holyrics: ainda não configurado (use Conectar Holyrics).";
        var text = new StringBuilder();
        var actions = RequiredActions.Concat(new[] { "GetCurrentQuickPresentation", "GetCurrentPresentation" });
        foreach (var action in actions)
        {
            try
            {
                var denied = await CheckPermissionAsync(settings, action);
                text.AppendLine((denied.Count == 0 ? "  OK      " : "  NEGADA  ") + action);
            }
            catch (Exception ex) { text.AppendLine("  ERRO    " + action + ": " + ex.Message); }
        }
        return text.ToString();
    }

    private static HttpRequestException HttpFailure(string action, HttpStatusCode status, string body)
    {
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new HttpRequestException(
                "o token do Holyrics não tem permissão para a ação \"" + action + "\". " +
                "No Holyrics, abra as configurações do API Server Local, edite as permissões do token e marque: " +
                string.Join(", ", RequiredActions) + ". Depois use o botão ↻ Holyrics (não precisa reconectar).");
        return new HttpRequestException($"Holyrics respondeu HTTP {(int)status} na ação {action}: {body}");
    }

    private static readonly string[] RequiredActions =
    {
        "ShowQuickPresentation", "CloseCurrentQuickPresentation", "CloseCurrentPresentation", "GetBibleVersionsV2", "ShowVerse"
    };

    /// <summary>Lê o retorno de CheckPermissions e devolve as ações que o token NÃO permite.</summary>
    private static List<string> DeniedActions(string body)
    {
        var denied = new List<string>();
        try
        {
            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("data", out var data)) return denied;
            if (data.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in data.EnumerateObject())
                    if (item.Value.ValueKind == JsonValueKind.False) denied.Add(item.Name);
            }
            else if (data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var name = item.TryGetProperty("action", out var a) ? a.GetString() : null;
                    var allowed = item.TryGetProperty("allowed", out var al) ? al.ValueKind != JsonValueKind.False
                                : item.TryGetProperty("permission", out var pe) ? pe.ValueKind != JsonValueKind.False : true;
                    if (!string.IsNullOrEmpty(name) && !allowed) denied.Add(name);
                }
            }
        }
        catch (JsonException) { }
        return denied;
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
