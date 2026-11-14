using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Lyravoxen.Integration;
using Microsoft.Web.WebView2.Core;
using Lyravoxen.Models;
using Forms = System.Windows.Forms;

namespace Lyravoxen;

public partial class MainWindow : Window
{
    private ProjectionWindow? _projection;
    private readonly List<LyricCue> _cues = new();
    private int _lastAutomaticCue = -1;
    private bool _webMessageHandlerAttached;
    private readonly HolyricsClient _holyrics = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            try
            {
                await YoutubePlayer.EnsureCoreWebView2Async();
                YoutubePlayer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                YoutubePlayer.CoreWebView2.Settings.IsStatusBarEnabled = false;

                if (!_webMessageHandlerAttached)
                {
                    YoutubePlayer.CoreWebView2.WebMessageReceived += Player_WebMessageReceived;
                    _webMessageHandlerAttached = true;
                }

                StatusText.Text = "Player pronto. Cole um link público do YouTube.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Não foi possível iniciar o WebView2: " + ex.Message;
            }
        };
    }

    private async void LoadYoutube_Click(object sender, RoutedEventArgs e)
    {
        var videoId = YouTubeLink.TryGetVideoId(YoutubeUrlBox.Text);
        if (videoId is null)
        {
            System.Windows.MessageBox.Show("Cole um link válido de vídeo do YouTube.", "Lyravoxen",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await YoutubePlayer.EnsureCoreWebView2Async();
            var html = YouTubePlayerPage.Build(videoId);
            YoutubePlayer.NavigateToString(html);
            _lastAutomaticCue = -1;
            StatusText.Text = "Vídeo carregado. Dê play e cadastre os tempos das frases.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Erro ao carregar o player: " + ex.Message;
        }
    }

    private void Player_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson);
            var root = message.RootElement;
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "time") return;
            if (!root.TryGetProperty("seconds", out var secondsElement)) return;

            var seconds = secondsElement.GetDouble();
            var state = root.TryGetProperty("state", out var stateElement) ? stateElement.GetInt32() : -1;
            if (state != 1 || _cues.Count == 0) return;

            var cueIndex = _cues.FindLastIndex(cue => cue.Seconds <= seconds);
            if (cueIndex == _lastAutomaticCue) return;

            _lastAutomaticCue = cueIndex;
            if (_projection is { IsVisible: true })
                _projection.ShowLyric(cueIndex >= 0 ? _cues[cueIndex].Text : "");
            if (cueIndex >= 0)
                _ = SendLyricToHolyricsAsync(_cues[cueIndex].Text);
        }
        catch (JsonException)
        {
            // Ignora mensagens incompletas ou inesperadas do player incorporado.
        }
        catch (InvalidOperationException)
        {
            // O player pode enviar uma mensagem durante a troca de vídeo.
        }
    }

    private async Task SendLyricToHolyricsAsync(string text)\n    {\n        try { await _holyrics.ShowTextAsync(text); }\n        catch (Exception ex) { Dispatcher.Invoke(() => StatusText.Text = "Falha ao enviar ao Holyrics: " + ex.Message); }\n    }\n\n    private async void HolyricsSettings_Click(object sender, RoutedEventArgs e)\n    {\n        var dialog = new HolyricsSettingsWindow(_holyrics) { Owner = this };\n        dialog.ShowDialog();\n        if (dialog.Saved) StatusText.Text = "Configuração do Holyrics salva. Use Testar conexão para validar o acesso.";\n    }\n\n    private void OpenProjection_Click(object sender, RoutedEventArgs e)
    {
        if (_projection is null || !_projection.IsVisible)
        {
            _projection = new ProjectionWindow();
            _projection.ShowOnOutputDisplay();
        }
        _projection.Activate();
    }

    private void AddCue_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(SecondsBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || seconds < 0 || string.IsNullOrWhiteSpace(LyricTextBox.Text))
        {
            System.Windows.MessageBox.Show("Informe o tempo em segundos (ex.: 12.5) e o texto da frase.",
                "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _cues.Add(new LyricCue { Seconds = seconds, Text = LyricTextBox.Text.Trim() });
        _cues.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        CueList.ItemsSource = null;
        CueList.ItemsSource = _cues;
        LyricTextBox.Clear();
        _lastAutomaticCue = -1;
        StatusText.Text = $"Frase adicionada em {seconds:0.0}s. Total: {_cues.Count}.";
    }

    private void CueList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CueList.SelectedItem is LyricCue cue)\n        {\n            if (_projection is { IsVisible: true }) _projection.ShowLyric(cue.Text);\n            _ = SendLyricToHolyricsAsync(cue.Text);\n        }
    }
}
