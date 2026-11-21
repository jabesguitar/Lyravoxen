using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

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
            Remember(requests.Select(r => r.Url));
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

            var known = new HashSet<string>(existing.Select(r => r.Url).Concat(RecentlySeen()), StringComparer.OrdinalIgnoreCase);
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

    private static readonly string SeenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "pedidos-vistos.txt");
    private static readonly TimeSpan SeenFor = TimeSpan.FromDays(2);

    /// <summary>Links já recebidos nos últimos 2 dias (mesmo que o pedido já tenha sido tocado ou descartado),
    /// para que importar de novo a mesma conversa do grupo não traga tudo outra vez.</summary>
    private static IEnumerable<string> RecentlySeen()
    {
        var result = new List<string>();
        try
        {
            if (!File.Exists(SeenPath)) return result;
            var limit = DateTime.UtcNow - SeenFor;
            foreach (var line in File.ReadAllLines(SeenPath))
            {
                var tab = line.IndexOf('\t');
                if (tab < 0 || !long.TryParse(line[..tab], out var ticks)) continue;
                if (new DateTime(ticks, DateTimeKind.Utc) >= limit) result.Add(line[(tab + 1)..]);
            }
        }
        catch { /* sem o histórico, só deixa de filtrar repetidos antigos */ }
        return result;
    }

    private static void Remember(IEnumerable<string> urls)
    {
        try
        {
            var now = DateTime.UtcNow;
            var limit = now - SeenFor;
            var lines = new List<string>();
            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(SeenPath))
            {
                foreach (var line in File.ReadAllLines(SeenPath))
                {
                    var tab = line.IndexOf('\t');
                    if (tab < 0 || !long.TryParse(line[..tab], out var ticks)) continue;
                    if (new DateTime(ticks, DateTimeKind.Utc) < limit) continue;
                    lines.Add(line);
                    have.Add(line[(tab + 1)..]);
                }
            }
            foreach (var url in urls)
                if (!string.IsNullOrWhiteSpace(url) && have.Add(url)) lines.Add(now.Ticks + "\t" + url);
            Directory.CreateDirectory(Path.GetDirectoryName(SeenPath)!);
            File.WriteAllLines(SeenPath, lines.TakeLast(1000));
        }
        catch { /* histórico é só uma conveniência */ }
    }

    // Cabeçalhos de mensagem de conversa do WhatsApp: exportação do Android ("19/11/2026 20:15 - Maria: ..."),
    // do iPhone ("[19/11/2026 20:15:10] Maria: ...") e cópia do WhatsApp Web ("[20:15, 19/11/2026] Maria: ...").
    private static readonly Regex ChatHeader = new(
        @"^\u200e?\u200f?(?:\[?\d{1,2}[/\.]\d{1,2}[/\.]\d{2,4},?\s+\d{1,2}:\d{2}(?::\d{2})?(?:\s?[APap]\.?[Mm]\.?)?\]?\s*-?\s*" +
        @"|\[\d{1,2}:\d{2}(?::\d{2})?(?:\s?[APap][Mm])?,?\s*\d{1,2}[/\.]\d{1,2}[/\.]\d{2,4}\]\s*)" +
        @"(?<sender>[^:\r\n]{1,60}?):\s?(?<msg>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex GroupLine = new(
        @"^(?:conjunto|grupo|banda|minist[eé]rio)\s*[:\-]\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Separa um texto de conversa do WhatsApp em mensagens (quem enviou + texto).
    /// Sem cabeçalhos de conversa, devolve o texto inteiro como uma única mensagem sem remetente.</summary>
    public static List<(string Sender, string Body)> SplitChat(string text)
    {
        var messages = new List<(string Sender, System.Text.StringBuilder Body)>();
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var match = ChatHeader.Match(raw);
            if (match.Success)
            {
                messages.Add((match.Groups["sender"].Value.Trim(), new System.Text.StringBuilder(match.Groups["msg"].Value)));
            }
            else if (messages.Count > 0) messages[^1].Body.Append('\n').Append(raw);
            else messages.Add(("", new System.Text.StringBuilder(raw)));
        }
        return messages.Select(m => (m.Sender, m.Body.ToString())).ToList();
    }

    /// <summary>True se o texto copiado parece um pedido de música feito de propósito (e não só um link solto
    /// que o operador copiou para outro fim): tem a marca do formulário, ou "Conjunto: X" / cabeçalho de conversa com link.</summary>
    public static bool LooksLikeRequest(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 20000) return false;
        if (text.Contains("#LYRA:", StringComparison.Ordinal)) return true;
        if (!Regex.IsMatch(text, @"https?://|youtu\.be/|youtube\.com/|www\.", RegexOptions.IgnoreCase)) return false;
        if (text.Contains("Pedido de músicas", StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (GroupLine.IsMatch(line)) return true;
            var header = ChatHeader.Match(line);
            if (header.Success && GroupLine.IsMatch(header.Groups["msg"].Value.Trim())) return true;
        }
        return SplitChat(text).Count(m => m.Sender.Length > 0) > 0;
    }

    /// <summary>
    /// Converte um texto colado (do WhatsApp, de uma pessoa ou de um grupo inteiro) em pedidos (JSON).
    /// Entende a mensagem do formulário (linha #LYRA:...), texto livre ("Conjunto: X" e "nome - link" por linha)
    /// e conversas copiadas/exportadas do WhatsApp (o nome de quem enviou vira o "conjunto" quando o texto não traz um).
    /// </summary>
    public static List<string> MessagesToJson(string text, int lastMessages = 40)
    {
        var result = new List<string>();
        foreach (var (sender, body) in SplitChat(text).TakeLast(lastMessages))
        {
            var json = MessageToJson(body, sender);
            if (json is not null) result.Add(json);
        }
        return result;
    }

    public static string? MessageToJson(string text, string defaultGroup = "")
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var token = Regex.Match(text, @"#LYRA:([A-Za-z0-9+/=]+)");
        if (token.Success)
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(token.Groups[1].Value));
                if (string.IsNullOrWhiteSpace(defaultGroup)) return json;
                if (JsonNode.Parse(json) is JsonObject node)
                {
                    if (string.IsNullOrWhiteSpace((string?)node["grupo"])) node["grupo"] = defaultGroup;
                    return node.ToJsonString();
                }
                return json;
            }
            catch (Exception ex) when (ex is FormatException or JsonException) { }
        }

        var group = defaultGroup;
        var items = new List<object>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            var groupMatch = GroupLine.Match(line);
            if (groupMatch.Success) { group = groupMatch.Groups[1].Value; continue; }

            var link = Regex.Match(line, @"(https?://\S+|(?:www\.|youtu\.be/|youtube\.com/)\S+)", RegexOptions.IgnoreCase);
            if (!link.Success) continue;
            // Tira só a numeração da lista ("1.", "2)", "3 -", "•"); nomes que começam com número ("10.000 Razões") ficam inteiros.
            var name = Regex.Replace(line[..link.Index], @"^\s*(?:(?:\d{1,2}\s*[\.\)\-–—:]\s+|\d{1,2}[\.\)](?=\D)|[•*\-–—]\s*)\s*)+|[\s\-–—:•*]+$", "");
            items.Add(new { nome = name, link = link.Value.TrimEnd(')', ',', '.', ';') });
        }

        return items.Count == 0 ? null : JsonSerializer.Serialize(new { grupo = group, itens = items });
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
