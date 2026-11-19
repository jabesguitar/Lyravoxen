using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Lyravoxen.Services;

/// <summary>Registro em arquivo (14 dias) e relatório de uso/erros para colar na conversa.</summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "logs");

    private static string TodayFile => Path.Combine(Folder, $"lyravoxen-{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message, Exception? ex = null) => Write("ERRO", ex is null ? message : message + " | " + ex);

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                File.AppendAllText(TodayFile, $"{DateTime.Now:HH:mm:ss} [{level}] {message}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch { /* o registro nunca pode derrubar o programa */ }
    }

    public static void Purge()
    {
        try
        {
            if (!Directory.Exists(Folder)) return;
            foreach (var file in Directory.EnumerateFiles(Folder, "lyravoxen-*.log"))
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-14)) File.Delete(file);
        }
        catch { }
    }

    public static string BuildReport()
    {
        var text = new StringBuilder();
        text.AppendLine("=== Relatório Lyravoxen ===");
        text.AppendLine("Gerado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        try { text.AppendLine("Versão compilada em: " + File.GetLastWriteTime(Environment.ProcessPath ?? "").ToString("dd/MM/yyyy HH:mm")); }
        catch { }
        text.AppendLine("Windows: " + Environment.OSVersion.VersionString + " | 64 bits: " + Environment.Is64BitProcess +
                        " | núcleos: " + Environment.ProcessorCount + " | AVX2: " + (Avx2.IsSupported ? "sim" : "não"));
        text.AppendLine("Log: " + Folder);
        text.AppendLine();
        try
        {
            lock (Gate)
            {
                var files = Directory.Exists(Folder)
                    ? Directory.EnumerateFiles(Folder, "lyravoxen-*.log").OrderByDescending(f => f).Take(2).Reverse().ToList()
                    : new List<string>();
                var lines = files.SelectMany(f => File.ReadAllLines(f, Encoding.UTF8)).ToList();
                foreach (var line in lines.Skip(Math.Max(0, lines.Count - 300))) text.AppendLine(line);
            }
        }
        catch (Exception ex) { text.AppendLine("(não foi possível ler o log: " + ex.Message + ")"); }
        return text.ToString();
    }
}
