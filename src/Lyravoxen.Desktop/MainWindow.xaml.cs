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
using Lyravoxen.Audio;

namespace Lyravoxen;

public partial class MainWindow : Window
{
    private ProjectionWindow? _projection;
    private string _currentProjectedLyric = "";
    private readonly List<LyricCue> _cues = new();
    private readonly List<VideoHistoryItem> _history = new();
    private readonly List<LibraryFolder> _library = new();
    private LibraryVideo? _currentVideo;
    private readonly DispatcherTimer _audioTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(80) };
    private bool _isLocalAudioPlaying;
    private bool _librarySelectionChanging;
    private int _lastAutomaticCue = -1;
    private bool _manualProjectionMode;
    private int _manualCueIndex = -1;
    private double _playerDurationSeconds;
    private double _lastKnownPlaybackSeconds;
    private const double ProjectionLeadSeconds = 1.5;
    private int _lastPlayerState = -1;
    private bool _playerReady;
    private bool _isLoadingVideo;
    private string _playerSessionId = "";
    private bool _webMessageHandlerAttached;
    private bool _searchWebViewReady;
    private int _browserTabNumber = 1;
    private string? _nextVideoUrl;
    private string? _pendingQueueVideoId;
    private bool _virtualHostMapped;
    private bool _historySelectionChanging;
    private readonly HolyricsClient _holyrics = new();
    private readonly SemaphoreSlim _holyricsSendLock = new(1, 1);
    private long _holyricsSendVersion;
    private bool _holyricsTakeoverActive;
    private long _lyricsSearchVersion;
    private bool _pauseOnPlayerReady;
    private readonly string _playerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Player");
    private readonly string _historyFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "video-history.json");
    private readonly string _libraryFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "library.json");
    private readonly string _preferencesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "preferences.json");
    private bool _loadingPreferences;
    private string? _selectedOutputDeviceName;
    private bool _displayChangeHandlerAttached;
    private ProcessAudioCapture? _youtubeAudioCapture;
    private LiveTranscriptionEngine? _transcriptionEngine;
    private Task? _transcriptionEngineStartTask;
    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private static readonly HttpClient MetadataHttp = new() { Timeout = TimeSpan.FromSeconds(8) };

    public MainWindow()
    {
        InitializeComponent();
        LoadHistory();
        LoadLibrary();
        _audioTimer.Tick += (_, _) => ProcessAudioCue();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += System_DisplaySettingsChanged;
        _displayChangeHandlerAttached = true;
        Closed += (_, _) =>
        {
            if (_displayChangeHandlerAttached)
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= System_DisplaySettingsChanged;
            _ = DisposeAudioServicesAsync();
        };
        Loaded += async (_, _) =>
        {
            try
            {
                Directory.CreateDirectory(_playerFolder);
                var playerWebViewData = Path.Combine(_playerFolder, "WebView2UserData");
                Directory.CreateDirectory(playerWebViewData);
                var playerEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: playerWebViewData);
                await YoutubePlayer.EnsureCoreWebView2Async(playerEnvironment);
                YoutubePlayer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                YoutubePlayer.CoreWebView2.Settings.IsStatusBarEnabled = false;
                if (!_webMessageHandlerAttached)
                {
                    YoutubePlayer.CoreWebView2.WebMessageReceived += Player_WebMessageReceived;
                    _webMessageHandlerAttached = true;
                }
                await InitializeBrowserViewAsync(YoutubeSearchView);
                LoadPreferences();
                YoutubeSearchView.CoreWebView2.Navigate("https://www.youtube.com/");
                _searchWebViewReady = true;
                StatusText.Text = "Player pronto. Use a aba Pesquisa YouTube para procurar a próxima música.";

            }
            catch (Exception ex) { StatusText.Text = "Não foi possível iniciar o WebView2: " + ex.Message; }
        };
    }

    private Microsoft.Web.WebView2.Wpf.WebView2? ActiveBrowserView => (BrowserTabs.SelectedItem as TabItem)?.Content as Microsoft.Web.WebView2.Wpf.WebView2;

    private async Task InitializeBrowserViewAsync(Microsoft.Web.WebView2.Wpf.WebView2 view)
    {
        await view.EnsureCoreWebView2Async();
        view.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        view.CoreWebView2.Settings.IsStatusBarEnabled = true;
        view.CoreWebView2.NewWindowRequested += Browser_NewWindowRequested;
        view.CoreWebView2.DocumentTitleChanged += (_, _) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (BrowserTabs.Items.OfType<TabItem>().FirstOrDefault(tab => ReferenceEquals(tab.Content, view)) is { } tab)
                {
                    var title = view.CoreWebView2.DocumentTitle;
                    tab.Header = string.IsNullOrWhiteSpace(title) ? "Nova aba" :
                        title.Length > 26 ? title[..23] + "..." : title;
                }
            });
        };
        view.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (ReferenceEquals(ActiveBrowserView, view) && view.Source is not null)
                    YoutubeSearchBox.Text = view.Source.ToString();
            });
        };
    }

    private async void NewBrowserTab_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var view = await CreateBrowserTabAsync(selectTab: true);
            view.CoreWebView2.Navigate("https://www.youtube.com/");
            _searchWebViewReady = true;
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível abrir nova aba: " + ex.Message; }
    }

    private async Task<Microsoft.Web.WebView2.Wpf.WebView2> CreateBrowserTabAsync(bool selectTab)
    {
        var view = new Microsoft.Web.WebView2.Wpf.WebView2();
        var tab = new TabItem { Header = "YouTube " + (++_browserTabNumber), Content = view };
        BrowserTabs.Items.Add(tab);
        if (selectTab) BrowserTabs.SelectedItem = tab;
        await InitializeBrowserViewAsync(view);
        return view;
    }

    private async void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // O WebView2 abre links target=_blank no navegador padrão se não receber outro WebView.
        // Direciona esses links para uma nova aba interna do Lyravoxen.
        var deferral = e.GetDeferral();
        e.Handled = true;
        try
        {
            var view = await CreateBrowserTabAsync(selectTab: true);
            e.NewWindow = view.CoreWebView2;
            _searchWebViewReady = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível abrir o link em uma aba interna: " + ex.Message;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void CloseBrowserTab_Click(object sender, RoutedEventArgs e)
    {
        if (BrowserTabs.Items.Count <= 1) { StatusText.Text = "Mantenha pelo menos uma aba aberta."; return; }
        var index = BrowserTabs.SelectedIndex;
        BrowserTabs.Items.RemoveAt(index);
        BrowserTabs.SelectedIndex = Math.Max(0, index - 1);
    }

    private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, BrowserTabs)) return;
        var view = ActiveBrowserView;
        _searchWebViewReady = view?.CoreWebView2 is not null;
        if (view?.Source is not null) YoutubeSearchBox.Text = view.Source.ToString();
    }

    private void YoutubeSearch_Click(object sender, RoutedEventArgs e)
    {
        var view = ActiveBrowserView;
        if (view?.CoreWebView2 is null) { StatusText.Text = "A aba ainda está inicializando."; return; }
        var text = YoutubeSearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (Uri.TryCreate(text, UriKind.Absolute, out var address) &&
            (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps))
            view.CoreWebView2.Navigate(address.ToString());
        else
            view.CoreWebView2.Navigate("https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(text));
    }

    private void YoutubeSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            YoutubeSearch_Click(sender, new RoutedEventArgs());
        }
    }

    private void YoutubeSearchHome_Click(object sender, RoutedEventArgs e)
    {
        var view = ActiveBrowserView;
        if (view?.CoreWebView2 is not null) view.CoreWebView2.Navigate("https://www.youtube.com/");
    }

    private void YoutubeSearchPaste_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Clipboard.ContainsText()) YoutubeSearchBox.Text = System.Windows.Clipboard.GetText();
        YoutubeSearchBox.Focus();
    }

    private void YoutubeSearchCopy_Click(object sender, RoutedEventArgs e)
    {
        var view = ActiveBrowserView;
        var address = view?.Source?.ToString();
        if (!string.IsNullOrWhiteSpace(address))
        {
            System.Windows.Clipboard.SetText(address);
            StatusText.Text = "Endereço copiado: " + address;
        }
    }

    private void YoutubeSearchBack_Click(object sender, RoutedEventArgs e)
    {
        var view = ActiveBrowserView;
        if (view?.CoreWebView2 is not null && view.CoreWebView2.CanGoBack) view.CoreWebView2.GoBack();
    }

    private void YoutubeSearchForward_Click(object sender, RoutedEventArgs e)
    {
        var view = ActiveBrowserView;
        if (view?.CoreWebView2 is not null && view.CoreWebView2.CanGoForward) view.CoreWebView2.GoForward();
    }

    private void YoutubeSearchRefresh_Click(object sender, RoutedEventArgs e)
    {
        var view = ActiveBrowserView;
        if (view?.CoreWebView2 is not null) view.CoreWebView2.Reload();
    }
    private async void PrepareNext_Click(object sender, RoutedEventArgs e)
    {
        var browser = ActiveBrowserView;
        if (browser?.CoreWebView2 is null) return;
        string currentUrl;
        try
        {
            var encodedUrl = await browser.CoreWebView2.ExecuteScriptAsync("window.location.href");
            currentUrl = JsonSerializer.Deserialize<string>(encodedUrl) ?? "";
        }
        catch { currentUrl = browser.Source?.ToString() ?? ""; }
        var videoId = YouTubeLink.TryGetVideoId(currentUrl);
        if (videoId is null)
        {
            System.Windows.MessageBox.Show("Abra o vídeo desejado na aba Próxima música (não apenas a página de resultados) e tente preparar novamente.", "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _nextVideoUrl = "https://www.youtube.com/watch?v=" + videoId;
        SaveNextVideoPreference();
        NextTrackText.Text = "Próxima faixa preparada (" + videoId + "). A música atual continua tocando até você selecionar Tocar próxima.";
        PlayNextButton.IsEnabled = true;
        StatusText.Text = "Próxima música preparada sem interromper a reprodução atual.";
    }

    private async void PlayNextPrepared_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_nextVideoUrl)) return;
        var url = _nextVideoUrl;
        var queuedVideoId = YouTubeLink.TryGetVideoId(url);
        if (queuedVideoId is null) return;
        _pendingQueueVideoId = queuedVideoId;
        _pauseOnPlayerReady = true;
        WorkspaceTabs.SelectedIndex = 0;
        if (!await LoadVideoAsync(url, true))
        {
            _pendingQueueVideoId = null;
            _pauseOnPlayerReady = false;
            PlayNextButton.IsEnabled = !string.IsNullOrWhiteSpace(_nextVideoUrl);
        }
    }

    private async void LoadYoutube_Click(object sender, RoutedEventArgs e)
    {
        await LoadVideoAsync(YoutubeUrlBox.Text, true);
    }

    private async Task<bool> LoadVideoAsync(string url, bool addToHistory)
    {
        if (_isLoadingVideo) return false;
        var videoId = YouTubeLink.TryGetVideoId(url);
        if (videoId is null)
        {
            System.Windows.MessageBox.Show("Cole um link válido de vídeo do YouTube.", "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }
        _isLoadingVideo = true;
        LoadYoutubeButton.IsEnabled = false;
        PlayNextButton.IsEnabled = false;
        try
        {
            await StopYoutubeCaptureAsync();
            _playerSessionId = Guid.NewGuid().ToString("N");
            _holyricsTakeoverActive = false;
            _transcriptionEngine?.ResetTimeline(0, _playerSessionId);
            Interlocked.Increment(ref _lyricsSearchVersion); // invalida qualquer busca da música anterior
            StatusText.Text = "Preparando vídeo e procurando letra já cadastrada...";
            PublishLyric("");
            _audioTimer.Stop(); _isLocalAudioPlaying = false; LocalAudioPlayer.Stop(); LocalAudioPlayer.Visibility = Visibility.Collapsed; YoutubePlayer.Visibility = Visibility.Visible;
            _playerReady = false; _lastPlayerState = -1; _playerDurationSeconds = 0; _lastKnownPlaybackSeconds = 0; _lastAutomaticCue = -1;
            _manualProjectionMode = false; _manualCueIndex = -1;

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
            MusicTrackBox.Text = _currentVideo.Title == "Vídeo do YouTube" ? "" : _currentVideo.Title.Split(" — ", 2)[0];
            MusicArtistBox.Clear();
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(_currentVideo.Cues));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            StatusText.Text = _cues.Count > 0
                ? $"Letra preparada: {_cues.Count} frases carregadas. Inicializando o vídeo..."
                : "Vídeo identificado. Nenhuma letra salva para ele; você pode cadastrar as frases enquanto o player carrega.";

            if (YoutubePlayer.CoreWebView2 is null)
                throw new InvalidOperationException("O ambiente isolado do player ainda não foi inicializado.");
            Directory.CreateDirectory(_playerFolder);
            if (!_virtualHostMapped)
            {
                YoutubePlayer.CoreWebView2.SetVirtualHostNameToFolderMapping("lyravoxen.local", _playerFolder, CoreWebView2HostResourceAccessKind.Allow);
                _virtualHostMapped = true;
            }
            var html = YouTubePlayerPage.Build(videoId, _playerSessionId, startPaused: _pauseOnPlayerReady);
            await File.WriteAllTextAsync(Path.Combine(_playerFolder, "index.html"), html);
            YoutubePlayer.CoreWebView2.Navigate("https://lyravoxen.local/index.html");
            YoutubeUrlBox.Text = url;
            if (addToHistory) await AddVideoToHistoryAsync(videoId, url);
            // Mantém o título obtido dos metadados do YouTube; o intérprete pode ser informado opcionalmente.
            StatusText.Text = _cues.Count > 0
                ? $"Player preparado com {_cues.Count} frases. A letra acompanhará a reprodução."
                : "Player preparado. Procurando automaticamente uma letra sincronizada para este título...";
            if (_cues.Count == 0 && !string.IsNullOrWhiteSpace(MusicTrackBox.Text))
                await TryLoadAutomaticLyricsAsync(MusicTrackBox.Text, "");
            return true;
        }
        catch (Exception ex)
        {
            _playerReady = false;
            StatusText.Text = "Erro ao carregar o player: " + ex.Message;
            return false;
        }
        finally
        {
            _isLoadingVideo = false;
            LoadYoutubeButton.IsEnabled = true;
            PlayNextButton.IsEnabled = !string.IsNullOrWhiteSpace(_nextVideoUrl);
        }
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

    private static double MusicTextSimilarity(string expected, string candidate)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(candidate)) return 0;
        if (expected == candidate) return 1;
        if (ContainsMusicPhrase(candidate, expected) || ContainsMusicPhrase(expected, candidate)) return 0.92;
        var expectedWords = expected.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var candidateWords = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var union = expectedWords.Union(candidateWords).Count();
        return union == 0 ? 0 : (double)expectedWords.Intersect(candidateWords).Count() / union;
    }

    private async void ImportHolyricsSong_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var query = PromptForText("Importar música do Holyrics", "Digite parte do título ou o nome do intérprete:");
            if (string.IsNullOrWhiteSpace(query)) return;
            StatusText.Text = "Pesquisando a biblioteca de músicas do Holyrics...";
            var results = await _holyrics.SearchSongsAsync(query.Trim());
            if (results.Count == 0)
            {
                StatusText.Text = "Nenhuma música encontrada na biblioteca do Holyrics.";
                return;
            }

            var choices = string.Join(Environment.NewLine, results.Select((song, index) =>
                $"{index + 1}. {song.Title}" + (string.IsNullOrWhiteSpace(song.Artist) ? "" : $" — {song.Artist}")));
            var selectedText = PromptForText("Escolher música do Holyrics",
                "Digite o número da música para importar:\n\n" + choices, "1");
            if (!int.TryParse(selectedText, out var selectedIndex) || selectedIndex < 1 || selectedIndex > results.Count)
                return;

            var selected = results[selectedIndex - 1];
            StatusText.Text = "Importando slides da música: " + selected.Title;
            var slides = await _holyrics.GetSongSlidesAsync(selected.Id);
            if (slides.Count == 0)
            {
                StatusText.Text = "A música foi encontrada, mas não possui slides de letra disponíveis.";
                return;
            }

            _cues.Clear();
            for (var index = 0; index < slides.Count; index++)
                _cues.Add(new LyricCue { Seconds = 0, Text = slides[index].Trim() });
            _currentVideo ??= new LibraryVideo
            {
                VideoId = YouTubeLink.TryGetVideoId(YoutubeUrlBox.Text) ?? "holyrics-" + selected.Id,
                Url = YoutubeUrlBox.Text,
                Title = selected.Title + (string.IsNullOrWhiteSpace(selected.Artist) ? "" : " — " + selected.Artist)
            };
            _currentVideo.Title = selected.Title + (string.IsNullOrWhiteSpace(selected.Artist) ? "" : " — " + selected.Artist);
            _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
            // Preserve imported arrangements and learned playback timings in the library
            // whenever this song is linked to a YouTube URL.
            if (!string.IsNullOrWhiteSpace(_currentVideo.Url))
            {
                var holyricsFolder = _library.FirstOrDefault(folder =>
                    string.Equals(folder.Name, "Músicas do Holyrics", StringComparison.OrdinalIgnoreCase));
                if (holyricsFolder is null)
                {
                    holyricsFolder = new LibraryFolder { Name = "Músicas do Holyrics" };
                    _library.Add(holyricsFolder);
                }
                var savedSong = holyricsFolder.Videos.FirstOrDefault(video =>
                    string.Equals(video.VideoId, _currentVideo.VideoId, StringComparison.Ordinal));
                if (savedSong is null)
                {
                    savedSong = new LibraryVideo
                    {
                        VideoId = _currentVideo.VideoId,
                        Url = _currentVideo.Url,
                        Title = _currentVideo.Title
                    };
                    holyricsFolder.Videos.Add(savedSong);
                }
                savedSong.Url = _currentVideo.Url;
                savedSong.Title = _currentVideo.Title;
                savedSong.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
                SaveLibrary();
                RefreshFolders(holyricsFolder.Id);
            }
            RefreshCueList(_cues.FirstOrDefault());
            PersistCurrentCues();
            _manualProjectionMode = true;
            _manualCueIndex = 0;
            _lastAutomaticCue = -1;
            LineCountBox.SelectedIndex = 0; // Cada slide importado do Holyrics ocupa uma tela no avanço manual.
            CaptureCueTimesCheckBox.IsChecked = true;
            CueList.SelectedItem = _cues[0];
            PublishLyric(BuildProjectionCueBlock(0));
            StatusText.Text = $"Importadas {slides.Count} telas de letra. A primeira tela já foi enviada ao telão. Como o áudio é playback instrumental, avance em Próxima no ritmo da música: os tempos serão registrados automaticamente para sincronizar as próximas reproduções.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível importar a música do Holyrics: " + ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Importação do Holyrics", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SearchHymnSources_Click(object sender, RoutedEventArgs e)
    {
        SearchAcrossLyricSites(new[]
        {
            "harpacrista.digital", "ogospel.com.br", "agendapregador.com.br",
            "mbiblia.com.br", "gospelsonymusic.com.br", "hinarioevangelico.com",
            "reformabrasil.com.br", "hinoteca.com.br", "hinarioonline.com.br",
            "hinarioccb.net", "hymnsforworship.app"
        }, "hinos evangélicos Harpa Cristã louvores");
    }

    private void SearchLyricsSources_Click(object sender, RoutedEventArgs e)
    {
        SearchAcrossLyricSites(new[]
        {
            "letras.mus.br", "cifraclub.com.br", "vagalume.com.br",
            "letras.com", "musixmatch.com", "genius.com"
        }, "letra música louvor");
    }

    private void SearchAcrossLyricSites(IEnumerable<string> domains, string category)
    {
        var track = SearchTrackBox.Text.Trim();
        var artist = SearchArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track))
        {
            StatusText.Text = "Informe o título ou número do hino antes de pesquisar nos sites.";
            SearchTrackBox.Focus();
            return;
        }

        var terms = string.Join(" ", new[] { track, artist, category }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var siteFilter = string.Join(" OR ", domains.Select(domain => "site:" + domain));
        var query = Uri.EscapeDataString(terms + " (" + siteFilter + ")");
        var view = ActiveBrowserView;
        if (view?.CoreWebView2 is null)
        {
            StatusText.Text = "A aba do navegador ainda está inicializando. Tente novamente em instantes.";
            return;
        }
        view.CoreWebView2.Navigate("https://www.google.com/search?q=" + query);
        StatusText.Text = "Pesquisando em " + domains.Count() + " fontes: " + string.Join(", ", domains);
    }

    private async void SearchLyricsFromBrowser_Click(object sender, RoutedEventArgs e)
    {
        var track = SearchTrackBox.Text.Trim();
        var artist = SearchArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track))
        {
            StatusText.Text = "Informe o título da música para pesquisar a letra.";
            SearchTrackBox.Focus();
            return;
        }
        MusicTrackBox.Text = track;
        MusicArtistBox.Text = artist;
        WorkspaceTabs.SelectedIndex = 0;
        await TryLoadAutomaticLyricsAsync(track, artist);
    }

    private async void SearchLyrics_Click(object sender, RoutedEventArgs e)
    {
        var track = MusicTrackBox.Text.Trim();
        var artist = MusicArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track))
        {
            System.Windows.MessageBox.Show("O título da música é necessário. O intérprete é opcional; se não souber, deixe em branco e confira a correspondência antes de aplicar.", "Buscar letra", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await TryLoadAutomaticLyricsAsync(track, artist);
    }

    private async Task TryLoadAutomaticLyricsAsync(string trackName, string artistName)
    {
        var searchVersion = Interlocked.Increment(ref _lyricsSearchVersion);
        var searchSession = _playerSessionId;
        var searchVideoId = _currentVideo?.VideoId;
        try
        {
            StatusText.Text = "Buscando letras e verificando correspondência: " + trackName + " — " + artistName;
            var query = Uri.EscapeDataString(string.Join(" ", new[] { trackName, artistName }.Where(value => !string.IsNullOrWhiteSpace(value))));
            var endpoint = "https://lrclib.net/api/search?q=" + query;
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.UserAgent.ParseAdd("Lyravoxen/1.0 (https://github.com/jabesguitar/Lyravoxen)");
            using var response = await MetadataHttp.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                StatusText.Text = "Catálogo de letras indisponível (HTTP " + (int)response.StatusCode + ").";
                return;
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.ValueKind != JsonValueKind.Array)
            {
                StatusText.Text = "O catálogo não retornou uma lista de músicas.";
                return;
            }

            var wantedTrack = NormalizeMusicText(trackName);
            var wantedArtist = NormalizeMusicText(artistName);
            var candidates = new List<(double Score, string Track, string Artist, string Album, List<LyricCue> Cues)>();
            foreach (var item in json.RootElement.EnumerateArray())
            {
                var foundTrack = item.TryGetProperty("trackName", out var tp) ? tp.GetString() ?? "" : "";
                var foundArtist = item.TryGetProperty("artistName", out var ap) ? ap.GetString() ?? "" : "";
                var foundAlbum = item.TryGetProperty("albumName", out var alp) ? alp.GetString() ?? "" : "";
                var normTrack = NormalizeMusicText(foundTrack);
                var normArtist = NormalizeMusicText(foundArtist);

                // O título do vídeo pode conter "ao vivo", "clipe oficial" ou outros sufixos.
                // Aceita candidatos aproximados, mas sempre pede confirmação antes de substituir a letra.
                var trackSimilarity = MusicTextSimilarity(wantedTrack, normTrack);
                var artistSimilarity = string.IsNullOrWhiteSpace(wantedArtist) ? 0 : MusicTextSimilarity(wantedArtist, normArtist);
                if (trackSimilarity < 0.42) continue;
                if (!string.IsNullOrWhiteSpace(wantedArtist) && artistSimilarity < 0.40) continue;
                if (!item.TryGetProperty("syncedLyrics", out var synced) || synced.ValueKind != JsonValueKind.String)
                    continue;
                var lrc = synced.GetString();
                if (string.IsNullOrWhiteSpace(lrc)) continue;
                var imported = ParseTimedLyrics(lrc, ".lrc").OrderBy(c => c.Seconds).ToList();
                if (imported.Count < 2) continue;

                var duration = item.TryGetProperty("duration", out var durationElement) &&
                    durationElement.ValueKind == JsonValueKind.Number ? durationElement.GetDouble() : 0;
                var score = string.IsNullOrWhiteSpace(wantedArtist)
                    ? trackSimilarity * 85 + 10
                    : trackSimilarity * 70 + artistSimilarity * 20 + 10;
                if (duration > 0 && _playerDurationSeconds > 0)
                {
                    var difference = Math.Abs(duration - _playerDurationSeconds);
                    score -= Math.Min(25, difference / Math.Max(1, _playerDurationSeconds) * 25);
                }
                candidates.Add((score, foundTrack, foundArtist, foundAlbum, imported));
            }

            if (candidates.Count == 0)
            {
                StatusText.Text = "Não encontrei letra sincronizada suficientemente parecida. Confira o título, tente informar o intérprete ou importe um arquivo LRC/SRT.";
                return;
            }

            if (!IsCurrentLyricsSearch(searchVersion, searchSession, searchVideoId)) return;

            var ordered = candidates.OrderByDescending(candidate => candidate.Score).ToList();
            var selected = ordered[0];
            var preview = string.Join(Environment.NewLine, selected.Cues.Take(8).Select(c => c.Text));
            var durationNote = _playerDurationSeconds > 0
                ? "\nDuração do vídeo: " + TimeSpan.FromSeconds(_playerDurationSeconds).ToString(@"mm\:ss")
                : "";
            var albumNote = string.IsNullOrWhiteSpace(selected.Album) ? "" : "\nÁlbum: " + selected.Album;
            var alternatives = string.Join(Environment.NewLine, ordered.Skip(1).Take(3)
                .Select(candidate => "• " + candidate.Track + " — " + candidate.Artist));
            var alternativesNote = string.IsNullOrWhiteSpace(alternatives) ? "" : "\n\nOutras correspondências encontradas:\n" + alternatives;
            var confirmation = System.Windows.MessageBox.Show(
                "Confira a correspondência antes de projetar.\n\nVídeo carregado: " + (_currentVideo?.Title ?? "") +
                "\nFaixa encontrada: " + selected.Track + "\nIntérprete: " + selected.Artist +
                "\nCompatibilidade estimada: " + selected.Score.ToString("0", CultureInfo.InvariantCulture) + "%" + albumNote + durationNote +
                "\n\nPrévia:\n" + preview + alternativesNote +
                "\n\nEsta é a gravação/letra correta? A aplicação só acontece se você confirmar.",
                "Confirmar letra sincronizada", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (!IsCurrentLyricsSearch(searchVersion, searchSession, searchVideoId)) return;
            if (confirmation != MessageBoxResult.Yes)
            {
                StatusText.Text = "Letra não aplicada. Nenhuma frase foi substituída.";
                return;
            }

            if (!IsCurrentLyricsSearch(searchVersion, searchSession, searchVideoId)) return;
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(selected.Cues));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            if (_currentVideo is not null)
            {
                _currentVideo.Cues = _cues.Select(c => new LyricCue { Seconds = c.Seconds, Text = c.Text }).ToList();
                foreach (var saved in _library.SelectMany(f => f.Videos).Where(v => v.VideoId == _currentVideo.VideoId && v.IsOffline == _currentVideo.IsOffline))
                    saved.Cues = _currentVideo.Cues.Select(c => new LyricCue { Seconds = c.Seconds, Text = c.Text }).ToList();
                SaveLibrary();
            }
            StatusText.Text = "Letra sincronizada confirmada: " + selected.Track + " — " + selected.Artist + ". Confira a sincronia com a gravação.";
        }
        catch (TaskCanceledException) { StatusText.Text = "A busca de letras excedeu o tempo limite."; }
        catch (HttpRequestException ex) { StatusText.Text = "Falha na busca de letras: " + ex.Message; }
        catch (JsonException) { StatusText.Text = "Resposta inválida do catálogo de letras."; }
        catch (InvalidOperationException ex) { StatusText.Text = "Não foi possível interpretar os dados da letra: " + ex.Message; }
    }

    private bool IsCurrentLyricsSearch(long version, string session, string? videoId) =>
        version == Interlocked.Read(ref _lyricsSearchVersion) &&
        string.Equals(session, _playerSessionId, StringComparison.Ordinal) &&
        string.Equals(videoId, _currentVideo?.VideoId, StringComparison.Ordinal);

    private async Task AddVideoToHistoryAsync(string videoId, string url)
    {
        string title = "Vídeo do YouTube";
        string author = "";
        try
        {
            var endpoint = "https://www.youtube.com/oembed?url=" + Uri.EscapeDataString("https://www.youtube.com/watch?v=" + videoId) + "&format=json";
            using var response = await MetadataHttp.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("title", out var titleElement))
                    title = titleElement.GetString() ?? title;
                if (json.RootElement.TryGetProperty("author_name", out var authorElement))
                    author = authorElement.GetString() ?? "";
            }
        }
        catch { title = "Vídeo do YouTube (" + videoId + ")"; }

        var displayTitle = string.IsNullOrWhiteSpace(author) ? title : title + " — " + author;
        if (_currentVideo?.VideoId == videoId)
        {
            _currentVideo.Title = displayTitle;
            var identity = SplitVideoTitle(title);
            MusicTrackBox.Text = identity.Track;
            MusicArtistBox.Text = identity.Artist;
            if (SearchTrackBox is not null) SearchTrackBox.Text = identity.Track;
            if (SearchArtistBox is not null) SearchArtistBox.Text = identity.Artist;
        }
        var existing = _history.FirstOrDefault(item => item.VideoId == videoId);
        if (existing is not null)
        {
            existing.Title = displayTitle;
            existing.Url = url;
            existing.LastUsed = DateTime.Now;
        }
        else _history.Insert(0, new VideoHistoryItem { VideoId = videoId, Title = displayTitle, Url = url, LastUsed = DateTime.Now });
        _history.Sort((a, b) => b.LastUsed.CompareTo(a.LastUsed));
        SaveHistory();
        RefreshHistory();
    }

    private static (string Track, string Artist) SplitVideoTitle(string title)
    {
        var separators = new[] { " - ", " – ", " — ", " | " };
        foreach (var separator in separators)
        {
            var parts = title.Split(separator, 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Length > 1 && parts[1].Length > 1)
                return (parts[1], parts[0]);
        }
        return (title.Trim(), "");
    }

    private void LoadHistory()
    {
        var backupFile = _historyFile + ".bak";
        try
        {
            var sourceFile = File.Exists(_historyFile) ? _historyFile : backupFile;
            if (File.Exists(sourceFile))
            {
                var loaded = JsonSerializer.Deserialize<List<VideoHistoryItem>>(File.ReadAllText(sourceFile)) ?? new();
                _history.AddRange(loaded.Where(item => item is not null &&
                    !string.IsNullOrWhiteSpace(item.VideoId) &&
                    !string.IsNullOrWhiteSpace(item.Url))
                    .GroupBy(item => item.VideoId, StringComparer.Ordinal)
                    .Select(group => group.OrderByDescending(item => item.LastUsed).First())
                    .OrderByDescending(item => item.LastUsed));
                if (sourceFile == backupFile)
                    StatusText.Text = "Histórico principal indisponível; cópia de segurança recuperada.";
            }
        }
        catch
        {
            _history.Clear();
            try
            {
                if (!File.Exists(backupFile)) throw new InvalidDataException("Cópia de segurança inexistente.");
                var recovered = JsonSerializer.Deserialize<List<VideoHistoryItem>>(File.ReadAllText(backupFile)) ?? new();
                _history.AddRange(recovered.Where(item => item is not null &&
                    !string.IsNullOrWhiteSpace(item.VideoId) &&
                    !string.IsNullOrWhiteSpace(item.Url))
                    .GroupBy(item => item.VideoId, StringComparer.Ordinal)
                    .Select(group => group.OrderByDescending(item => item.LastUsed).First())
                    .OrderByDescending(item => item.LastUsed));
                StatusText.Text = "Histórico recuperado da cópia de segurança. O arquivo principal foi preservado.";
            }
            catch
            {
                _history.Clear();
                StatusText.Text = "Não foi possível ler o histórico nem sua cópia. Os arquivos originais foram preservados.";
            }
        }
        RefreshHistory();
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFile)!;
            Directory.CreateDirectory(directory);
            var tempFile = _historyFile + ".tmp";
            var backupFile = _historyFile + ".bak";
            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempFile, json);
            if (File.Exists(_historyFile))
                File.Copy(_historyFile, backupFile, true);
            File.Move(tempFile, _historyFile, true);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível salvar o histórico; o arquivo anterior foi preservado: " + ex.Message;
        }
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
            if (!root.TryGetProperty("session", out var sessionElement) ||
                !string.Equals(sessionElement.GetString(), _playerSessionId, StringComparison.Ordinal))
                return; // Ignora eventos atrasados de um player/vídeo anterior.
            if (root.TryGetProperty("type", out var type) && type.GetString() == "playerError")
            {
                var code = root.TryGetProperty("code", out var c) && c.TryGetInt32(out var parsedCode) ? parsedCode : 0;
                var detail = root.TryGetProperty("detail", out var detailValue) ? detailValue.GetString() : null;
                var explanation = code switch
                {
                    -1 => detail ?? "Não foi possível carregar a API oficial do YouTube. Verifique a conexão.",
                    2 => "O identificador do vídeo é inválido.",
                    5 => "O vídeo não pôde ser reproduzido no player HTML5.",
                    100 => "O vídeo não existe, foi removido ou está privado.",
                    101 or 150 => "O proprietário não permite a reprodução incorporada deste vídeo.",
                    153 => "O YouTube recusou o player por falta de identificação válida do cliente/referenciador. Atualize o WebView2 e tente novamente.",
                    _ => "O YouTube informou erro " + code + ". Confira a disponibilidade e as permissões de incorporação do vídeo."
                };
                _ = StopYoutubeCaptureAsync();
                Dispatcher.Invoke(() =>
                {
                    _playerReady = false;
                    _pendingQueueVideoId = null;
                    PlayNextButton.IsEnabled = !string.IsNullOrWhiteSpace(_nextVideoUrl);
                    StatusText.Text = "Falha no player: " + explanation + " A próxima música continua guardada na fila.";
                });
                return;
            }
            if (!root.TryGetProperty("type", out type)) return;
            if (type.GetString() == "ready")
            {
                _playerReady = true;
                if (_pauseOnPlayerReady)
                {
                    _pauseOnPlayerReady = false;
                    _ = YoutubePlayer.CoreWebView2.ExecuteScriptAsync("if (typeof player !== 'undefined' && player) player.pauseVideo();");
                }
                Dispatcher.Invoke(() =>
                {
                    if (_pendingQueueVideoId is not null &&
                        string.Equals(_currentVideo?.VideoId, _pendingQueueVideoId, StringComparison.Ordinal))
                    {
                        _nextVideoUrl = null;
                        _pendingQueueVideoId = null;
                        SaveNextVideoPreference();
                        PlayNextButton.IsEnabled = false;
                        NextTrackText.Text = "Pesquise e prepare a próxima música.";
                        StatusText.Text = "Próxima música carregada e player confirmado.";
                    }
                    else
                    {
                        StatusText.Text = "Player do YouTube pronto. Aguardando reprodução.";
                    }
                });
                return;
            }
            if (type.GetString() == "state")
            {
                var playerState = root.TryGetProperty("state", out var stateValue) ? stateValue.GetInt32() : -1;
                Dispatcher.Invoke(() => StatusText.Text = playerState == 1 ? "YouTube tocando." : playerState == 2 ? "YouTube pausado." : playerState == 0 ? "YouTube finalizado." : "YouTube aguardando.");
                return;
            }
            if (type.GetString() != "time") return;
            if (!root.TryGetProperty("seconds", out var secondsElement)) return;
            var seconds = secondsElement.GetDouble();
            if (!double.IsFinite(seconds) || seconds < 0) return;
            _lastKnownPlaybackSeconds = seconds;
            var state = root.TryGetProperty("state", out var stateElement) ? stateElement.GetInt32() : -1;
            if (root.TryGetProperty("duration", out var durationElement) && durationElement.TryGetDouble(out var durationValue)) _playerDurationSeconds = durationValue;
            if (state != _lastPlayerState)
            {
                _lastPlayerState = state;
                if (state == 1)
                {
                    Dispatcher.Invoke(() => StatusText.Text = !_playerReady ? "YouTube tocando; aguardando confirmação do player." : "YouTube tocando. Preparando captura isolada de áudio.");
                    _ = TakeOverHolyricsForPlaybackAsync();
                    _ = StartYoutubeCaptureAsync();
                }
                else if (state == 2)
                {
                    _ = StopYoutubeCaptureAsync();
                    Dispatcher.Invoke(() => StatusText.Text = "YouTube pausado.");
                }
                else if (state == 0)
                {
                    _ = StopYoutubeCaptureAsync();
                    Dispatcher.Invoke(() => { PublishLyric(""); _lastAutomaticCue = -1; StatusText.Text = "Reprodução do YouTube finalizada."; });
                }
            }
            if (state != 1 || _cues.Count == 0 || _manualProjectionMode) return;
            var cueIndex = FindActiveCue(seconds);
            if (cueIndex == _lastAutomaticCue) return;
            _lastAutomaticCue = cueIndex;
            PublishLyric(BuildProjectionCueBlock(cueIndex));
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }


    // O player usa um ambiente WebView2 próprio, separado das abas de pesquisa.
    // A captura fica restrita à árvore de processos desse ambiente.
    private async Task StartYoutubeCaptureAsync()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) || YoutubePlayer.CoreWebView2 is null) return;
        await _captureLock.WaitAsync();
        try
        {
            if (_lastPlayerState != 1 || _youtubeAudioCapture?.IsCapturing == true) return;

            if (_transcriptionEngine is null)
            {
                if (Environment.OSVersion.Version.Build < 20348)
                    throw new PlatformNotSupportedException("A transcrição local exige Windows 11/Server 2022. O player continua funcionando com letras de catálogo, LRC/SRT ou cadastro manual.");
                var modelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Lyravoxen", "Models", "ggml-tiny-q5_0.bin");
                var hardware = HardwareDiagnostics.Collect();
                var engine = new LiveTranscriptionEngine(modelPath, hardware.RecommendedInferenceThreads);
                engine.StatusChanged += message => Dispatcher.BeginInvoke(() => StatusText.Text = message);
                engine.Failed += ex => Dispatcher.BeginInvoke(() => StatusText.Text = "Reconhecimento indisponível: " + ex.Message);
                engine.SegmentRecognized += segment => Dispatcher.BeginInvoke(() => ApplyRecognizedSegment(segment));
                _transcriptionEngine = engine;
                // Start the model download/load in the background. Capture begins immediately,
                // so the first song is not lost while the model is being prepared.
                _transcriptionEngineStartTask = InitializeTranscriptionEngineAsync(engine);
            }

            if (_lastPlayerState != 1) return;
            _transcriptionEngine.ResetTimeline(_lastKnownPlaybackSeconds, _playerSessionId);
            var pid = YoutubePlayer.CoreWebView2.BrowserProcessId;
            if (pid == 0) throw new InvalidOperationException("O WebView2 não informou o PID do player.");
            var capture = new ProcessAudioCapture(() => Volatile.Read(ref _lastKnownPlaybackSeconds), () => Volatile.Read(ref _playerSessionId));
            capture.AudioChunkAvailable += (_, chunk) => _transcriptionEngine?.TryWrite(chunk);
            capture.CaptureFailed += ex => Dispatcher.BeginInvoke(() =>
                StatusText.Text = "Falha na captura isolada do YouTube: " + ex.Message);
            _youtubeAudioCapture = capture;
            try
            {
                await capture.StartAsync(pid);
                Dispatcher.Invoke(() => StatusText.Text = "Captura isolada ativa. Preparando a transcrição local em português...");
            }
            catch
            {
                _youtubeAudioCapture = null;
                await capture.DisposeAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => StatusText.Text = "Transcrição/captura indisponível: " + ex.Message);
        }
        finally { _captureLock.Release(); }
    }

    private async Task InitializeTranscriptionEngineAsync(LiveTranscriptionEngine engine)
    {
        try { await engine.StartAsync(); }
        catch (Exception ex)
        {
            if (ReferenceEquals(_transcriptionEngine, engine))
                _transcriptionEngine = null;
            try { await StopYoutubeCaptureAsync(); } catch { }
            try { await engine.DisposeAsync(); } catch { }
            Dispatcher.BeginInvoke(() => StatusText.Text = "Transcrição local indisponível: " + ex.Message);
        }
    }

    private void ApplyRecognizedSegment(TranscriptionSegment segment)
    {
        if (_lastPlayerState != 1 || string.IsNullOrWhiteSpace(segment.Text)) return;
        var normalized = NormalizeMusicText(segment.Text);
        if (string.IsNullOrWhiteSpace(normalized)) return;
        var existing = _cues.FirstOrDefault(cue =>
            Math.Abs(cue.Seconds - segment.StartSeconds) < 1.8 &&
            MusicTextSimilarity(NormalizeMusicText(cue.Text), normalized) > 0.72);
        if (existing is not null) return;

        var cue = new LyricCue { Seconds = Math.Max(0, segment.StartSeconds), Text = segment.Text.Trim() };
        _cues.Add(cue);
        _cues.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        RefreshCueList(cue);
        PersistCurrentCues();

        var activeIndex = FindActiveCue(GetPlaybackSeconds());
        if (!_manualProjectionMode && activeIndex >= 0)
        {
            _lastAutomaticCue = activeIndex;
            PublishLyric(BuildProjectionCueBlock(activeIndex));
        }
        else
        {
            ProjectionPreviewText.Text = "Transcrição: " + cue.Text;
            NextProjectionPreviewText.Text = "Reconhecida em " + TimeSpan.FromSeconds(cue.Seconds).ToString(@"mm\\:ss");
        }
    }

    private async Task DisposeAudioServicesAsync()
    {
        await StopYoutubeCaptureAsync();
        var engine = _transcriptionEngine;
        _transcriptionEngine = null;
        if (engine is not null) await engine.DisposeAsync();
        if (_transcriptionEngineStartTask is not null)
        {
            try { await _transcriptionEngineStartTask; } catch { }
            _transcriptionEngineStartTask = null;
        }
    }

    private async Task StopYoutubeCaptureAsync()
    {
        await _captureLock.WaitAsync();
        try
        {
            var capture = _youtubeAudioCapture;
            _youtubeAudioCapture = null;
            if (capture is not null && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                await capture.DisposeAsync();
        }
        finally { _captureLock.Release(); }
    }

    private async Task TakeOverHolyricsForPlaybackAsync()
    {
        if (!_holyrics.IsConfigured || _holyricsTakeoverActive) return;
        try
        {
            await _holyricsSendLock.WaitAsync();
            try
            {
                if (_holyricsTakeoverActive) return;
                await _holyrics.TakeOverProjectionAsync("");
                _holyricsTakeoverActive = true;
                Dispatcher.BeginInvoke(() => StatusText.Text = "Player do Holyrics parado; Lyravoxen assumiu a projeção para esta música.");
            }
            finally { _holyricsSendLock.Release(); }
        }
        catch (Exception ex)
        {
            Dispatcher.BeginInvoke(() => StatusText.Text = "Não foi possível assumir o Holyrics: " + ex.Message);
        }
    }

    private async Task SendLyricToHolyricsAsync(string text)
    {
        // Não anunciar entrega ao Holyrics se o usuário ainda não configurou a integração.
        if (!_holyrics.IsConfigured) return;

        // Mantém a ordem da apresentação: uma resposta lenta não pode sobrescrever uma frase mais nova.
        var requestVersion = Interlocked.Increment(ref _holyricsSendVersion);
        try
        {
            await _holyricsSendLock.WaitAsync();
            try
            {
                if (requestVersion != Interlocked.Read(ref _holyricsSendVersion)) return;
                if (!string.IsNullOrWhiteSpace(text) && !_holyricsTakeoverActive)
                {
                    await _holyrics.TakeOverProjectionAsync(text);
                    _holyricsTakeoverActive = true;
                }
                else
                {
                    await _holyrics.ShowTextAsync(text);
                }
                if (requestVersion == Interlocked.Read(ref _holyricsSendVersion))
                    Dispatcher.Invoke(() => StatusText.Text = string.IsNullOrWhiteSpace(text)
                        ? "Holyrics confirmou o fechamento da apresentação rápida."
                        : "Holyrics confirmou o recebimento da frase.");
            }
            finally
            {
                _holyricsSendLock.Release();
            }
        }
        catch (Exception ex)
        {
            if (requestVersion == Interlocked.Read(ref _holyricsSendVersion))
                Dispatcher.Invoke(() => StatusText.Text = "Falha ao enviar ao Holyrics: " + ex.Message);
        }
    }

    private async void TestHolyrics_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Verificando conexão e permissões do Holyrics...";
            await _holyrics.TestConnectionAsync();
            await _holyrics.ShowTextAsync("TESTE DE CONEXÃO — LYRAVOXEN");
            StatusText.Text = "Holyrics confirmou o envio. Confira se a frase de teste apareceu na saída do Holyrics.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Falha no teste do Holyrics: " + ex.Message;
            System.Windows.MessageBox.Show(this, "Não foi possível enviar a frase de teste ao Holyrics.\n\n" + ex.Message,
                "Lyravoxen — Holyrics", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void HolyricsSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HolyricsSettingsWindow(_holyrics) { Owner = this };
        dialog.ShowDialog();
        if (dialog.Saved) StatusText.Text = "Configuração do Holyrics salva. A conexão foi testada.";
    }

    private void ProjectionTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectionThemeBox?.SelectedItem is not ComboBoxItem item) return;
        var theme = item.Content?.ToString() ?? "Noturno";
        _projection?.SetTheme(theme);
        if (!_loadingPreferences) SavePreferences(theme, null);
    }

    private void LineCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LineCountBox?.SelectedItem is not ComboBoxItem item ||
            !int.TryParse(item.Content?.ToString(), out var count)) return;
        _projection?.SetMaxLines(Math.Clamp(count, 1, 10));
        if (!_loadingPreferences) SavePreferences(null, count);
        var activeIndex = _manualProjectionMode ? _manualCueIndex : FindActiveCue(GetPlaybackSeconds());
        if (activeIndex >= 0) PublishLyric(BuildProjectionCueBlock(activeIndex));
    }

    private void LoadPreferences()
    {
        try
        {
            if (!File.Exists(_preferencesFile)) return;
            var preferences = JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile));
            if (preferences is null) return;
            _loadingPreferences = true;
            SelectComboItem(ProjectionThemeBox, preferences.Theme ?? "Noturno");
            SelectComboItem(LineCountBox, (preferences.MaxLines ?? 4).ToString(CultureInfo.InvariantCulture));
            _selectedOutputDeviceName = preferences.OutputDeviceName;
            _nextVideoUrl = YouTubeLink.TryGetVideoId(preferences.NextVideoUrl) is { } queuedId
                ? "https://www.youtube.com/watch?v=" + queuedId : null;
            PlayNextButton.IsEnabled = !string.IsNullOrWhiteSpace(_nextVideoUrl);
            NextTrackText.Text = _nextVideoUrl is null
                ? "Pesquise e prepare a próxima música."
                : "Próxima faixa recuperada da sessão anterior.";
            PopulateOutputDisplays();
            SelectOutputDisplay(preferences.OutputDeviceName);
            _loadingPreferences = false;
            _projection?.SetTheme(preferences.Theme ?? "Noturno");
            _projection?.SetMaxLines(preferences.MaxLines ?? 4);
        }
        catch
        {
            _loadingPreferences = false;
            StatusText.Text = "Preferências não puderam ser carregadas; usando o padrão.";
        }
    }

    private static void SelectComboItem(System.Windows.Controls.ComboBox combo, string value)
    {
        foreach (var candidate in combo.Items.OfType<ComboBoxItem>())
            if (string.Equals(candidate.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            { combo.SelectedItem = candidate; return; }
    }

    private void SavePreferences(string? theme, int? maxLines, string? outputDeviceName = null)
    {
        try
        {
            var current = File.Exists(_preferencesFile)
                ? JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile)) ?? new()
                : new ProjectionPreferences();
            if (theme is not null) current.Theme = theme;
            if (maxLines is not null) current.MaxLines = maxLines;
            if (outputDeviceName is not null) current.OutputDeviceName = outputDeviceName;
            var directory = Path.GetDirectoryName(_preferencesFile)!;
            Directory.CreateDirectory(directory);
            var temp = _preferencesFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _preferencesFile, true);
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível salvar as preferências: " + ex.Message; }
    }

    private sealed class ProjectionPreferences
    {
        public string? Theme { get; set; } = "Noturno";
        public int? MaxLines { get; set; } = 4;
        public string? OutputDeviceName { get; set; }
        public string? NextVideoUrl { get; set; }
    }

    private void SaveNextVideoPreference()
    {
        try
        {
            var current = File.Exists(_preferencesFile)
                ? JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile)) ?? new()
                : new ProjectionPreferences();
            current.NextVideoUrl = _nextVideoUrl;
            var directory = Path.GetDirectoryName(_preferencesFile)!;
            Directory.CreateDirectory(directory);
            var temp = _preferencesFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _preferencesFile, true);
        }
        catch (Exception ex)
        {
            StatusText.Text = "A próxima música não pôde ser salva: " + ex.Message;
        }
    }

    private sealed class OutputDisplayItem
    {
        public string DeviceName { get; init; } = "";
        public string Label { get; init; } = "";
        public override string ToString() => Label;
    }

    private void PopulateOutputDisplays()
    {
        if (OutputDisplayBox is null) return;
        OutputDisplayBox.Items.Clear();
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var label = screen.Primary ? "Principal" : "Secundário";
            OutputDisplayBox.Items.Add(new OutputDisplayItem
            {
                DeviceName = screen.DeviceName,
                Label = $"{label} — {screen.Bounds.Width}×{screen.Bounds.Height}"
            });
        }
        if (OutputDisplayBox.Items.Count == 0)
            OutputDisplayBox.Items.Add(new OutputDisplayItem { DeviceName = "", Label = "Nenhuma tela detectada" });
        if (OutputDisplayBox.SelectedIndex < 0) OutputDisplayBox.SelectedIndex = 0;
    }

    private void SelectOutputDisplay(string? deviceName)
    {
        if (OutputDisplayBox is null) return;
        var match = OutputDisplayBox.Items.OfType<OutputDisplayItem>()
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(deviceName) &&
                string.Equals(item.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        if (match is not null) OutputDisplayBox.SelectedItem = match;
        else if (OutputDisplayBox.Items.Count > 0) OutputDisplayBox.SelectedIndex = Math.Min(1, OutputDisplayBox.Items.Count - 1);
    }

    private void System_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => System_DisplaySettingsChanged(sender, e)));
            return;
        }

        if (!IsLoaded) return;
        var previousLoadingState = _loadingPreferences;
        _loadingPreferences = true;
        PopulateOutputDisplays();
        SelectOutputDisplay(_selectedOutputDeviceName);
        _loadingPreferences = previousLoadingState;

        if (_projection is { IsVisible: true })
        {
            var actualDevice = _projection.ShowOnOutputDisplay(_selectedOutputDeviceName);
            _projection.RestoreCurrentDisplay();
            var actualScreen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(screen =>
                string.Equals(screen.DeviceName, actualDevice, StringComparison.OrdinalIgnoreCase));
            StatusText.Text = actualScreen is null
                ? "A tela de saída foi desconectada. A projeção foi recuperada na tela disponível."
                : "Alteração de monitores detectada. A projeção foi reposicionada e o conteúdo foi restaurado.";
        }
        else
        {
            StatusText.Text = "Monitores atualizados. Confira a tela de saída antes de projetar.";
        }
    }

    private void OutputDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OutputDisplayBox?.SelectedItem is not OutputDisplayItem item || string.IsNullOrWhiteSpace(item.DeviceName)) return;
        _selectedOutputDeviceName = item.DeviceName;
        if (!_loadingPreferences) SavePreferences(null, null, item.DeviceName);
        if (_projection is { IsVisible: true })
        {
            var actual = _projection.ShowOnOutputDisplay(item.DeviceName);
            StatusText.Text = string.Equals(actual, item.DeviceName, StringComparison.OrdinalIgnoreCase)
                ? "Projeção movida para a tela selecionada."
                : "Não foi possível usar a tela escolhida; a projeção foi aberta na tela disponível.";
        }
    }

    private async void RunDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var lines = new List<string>();
        var warnings = new List<string>();

        var hardware = Lyravoxen.Audio.HardwareDiagnostics.Collect();
        lines.Add("HARDWARE E COMPATIBILIDADE");
        lines.Add($"CPU: {hardware.CpuName}");
        lines.Add($"Arquitetura: {hardware.Architecture}; núcleos lógicos: {hardware.LogicalProcessors}");
        lines.Add($"Memória física disponível: {hardware.AvailableMemoryBytes / (1024 * 1024)} MB");
        lines.Add($"Instruções: SSE2={hardware.Sse2}; AVX={hardware.Avx}; AVX2={hardware.Avx2}");
        lines.Add($"Perfil sugerido: {hardware.RecommendedProfile}; threads de IA: {hardware.RecommendedInferenceThreads}");
        lines.Add($"Modelo sugerido para futura transcrição: {Lyravoxen.Audio.PerformanceProfileSettings.GetModelHint(hardware.Tier, hardware.RecommendedProfile)}");
        lines.Add("A reprodução e a projeção não dependem do motor de transcrição.");
        var whisperWindowsSupported = OperatingSystem.IsWindows() && Environment.OSVersion.Version.Build >= 20348;
        lines.Add($"Sistema compatível com o runtime Whisper (Windows 11/Server 2022+): {whisperWindowsSupported}");
        if (!whisperWindowsSupported)
            warnings.Add("O runtime Whisper escolhido exige Windows 11 ou Windows Server 2022. Neste Windows, use letras de catálogo, LRC/SRT ou cadastro manual.");
        lines.Add($"Captura por processo disponível no Windows: {hardware.ProcessLoopbackApiAvailable}");
        if (!hardware.Sse2 && hardware.Architecture == "X64")
            warnings.Add("O processador não reportou SSE2. Alguns motores de IA podem não ser compatíveis.");
        if (!hardware.ProcessLoopbackApiAvailable)
            warnings.Add("Este Windows não oferece captura WASAPI por processo. Reprodução e letras de catálogo continuam disponíveis.");

        bool webViewReady = false;
        try { webViewReady = YoutubePlayer.CoreWebView2 is not null; } catch { }
        lines.Add((webViewReady ? "✓" : "✗") + " Player/WebView2 inicializado");
        if (!webViewReady) warnings.Add("O WebView2 ainda não inicializou. Verifique a instalação do runtime e a conexão.");

        lines.Add((_playerReady ? "✓" : "○") + (_playerReady ? " Player do YouTube respondeu" : " Player do YouTube ainda não confirmou prontidão"));
        if (!_playerReady && _currentVideo is not null) warnings.Add("O vídeo foi carregado, mas o player ainda não confirmou prontidão. Aguarde ou tente recarregar.");

        lines.Add((_currentVideo is not null ? "✓" : "○") + (_currentVideo is not null ? " Música selecionada: " + _currentVideo.Title : " Nenhuma música selecionada"));
        lines.Add((_cues.Count > 0 ? "✓" : "○") + $" Letras sincronizadas: {_cues.Count} frases");
        if (_currentVideo is not null && _cues.Count == 0) warnings.Add("Esta música ainda não tem frases sincronizadas. Importe uma letra ou cadastre os tempos.");

        var screens = System.Windows.Forms.Screen.AllScreens;
        var hasExternalScreen = screens.Any(screen => !screen.Primary);
        var selectedOutput = _selectedOutputDeviceName ?? "seleção automática";
        lines.Add((hasExternalScreen ? "✓" : "○") + $" Monitores detectados: {screens.Length}; saída preferida: {selectedOutput}");
        if (!hasExternalScreen) warnings.Add("Nenhum monitor secundário foi detectado. A projeção ficará na tela principal.");

        var projectionOpen = _projection is { IsVisible: true };
        lines.Add((projectionOpen ? "✓" : "○") + (projectionOpen ? " Janela de projeção aberta" : " Janela de projeção ainda não aberta"));
        if (!projectionOpen) warnings.Add("Abra a projeção e confira se ela aparece na tela correta.");

        try
        {
            await _holyrics.TestConnectionAsync();
            lines.Add("✓ Holyrics respondeu e autorizou ShowQuickPresentation e CloseCurrentQuickPresentation");
        }
        catch (Exception ex)
        {
            lines.Add("✗ Holyrics: " + ex.Message);
            warnings.Add("A integração com o Holyrics não está pronta. Confira API Server Local, IP, porta e token.");
        }

        var report = "DIAGNÓSTICO PRÉ-APRESENTAÇÃO\n\n" + string.Join("\n", lines) +
            (warnings.Count > 0 ? "\n\nPONTOS A VERIFICAR\n• " + string.Join("\n• ", warnings) : "\n\nTudo certo nas verificações disponíveis.");
        StatusText.Text = warnings.Count == 0
            ? "Diagnóstico concluído: verificações disponíveis aprovadas."
            : $"Diagnóstico concluído: {warnings.Count} ponto(s) precisam de atenção.";
        System.Windows.MessageBox.Show(this, report, "Lyravoxen — Diagnóstico", MessageBoxButton.OK,
            warnings.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void OpenProjection_Click(object sender, RoutedEventArgs e)
    {
        EnsureProjectionWindow();
        if (StatusText.Text.StartsWith("Projeção aberta", StringComparison.Ordinal) ||
            StatusText.Text.StartsWith("Nenhum monitor", StringComparison.Ordinal))
            StatusText.Text += " Use Testar projeção para validar o envio de texto.";
    }

    private void EnsureProjectionWindow()
    {
        if (_projection is null || !_projection.IsVisible)
        {
            _projection = new ProjectionWindow();
            var actualDevice = _projection.ShowOnOutputDisplay(_selectedOutputDeviceName);
            var actualScreen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(screen =>
                string.Equals(screen.DeviceName, actualDevice, StringComparison.OrdinalIgnoreCase));
            StatusText.Text = actualScreen is null
                ? "Não foi possível identificar a tela de saída."
                : actualScreen.Primary
                    ? "Projeção aberta na tela principal."
                    : "Projeção aberta na tela secundária selecionada.";
        }
        if (LineCountBox?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var lines))
            _projection.SetMaxLines(lines);
        if (ProjectionThemeBox?.SelectedItem is ComboBoxItem theme)
            _projection.SetTheme(theme.Content?.ToString() ?? "Noturno");
        // Ao abrir/reabrir a saída, recuperar imediatamente a frase que já está no ar.
        _projection.ShowLyric(_currentProjectedLyric);
    }

    private void ProjectionStandby_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureProjectionWindow();
            _projection!.ShowStandby();
            StatusText.Text = "Tela de espera exibida no telão. A frase atual continua preservada.";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível exibir a tela de espera: " + ex.Message; }
    }

    private void ProjectionEnding_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureProjectionWindow();
            _projection!.ShowEnding();
            StatusText.Text = "Tela de encerramento exibida no telão. A frase atual continua preservada.";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível exibir a tela de encerramento: " + ex.Message; }
    }

    private void TestProjection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureProjectionWindow();
            const string testText = "TESTE DE PROJEÇÃO — LYRAVOXEN";
            PublishProjectionOnly(testText);
            _projection!.Activate();
            StatusText.Text = "Teste exibido na prévia e na janela de projeção. Confira o monitor de saída.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Falha ao abrir/enviar texto à projeção: " + ex.Message;
            System.Windows.MessageBox.Show("Não foi possível testar a projeção.\\n\\n" + ex.Message, "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int FindActiveCue(double playbackSeconds)
    {
        // A busca binária não guarda estado: funciona também após seek para trás.
        // A antecipação é aplicada na comparação, sem alterar os tempos salvos.
        return CueTimeline.FindActiveIndex(_cues, playbackSeconds, ProjectionLeadSeconds);
    }

    private string BuildProjectionCueBlock(int activeIndex)
    {
        if (activeIndex < 0 || activeIndex >= _cues.Count) return "";
        var count = 4;
        if (LineCountBox?.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Content?.ToString(), out var selectedCount))
            count = Math.Clamp(selectedCount, 1, 10);
        // Blocos fixos: o bloco anterior desaparece quando o próximo começa.
        var firstIndex = (activeIndex / count) * count;
        return string.Join(Environment.NewLine, _cues.Skip(firstIndex).Take(activeIndex - firstIndex + 1)
            .Select(cue => cue.Text.Trim()).Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private void PublishProjectionOnly(string text)
    {
        var safeText = text ?? "";
        _currentProjectedLyric = safeText;
        if (ProjectionPreviewText is not null)
            ProjectionPreviewText.Text = string.IsNullOrWhiteSpace(safeText) ? "Aguardando a próxima frase..." : safeText;
        var activeIndex = _manualProjectionMode ? _manualCueIndex : _lastAutomaticCue;
        var nextIndex = activeIndex + 1;
        if (NextProjectionPreviewText is not null)
            NextProjectionPreviewText.Text = nextIndex >= 0 && nextIndex < _cues.Count
                ? "Próxima: " + _cues[nextIndex].Text
                : "Próxima: —";
        if (_projection is { IsVisible: true })
            _projection.ShowLyric(safeText);
    }

    private void PublishLyric(string text)
    {
        PublishProjectionOnly(text);
        // Texto vazio fecha a apresentação rápida no Holyrics e libera o próximo takeover.
        if (string.IsNullOrWhiteSpace(text)) _holyricsTakeoverActive = false;
        _ = SendLyricToHolyricsAsync(text);
    }

    private async void MarkCueAtCurrentTime_Click(object sender, RoutedEventArgs e)
    {
        var lyric = LyricTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(lyric)) { if (CueList.SelectedItem is LyricCue selected) lyric = selected.Text; else { StatusText.Text = "Digite uma frase ou selecione uma linha antes de marcar."; return; } }
        try
        {
            double currentSeconds;
            if (_isLocalAudioPlaying) currentSeconds = LocalAudioPlayer.Position.TotalSeconds;
            else { var encoded = await YoutubePlayer.CoreWebView2.ExecuteScriptAsync("typeof player !== 'undefined' && player && typeof player.getCurrentTime === 'function' ? player.getCurrentTime() : null"); if (!double.TryParse(encoded, NumberStyles.Float, CultureInfo.InvariantCulture, out currentSeconds) || currentSeconds < 0) { StatusText.Text = "O player ainda não informou o tempo atual. Inicie a música e tente novamente."; return; } }
            var cueSeconds = Math.Max(0, currentSeconds);
            _cues.RemoveAll(c => string.Equals(c.Text, lyric, StringComparison.OrdinalIgnoreCase));
            _cues.Add(new LyricCue { Seconds = cueSeconds, Text = lyric }); _cues.Sort((a,b) => a.Seconds.CompareTo(b.Seconds));
            CueList.ItemsSource = null; CueList.ItemsSource = _cues;
            if (_currentVideo is not null) { _currentVideo.Cues = _cues.Select(c => new LyricCue { Seconds = c.Seconds, Text = c.Text }).ToList(); foreach (var saved in _library.SelectMany(f => f.Videos).Where(v => v.VideoId == _currentVideo.VideoId && v.IsOffline == _currentVideo.IsOffline)) saved.Cues = _currentVideo.Cues.Select(c => new LyricCue { Seconds = c.Seconds, Text = c.Text }).ToList(); SaveLibrary(); }
            SecondsBox.Text = cueSeconds.ToString("0.00", CultureInfo.InvariantCulture); LyricTextBox.Clear(); _lastAutomaticCue = -1;
            StatusText.Text = "Frase registrada em " + cueSeconds.ToString("0.00", CultureInfo.InvariantCulture) + "s. Ela será projetada 1,5s antes de começar.";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível ler o tempo atual: " + ex.Message; }
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
        SecondsBox.Text = cue.Seconds.ToString("0.00", CultureInfo.InvariantCulture);
        LyricTextBox.Text = cue.Text;
        StatusText.Text = $"Frase selecionada: {cue.Seconds:0.00}s. Use Editar, Atualizar ou Reproduzir para controlar a projeção.";
    }

    private void EditCue_Click(object sender, RoutedEventArgs e)
    {
        if (CueList.SelectedItem is not LyricCue cue)
        {
            StatusText.Text = "Selecione uma frase na lista para editar.";
            return;
        }
        SecondsBox.Text = cue.Seconds.ToString("0.00", CultureInfo.InvariantCulture);
        LyricTextBox.Text = cue.Text;
        LyricTextBox.Focus();
        LyricTextBox.SelectAll();
    }

    private void UpdateCue_Click(object sender, RoutedEventArgs e)
    {
        if (CueList.SelectedItem is not LyricCue selected ||
            !double.TryParse(SecondsBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
            seconds < 0 || string.IsNullOrWhiteSpace(LyricTextBox.Text))
        {
            StatusText.Text = "Selecione uma frase e informe um tempo e texto válidos.";
            return;
        }
        selected.Seconds = seconds;
        selected.Text = LyricTextBox.Text.Trim();
        _cues.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        RefreshCueList(selected);
        PersistCurrentCues();
        _lastAutomaticCue = -1;
        StatusText.Text = "Frase atualizada e salva na biblioteca.";
    }

    private void DeleteCue_Click(object sender, RoutedEventArgs e)
    {
        if (CueList.SelectedItem is not LyricCue selected)
        {
            StatusText.Text = "Selecione uma frase para excluir.";
            return;
        }
        if (System.Windows.MessageBox.Show("Excluir esta frase sincronizada?", "Lyravoxen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _cues.Remove(selected);
        RefreshCueList();
        PersistCurrentCues();
        _lastAutomaticCue = -1;
        StatusText.Text = "Frase excluída.";
    }

    private void MoveCue_Click(object sender, RoutedEventArgs e)
    {
        if (CueList.SelectedItem is not LyricCue selected)
        {
            StatusText.Text = "Selecione uma frase para reordenar.";
            return;
        }
        var index = _cues.IndexOf(selected);
        var direction = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0 ? 1 : -1;
        var target = index + direction;
        if (target < 0 || target >= _cues.Count)
        {
            StatusText.Text = direction < 0 ? "A frase já está no início." : "A frase já está no final.";
            return;
        }
        // A ordem temporal permanece coerente; reordenar significa ajustar o tempo para trocar as posições.
        var other = _cues[target];
        var temp = selected.Seconds;
        selected.Seconds = other.Seconds;
        other.Seconds = temp;
        _cues.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        RefreshCueList(selected);
        PersistCurrentCues();
        _lastAutomaticCue = -1;
        StatusText.Text = direction < 0 ? "Frase movida para cima no tempo." : "Frase movida para baixo no tempo.";
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Não interceptar atalhos enquanto o operador digita ou navega no navegador incorporado.
        if (e.OriginalSource is System.Windows.Controls.TextBox or
            System.Windows.Controls.ComboBox or
            System.Windows.Controls.Primitives.TextBoxBase or
            Microsoft.Web.WebView2.Wpf.WebView2)
            return;

        switch (e.Key)
        {
            case System.Windows.Input.Key.F6:
                NavigateCue(-1);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.F7:
                NavigateCue(1);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.F8:
                ProjectSelectedCue_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case System.Windows.Input.Key.F9:
                ResumeAutomaticProjection_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case System.Windows.Input.Key.F10:
                _projection?.RestoreDisplay();
                StatusText.Text = "Projeção restaurada.";
                e.Handled = true;
                break;
            case System.Windows.Input.Key.F11:
                ProjectionStandby_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case System.Windows.Input.Key.F12:
                ProjectionEnding_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private void PreviousCue_Click(object sender, RoutedEventArgs e) => NavigateCue(-1);
    private void NextCue_Click(object sender, RoutedEventArgs e) => NavigateCue(1);

    private void NavigateCue(int direction)
    {
        if (_cues.Count == 0) { StatusText.Text = "Nenhuma frase cadastrada."; return; }
        _manualProjectionMode = true;
        var current = _manualCueIndex >= 0
            ? _manualCueIndex
            : CueList.SelectedItem is LyricCue selected
                ? _cues.IndexOf(selected)
                : FindActiveCue(GetPlaybackSeconds());
        // Sem frase ativa ainda: "Próxima" começa na primeira; "Anterior" permanece na primeira.
        if (current < 0) current = direction > 0 ? -1 : 0;
        var nextIndex = Math.Clamp(current + direction, 0, _cues.Count - 1);
        if (direction > 0 && CaptureCueTimesCheckBox?.IsChecked == true && nextIndex > 0)
        {
            // Ao avançar durante o playback instrumental, registra o instante real
            // em que a próxima tela deve entrar. Slides repetidos continuam separados.
            _cues[nextIndex].Seconds = GetPlaybackSeconds() + ProjectionLeadSeconds;
            if (_currentVideo is not null)
            {
                _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
                PersistCurrentCues();
            }
        }
        _manualCueIndex = nextIndex;
        CueList.ItemsSource = null;
        CueList.ItemsSource = _cues;
        CueList.SelectedItem = _cues[_manualCueIndex];
        ProjectSelectedCue_Click(this, new RoutedEventArgs());
    }

    private double GetPlaybackSeconds()
    {
        // Use o último relógio recebido pelo evento assíncrono do player.
        // Não bloquear a thread da interface aguardando JavaScript do WebView2.
        if (_isLocalAudioPlaying)
            return Math.Max(0, LocalAudioPlayer.Position.TotalSeconds);
        return Math.Max(0, _lastKnownPlaybackSeconds);
    }

    private void ProjectSelectedCue_Click(object sender, RoutedEventArgs e)
    {
        if (CueList.SelectedItem is not LyricCue cue) { StatusText.Text = "Selecione uma frase para projetar."; return; }
        _manualProjectionMode = true;
        _manualCueIndex = _cues.IndexOf(cue);
        PublishLyric(BuildProjectionCueBlock(_manualCueIndex));
        StatusText.Text = $"Projeção manual: frase {_manualCueIndex + 1} de {_cues.Count}. A sincronização automática está pausada até retomar o modo automático.";
    }

    private void ResumeAutomaticProjection_Click(object sender, RoutedEventArgs e)
    {
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        var currentIndex = FindActiveCue(GetPlaybackSeconds());
        _lastAutomaticCue = currentIndex;
        PublishLyric(BuildProjectionCueBlock(currentIndex));
        StatusText.Text = "Modo automático retomado. A projeção foi alinhada à posição atual da música.";
    }

    private void RefreshCueList(LyricCue? selected = null)
    {
        CueList.ItemsSource = null;
        CueList.ItemsSource = _cues;
        if (selected is not null) CueList.SelectedItem = selected;
    }

    private void PersistCurrentCues()
    {
        if (_currentVideo is null) return;
        _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
        foreach (var saved in _library.SelectMany(folder => folder.Videos)
                     .Where(item => item.VideoId == _currentVideo.VideoId && item.IsOffline == _currentVideo.IsOffline))
            saved.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
        SaveLibrary();
    }


    private void LoadLibrary()
    {
        var backupFile = _libraryFile + ".bak";
        try
        {
            if (File.Exists(_libraryFile))
                _library.AddRange(JsonSerializer.Deserialize<List<LibraryFolder>>(File.ReadAllText(_libraryFile)) ?? new());
            else if (File.Exists(backupFile))
                _library.AddRange(JsonSerializer.Deserialize<List<LibraryFolder>>(File.ReadAllText(backupFile)) ?? new());
        }
        catch
        {
            _library.Clear();
            try
            {
                if (!File.Exists(backupFile)) throw;
                _library.AddRange(JsonSerializer.Deserialize<List<LibraryFolder>>(File.ReadAllText(backupFile)) ?? new());
                StatusText.Text = "A biblioteca principal apresentou erro; os dados foram recuperados da cópia de segurança.";
            }
            catch
            {
                _library.Clear();
                StatusText.Text = "Não foi possível ler a biblioteca nem sua cópia de segurança. Os arquivos originais foram preservados.";
            }
        }
        RefreshFolders();
    }

    private void SaveLibrary()
    {
        try
        {
            var directory = Path.GetDirectoryName(_libraryFile)!;
            Directory.CreateDirectory(directory);
            var tempFile = _libraryFile + ".tmp";
            var backupFile = _libraryFile + ".bak";
            var json = JsonSerializer.Serialize(_library, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempFile, json);
            // Só substitui o arquivo principal depois que a nova versão foi gravada por completo.
            if (File.Exists(_libraryFile))
                File.Copy(_libraryFile, backupFile, true);
            File.Move(tempFile, _libraryFile, true);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível salvar a biblioteca; o arquivo anterior foi preservado: " + ex.Message;
        }
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
            PublishLyric("");
            YoutubePlayer.Visibility = Visibility.Collapsed;
            LocalAudioPlayer.Visibility = Visibility.Visible;
            LocalAudioPlayer.Source = new Uri(video.LocalPath);
            LocalAudioPlayer.Position = TimeSpan.Zero;
            LocalAudioPlayer.Play();
            _isLocalAudioPlaying = true;
            _lastAutomaticCue = -1;
            _manualProjectionMode = false; _manualCueIndex = -1; _lastKnownPlaybackSeconds = 0;
            _audioTimer.Start();
            _currentVideo = video;
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(video.Cues));
            CueList.ItemsSource = null; CueList.ItemsSource = _cues;
            StatusText.Text = $"Reproduzindo offline: {video.Title} ({video.Format?.ToUpperInvariant()})";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível reproduzir o áudio: " + ex.Message; }
    }

    private void ProcessAudioCue()
    {
        if (!_isLocalAudioPlaying || _cues.Count == 0 || _manualProjectionMode) return;
        var seconds = LocalAudioPlayer.Position.TotalSeconds;
        _lastKnownPlaybackSeconds = seconds;
        var cueIndex = FindActiveCue(seconds);
        if (cueIndex == _lastAutomaticCue) return;
        _lastAutomaticCue = cueIndex;
        PublishLyric(BuildProjectionCueBlock(cueIndex));
    }

    private void LocalAudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        _audioTimer.Stop();
        _isLocalAudioPlaying = false;
        PublishLyric("");
        _lastAutomaticCue = -1;
        StatusText.Text = "Áudio offline finalizado.";
    }

    private void LocalAudioPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _audioTimer.Stop();
        _isLocalAudioPlaying = false;
        PublishLyric("");
        _lastAutomaticCue = -1;
        var detail = e.ErrorException?.Message ?? "Falha desconhecida ao abrir o arquivo.";
        StatusText.Text = "Não foi possível decodificar o áudio: " + detail;
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