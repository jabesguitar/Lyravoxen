using System.Text.Json;
using Lyravoxen.Models;

namespace Lyravoxen;

public partial class MainWindow
{
    private PermanentLyricsArchive? _permanentLyrics;

    private void LoadOfflineStorageSettings()
    {
        try
        {
            if (File.Exists(OfflineStorageSettingsFile))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(OfflineStorageSettingsFile));
                var path = doc.RootElement.TryGetProperty("rootFolder", out var value) ? value.GetString() : null;
                if (!string.IsNullOrWhiteSpace(path)) _offlineRootFolder = Path.GetFullPath(path);
            }
        }
        catch (Exception ex) { AppLog.Error("Falha ao ler pasta da Biblioteca Offline; usando o local padrão.", ex); }
        _permanentLyrics = new PermanentLyricsArchive(OfflineLyricsArchiveFile);
    }

    private void EnsureOfflineStorageFolders()
    {
        Directory.CreateDirectory(OfflineMediaFolder);
        Directory.CreateDirectory(OfflineLyricsFolder);
        Directory.CreateDirectory(OfflineDataFolder);
        Directory.CreateDirectory(OfflineTempFolder);
        if (IsInitialized && OfflineStoragePathText is not null)
            OfflineStoragePathText.Text = _offlineRootFolder;
    }

    private void ArchiveCurrentLyrics()
    {
        if (_currentVideo is null || _permanentLyrics is null) return;
        try { _permanentLyrics.Save(_currentVideo); }
        catch (Exception ex) { AppLog.Error("Não foi possível arquivar a letra permanentemente.", ex); }
    }

    private void ChooseOfflineStorageFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Escolha a pasta principal onde o Lyravoxen guardará músicas, letras e dados.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(_offlineRootFolder) ? _offlineRootFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        var selected = Path.GetFullPath(dialog.SelectedPath);
        try
        {
            Directory.CreateDirectory(selected);
            var settingsDirectory = Path.GetDirectoryName(OfflineStorageSettingsFile)!;
            Directory.CreateDirectory(settingsDirectory);
            var temp = OfflineStorageSettingsFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new { rootFolder = selected }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, OfflineStorageSettingsFile, true);
            var previous = _offlineRootFolder;
            _offlineRootFolder = selected;
            EnsureOfflineStorageFolders();
            StatusText.Text = "Pasta da Biblioteca Offline definida. Novos arquivos serão guardados em: " + selected;
            if (!string.Equals(previous, selected, StringComparison.OrdinalIgnoreCase))
                System.Windows.MessageBox.Show(this, "A nova pasta foi configurada. Os arquivos que já estavam na pasta anterior não foram movidos nem apagados; os novos downloads e importações usarão o novo local.", "Biblioteca Offline", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Não foi possível configurar a pasta: " + ex.Message, "Biblioteca Offline", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}