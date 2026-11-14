using System.Text;
using System.IO;
using System.Windows.Threading;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    private readonly DispatcherTimer _audioTimer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(30) };
    private bool _isLocalAudioPlaying;
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
        _audioTimer.Tick += (_, _) => ProcessAudioCue();
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
            StatusText.Text = "Preparando vídeo e procurando letra já cadastrada...";
            _audioTimer.Stop(); _isLocalAudioPlaying = false; LocalAudioPlayer.Stop(); LocalAudioPlayer.Visibility = Visibility.Collapsed; YoutubePlayer.Visibility = Visibility.Visible;

            // Pré-carrega as frases salvas para este vídeo antes de inicializar o player.
            var preparedVideo = _library.SelectMany(folder => folder.Videos)
                .FirstOrDefault(item => !item.IsOffline && item.VideoId == videoId);
            _currentVideo = preparedVideo ?? new LibraryVideo
            {
                VideoId = videoId,
                Url = url,
                Title = _history.FirstOrDefault(v => v.VideoId == videoId)?.Title ?? "Vídeo do YouTube"
            };
            _currentVideo.Url = url;
            _cues.Clear();
            _cues.AddRange(_currentVideo.Cues.OrderBy(cue => cue.Seconds)
                .Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            StatusText.Text = _cues.Count > 0
                ? $"Letra preparada: {_cues.Count} frases carregadas. Inicializando o vídeo..."
                : "Vídeo identificado. Nenhuma letra salva para ele; você pode cadastrar as frases enquanto o player carrega.";

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
            YoutubeUrlBox.Text = url;
            if (addToHistory) await AddVideoToHistoryAsync(videoId, url);
            if (_cues.Count == 0 && !string.IsNullOrWhiteSpace(_currentVideo.Title) && _currentVideo.Title != "Vídeo do YouTube")
                await TryLoadAutomaticLyricsAsync(_currentVideo.Title);
            StatusText.Text = _cues.Count > 0
                ? $"Player preparado com {_cues.Count} frases. A letra acompanhará a reprodução."
                : "Player preparado. Cadastre a letra e os tempos para sincronização automática.";
        }
        catch (Exception ex) { StatusText.Text = "Erro ao carregar o player: " + ex.Message; }
    }

    private static string NormalizeMusicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var chars = decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
        return Regex.Replace(new string(chars).ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
    }

    private static bool ContainsMusicPhrase(string normalizedText, string normalizedPhrase)
    {
        if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(normalizedPhrase)) return false;
        return (" " + normalizedText + " ").Contains(" " + normalizedPhrase + " ", StringComparison.Ordinal);
    }

    private async Task TryLoadAutomaticLyricsAsync(string videoTitle)
    {
        try
        {
            StatusText.Text = "Buscando letra; nenhuma será projetada sem conferência.";
            var endpoint = "https://lrclib.net/api/search?q=" + Uri.EscapeDataString(videoTitle);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.UserAgent.ParseAdd("Lyravoxen/1.0 (https://github.com/jabesguitar/Lyravoxen)");
            using var response = await MetadataHttp.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                StatusText.Text = "Não foi possível consultar o catálogo de letras (HTTP " + (int)response.StatusCode + ").";
                return;
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.ValueKind != JsonValueKind.Array) return;
            var normalizedVideo = NormalizeMusicText(videoTitle);
            foreach (var item in json.RootElement.EnumerateArray())
            {
                var track = item.TryGetProperty("trackName", out var trackProp) ? trackProp.GetString() : null;
                var artist = item.TryGetProperty("artistName", out var artistProp) ? artistProp.GetString() : null;
                var normalizedTrack = NormalizeMusicText(track);
                var normalizedArtist = NormalizeMusicText(artist);

                // Correspondência por palavras inteiras para título E artista. Sem ambos, não aceitar.
                if (!ContainsMusicPhrase(normalizedVideo, normalizedTrack) ||
                    !ContainsMusicPhrase(normalizedVideo, normalizedArtist))
                    continue;
                if (!item.TryGetProperty("syncedLyrics", out var synced) || synced.ValueKind != JsonValueKind.String) continue;
                var lrc = synced.GetString();
                if (string.IsNullOrWhiteSpace(lrc)) continue;
                var imported = ParseTimedLyrics(lrc, ".lrc").OrderBy(cue => cue.Seconds).ToList();
                if (imported.Count < 2) continue;

                var preview = string.Join(Environment.NewLine, imported.Take(5).Select(cue => cue.Text));
                var confirmation = System.Windows.MessageBox.Show(
                    "O catálogo encontrou esta possível correspondência:\n\n" +
                    "Vídeo: " + videoTitle + "\nFaixa: " + track + "\nArtista: " + artist +
                    "\n\nPrévia da letra:\n" + preview +
                    "\n\nA letra corresponde exatamente à música que você carregou? Só carregaremos após sua confirmação.",
                    "Conferir letra antes de projetar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes) continue;

                _cues.Clear();
                _cues.AddRange(imported);
                CueList.ItemsSource = null;
                CueList.ItemsSource = _cues;
                _lastAutomaticCue = -1;
                if (_currentVideo is not null)
                {
                    _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
                    foreach (var saved in _library.SelectMany(folder => folder.Videos).Where(v => v.VideoId == _currentVideo.VideoId && v.IsOffline == _currentVideo.IsOffline))
                        saved.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
                    SaveLibrary();
                }
                StatusText.Text = "Letra confirmada por você: " + artist + " — " + track + ". Confira os tempos antes da apresentação.";
                return;
            }
            StatusText.Text = "Nenhuma letra foi carregada: não houve correspondência segura de título e artista confirmada.";
        }
        catch (TaskCanceledException) { StatusText.Text = "A busca de letras demorou demais. Tente novamente mais tarde."; }
        catch (HttpRequestException ex) { StatusText.Text = "Falha na busca de letras: " + ex.Message; }
        catch (JsonException) { StatusText.Text = "O catálogo de letras retornou uma resposta inválida."; }
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

    private void LineCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LineCountBox?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var lines)) _projection?.SetMaxLines(lines);
    }

    private void OpenProjection_Click(object sender, RoutedEventArgs e)
    {
        if (_projection is null || !_projection.IsVisible) { _projection = new ProjectionWindow(); _projection.ShowOnOutputDisplay(); }
        if (LineCountBox?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var lines)) _projection.SetMaxLines(lines);
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
        if (_currentVideo is not null)
        {
            _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
            foreach (var saved in _library.SelectMany(folder => folder.Videos)
                         .Where(item => item.VideoId == _currentVideo.VideoId && item.IsOffline == _currentVideo.IsOffline))
                saved.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
            SaveLibrary();
        }
        StatusText.Text = $"Frase adicionada em {seconds:0.0}s. Total: {_cues.Count}.";
    }

    private void ImportLyrics_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null)
        {
            System.Windows.MessageBox.Show("Carregue primeiro um vídeo do YouTube ou selecione uma música offline.", "Importar letra", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Selecione uma letra com marcações de tempo",
            Filter = "Letras sincronizadas (*.lrc;*.srt)|*.lrc;*.srt",
            Multiselect = false
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        try
        {
            var content = File.ReadAllText(dialog.FileName);
            var imported = ParseTimedLyrics(content, Path.GetExtension(dialog.FileName));
            if (imported.Count == 0)
                throw new InvalidDataException("Não encontrei marcações de tempo válidas. Use um arquivo LRC ou SRT.");

            _cues.Clear();
            _cues.AddRange(imported.OrderBy(cue => cue.Seconds));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();

            foreach (var saved in _library.SelectMany(folder => folder.Videos)
                         .Where(item => item.VideoId == _currentVideo.VideoId && item.IsOffline == _currentVideo.IsOffline))
                saved.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();

            SaveLibrary();
            StatusText.Text = $"Letra importada e preparada: {_cues.Count} frases sincronizadas.";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Não foi possível importar a letra: " + ex.Message, "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static List<LyricCue> ParseTimedLyrics(string content, string extension)
    {
        var result = new List<LyricCue>();
        if (extension.Equals(".lrc", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var line in content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var matches = Regex.Matches(line, @"\[(?<m>\d{1,2}):(?<s>\d{2})(?:[.:](?<f>\d{1,3}))?\]");
                if (matches.Count == 0) continue;
                var lyric = Regex.Replace(line, @"\[[^\]]+\]", "").Trim();
                if (string.IsNullOrWhiteSpace(lyric)) continue;
                foreach (Match match in matches)
                {
                    var minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
                    var seconds = int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture);
                    var fraction = match.Groups["f"].Success ? match.Groups["f"].Value.PadRight(3, '0') : "0";
                    var milliseconds = int.Parse(fraction, CultureInfo.InvariantCulture);
                    result.Add(new LyricCue { Seconds = minutes * 60 + seconds + milliseconds / 1000d, Text = lyric });
                }
            }
        }
        else if (extension.Equals(".srt", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var block in Regex.Split(content, @"\r?\n\s*\r?\n"))
            {
                var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var timeLine = lines.FirstOrDefault(line => line.Contains("-->", StringComparison.Ordinal));
                if (timeLine is null) continue;
                var match = Regex.Match(timeLine, @"(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2})[,.](?<f>\d{3})");
                if (!match.Success) continue;
                var textStart = Array.IndexOf(lines, timeLine) + 1;
                var lyric = string.Join(" ", lines.Skip(textStart)).Trim();
                if (string.IsNullOrWhiteSpace(lyric)) continue;
                var seconds = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) * 3600
                    + int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture) * 60
                    + int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture)
                    + int.Parse(match.Groups["f"].Value, CultureInfo.InvariantCulture) / 1000d;
                result.Add(new LyricCue { Seconds = seconds, Text = lyric });
            }
        }
        return result;
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
            folder.Videos.Add(new LibraryVideo
            {
                VideoId = _currentVideo.VideoId, Title = _currentVideo.Title, Url = _currentVideo.Url,
                Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList()
            });
        else
        {
            video.Title = _currentVideo.Title;
            video.Url = _currentVideo.Url;
            video.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
        }
        SaveLibrary();
        RefreshLibrarySongs();
        StatusText.Text = $"Vídeo adicionado à pasta '{folder.Name}'.";
    }

    private async void LibrarySong_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_librarySelectionChanging || LibrarySongList.SelectedItem is not LibraryVideo video) return;
        LibrarySongList.SelectedItem = null;
        if (video.IsOffline && !string.IsNullOrWhiteSpace(video.LocalPath)) { PlayLocalAudio(video); return; }
        await LoadVideoAsync(video.Url, true);
    }


    private void ImportAudio_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryFolderList.SelectedItem is not LibraryFolder folder)
        {
            System.Windows.MessageBox.Show("Crie ou selecione uma pasta primeiro.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Selecione um áudio que você tem direito de usar",
            Filter = "Áudios MP3 ou WAV (*.mp3;*.wav)|*.mp3;*.wav",
            Multiselect = true
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        var mediaFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Media");
        Directory.CreateDirectory(mediaFolder);
        foreach (var source in dialog.FileNames)
        {
            var ext = Path.GetExtension(source).ToLowerInvariant();
            if (ext is not ".mp3" and not ".wav") continue;
            var target = Path.Combine(mediaFolder, Guid.NewGuid().ToString("N") + ext);
            File.Copy(source, target, false);
            var title = Path.GetFileNameWithoutExtension(source);
            folder.Videos.RemoveAll(v => v.IsOffline && string.Equals(v.Title, title, StringComparison.OrdinalIgnoreCase));
            folder.Videos.Add(new LibraryVideo { Title = title, IsOffline = true, LocalPath = target, Format = ext.TrimStart('.') });
        }
        SaveLibrary();
        RefreshLibrarySongs();
        StatusText.Text = "Áudio importado. Disponível sem internet.";
    }

    private void PlayLocalAudio(LibraryVideo video)
    {
        try
        {
            if (!File.Exists(video.LocalPath)) throw new FileNotFoundException("O arquivo de áudio não foi encontrado.", video.LocalPath);
            _audioTimer.Stop();
            YoutubePlayer.Visibility = Visibility.Collapsed;
            LocalAudioPlayer.Visibility = Visibility.Visible;
            LocalAudioPlayer.Source = new Uri(video.LocalPath);
            LocalAudioPlayer.Position = TimeSpan.Zero;
            LocalAudioPlayer.Play();
            _isLocalAudioPlaying = true;
            _lastAutomaticCue = -1;
            _audioTimer.Start();
            _currentVideo = video;
            _cues.Clear(); _cues.AddRange(video.Cues); CueList.ItemsSource = null; CueList.ItemsSource = _cues;
            StatusText.Text = $"Reproduzindo offline: {video.Title} ({video.Format?.ToUpperInvariant()})";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível reproduzir o áudio: " + ex.Message; }
    }

    private void ProcessAudioCue()
    {
        if (!_isLocalAudioPlaying || _cues.Count == 0) return;
        var seconds = LocalAudioPlayer.Position.TotalSeconds;
        var cueIndex = _cues.FindLastIndex(cue => cue.Seconds <= seconds);
        if (cueIndex == _lastAutomaticCue) return;
        _lastAutomaticCue = cueIndex;
        var lyric = cueIndex >= 0 ? _cues[cueIndex].Text : "";
        if (_projection is { IsVisible: true }) _projection.ShowLyric(lyric);
        if (!string.IsNullOrWhiteSpace(lyric)) _ = SendLyricToHolyricsAsync(lyric);
    }

    private void LocalAudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        _audioTimer.Stop();
        _isLocalAudioPlaying = false;
        StatusText.Text = "Áudio offline finalizado.";
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