using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Lyravoxen.Services;

public static class LiveRecognitionSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Lyravoxen", "live-recognition.dat");

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    public static string? GetApiKey()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var encrypted = File.ReadAllBytes(SettingsPath);
            var clear = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear);
        }
        catch { return null; }
    }

    public static void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Informe uma chave de API válida.");
        var folder = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(folder);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(SettingsPath, encrypted);
    }
}
