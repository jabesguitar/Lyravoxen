using System.IO;
using System.Text;
using System.Text.Json;

namespace Lyravoxen.Services;

internal sealed class SongRequest
{
    public string Id { get; set; } = "";
    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime ReceivedAt { get; set; }
}

/// <summary>Lista de espera dos pedidos de música enviados pela página de pedidos.</summary>
internal static class SongRequestStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "pedidos.json");

    public static List<SongRequest> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<SongRequest>();
            return JsonSerializer.Deserialize<List<SongRequest>>(File.ReadAllText(FilePath)) ?? new List<SongRequest>();
        }
        catch (Exception ex)
        {
            AppLog.Error("Não foi possível ler os pedidos", ex);
            return new List<SongRequest>();
        }
    }

    public static void Save(List<SongRequest> requests)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(requests, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { AppLog.Error("Não foi possível salvar os pedidos", ex); }
    }

    /// <summary>Valida o envio da página de pedidos. Links repetidos e inválidos são ignorados.</summary>
    public static (List<SongRequest> Added, bool Ok, string Message) Parse(string json, IEnumerable<SongRequest> existing, DateTime now)
    {
        var added = new List<SongRequest>();
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var group = Clean(root.TryGetProperty("grupo", out var groupElement) ? groupElement.GetString() : "", 60);
            if (!root.TryGetProperty("itens", out var items) || items.ValueKind != JsonValueKind.Array)
                return (added, false, "Nenhuma música recebida.");

            var known = new HashSet<string>(existing.Select(r => r.Url), StringComparer.OrdinalIgnoreCase);
            var ignored = 0;
            foreach (var item in items.EnumerateArray().Take(10))
            {
                var link = Clean(item.TryGetProperty("link", out var linkElement) ? linkElement.GetString() : "", 500);
                var name = Clean(item.TryGetProperty("nome", out var nameElement) ? nameElement.GetString() : "", 80);
                if (link.Length == 0) continue;
                if (!link.Contains("://", StringComparison.Ordinal)) link = "https://" + link;
                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                    !uri.Host.Contains('.'))
                {
                    ignored++;
                    continue;
                }
                if (!known.Add(uri.ToString())) { ignored++; continue; }

                added.Add(new SongRequest
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Group = group,
                    Name = name,
                    Url = uri.ToString(),
                    ReceivedAt = now
                });
            }

            if (added.Count == 0) return (added, false, "Nenhum link válido (ou já enviado). Confira e tente de novo.");
            var message = "Enviado! " + added.Count + (added.Count == 1 ? " música recebida." : " músicas recebidas.");
            if (ignored > 0) message += " " + ignored + " link(s) inválido(s) ou repetido(s) ignorado(s).";
            return (added, true, message);
        }
        catch (JsonException)
        {
            return (added, false, "Pedido inválido.");
        }
    }

    private static string Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var builder = new StringBuilder(value.Length);
        foreach (var c in value) builder.Append(char.IsControl(c) ? ' ' : c);
        var text = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return text.Length > maxLength ? text[..maxLength] : text;
    }
}
