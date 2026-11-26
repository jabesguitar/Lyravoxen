using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Lyravoxen.Services;

internal sealed record CloudRequest(string Id, string Text, string SenderIp);
internal sealed record CloudBlockedSender(string Ip, string Name);

/// <summary>Conversa do Lyravoxen com o serviço de pedidos na nuvem (ver CloudWorker).</summary>
internal static class CloudRequestClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

    private static HttpRequestMessage Build(HttpMethod method, string baseUrl, string path, string token)
    {
        var request = new HttpRequestMessage(method, baseUrl.TrimEnd('/') + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static string Explain(Exception ex) => ex switch
    {
        TaskCanceledException => "O serviço demorou a responder.",
        HttpRequestException => "Sem conexão com o serviço (confira a internet e o endereço).",
        _ => "Resposta inesperada do serviço."
    };

    public static async Task<(bool Ok, string Message)> PingAsync(string baseUrl, string token)
    {
        try
        {
            using var request = Build(HttpMethod.Get, baseUrl, "/ping", token);
            using var response = await Http.SendAsync(request);
            if ((int)response.StatusCode == 401) return (false, "Senha incorreta: confira o passo 5 (ADMIN_TOKEN) e se fez Deploy.");
            if ((int)response.StatusCode == 404) return (false, "Esse endereço não parece ser o serviço de pedidos (confira o código colado e o Deploy).");
            if (!response.IsSuccessStatusCode) return (false, "O serviço respondeu com erro " + (int)response.StatusCode + ".");
            return (true, "Conectado.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return (false, Explain(ex)); }
    }

    /// <summary>Busca pedidos novos. "version" é o marcador devolvido na busca anterior (vazio = trazer tudo).</summary>
    public static async Task<(bool Ok, string Version, List<CloudRequest> Items, string Message)> FetchAsync(string baseUrl, string token, string version)
    {
        var items = new List<CloudRequest>();
        try
        {
            using var request = Build(HttpMethod.Get, baseUrl, "/pedidos?v=" + Uri.EscapeDataString(version ?? ""), token);
            using var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return (false, "", items, "Erro " + (int)response.StatusCode + " ao buscar pedidos.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            var newVersion = root.TryGetProperty("v", out var v) ? v.GetString() ?? "" : "";
            if (root.TryGetProperty("pedidos", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in list.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "" : "";
                    var text = item.TryGetProperty("text", out var textElement) ? textElement.GetString() ?? "" : "";
                    var senderIp = item.TryGetProperty("ip", out var ipElement) ? ipElement.GetString() ?? "" : "";
                    if (id.Length > 0) items.Add(new CloudRequest(id, text, senderIp));
                }
            }
            return (true, newVersion, items, "");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return (false, "", items, Explain(ex)); }
    }

    public static async Task<(bool Ok, List<CloudBlockedSender> Items, string Message)> GetBlockedAsync(string baseUrl, string token)
    {
        var items = new List<CloudBlockedSender>();
        try
        {
            using var request = Build(HttpMethod.Get, baseUrl, "/bloqueios", token);
            using var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return (false, items, "Erro " + (int)response.StatusCode + " ao consultar bloqueios.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("blocked", out var list) && list.ValueKind == JsonValueKind.Array)
                foreach (var item in list.EnumerateArray())
                {
                    var ip = item.TryGetProperty("ip", out var ipValue) ? ipValue.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var nameValue) ? nameValue.GetString() ?? "" : "";
                    if (ip.Length > 0) items.Add(new CloudBlockedSender(ip, name));
                }
            return (true, items, "");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return (false, items, Explain(ex)); }
    }

    public static async Task<(bool Ok, string Message)> SetBlockedAsync(string baseUrl, string token, string ip, string name, bool blocked)
    {
        try
        {
            using var request = Build(HttpMethod.Post, baseUrl, "/bloqueios", token);
            request.Content = new StringContent(JsonSerializer.Serialize(new { ip, name, blocked }), Encoding.UTF8, "application/json");
            using var response = await Http.SendAsync(request);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            return (response.IsSuccessStatusCode && root.TryGetProperty("ok", out var ok) && ok.GetBoolean(),
                root.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "Falha ao atualizar bloqueio.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return (false, Explain(ex)); }
    }

    /// <summary>Apaga do serviço os pedidos já recebidos.</summary>
    public static async Task<bool> AckAsync(string baseUrl, string token, IEnumerable<string> ids)
    {
        try
        {
            using var request = Build(HttpMethod.Post, baseUrl, "/confirmar", token);
            request.Content = new StringContent(JsonSerializer.Serialize(new { ids = ids.ToArray() }), Encoding.UTF8, "application/json");
            using var response = await Http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { return false; }
    }
}
