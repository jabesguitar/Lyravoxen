using System.IO;
using System.Text.RegularExpressions;

namespace Lyravoxen.Services;

/// <summary>Configuração do recebimento de pedidos pelo WhatsApp: número do operador e/ou grupo.</summary>
internal sealed class WhatsAppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "pedidos-whatsapp.txt");

    /// <summary>Somente dígitos, com país e DDD (ex.: 5582999999999). Vazio = a pessoa escolhe o contato.</summary>
    public string Number { get; set; } = "";
    /// <summary>Nome do grupo, só para mostrar no formulário.</summary>
    public string GroupName { get; set; } = "";
    /// <summary>Link de convite do grupo (https://chat.whatsapp.com/CODIGO). Vazio = sem grupo.</summary>
    public string GroupLink { get; set; } = "";
    /// <summary>Importar sozinho os pedidos que forem copiados do WhatsApp (Web/Desktop) para a área de transferência.</summary>
    public bool WatchClipboard { get; set; } = true;

    public static string DigitsOnly(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());

    /// <summary>Aceita só links de convite de grupo do WhatsApp; devolve "" se não for um.</summary>
    public static string NormalizeGroupLink(string? value)
    {
        var match = Regex.Match(value ?? "", @"chat\.whatsapp\.com/([A-Za-z0-9_\-]{10,40})", RegexOptions.IgnoreCase);
        return match.Success ? "https://chat.whatsapp.com/" + match.Groups[1].Value : "";
    }

    /// <summary>Nome seguro para ir dentro do HTML/JavaScript do formulário.</summary>
    public static string SafeName(string? value)
    {
        var cleaned = new string((value ?? "").Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '.' || c == ',').ToArray());
        cleaned = string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return cleaned.Length > 60 ? cleaned[..60] : cleaned;
    }

    public static WhatsAppSettings Load()
    {
        var settings = new WhatsAppSettings();
        try
        {
            if (!File.Exists(FilePath)) return settings;
            foreach (var raw in File.ReadAllLines(FilePath))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var separator = line.IndexOf('=');
                if (separator < 0)
                {
                    // Formato antigo: o arquivo tinha só o número.
                    if (settings.Number.Length == 0) settings.Number = DigitsOnly(line);
                    continue;
                }
                var key = line[..separator].Trim().ToLowerInvariant();
                var value = line[(separator + 1)..].Trim();
                switch (key)
                {
                    case "numero": settings.Number = DigitsOnly(value); break;
                    case "grupo": settings.GroupName = SafeName(value); break;
                    case "link": settings.GroupLink = NormalizeGroupLink(value); break;
                    case "copiados": settings.WatchClipboard = value != "0"; break;
                }
            }
        }
        catch (Exception ex) { AppLog.Error("Não foi possível ler a configuração do WhatsApp", ex); }
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllLines(FilePath, new[]
            {
                "numero=" + DigitsOnly(Number),
                "grupo=" + SafeName(GroupName),
                "link=" + NormalizeGroupLink(GroupLink),
                "copiados=" + (WatchClipboard ? "1" : "0")
            });
        }
        catch (Exception ex) { AppLog.Error("Não foi possível salvar a configuração do WhatsApp", ex); }
    }
}
