using System.IO;

namespace Lyravoxen.Services;

/// <summary>Fontes extras de texto de letra. Hoje: chave gratuita da API do Vagalume (arquivo fontes-letra.txt, entra no backup).</summary>
internal sealed class LyricsSourceSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "fontes-letra.txt");

    public string VagalumeKey { get; set; } = "";

    public static string SafeKey(string? value) =>
        new string((value ?? "").Where(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_').Take(80).ToArray());

    public static LyricsSourceSettings Load()
    {
        var settings = new LyricsSourceSettings();
        try
        {
            if (!File.Exists(FilePath)) return settings;
            foreach (var raw in File.ReadAllLines(FilePath))
            {
                var separator = raw.IndexOf('=');
                if (separator < 0) continue;
                if (raw[..separator].Trim().Equals("vagalume", StringComparison.OrdinalIgnoreCase))
                    settings.VagalumeKey = SafeKey(raw[(separator + 1)..].Trim());
            }
        }
        catch (Exception ex) { AppLog.Error("Não foi possível ler as fontes de letra", ex); }
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllLines(FilePath, new[] { "vagalume=" + SafeKey(VagalumeKey) });
        }
        catch (Exception ex) { AppLog.Error("Não foi possível salvar as fontes de letra", ex); }
    }
}
