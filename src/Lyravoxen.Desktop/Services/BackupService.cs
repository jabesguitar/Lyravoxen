using System.IO;
using System.IO.Compression;

namespace Lyravoxen.Services;

/// <summary>
/// Backup da biblioteca (letras, tempos, mídia, preferências) em uma pasta escolhida pelo usuário.
/// Se a pasta for do OneDrive ou do Google Drive, a nuvem sincroniza sozinha.
/// </summary>
internal static class BackupService
{
    public const string BackupFileName = "Lyravoxen-backup.zip";

    private static readonly object Gate = new();
    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen");
    private static readonly string SettingsFile = Path.Combine(Root, "backup-folder.txt");

    private static readonly string[] Files = { "library.json", "video-history.json", "preferences.json", "pedidos.json", "pedidos-whatsapp.txt", "fontes-letra.txt" };
    private static readonly string[] Folders = { "Media", "ProjectionBackgrounds" };

    public static string? GetFolder()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return null;
            var value = File.ReadAllText(SettingsFile).Trim();
            return value.Length == 0 ? null : value;
        }
        catch { return null; }
    }

    public static void SetFolder(string folder)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(SettingsFile, folder);
    }

    /// <summary>Pasta sugerida: a do OneDrive, se existir.</summary>
    public static string? SuggestedOneDriveFolder()
    {
        var oneDrive = Environment.GetEnvironmentVariable("OneDriveConsumer")
                       ?? Environment.GetEnvironmentVariable("OneDrive");
        return string.IsNullOrWhiteSpace(oneDrive) || !Directory.Exists(oneDrive)
            ? null
            : Path.Combine(oneDrive, "Lyravoxen");
    }

    public static string BackupPath(string folder) => Path.Combine(folder, BackupFileName);

    private static IEnumerable<string> EnumerateItems()
    {
        foreach (var name in Files)
        {
            var path = Path.Combine(Root, name);
            if (File.Exists(path)) yield return path;
        }
        foreach (var name in Folders)
        {
            var directory = Path.Combine(Root, name);
            if (!Directory.Exists(directory)) continue;
            foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)) yield return path;
        }
    }

    /// <summary>Verdadeiro se algo mudou desde o último backup.</summary>
    public static bool NeedsBackup(string folder)
    {
        try
        {
            var target = BackupPath(folder);
            if (!File.Exists(target)) return EnumerateItems().Any();
            var last = File.GetLastWriteTimeUtc(target);
            return EnumerateItems().Any(path => File.GetLastWriteTimeUtc(path) > last);
        }
        catch { return false; }
    }

    public static string CreateBackup(string folder)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(folder);
            var temporary = Path.Combine(folder, "Lyravoxen-backup.tmp");
            var target = BackupPath(folder);
            if (File.Exists(temporary)) File.Delete(temporary);

            using (var zip = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                foreach (var path in EnumerateItems())
                {
                    var entryName = Path.GetRelativePath(Root, path).Replace('\\', '/');
                    var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                    entry.LastWriteTime = File.GetLastWriteTime(path);
                    using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var output = entry.Open();
                    input.CopyTo(output);
                }
            }

            File.Move(temporary, target, overwrite: true);
            AppLog.Info("Backup criado: " + target);
            return target;
        }
    }

    /// <summary>Restaura o backup. O que já existia vira ".antes-da-restauracao". Devolve (restaurados, ignorados).</summary>
    public static (int Restored, int Skipped) Restore(string zipPath)
    {
        lock (Gate)
        {
            var restored = 0;
            var skipped = 0;
            var rootFull = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith('/') || entry.Length == 0 && entry.Name.Length == 0) continue;
                var target = Path.GetFullPath(Path.Combine(Root, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                if (!target.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) { skipped++; continue; }
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    if (File.Exists(target) && Files.Contains(Path.GetFileName(target), StringComparer.OrdinalIgnoreCase))
                        File.Copy(target, target + ".antes-da-restauracao", overwrite: true);
                    entry.ExtractToFile(target, overwrite: true);
                    restored++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    AppLog.Error("Restauração: não foi possível gravar " + entry.FullName, ex);
                }
            }
            AppLog.Info($"Backup restaurado de {zipPath}: {restored} arquivo(s), {skipped} ignorado(s)");
            return (restored, skipped);
        }
    }
}
