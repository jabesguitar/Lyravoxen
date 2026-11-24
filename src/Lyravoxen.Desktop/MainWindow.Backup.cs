using System.IO;
using System.Windows;
using System.Windows.Threading;
using Lyravoxen.Services;

namespace Lyravoxen;

// Botão ☁ Backup: cópia automática da biblioteca numa pasta (ex.: OneDrive ou Google Drive).
public partial class MainWindow
{
    private DispatcherTimer? _backupTimer;

    private void InitializeBackup()
    {
        _backupTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _backupTimer.Tick += (_, _) => RunAutoBackup();
        _backupTimer.Start();
        Loaded += (_, _) => RunAutoBackup();
        Closed += (_, _) =>
        {
            var folder = BackupService.GetFolder();
            if (folder is null) return;
            try { if (BackupService.NeedsBackup(folder)) BackupService.CreateBackup(folder); }
            catch (Exception ex) { AppLog.Error("Backup ao fechar falhou", ex); }
        };
    }

    private void RunAutoBackup()
    {
        var folder = BackupService.GetFolder();
        if (folder is null) return;
        _ = Task.Run(() =>
        {
            try
            {
                if (BackupService.NeedsBackup(folder)) BackupService.CreateBackup(folder);
            }
            catch (Exception ex) { AppLog.Error("Backup automático falhou", ex); }
        });
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        var folder = BackupService.GetFolder();
        var menu = new System.Windows.Controls.ContextMenu();

        void Add(string header, RoutedEventHandler handler, bool enabled = true)
        {
            var item = new System.Windows.Controls.MenuItem { Header = header, IsEnabled = enabled };
            item.Click += handler;
            menu.Items.Add(item);
        }

        menu.Items.Add(new System.Windows.Controls.MenuItem
        {
            Header = folder is null ? "Pasta de backup: (não escolhida)" : "Pasta de backup: " + folder,
            IsEnabled = false
        });
        Add("Escolher pasta de backup (OneDrive/Drive)…", (_, _) => ChooseBackupFolder());
        Add("Fazer backup agora", (_, _) => BackupNow(), folder is not null);
        Add("Restaurar de um backup…", (_, _) => RestoreBackup());
        Add("Abrir a pasta do backup", (_, _) =>
        {
            if (folder is not null && Directory.Exists(folder))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
        }, folder is not null);

        menu.PlacementTarget = (UIElement)sender;
        menu.IsOpen = true;
    }

    private bool ChooseBackupFolder()
    {
        string? chosen = null;
        var suggestion = BackupService.SuggestedOneDriveFolder();
        if (suggestion is not null)
        {
            var answer = System.Windows.MessageBox.Show(this,
                "Guardar o backup no seu OneDrive?\n\n" + suggestion +
                "\n\nSim = usar essa pasta (a nuvem sincroniza sozinha).\nNão = escolher outra pasta (Google Drive, pendrive…).",
                "Lyravoxen — Backup", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return false;
            if (answer == MessageBoxResult.Yes) chosen = suggestion;
        }

        if (chosen is null)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Escolha a pasta do backup (de preferência dentro do OneDrive ou do Google Drive)",
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return false;
            chosen = dialog.SelectedPath;
        }

        try
        {
            Directory.CreateDirectory(chosen);
            BackupService.SetFolder(chosen);
            BackupNow();
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível usar essa pasta: " + ex.Message;
            return false;
        }
    }

    private void BackupNow()
    {
        var folder = BackupService.GetFolder();
        if (folder is null) { ChooseBackupFolder(); return; }
        try
        {
            var path = BackupService.CreateBackup(folder);
            StatusText.Text = "Backup salvo em " + path;
        }
        catch (Exception ex)
        {
            AppLog.Error("Backup falhou", ex);
            StatusText.Text = "Backup falhou: " + ex.Message;
        }
    }

    private void RestoreBackup()
    {
        var folder = BackupService.GetFolder();
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Escolha o backup do Lyravoxen",
            Filter = "Backup do Lyravoxen (*.zip)|*.zip",
            InitialDirectory = folder is not null && Directory.Exists(folder) ? folder : null
        };
        if (dialog.ShowDialog(this) != true) return;

        if (System.Windows.MessageBox.Show(this,
                "Restaurar este backup substitui a biblioteca atual (uma cópia da atual fica ao lado, com final .antes-da-restauracao).\n\nContinuar?",
                "Lyravoxen — Restaurar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            var (restored, skipped) = BackupService.Restore(dialog.FileName);
            System.Windows.MessageBox.Show(this,
                $"Restauração concluída: {restored} arquivo(s)" + (skipped > 0 ? $", {skipped} ignorado(s)" : "") +
                ".\n\nFeche e abra o Lyravoxen para carregar a biblioteca restaurada.",
                "Lyravoxen — Restaurar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error("Restauração falhou", ex);
            StatusText.Text = "Restauração falhou: " + ex.Message;
        }
    }
}
