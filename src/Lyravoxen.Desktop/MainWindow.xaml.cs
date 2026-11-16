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
    private string _currentProjectedLyric = "";
    private int _maxProjectionLines = 4;
    private long _holyricsUpdateVersion;
    private readonly object _holyricsQueueSync = new();
    private Task _holyricsSendQueue = Task.CompletedTask;
    private readonly List<LyricCue> _cues = new();
    private List<string> _manualDraftLines = new();
    private int _manualDraftIndex;
    private bool _manualDraftActive;
    private readonly List<VideoHistoryItem> _history = new();
    private readonly List<LibraryFolder> _library = new();
    private LibraryVideo? _currentVideo;
    private readonly DispatcherTimer _audioTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(80) };
    private bool _isLocalAudioPlaying;
    private bool _playbackEnded;
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
    private readonly string _playerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Player");
    private readonly string _historyFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "video-history.json");
    private readonly string _libraryFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "library.json");
    private readonly string _preferencesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "preferences.json");
    private bool _loadingPreferences;
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
                await InitializeBrowserViewAsync(YoutubeSearchView);
                LoadPreferences();
                YoutubeSearchView.CoreWebView2.Navigate("https://www.youtube.com/");
                _searchWebViewReady = true;
                StatusText.Text = "Player pronto. Use a aba Próxima música para pesquisar e preparar a próxima faixa.";

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
        view.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            if (ReferenceEquals(ActiveBrowserView, view) && view.Source is not null)
                Dispatcher.Invoke(() => YoutubeSearchBox.Text = view.Source.ToString());
        };
    }

    private async void NewBrowserTab_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var view = new Microsoft.Web.WebView2.Wpf.WebView2();
            var tab = new TabItem { Header = "YouTube " + (++_browserTabNumber), Content = view };
            BrowserTabs.Items.Add(tab);
            BrowserTabs.SelectedItem = tab;
            await InitializeBrowserViewAsync(view);
            view.CoreWebView2.Navigate("https://www.youtube.com/");
            _searchWebViewReady = true;
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível abrir nova aba: " + ex.Message; }
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
        if (YouTubeLink.TryGetVideoId(text) is not null) view.CoreWebView2.Navigate(text);
        else view.CoreWebView2.Navigate("https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(text));
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

    private async void OpenQuickSite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string site) return;
        var songQuery = string.Join(" ", new[] { MusicTrackBox.Text.Trim(), MusicArtistBox.Text.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var target = site switch
        {
            "youtube" => string.IsNullOrWhiteSpace(songQuery) ? "https://www.youtube.com/" :
                "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(songQuery),
            "google" => string.IsNullOrWhiteSpace(songQuery) ? "https://www.google.com/" :
                "https://www.google.com/search?q=" + Uri.EscapeDataString(songQuery + " letra"),
            "cifraclub" => "https://www.cifraclub.com.br/",
            "vagalume" => "https://www.vagalume.com.br/",
            "letras" => "https://www.letras.mus.br/",
            "ouvir" => "https://www.ouvirmusica.com.br/",
            "harpa" => "https://www.google.com/search?q=" + Uri.EscapeDataString("Harpa Cristã hinos letras"),
            _ => "https://www.google.com/"
        };
        try
        {
            var header = site switch
            {
                "youtube" => "YouTube",
                "google" => "Google",
                "cifraclub" => "Cifra Club",
                "vagalume" => "Vagalume",
                "letras" => "Letras.mus.br",
                "ouvir" => "Ouvir Música",
                _ => "Harpa Cristã"
            };
            var tab = BrowserTabs.Items.OfType<TabItem>()
                .FirstOrDefault(item => string.Equals(item.Header?.ToString(), header, StringComparison.Ordinal));
            Microsoft.Web.WebView2.Wpf.WebView2 view;
            if (tab?.Content is Microsoft.Web.WebView2.Wpf.WebView2 existing)
            {
                view = existing;
                if (view.CoreWebView2 is null) await InitializeBrowserViewAsync(view);
            }
            else
            {
                view = new Microsoft.Web.WebView2.Wpf.WebView2();
                tab = new TabItem { Header = header, Content = view, Tag = "quick-site" };
                BrowserTabs.Items.Add(tab);
                await InitializeBrowserViewAsync(view);
            }
            view.CoreWebView2.Navigate(target);
            BrowserTabs.SelectedItem = tab;
            StatusText.Text = header + " aberto dentro do Lyravoxen.";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível abrir " + site + ": " + ex.Message; }
    }

    private void CopyBrowserLink_Click(object sender, RoutedEventArgs e)
    {
        var url = ActiveBrowserView?.Source?.ToString();
        if (string.IsNullOrWhiteSpace(url)) { StatusText.Text = "Nenhum endereço disponível nesta aba."; return; }
        try { System.Windows.Clipboard.SetText(url); StatusText.Text = "Link copiado: " + url; }
        catch (Exception ex) { StatusText.Text = "Não foi possível copiar o link: " + ex.Message; }
    }

    private void CopyCurrentVideoLink_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo?.IsOffline == true)
        {
            StatusText.Text = "A música atual é um arquivo offline e não possui link do YouTube.";
            return;
        }
        var url = _currentVideo?.Url;
        if (string.IsNullOrWhiteSpace(url)) url = YoutubeUrlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) { StatusText.Text = "Carregue uma música antes de copiar o link."; return; }
        try { System.Windows.Clipboard.SetText(url); StatusText.Text = "Link da música atual copiado."; }
        catch (Exception ex) { StatusText.Text = "Não foi possível copiar o link: " + ex.Message; }
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
        WorkspaceTabs.SelectedIndex = 0;
        if (!await LoadVideoAsync(url, true))
        {
            _pendingQueueVideoId = null;
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
            _playerSessionId = Guid.NewGuid().ToString("N");
            _playbackEnded = false;
            _manualDraftActive = false;
            _manualDraftLines.Clear();
            _manualDraftIndex = 0;
            ManualLyricsBox.Clear();
            MusicTrackBox.Clear();
            MusicArtistBox.Clear();
            StatusText.Text = "Preparando vídeo e procurando letra já cadastrada...";
            PublishProjectionOnly("");
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
            MusicTrackBox.Text = _currentVideo.TrackName;
            MusicArtistBox.Text = _currentVideo.Artist;
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(_currentVideo.Cues));
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
            var html = YouTubePlayerPage.Build(videoId, _playerSessionId);
            await File.WriteAllTextAsync(Path.Combine(_playerFolder, "index.html"), html);
            YoutubePlayer.CoreWebView2.Navigate("https://lyravoxen.local/index.html");
            YoutubeUrlBox.Text = url;
            await AddVideoToHistoryAsync(videoId, url, addToHistory);
            // O título salvo no histórico pode conter também o nome do canal.
            // A busca usa o título puro do vídeo; o intérprete é informado separadamente.
            StatusText.Text = _cues.Count > 0
                ? $"Player preparado com {_cues.Count} frases. A letra acompanhará a reprodução."
                : "Player preparado. Cadastre a letra e os tempos para sincronização automática.";
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

    private void OpenManualLyricsEditor(string? lyricsToEdit = null)
    {
        if (lyricsToEdit is not null)
            ManualLyricsBox.Text = lyricsToEdit;
        WorkspaceTabs.SelectedItem = ManualSyncTab;
        ManualLyricsBox.Focus();
        StatusText.Text = "Modo Criar / editar letra aberto. Cole ou ajuste a letra; depois use Controles manuais para marcar os tempos e associar as frases à música atual.";
    }

    private async void SearchLyrics_Click(object sender, RoutedEventArgs e)
    {
        var track = MusicTrackBox.Text.Trim();
        var artist = MusicArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track) || string.IsNullOrWhiteSpace(artist))
        {
            System.Windows.MessageBox.Show("Informe o título da música e o nome do intérprete para evitar carregar a letra de outra faixa.", "Buscar letra", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await TryLoadAutomaticLyricsAsync(track, artist);
    }

    private async void OpenLyricsSourceSearches_Click(object sender, RoutedEventArgs e)
    {
        var track = MusicTrackBox.Text.Trim();
        var artist = MusicArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track))
        {
            System.Windows.MessageBox.Show("Informe ao menos o título da música. Para hinos, use Harpa Cristã como intérprete quando for o caso.", "Outras fontes de letras", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var searchTerms = string.IsNullOrWhiteSpace(artist) ? "\"" + track + "\"" : "\"" + track + "\" \"" + artist + "\"";
        var sources = new[]
        {
            (Name: "Cifra Club", Domain: "cifraclub.com.br"),
            (Name: "Vagalume", Domain: "vagalume.com.br"),
            (Name: "Letras.mus.br", Domain: "letras.mus.br"),
            (Name: "Ouvir Música", Domain: "ouvirmusica.com.br"),
            (Name: "Harpa Cristã", Domain: ""),
            (Name: "Busca ampla", Domain: "")
        };

        StatusText.Text = "Abrindo fontes alternativas. Confira a autoria e a versão antes de usar qualquer letra.";
        TabItem? firstSourceTab = null;
        foreach (var source in sources)
        {
            try
            {
                var query = source.Name == "Harpa Cristã"
                    ? searchTerms + " \"Harpa Cristã\" hino letra"
                    : string.IsNullOrWhiteSpace(source.Domain)
                        ? searchTerms + " letra música hino louvor"
                        : "site:" + source.Domain + " " + searchTerms;
                var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(query);

                // Reutiliza uma aba por fonte. Evita criar WebViews indefinidamente
                // quando o usuário pesquisa várias músicas durante a apresentação.
                var tab = BrowserTabs.Items.OfType<TabItem>()
                    .FirstOrDefault(item => string.Equals(item.Header?.ToString(), source.Name, StringComparison.Ordinal));
                Microsoft.Web.WebView2.Wpf.WebView2 view;
                if (tab?.Content is Microsoft.Web.WebView2.Wpf.WebView2 existingView)
                {
                    view = existingView;
                    if (view.CoreWebView2 is null)
                        await InitializeBrowserViewAsync(view);
                }
                else
                {
                    view = new Microsoft.Web.WebView2.Wpf.WebView2();
                    tab = new TabItem { Header = source.Name, Content = view, Tag = "lyrics-source" };
                    BrowserTabs.Items.Add(tab);
                    await InitializeBrowserViewAsync(view);
                }

                view.CoreWebView2.Navigate(url);
                firstSourceTab ??= tab;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Não foi possível abrir a fonte " + source.Name + ": " + ex.Message;
            }
        }

        WorkspaceTabs.SelectedIndex = 2;
        if (firstSourceTab is not null)
            BrowserTabs.SelectedItem = firstSourceTab;
        StatusText.Text = "Pesquisas abertas em Cifra Club, Vagalume, Letras.mus.br, Ouvir Música, Harpa Cristã e busca ampla. As abas de cada fonte são reutilizadas para evitar acúmulo. Confira a letra e importe o LRC/SRT para sincronizar.";
    }

    private void StartLiveManualSync_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo e deixe a reprodução começar.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "Inicie a reprodução da música antes de começar a marcação ao vivo. Não é necessário reiniciar o vídeo.";
            return;
        }

        _manualDraftLines = ManualLyricsBox.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (_manualDraftLines.Count == 0)
        {
            StatusText.Text = "Cole a letra, separando as frases por linha, antes de iniciar.";
            return;
        }

        _manualDraftIndex = 0;
        _manualDraftActive = true;
        _manualProjectionMode = false;
        LyricTextBox.Text = _manualDraftLines[0];
        SecondsBox.Text = "0.00";
        ManualSyncProgressText.Text = "Sincronização ao vivo iniciada: frase 1 de " + _manualDraftLines.Count + ". Marque cada entrada enquanto a música toca.";
        ManualCurrentLineText.Text = _manualDraftLines[0];
        StatusText.Text = "Sincronização ao vivo iniciada. A música continua tocando; use Marcar próxima frase no tempo atual.";
    }

    private void MarkNextManualDraftLine_Click(object sender, RoutedEventArgs e)
    {
        if (!_manualDraftActive)
        {
            StatusText.Text = "Primeiro clique em Iniciar sincronização ao vivo.";
            return;
        }
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "A música está pausada. Retome a reprodução antes de marcar a próxima frase.";
            return;
        }
        MarkCueAtCurrentTime_Click(sender, e);
    }

    private async void ManualSync_Click(object sender, RoutedEventArgs e)
    {
        var pastedLyrics = ManualLyricsBox.Text.Trim();
        var track = MusicTrackBox.Text.Trim();
        var artist = MusicArtistBox.Text.Trim();

        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo.";
            return;
        }
        if (string.IsNullOrWhiteSpace(track) || string.IsNullOrWhiteSpace(artist))
        {
            StatusText.Text = "Informe o título e o autor/intérprete na aba Ao vivo antes de sincronizar.";
            WorkspaceTabs.SelectedIndex = 0;
            MusicTrackBox.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(pastedLyrics))
        {
            StatusText.Text = "Cole a letra completa no campo antes de sincronizar.";
            return;
        }

        await TryLoadAutomaticLyricsAsync(track, artist, pastedLyrics);
    }

    private static double CalculateLyricMatch(string expectedLyrics, IEnumerable<LyricCue> timedCues)
    {
        static Dictionary<string, int> CountWords(string value) =>
            Regex.Matches(NormalizeMusicText(value), @"[a-z0-9]+")
                .Select(match => match.Value)
                .Where(word => word.Length > 1)
                .GroupBy(word => word)
                .ToDictionary(group => group.Key, group => group.Count());

        var expected = CountWords(expectedLyrics);
        var actual = CountWords(string.Join(" ", timedCues.Select(cue => cue.Text)));
        var total = expected.Values.Sum();
        if (total == 0) return 0;

        var matched = expected.Sum(pair =>
            Math.Min(pair.Value, actual.TryGetValue(pair.Key, out var count) ? count : 0));
        return matched / (double)total;
    }

    private async Task TryLoadAutomaticLyricsAsync(string trackName, string artistName, string? expectedLyrics = null)
    {
        try
        {
            var normalizedTrack = NormalizeMusicText(trackName);
            var normalizedArtist = NormalizeMusicText(artistName);
            var localMatch = expectedLyrics is null ? _library.SelectMany(folder => folder.Videos)
                .Where(video => video.Cues.Count > 0 && !string.IsNullOrWhiteSpace(video.TrackName) && !string.IsNullOrWhiteSpace(video.Artist))
                .FirstOrDefault(video =>
                    NormalizeMusicText(video.TrackName) == normalizedTrack &&
                    NormalizeMusicText(video.Artist) == normalizedArtist) : null;

            if (localMatch is not null)
            {
                var localPreview = string.Join(Environment.NewLine, localMatch.Cues.Take(8).Select(cue => cue.Text));
                var localDetails = "Letra salva neste computador.\nFaixa: " + localMatch.TrackName +
                    "\nAutor / intérprete: " + localMatch.Artist +
                    "\nFrases sincronizadas: " + localMatch.Cues.Count +
                    "\nVídeo original: " + localMatch.Title +
                    "\n\nPrévia:\n" + localPreview +
                    "\n\nSim: reutilizar esta letra.\nNão: procurar outra versão no catálogo online.\nCriar: editar ou cadastrar outra letra.";
                var localDialog = new MusicMatchDialog("Encontrei esta música na sua biblioteca", localDetails) { Owner = this };
                localDialog.ShowDialog();
                if (localDialog.Choice == MusicMatchChoice.Create)
                {
                    OpenManualLyricsEditor(expectedLyrics);
                    return;
                }
                if (localDialog.Choice == MusicMatchChoice.Yes)
                {
                    _cues.Clear();
                    _cues.AddRange(CueTimeline.Normalize(localMatch.Cues));
                    CueList.ItemsSource = null;
                    CueList.ItemsSource = _cues;
                    _lastAutomaticCue = -1;
                    PersistCurrentCues();
                    StatusText.Text = "Letra recuperada da biblioteca local: " + localMatch.TrackName + " — " + localMatch.Artist;
                    return;
                }
            }

            StatusText.Text = "Buscando letras e verificando correspondência: " + trackName + " — " + artistName;
            var searchItems = new List<JsonElement>();
            var searchQueries = new[] { trackName + " " + artistName, trackName }
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var searchTerm in searchQueries)
            {
                var endpoint = "https://lrclib.net/api/search?q=" + Uri.EscapeDataString(searchTerm);
                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.UserAgent.ParseAdd("Lyravoxen/1.0 (https://github.com/jabesguitar/Lyravoxen)");
                using var response = await MetadataHttp.SendAsync(request);
                if (!response.IsSuccessStatusCode) continue;
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.ValueKind != JsonValueKind.Array) continue;
                searchItems.AddRange(json.RootElement.EnumerateArray().Select(item => item.Clone()));
            }
            if (searchItems.Count == 0)
            {
                StatusText.Text = "O catálogo de letras não retornou resultados. Você pode abrir as outras fontes ou criar a sincronização ao vivo.";
                return;
            }

            var wantedTrack = NormalizeMusicText(trackName);
            var wantedArtist = NormalizeMusicText(artistName);
            var candidates = new List<(double Score, double LyricMatch, string Track, string Artist, string Album, List<LyricCue> Cues)>();
            foreach (var item in searchItems)
            {
                var foundTrack = item.TryGetProperty("trackName", out var tp) ? tp.GetString() ?? "" : "";
                var foundArtist = item.TryGetProperty("artistName", out var ap) ? ap.GetString() ?? "" : "";
                var foundAlbum = item.TryGetProperty("albumName", out var alp) ? alp.GetString() ?? "" : "";
                var normTrack = NormalizeMusicText(foundTrack);
                var normArtist = NormalizeMusicText(foundArtist);

                // Aceita pequenas variações de título e créditos (ex.: "ao vivo", "feat."),
                // mas sempre mostra a faixa encontrada para confirmação antes de aplicar.
                var trackMatches = normTrack == wantedTrack ||
                    ContainsMusicPhrase(normTrack, wantedTrack) ||
                    ContainsMusicPhrase(wantedTrack, normTrack);
                var artistMatches = normArtist == wantedArtist ||
                    ContainsMusicPhrase(normArtist, wantedArtist) ||
                    ContainsMusicPhrase(wantedArtist, normArtist);
                if (!trackMatches || !artistMatches)
                    continue;
                if (!item.TryGetProperty("syncedLyrics", out var synced) || synced.ValueKind != JsonValueKind.String)
                    continue;
                var lrc = synced.GetString();
                if (string.IsNullOrWhiteSpace(lrc)) continue;
                var imported = ParseTimedLyrics(lrc, ".lrc").OrderBy(c => c.Seconds).ToList();
                if (imported.Count < 2) continue;

                var lyricMatch = expectedLyrics is null ? 1d : CalculateLyricMatch(expectedLyrics, imported);
                // A letra colada precisa corresponder substancialmente à versão temporizada.
                // Sem essa validação, não substituímos a sincronização atual.
                if (expectedLyrics is not null && lyricMatch < 0.72d)
                    continue;

                var duration = item.TryGetProperty("duration", out var durationElement) &&
                    durationElement.ValueKind == JsonValueKind.Number ? durationElement.GetDouble() : 0;
                var score = 100d - (normTrack == wantedTrack ? 0 : 12) - (normArtist == wantedArtist ? 0 : 18);
                if (duration > 0 && _playerDurationSeconds > 0)
                {
                    var difference = Math.Abs(duration - _playerDurationSeconds);
                    score -= Math.Min(35, difference / Math.Max(1, _playerDurationSeconds) * 100);
                }
                candidates.Add((score, lyricMatch, foundTrack, foundArtist, foundAlbum, imported));
            }

            if (candidates.Count == 0)
            {
                if (expectedLyrics is not null)
                {
                    var noTimedMatch = new MusicMatchDialog(
                        "Não encontrei uma versão sincronizada compatível",
                        "A letra foi recebida, mas não encontrei tempos que correspondam a ela.\n\nSim: abrir outras fontes para procurar uma versão temporizada.\nNão: manter a letra atual sem alterações.\nCriar: sincronizar esta letra ao vivo com a música que já está tocando.") { Owner = this };
                    noTimedMatch.ShowDialog();
                    if (noTimedMatch.Choice == MusicMatchChoice.Create)
                        OpenManualLyricsEditor(expectedLyrics);
                    else if (noTimedMatch.Choice == MusicMatchChoice.Yes)
                        OpenLyricsSourceSearches_Click(this, new RoutedEventArgs());
                    else
                        StatusText.Text = "A sincronização atual foi preservada. Você pode iniciar a marcação ao vivo quando quiser.";
                    return;
                }

                var noMatchDialog = new MusicMatchDialog(
                    "Não encontrei esta letra sincronizada",
                    "Música: " + trackName + "\nAutor / intérprete: " + artistName +
                    "\n\nNão há uma versão temporizada compatível no catálogo online nem na biblioteca local.\n\nSim: abrir outras fontes de pesquisa.\nNão: deixar para depois.\nCriar: cadastrar a letra e marcar os tempos manualmente.") { Owner = this };
                noMatchDialog.ShowDialog();
                if (noMatchDialog.Choice == MusicMatchChoice.Create)
                    OpenManualLyricsEditor();
                else if (noMatchDialog.Choice == MusicMatchChoice.Yes)
                    OpenLyricsSourceSearches_Click(this, new RoutedEventArgs());
                else
                    StatusText.Text = "Nenhuma letra foi alterada. Você pode pesquisar novamente ou criar a letra quando quiser.";
                return;
            }

            var ordered = expectedLyrics is null
                ? candidates.OrderByDescending(candidate => candidate.Score).ToList()
                : candidates.OrderByDescending(candidate => candidate.LyricMatch).ThenByDescending(candidate => candidate.Score).ToList();
            var selected = ordered[0];
            var preview = string.Join(Environment.NewLine, selected.Cues.Take(8).Select(c => c.Text));
            var durationNote = _playerDurationSeconds > 0
                ? "\nDuração do vídeo: " + TimeSpan.FromSeconds(_playerDurationSeconds).ToString(@"mm\:ss")
                : "";
            var albumNote = string.IsNullOrWhiteSpace(selected.Album) ? "" : "\nÁlbum: " + selected.Album;
            var matchNote = expectedLyrics is null ? "" : "\nCorrespondência da letra colada: " + selected.LyricMatch.ToString("P0", CultureInfo.InvariantCulture);
            var dialogDetails =
                "Vídeo carregado: " + (_currentVideo?.Title ?? "") +
                "\nFaixa encontrada: " + selected.Track + "\nAutor / intérprete: " + selected.Artist + albumNote + durationNote + matchNote +
                "\n\nPrévia da letra sincronizada:\n" + preview +
                "\n\nSim: usar esta letra e seus tempos.\nNão: manter a letra atual.\nCriar: inserir ou editar a letra para esta música.";
            var matchDialog = new MusicMatchDialog("Esta é a música correta?", dialogDetails) { Owner = this };
            matchDialog.ShowDialog();
            if (matchDialog.Choice == MusicMatchChoice.Create)
            {
                OpenManualLyricsEditor(expectedLyrics);
                return;
            }
            if (matchDialog.Choice != MusicMatchChoice.Yes)
            {
                StatusText.Text = "Letra não aplicada. Nenhuma frase foi substituída.";
                return;
            }

            _manualDraftActive = false;
            _manualDraftLines.Clear();
            _manualDraftIndex = 0;
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(selected.Cues));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            PersistCurrentCues();
            StatusText.Text = "Letra sincronizada confirmada: " + selected.Track + " — " + selected.Artist + ". Confira a sincronia com a gravação.";
        }
        catch (TaskCanceledException) { StatusText.Text = "A busca de letras excedeu o tempo limite."; }
        catch (HttpRequestException ex) { StatusText.Text = "Falha na busca de letras: " + ex.Message; }
        catch (JsonException) { StatusText.Text = "Resposta inválida do catálogo de letras."; }
        catch (InvalidOperationException ex) { StatusText.Text = "Não foi possível interpretar os dados da letra: " + ex.Message; }
    }

    private async Task AddVideoToHistoryAsync(string videoId, string url, bool saveToHistory)
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
            // Preserve metadata corrected by the user; use YouTube oEmbed only as a first suggestion.
            if (string.IsNullOrWhiteSpace(_currentVideo.TrackName))
                _currentVideo.TrackName = title;
            if (string.IsNullOrWhiteSpace(_currentVideo.Artist))
                _currentVideo.Artist = author;
            MusicTrackBox.Text = _currentVideo.TrackName;
            MusicArtistBox.Text = _currentVideo.Artist;
        }
        if (!saveToHistory) return;

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
                Dispatcher.Invoke(() =>
                {
                    if (playerState == 0) EndPlayback("Reprodução do YouTube finalizada. Projeção encerrada automaticamente.");
                    else StatusText.Text = playerState == 1 ? "YouTube tocando." : playerState == 2 ? "YouTube pausado." : "YouTube aguardando.";
                });
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
                if (state == 1) Dispatcher.Invoke(() => StatusText.Text = !_playerReady ? "YouTube tocando; aguardando confirmação do player." : _cues.Count > 0 ? "YouTube tocando. Sincronização de letras ativa." : "YouTube tocando. Nenhuma letra sincronizada foi carregada.");
                else if (state == 2) Dispatcher.Invoke(() => StatusText.Text = "YouTube pausado.");
                else if (state == 0) Dispatcher.Invoke(() =>
                    EndPlayback("Reprodução do YouTube finalizada. Projeção encerrada automaticamente."));
            }
            if (state != 1 || _cues.Count == 0 || _manualProjectionMode) return;
            var cueIndex = FindActiveCue(seconds);
            if (cueIndex == _lastAutomaticCue) return;
            _lastAutomaticCue = cueIndex;
            PublishCueBlock(cueIndex);
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }

    private void QueueHolyricsUpdate(string text)
    {
        var version = Interlocked.Increment(ref _holyricsUpdateVersion);
        lock (_holyricsQueueSync)
        {
            _holyricsSendQueue = _holyricsSendQueue.ContinueWith(async _ =>
            {
                if (version != Interlocked.Read(ref _holyricsUpdateVersion)) return;
                await _holyrics.ShowTextAsync(text);
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            _ = ObserveHolyricsQueueAsync(_holyricsSendQueue);
        }
    }

    private async Task ObserveHolyricsQueueAsync(Task queuedSend)
    {
        try { await queuedSend; }
        catch (Exception ex)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                StatusText.Text = "Falha ao enviar a letra ao Holyrics: " + ex.Message));
        }
    }


    private async void HolyricsSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HolyricsSettingsWindow(_holyrics) { Owner = this };
        dialog.ShowDialog();
        if (dialog.Saved) StatusText.Text = "Configuração do Holyrics salva. A conexão foi testada.";
    }

    private void LineCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LineCountBox?.SelectedItem is not ComboBoxItem item ||
            !int.TryParse(item.Content?.ToString(), out var count)) return;
        _maxProjectionLines = Math.Clamp(count, 1, 10);
        if (!_loadingPreferences) SavePreferences(count);
        RefreshCurrentProjectionBlock();
    }

    private void LoadPreferences()
    {
        try
        {
            if (!File.Exists(_preferencesFile)) return;
            var preferences = JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile));
            if (preferences is null) return;
            _loadingPreferences = true;
            _maxProjectionLines = Math.Clamp(preferences.MaxLines ?? 4, 1, 10);
            SelectComboItem(LineCountBox, _maxProjectionLines.ToString(CultureInfo.InvariantCulture));
            _nextVideoUrl = YouTubeLink.TryGetVideoId(preferences.NextVideoUrl) is { } queuedId
                ? "https://www.youtube.com/watch?v=" + queuedId : null;
            PlayNextButton.IsEnabled = !string.IsNullOrWhiteSpace(_nextVideoUrl);
            NextTrackText.Text = _nextVideoUrl is null
                ? "Pesquise e prepare a próxima música."
                : "Próxima faixa recuperada da sessão anterior.";
            _loadingPreferences = false;
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

    private void SavePreferences(int? maxLines)
    {
        try
        {
            var current = File.Exists(_preferencesFile)
                ? JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile)) ?? new()
                : new ProjectionPreferences();
            if (maxLines is not null) current.MaxLines = maxLines;
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
        public int? MaxLines { get; set; } = 4;
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

    private async void ExitProjection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _holyrics.CloseProjectionAsync();
            StatusText.Text = "Projeção do Holyrics encerrada. O Lyravoxen permanece aberto para a próxima música.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível encerrar a projeção do Holyrics: " + ex.Message;
        }
    }

    private int FindActiveCue(double playbackSeconds)
    {
        // A busca binária não guarda estado: funciona também após seek para trás.
        // A antecipação é aplicada na comparação, sem alterar os tempos salvos.
        return CueTimeline.FindActiveIndex(_cues, playbackSeconds, ProjectionLeadSeconds);
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
    }

    private void PublishCueBlock(int activeCueIndex)
    {
        var block = CueTimeline.BuildProjectionBlock(_cues, activeCueIndex, _maxProjectionLines);
        PublishLyric(block);
    }

    private void RefreshCurrentProjectionBlock()
    {
        if (_cues.Count == 0) return;
        var activeIndex = _manualProjectionMode ? _manualCueIndex : FindActiveCue(GetPlaybackSeconds());
        if (activeIndex < 0) return;
        _lastAutomaticCue = activeIndex;
        PublishCueBlock(activeIndex);
    }

    private void PublishLyric(string text)
    {
        PublishProjectionOnly(text);
        if (!string.IsNullOrWhiteSpace(text))
            QueueHolyricsUpdate(text);
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
            if (!_manualDraftActive)
                _cues.RemoveAll(c => string.Equals(c.Text, lyric, StringComparison.OrdinalIgnoreCase));
            var addedCue = new LyricCue { Seconds = cueSeconds, Text = lyric };
            _cues.Add(addedCue);
            _cues.Sort((a,b) => a.Seconds.CompareTo(b.Seconds));
            if (_manualDraftActive)
                PublishCueBlock(_cues.IndexOf(addedCue));
            CueList.ItemsSource = null; CueList.ItemsSource = _cues;
            PersistCurrentCues();
            SecondsBox.Text = cueSeconds.ToString("0.00", CultureInfo.InvariantCulture);
            _lastAutomaticCue = -1;
            if (_manualDraftActive)
            {
                _manualDraftIndex++;
                if (_manualDraftIndex < _manualDraftLines.Count)
                {
                    LyricTextBox.Text = _manualDraftLines[_manualDraftIndex];
                    ManualCurrentLineText.Text = _manualDraftLines[_manualDraftIndex];
                    ManualSyncProgressText.Text = $"Sincronização ao vivo: frase {_manualDraftIndex + 1} de {_manualDraftLines.Count}. Última marcação: {cueSeconds:0.00}s.";
                    StatusText.Text = $"Frase registrada em {cueSeconds:0.00}s. Próxima frase ({_manualDraftIndex + 1}/{_manualDraftLines.Count}) carregada.";
                }
                else
                {
                    _manualDraftActive = false;
                    LyricTextBox.Clear();
                    ManualCurrentLineText.Text = "Sincronização concluída. A letra está salva na biblioteca local.";
                    ManualSyncProgressText.Text = $"Concluído: {_manualDraftLines.Count} frases sincronizadas e salvas para {MusicTrackBox.Text}.";
                    StatusText.Text = $"Letra concluída: {_manualDraftLines.Count} frases foram marcadas e associadas à música.";
                }
            }
            else
            {
                LyricTextBox.Clear();
                StatusText.Text = "Frase registrada em " + cueSeconds.ToString("0.00", CultureInfo.InvariantCulture) + "s. Ela será projetada 1,5s antes de começar.";
            }
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
        PersistCurrentCues();
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

            _manualDraftActive = false;
            _manualDraftLines.Clear();
            _manualDraftIndex = 0;
            _cues.Clear();
            _cues.AddRange(imported.OrderBy(cue => cue.Seconds));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            PersistCurrentCues();
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
        _manualCueIndex = Math.Clamp(current + direction, 0, _cues.Count - 1);
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
        PublishCueBlock(_manualCueIndex);
        StatusText.Text = $"Projeção manual: frase {_manualCueIndex + 1} de {_cues.Count}. A sincronização automática está pausada até retomar o modo automático.";
    }

    private void ResumeAutomaticProjection_Click(object sender, RoutedEventArgs e)
    {
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        var currentIndex = FindActiveCue(GetPlaybackSeconds());
        _lastAutomaticCue = currentIndex;
        PublishCueBlock(currentIndex);
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
        _currentVideo.TrackName = MusicTrackBox.Text.Trim();
        _currentVideo.Artist = MusicArtistBox.Text.Trim();
        _currentVideo.Cues = _cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();

        var savedItems = _library.SelectMany(folder => folder.Videos)
            .Where(item => item.IsOffline == _currentVideo.IsOffline &&
                (item.IsOffline
                    ? string.Equals(item.LocalPath, _currentVideo.LocalPath, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(item.VideoId, _currentVideo.VideoId, StringComparison.Ordinal)))
            .ToList();
        if (savedItems.Count == 0)
        {
            var folder = _library.FirstOrDefault(item =>
                string.Equals(item.Name, "Músicas sincronizadas", StringComparison.OrdinalIgnoreCase));
            if (folder is null)
            {
                folder = new LibraryFolder { Name = "Músicas sincronizadas" };
                _library.Add(folder);
                RefreshFolders(folder.Id);
            }

            folder.Videos.Add(new LibraryVideo
            {
                VideoId = _currentVideo.VideoId,
                Title = _currentVideo.Title,
                TrackName = _currentVideo.TrackName,
                Artist = _currentVideo.Artist,
                Url = _currentVideo.Url,
                IsOffline = _currentVideo.IsOffline,
                LocalPath = _currentVideo.LocalPath,
                Format = _currentVideo.Format,
                Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList()
            });
        }
        else
        {
            foreach (var saved in savedItems)
            {
                saved.Title = _currentVideo.Title;
                saved.TrackName = _currentVideo.TrackName;
                saved.Artist = _currentVideo.Artist;
                saved.Url = _currentVideo.Url;
                saved.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
            }
        }
        SaveLibrary();
        RefreshLibrarySongs();
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
                VideoId = _currentVideo.VideoId, Title = _currentVideo.Title, TrackName = MusicTrackBox.Text.Trim(), Artist = MusicArtistBox.Text.Trim(), Url = _currentVideo.Url,
                Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList()
            });
        else
        {
            video.Title = _currentVideo.Title;
            video.TrackName = MusicTrackBox.Text.Trim();
            video.Artist = MusicArtistBox.Text.Trim();
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
            _playbackEnded = false;
            _manualDraftActive = false;
            _manualDraftLines.Clear();
            _manualDraftIndex = 0;
            ManualLyricsBox.Clear();
            PublishProjectionOnly("");
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
            MusicTrackBox.Text = string.IsNullOrWhiteSpace(video.TrackName) ? video.Title : video.TrackName;
            MusicArtistBox.Text = video.Artist;
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
        PublishCueBlock(cueIndex);
    }

    private void LocalAudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        => EndPlayback("Áudio offline finalizado. Projeção encerrada automaticamente.");

    private void EndPlayback(string status)
    {
        if (_playbackEnded) return;
        _playbackEnded = true;
        _audioTimer.Stop();
        _isLocalAudioPlaying = false;
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        PublishProjectionOnly("");
        StatusText.Text = status;
        _ = CloseHolyricsQuickPresentationAsync();
    }

    private async Task CloseHolyricsQuickPresentationAsync()
    {
        try { await _holyrics.CloseQuickPresentationAsync(); }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => StatusText.Text += " Não foi possível fechar a apresentação rápida do Holyrics: " + ex.Message);
        }
    }

    private async void EndPlaybackManually_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isLocalAudioPlaying)
                LocalAudioPlayer.Stop();
            else if (YoutubePlayer.CoreWebView2 is not null)
                await YoutubePlayer.CoreWebView2.ExecuteScriptAsync("if(typeof player !== 'undefined' && player && typeof player.stopVideo === 'function') player.stopVideo();");
        }
        catch { /* A limpeza da projeção não depende de o player aceitar o comando de parada. */ }
        EndPlayback("Música encerrada manualmente. Projeção limpa; aplicativo pronto para a próxima faixa.");
    }

    private void LocalAudioPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        _audioTimer.Stop();
        _isLocalAudioPlaying = false;
        PublishProjectionOnly("");
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