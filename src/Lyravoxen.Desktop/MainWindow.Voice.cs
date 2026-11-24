using System.IO;
using System.Windows;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

// Central Bíblica por voz: apertar para falar, escolher o dispositivo, medidor de nível
// e confirmação "Entendi: João 3:16" antes de projetar.
public partial class MainWindow
{
    private static readonly string MicSettingPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "microfone.txt");

    private MicrophoneReferenceService? _micService;
    private bool _micRecording;
    private bool _micBusy;
    private bool _micDevicesLoading;

    private const string MicIdleText = "🎙 Segure para falar (F4)";

    private void BibleMicDeviceBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (BibleMicDeviceBox.Items.Count == 0) RefreshMicrophoneDevices();
    }

    private void RefreshMicDevices_Click(object sender, RoutedEventArgs e) => RefreshMicrophoneDevices();

    private void RefreshMicrophoneDevices()
    {
        _micDevicesLoading = true;
        try
        {
            string? savedName = null;
            try { if (File.Exists(MicSettingPath)) savedName = File.ReadAllText(MicSettingPath).Trim(); }
            catch { }
            if (string.IsNullOrWhiteSpace(savedName) && BibleMicDeviceBox.SelectedItem is MicrophoneDevice current)
                savedName = current.Name;

            var devices = MicrophoneReferenceService.ListDevices();
            BibleMicDeviceBox.ItemsSource = devices;
            BibleMicDeviceBox.SelectedItem = devices.FirstOrDefault(d => d.Name == savedName) ?? devices[0];
            SetBibleStatus(devices.Count > 1
                ? "Microfones encontrados: " + (devices.Count - 1) + ". Escolha o dispositivo e segure o botão (ou F4) para falar."
                : "Nenhum microfone dedicado encontrado; será usado o padrão do Windows.");
        }
        catch (Exception ex)
        {
            SetBibleStatus("Não foi possível listar os microfones: " + ex.Message);
        }
        finally
        {
            _micDevicesLoading = false;
        }
    }

    private void BibleMicDeviceBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_micDevicesLoading || BibleMicDeviceBox.SelectedItem is not MicrophoneDevice device) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MicSettingPath)!);
            File.WriteAllText(MicSettingPath, device.Name);
        }
        catch { }
    }

    private MicrophoneReferenceService GetMicService()
    {
        if (_micService is not null) return _micService;
        var service = new MicrophoneReferenceService();
        service.LevelChanged += (_, level) => Dispatcher.BeginInvoke(new Action(() => BibleMicLevelBar.Value = level));
        service.StatusChanged += (_, message) => Dispatcher.BeginInvoke(new Action(() => SetBibleStatus(message)));
        service.MaxDurationReached += (_, _) => Dispatcher.BeginInvoke(new Action(async () => await EndVoiceCaptureAsync()));
        _micService = service;
        return service;
    }

    private void BibleMicButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => BeginVoiceCapture();

    private async void BibleMicButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => await EndVoiceCaptureAsync();

    private void BeginVoiceCapture()
    {
        if (_micRecording || _micBusy) return;
        try
        {
            var device = BibleMicDeviceBox.SelectedItem as MicrophoneDevice;
            GetMicService().StartRecording(device?.Number ?? -1);
        }
        catch (Exception ex)
        {
            SetBibleStatus(ex.Message);
            return;
        }

        _micRecording = true;
        HideVoiceConfirmation();
        BibleMicButton.Content = "● Gravando… solte para enviar";
        SetBibleStatus("Ouvindo… diga, por exemplo: “João 3:16” ou “Salmos 23”. Solte o botão (ou F4) ao terminar.");
        // Carrega o modelo em segundo plano enquanto o operador fala.
        _ = GetMicService().PrepareAsync().ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default);
    }

    private async Task EndVoiceCaptureAsync()
    {
        if (!_micRecording || _micService is null) return;
        _micRecording = false;
        _micBusy = true;
        BibleMicButton.IsEnabled = false;
        BibleMicButton.Content = "Reconhecendo…";
        try
        {
            var samples = _micService.StopRecording();
            if (samples.Length < 8000)
            {
                SetBibleStatus("Gravação muito curta. Segure o botão (ou F4) enquanto fala a referência inteira.");
                return;
            }

            SetBibleStatus("Reconhecendo a fala… (na primeira vez o modelo de voz é baixado)");
            var heard = await _micService.TranscribeAsync(samples);
            if (string.IsNullOrWhiteSpace(heard))
            {
                SetBibleStatus("Não ouvi nada. Aproxime-se do microfone, confira o medidor de nível e tente de novo.");
                return;
            }

            AppLog.Info("[voz] ouvi: " + heard);
            var command = VoiceBibleCommandInterpreter.TryInterpret(heard);
            if (command is not null && await ExecuteVoiceCommandAsync(command, heard)) return;

            var parsed = VoiceBibleReferenceParser.TryParse(heard);
            if (parsed is null)
            {
                SetBibleStatus("Ouvi “" + heard + "”, mas não identifiquei uma referência. Tente: “João três dezesseis”.");
                return;
            }

            BibleSearchBox.Text = parsed.ToString();
            await LocateBibleReferenceAsync(project: false);
            ShowVoiceConfirmation(parsed, heard);
        }
        catch (Exception ex)
        {
            SetBibleStatus("Falha no reconhecimento por voz: " + ex.Message);
        }
        finally
        {
            _micBusy = false;
            BibleMicButton.IsEnabled = true;
            BibleMicButton.Content = MicIdleText;
            BibleMicLevelBar.Value = 0;
        }
    }

    private void ShowVoiceConfirmation(BibleReference reference, string heard)
    {
        var canProject = reference.VerseStart.HasValue;
        BibleVoiceHeardText.Text = "Entendi: " + reference + "    (ouvi: “" + heard + "”)";
        BibleVoiceHintText.Text = canProject
            ? "Enter confirma e projeta · Esc descarta"
            : "Falta o versículo: escolha-o na lista ou fale de novo com o versículo.";
        BibleVoiceConfirmButton.IsEnabled = canProject;
        BibleVoiceConfirmPanel.Visibility = Visibility.Visible;
        SetBibleStatus("Confira a referência entendida antes de projetar.");
    }

    private void HideVoiceConfirmation() => BibleVoiceConfirmPanel.Visibility = Visibility.Collapsed;

    private async void BibleVoiceConfirm_Click(object sender, RoutedEventArgs e) => await ConfirmVoiceProjectionAsync();

    private void BibleVoiceDiscard_Click(object sender, RoutedEventArgs e)
    {
        HideVoiceConfirmation();
        SetBibleStatus("Referência por voz descartada.");
    }

    private async Task ConfirmVoiceProjectionAsync()
    {
        if (BibleVoiceConfirmPanel.Visibility != Visibility.Visible || !BibleVoiceConfirmButton.IsEnabled) return;
        HideVoiceConfirmation();
        await ProjectBibleSelectionAsync();
    }

    private void MainWindow_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.F4) return;
        e.Handled = true;
        _ = EndVoiceCaptureAsync();
    }

    /// <summary>Atalhos da voz (F4, Enter/Esc na confirmação) e da marcação por toque (Espaço/Backspace).</summary>
    private bool HandleVoiceAndTapKeys(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F1)
        {
            e.Handled = true;
            ShowHelp(WorkspaceTabs.SelectedItem);
            return true;
        }

        if (HandleBibleNavigationKeys(e)) return true;

        var typing = e.OriginalSource is System.Windows.Controls.TextBox or
            System.Windows.Controls.Primitives.TextBoxBase or
            System.Windows.Controls.ComboBox;

        if (e.Key == System.Windows.Input.Key.F4 && WorkspaceTabs.SelectedItem == BibleWorkspaceTab)
        {
            e.Handled = true;
            if (!e.IsRepeat) BeginVoiceCapture();
            return true;
        }

        if (WorkspaceTabs.SelectedItem == BibleWorkspaceTab &&
            (e.Key == System.Windows.Input.Key.F2 || e.Key == System.Windows.Input.Key.F3))
        {
            e.Handled = true;
            if (!e.IsRepeat) _ = StepBibleVerseAsync(e.Key == System.Windows.Input.Key.F3 ? 1 : -1);
            return true;
        }

        if (!typing && BibleVoiceConfirmPanel.Visibility == Visibility.Visible &&
            WorkspaceTabs.SelectedItem == BibleWorkspaceTab)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                _ = ConfirmVoiceProjectionAsync();
                return true;
            }
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                HideVoiceConfirmation();
                SetBibleStatus("Referência por voz descartada.");
                return true;
            }
        }

        if (_tapActive && !typing && e.OriginalSource is not Microsoft.Web.WebView2.Wpf.WebView2)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                e.Handled = true;
                if (!e.IsRepeat) TapMarkStanza();
                return true;
            }
            if (e.Key == System.Windows.Input.Key.Back)
            {
                e.Handled = true;
                if (!e.IsRepeat) TapUndo();
                return true;
            }
        }

        return false;
    }

    /// <summary>Executa um comando falado. Devolve false se o comando não se aplica (cai na referência).</summary>
    private async Task<bool> ExecuteVoiceCommandAsync(VoiceBibleCommand command, string heard)
    {
        var confirmationOpen = BibleVoiceConfirmPanel.Visibility == Visibility.Visible && BibleVoiceConfirmButton.IsEnabled;
        switch (command.Kind)
        {
            case VoiceBibleCommandKind.Confirm:
                if (!confirmationOpen) return false;
                await ConfirmVoiceProjectionAsync();
                return true;
            case VoiceBibleCommandKind.Cancel:
                HideVoiceConfirmation();
                SetBibleStatus("Cancelado por voz.");
                return true;
            case VoiceBibleCommandKind.Next:
                await StepBibleVerseAsync(1);
                return true;
            case VoiceBibleCommandKind.Previous:
                await StepBibleVerseAsync(-1);
                return true;
            case VoiceBibleCommandKind.Project:
                HideVoiceConfirmation();
                await ProjectBibleSelectionAsync();
                return true;
            case VoiceBibleCommandKind.Clear:
                ClearBibleProjection_Click(this, new RoutedEventArgs());
                return true;
            case VoiceBibleCommandKind.Verse:
                if (_bibleVerses.Count == 0)
                {
                    SetBibleStatus("Abra um capítulo antes de pedir um versículo (ouvi: “" + heard + "”).");
                    return true;
                }
                SelectBibleVerses(command.VerseStart!.Value, command.VerseEnd);
                BibleVoiceHeardText.Text = "Entendi: versículo" + (command.VerseEnd.HasValue ? "s " + command.VerseStart + " a " + command.VerseEnd : " " + command.VerseStart)
                    + "    (ouvi: “" + heard + "”)";
                BibleVoiceHintText.Text = "Enter confirma e projeta · Esc descarta";
                BibleVoiceConfirmButton.IsEnabled = true;
                BibleVoiceConfirmPanel.Visibility = Visibility.Visible;
                return true;
        }
        return false;
    }
}
