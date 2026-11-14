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
    private readonly List<LibraryFolder> _library = new();
    private LibraryVideo? _currentVideo;
    private bool _librarySelectionChanging;
    private int _lastAutomaticCue = -1;
    private bool _webMessageHandlerAttached;
    private bool _virtualHostMapped;
    private bool _historySelectionChanging;
    private readonly HolyricsClient _holyrics = new();
    private readonly string _playerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Player");
    private readonly string _historyFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "video-history.json");
    private readonly string _libraryFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "library.json");
    private static readonly HttpClient MetadataHttp = new() { Timeout = TimeSpan.FromSeconds(8) };

    public MainWindow()
    {
        InitializeComponent();
        LoadHistory();
        LoadLibrary();
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
            _currentVideo = new LibraryVideo { VideoId = videoId, Url = url, Title = _history.FirstOrDefault(v => v.VideoId == videoId)?.Title ?? "Vídeo do YouTube" };
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

        if (_currentVideo?.VideoId == videoId) _currentVideo.Title = title;
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


    private void LoadLibrary()
    {
        try
        {
            if (File.Exists(_libraryFile))
                _library.AddRange(JsonSerializer.Deserialize<List<LibraryFolder>>(File.ReadAllText(_libraryFile)) ?? new());
        }
        catch { _library.Clear(); }
        RefreshFolders();
    }

    private void SaveLibrary()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_libraryFile)!);
            File.WriteAllText(_libraryFile, JsonSerializer.Serialize(_library, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível salvar a biblioteca: " + ex.Message; }
    }

    private void RefreshFolders(string? selectFolderId = null)
    {
        if (!IsInitialized) return;
        _librarySelectionChanging = true;
        LibraryFolderList.ItemsSource = null;
        LibraryFolderList.ItemsSource = _library;
        if (selectFolderId is not null)
            LibraryFolderList.SelectedItem = _library.FirstOrDefault(f => f.Id == selectFolderId);
        else if (LibraryFolderList.SelectedItem is null && _library.Count > 0)
            LibraryFolderList.SelectedIndex = 0;
        _librarySelectionChanging = false;
        RefreshLibrarySongs();
    }

    private void RefreshLibrarySongs()
    {
        if (!IsInitialized) return;
        var folder = LibraryFolderList.SelectedItem as LibraryFolder;
        var search = LibrarySearchBox?.Text?.Trim() ?? "";
        var songs = folder?.Videos ?? new List<LibraryVideo>();
        var filtered = string.IsNullOrWhiteSpace(search)
            ? songs
            : songs.Where(v => v.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        _librarySelectionChanging = true;
        LibrarySongList.ItemsSource = null;
        LibrarySongList.ItemsSource = filtered;
        _librarySelectionChanging = false;
    }

    private void LibraryFolder_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshLibrarySongs();
    private void LibrarySearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshLibrarySongs();
    private void LibrarySearch_Click(object sender, RoutedEventArgs e) => RefreshLibrarySongs();

    private void CreateFolder_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForText("Criar pasta", "Nome da nova pasta:");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_library.Any(f => string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            System.Windows.MessageBox.Show("Já existe uma pasta com esse nome.", "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var folder = new LibraryFolder { Name = name.Trim() };
        _library.Add(folder);
        SaveLibrary();
        RefreshFolders(folder.Id);
        StatusText.Text = "Pasta criada: " + folder.Name;
    }

    private void RenameFolder_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryFolderList.SelectedItem is not LibraryFolder folder) return;
        var name = PromptForText("Renomear pasta", "Novo nome:", folder.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        folder.Name = name.Trim();
        SaveLibrary();
        RefreshFolders(folder.Id);
    }

    private void DeleteFolder_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryFolderList.SelectedItem is not LibraryFolder folder) return;
        var result = System.Windows.MessageBox.Show(
            $"Excluir a pasta '{folder.Name}' e os atalhos de vídeos guardados nela? Os vídeos originais não serão apagados.",
            "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _library.Remove(folder);
        SaveLibrary();
        RefreshFolders();
    }

    private void AddCurrentVideoToFolder_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryFolderList.SelectedItem is not LibraryFolder folder)
        {
            System.Windows.MessageBox.Show("Crie ou selecione uma pasta primeiro.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_currentVideo is null)
        {
            System.Windows.MessageBox.Show("Carregue um vídeo do YouTube antes de adicioná-lo à pasta.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var video = folder.Videos.FirstOrDefault(v => v.VideoId == _currentVideo.VideoId);
        if (video is null)
            folder.Videos.Add(new LibraryVideo { VideoId = _currentVideo.VideoId, Title = _currentVideo.Title, Url = _currentVideo.Url });
        else
        {
            video.Title = _currentVideo.Title;
            video.Url = _currentVideo.Url;
        }
        SaveLibrary();
        RefreshLibrarySongs();
        StatusText.Text = $"Vídeo adicionado à pasta '{folder.Name}'.";
    }

    private async void LibrarySong_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_librarySelectionChanging || LibrarySongList.SelectedItem is not LibraryVideo video) return;
        LibrarySongList.SelectedItem = null;
        await LoadVideoAsync(video.Url, true);
    }

    private static string? PromptForText(string title, string label, string initialValue = "")
    {
        var dialog = new Window
        {
            Title = title, Width = 390, Height = 165, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize, Owner = System.Windows.Application.Current.MainWindow,
            Background = System.Windows.Media.Brushes.White
        };
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 8) });
        var input = new System.Windows.Controls.TextBox { Text = initialValue, Height = 28, Padding = new Thickness(5) };
        panel.Children.Add(input);
        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var ok = new System.Windows.Controls.Button { Content = "Salvar", Width = 80, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new System.Windows.Controls.Button { Content = "Cancelar", Width = 80, IsCancel = true };
        ok.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(ok); buttons.Children.Add(cancel); panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        return dialog.ShowDialog() == true ? input.Text : null;
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