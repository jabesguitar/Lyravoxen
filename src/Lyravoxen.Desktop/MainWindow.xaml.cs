using System.IO;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Lyravoxen.Integration;
using Microsoft.Web.WebView2.Core;
using Lyravoxen.Models;

namespace Lyravoxen;

public partial class MainWindow : Window
{
    private ProjectionWindow? _projection;
    private readonly List<LyricCue> _cues = new();
    private readonly List<VideoHistoryItem> _history = new();
    private int _lastAutomaticCue = -1;
    private bool _webMessageHandlerAttached;
    private bool _virtualHostMapped;
    private bool _historySelectionChanging;
    private readonly HolyricsClient _holyrics = new();
    private readonly string _playerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Player");
    private readonly string _historyFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "video-history.json");
    private static readonly HttpClient MetadataHttp = new() { Timeout = TimeSpan.FromSeconds(8) };

    public MainWindow()
    {
        InitializeComponent();
        LoadHistory();
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
            catch (Exception ex) { StatusText.Text = "Não foi possível iniciar o WebView2: " + ex.Message; }
        };
    }

    private async void LoadYoutube_Click(object sender, RoutedEventArgs e)
    {
        await LoadVideoAsync(YoutubeUrlBox.Text, true);
    }

    private async Task LoadVideoAsync(string url, bool addToHistory)
    {
        var videoId = YouTubeLink.TryGetVideoId(url);
        if (videoId is null)
        {
            System.Windows.MessageBox.Show("Cole um link válido de vídeo do YouTube.", "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            StatusText.Text = "Preparando player seguro do YouTube...";
            await YoutubePlayer.EnsureCoreWebView2Async();
            Directory.CreateDirectory(_playerFolder);
            if (!_virtualHostMapped)
            {
                YoutubePlayer.CoreWebView2.SetVirtualHostNameToFolderMapping("lyravoxen.local", _playerFolder, CoreWebView2HostResourceAccessKind.Allow);
                _virtualHostMapped = true;
            }
            var html = YouTubePlayerPage.Build(videoId);
            await File.WriteAllTextAsync(Path.Combine(_playerFolder, "index.html"), html);
            YoutubePlayer.CoreWebView2.Navigate("https://lyravoxen.local/index.html");
            _lastAutomaticCue = -1;
            YoutubeUrlBox.Text = url;
            StatusText.Text = "Vídeo carregado. Consultando título...";
            if (addToHistory) await AddVideoToHistoryAsync(videoId, url);
            StatusText.Text = "Vídeo carregado. Dê play e cadastre os tempos das frases.";
        }
        catch (Exception ex) { StatusText.Text = "Erro ao carregar o player: " + ex.Message; }
    }

    private async Task AddVideoToHistoryAsync(string videoId, string url)
    {
        string title = "Vídeo do YouTube";
        try
        {
            var endpoint = "https://www.youtube.com/oembed?url=" + Uri.EscapeDataString("https://www.youtube.com/watch?v=" + videoId) + "&format=json";
            using var response = await MetadataHttp.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("title", out var titleElement))
                    title = titleElement.GetString() ?? title;
            }
        }
        catch { title = "Vídeo do YouTube (" + videoId + ")"; }

        var existing = _history.FirstOrDefault(item => item.VideoId == videoId);
        if (existing is not null)
        {
            existing.Title = title;
            existing.Url = url;
            existing.LastUsed = DateTime.Now;
        }
        else _history.Insert(0, new VideoHistoryItem { VideoId = videoId, Title = title, Url = url, LastUsed = DateTime.Now });
        _history.Sort((a, b) => b.LastUsed.CompareTo(a.LastUsed));
        SaveHistory();
        RefreshHistory();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFile))
                _history.AddRange(JsonSerializer.Deserialize<List<VideoHistoryItem>>(File.ReadAllText(_historyFile)) ?? new());
        }
        catch { _history.Clear(); }
        RefreshHistory();
    }

    private void SaveHistory()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_historyFile)!);
            File.WriteAllText(_historyFile, JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível salvar o histórico: " + ex.Message; }
    }

    private void RefreshHistory()
    {
        if (!IsInitialized) return;
        var search = HistorySearchBox?.Text?.Trim() ?? "";
        var filtered = string.IsNullOrWhiteSpace(search) ? _history : _history.Where(item => item.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        _historySelectionChanging = true;
        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = filtered;
        _historySelectionChanging = false;
    }

    private void HistorySearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshHistory();
    private void HistorySearch_Click(object sender, RoutedEventArgs e) => RefreshHistory();

    private async void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_historySelectionChanging || HistoryList.SelectedItem is not VideoHistoryItem item) return;
        HistoryList.SelectedItem = null;
        await LoadVideoAsync(item.Url, false);
        item.LastUsed = DateTime.Now;
        _history.Sort((a, b) => b.LastUsed.CompareTo(a.LastUsed));
        SaveHistory();
        RefreshHistory();
    }

    private void Player_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson);
            var root = message.RootElement;
            if (root.TryGetProperty("type", out var type) && type.GetString() == "playerError")
            {
                var code = root.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
                Dispatcher.Invoke(() => StatusText.Text = "O YouTube informou erro " + code + ". Verifique se o vídeo permite reprodução incorporada.");
                return;
            }
            if (!root.TryGetProperty("type", out type) || type.GetString() != "time") return;
            if (!root.TryGetProperty("seconds", out var secondsElement)) return;
            var seconds = secondsElement.GetDouble();
            var state = root.TryGetProperty("state", out var stateElement) ? stateElement.GetInt32() : -1;
            if (state != 1 || _cues.Count == 0) return;
            var cueIndex = _cues.FindLastIndex(cue => cue.Seconds <= seconds);
            if (cueIndex == _lastAutomaticCue) return;
            _lastAutomaticCue = cueIndex;
            var lyric = cueIndex >= 0 ? _cues[cueIndex].Text : "";
            if (_projection is { IsVisible: true }) _projection.ShowLyric(lyric);
            if (!string.IsNullOrWhiteSpace(lyric)) _ = SendLyricToHolyricsAsync(lyric);
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }

    private async Task SendLyricToHolyricsAsync(string text)
    {
        try { await _holyrics.ShowTextAsync(text); }
        catch (Exception ex) { Dispatcher.Invoke(() => StatusText.Text = "Falha ao enviar ao Holyrics: " + ex.Message); }
    }

    private async void HolyricsSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HolyricsSettingsWindow(_holyrics) { Owner = this };
        dialog.ShowDialog();
        if (dialog.Saved) StatusText.Text = "Configuração do Holyrics salva. A conexão foi testada.";
    }

    private void OpenProjection_Click(object sender, RoutedEventArgs e)
    {
        if (_projection is null || !_projection.IsVisible) { _projection = new ProjectionWindow(); _projection.ShowOnOutputDisplay(); }
        _projection.Activate();
    }

    private void AddCue_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(SecondsBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds < 0 || string.IsNullOrWhiteSpace(LyricTextBox.Text))
        {
            System.Windows.MessageBox.Show("Informe o tempo em segundos (ex.: 12.5) e o texto da frase.", "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
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
        if (CueList.SelectedItem is not LyricCue cue) return;
        if (_projection is { IsVisible: true }) _projection.ShowLyric(cue.Text);
        _ = SendLyricToHolyricsAsync(cue.Text);
    }

    private sealed class VideoHistoryItem
    {
        public string VideoId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime LastUsed { get; set; }
        public override string ToString() => Title;
    }
}