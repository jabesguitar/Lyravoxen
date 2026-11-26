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
using Lyravoxen.Offline;
using Microsoft.Web.WebView2.Core;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

public partial class MainWindow : Window
{
    private ProjectionWindow? _projection;
    private string _currentProjectedLyric = "";
    private string? _selectedProjectionBackgroundPath;
    private int _maxProjectionLines = 4;
    private long _holyricsUpdateVersion;
    private readonly object _holyricsQueueSync = new();
    private Task _holyricsSendQueue = Task.CompletedTask;
    private readonly List<LyricCue> _cues = new();
    private List<string> _manualDraftLines = new();
    private readonly List<LyricCue> _manualDraftCues = new();
    private LiveAudioTranscriptionService? _liveRecognition;
    private readonly BibleReaderService _bibleReader = new();
    private IReadOnlyList<BibleBook> _bibleCatalog = Array.Empty<BibleBook>();
    private readonly Dictionary<int, BibleBook> _bibleLocalMap = new();
    private List<BibleVerseItem> _bibleVerses = new();
    private BibleReference? _bibleTypedReference;
    private bool _bibleUiUpdating;
    private bool _bibleSourceChosenByUser;
    private int _bibleLoadVersion;
    private bool _automaticLyricsActive;
    // Letra salva antes de a música tocar: a sincronização começa sozinha no primeiro play.
    private bool _autoSyncPending;
    // Resultados seguidos sem casar com a letra; alarga a busca para o reconhecimento não travar.
    private int _unmatchedTranscriptBlocks;
    private readonly StringBuilder _liveTranscriptBuffer = new();
    private int _manualDraftIndex;
    private bool _manualDraftActive;
    private readonly List<VideoHistoryItem> _history = new();
    private readonly List<LibraryFolder> _library = new();
    private LibraryVideo? _currentVideo;
    private LibraryVideo? _selectedLibraryVideo;
    private readonly DispatcherTimer _audioTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly DispatcherTimer _offlineCleanupTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMinutes(1) };
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
    private LibraryVideo? _nextOfflineVideo;
    private string? _pendingQueueVideoId;
    private bool _virtualHostMapped;
    private bool _historySelectionChanging;
    private readonly HolyricsClient _holyrics = new();
    private bool _holyricsEnabled = true;
    private readonly string _playerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Player");
    private readonly string _historyFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "video-history.json");
    private readonly string _libraryFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "library.json");
    private readonly string _preferencesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "preferences.json");
    private bool _loadingPreferences;
    private static readonly HttpClient MetadataHttp = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly OfflineMediaService _offlineMedia = new();
    private static string OfflineMediaFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Media");

    public MainWindow()
    {
        InitializeComponent();
        ThemeToggleButton.Content = App.IsLightTheme ? "☾" : "☀";
        ThemeToggleButton.ToolTip = App.IsLightTheme ? "Mudar para modo escuro" : "Mudar para modo claro";
        InitializeBackup();
        InitializeRemote();
        InitializeRequests();
        InitializeNextLyrics();
        InitializeHelp();
        LoadHistory();
        LoadLibrary();
        Closing += async (_, _) =>
        {
            _offlineCleanupTimer.Stop();
            _offlineMedia.Dispose();
            if (_liveRecognition is not null) await StopAutoRecognitionAsync(savePartial: false);
            _micService?.Dispose();
            _micService = null;
        };
        _audioTimer.Tick += (_, _) => ProcessAudioCue();
        _offlineCleanupTimer.Tick += (_, _) => CleanupExpiredOfflineCopies();
        Loaded += async (_, _) =>
        {
            _offlineCleanupTimer.Start();
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
                await LoadBibleCatalogAsync();
                YoutubeSearchView.CoreWebView2.Navigate("https://www.youtube.com/");
                _searchWebViewReady = true;
                StatusText.Text = "Player pronto. Use a aba Próxima música para pesquisar e preparar a próxima faixa.";

            }
            catch (Exception ex) { StatusText.Text = "Não foi possível iniciar o WebView2: " + ex.Message; }
        };
    }

    // ====================================================================== Central Bíblica
    private sealed record BibleSourceOption(bool IsHolyrics, string Id, string Title, string Note);

    private sealed record BibleVerseItem(int Chapter, int Number, string Text);

    private Task LoadBibleCatalogAsync()
    {
        // Livros e capítulos vêm da tabela interna: a tela funciona mesmo sem internet.
        var view = new System.Windows.Data.ListCollectionView(BibleReferenceParser.Books.ToList());
        view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription(nameof(BibleBookInfo.Testament)));
        BibleBooksList.ItemsSource = view;
        RebuildBibleSources(null);
        SetBibleStatus("Digite uma referência (ex.: Jo 3:16) ou escolha o livro, o capítulo e o versículo.");
        BibleBooksList.SelectedItem = BibleReferenceParser.Books[0];
        _ = LoadBibleLocalCatalogAsync();
        _ = RefreshBibleHolyricsVersionsAsync(showErrors: false);
        return Task.CompletedTask;
    }

    private async Task LoadBibleLocalCatalogAsync()
    {
        try
        {
            _bibleCatalog = await _bibleReader.GetBooksAsync();
            BuildLocalBibleBookMap();
            var current = SelectedBibleVerses();
            await ReloadBibleChapterTextAsync(current.FirstOrDefault()?.Number, current.Count > 1 ? (int?)current[^1].Number : null);
        }
        catch (Exception ex)
        {
            SetBibleStatus("Textos locais indisponíveis sem conexão inicial. A projeção pelo Holyrics continua funcionando. (" + ex.Message + ")");
        }
    }

    // Liga a tabela interna de livros ao catálogo do leitor local, sem confiar só no nome do livro.
    private void BuildLocalBibleBookMap()
    {
        _bibleLocalMap.Clear();
        var catalog = _bibleCatalog;
        if (catalog.Count == 0) return;

        var parsed = new int[catalog.Count];
        for (var i = 0; i < catalog.Count; i++)
            parsed[i] = BibleReferenceParser.TryParseReference(catalog[i].Name + " 1")?.Book.Number ?? 0;

        var recognised = parsed.Count(n => n > 0);
        var inOrder = catalog.Count == BibleReferenceParser.Books.Count;
        if (inOrder)
        {
            var agree = Enumerable.Range(0, catalog.Count).Count(i => parsed[i] == i + 1);
            inOrder = recognised > 0 && agree >= recognised * 0.85;
        }

        if (inOrder)
        {
            for (var i = 0; i < catalog.Count; i++)
                if (catalog[i].Chapters == BibleReferenceParser.Books[i].Chapters)
                    _bibleLocalMap[i + 1] = catalog[i];
            return;
        }

        foreach (var group in Enumerable.Range(0, catalog.Count)
                     .Where(i => parsed[i] > 0)
                     .GroupBy(i => parsed[i])
                     .Where(g => g.Count() == 1))
        {
            var index = group.First();
            if (catalog[index].Chapters == BibleReferenceParser.Books[group.Key - 1].Chapters)
                _bibleLocalMap[group.Key] = catalog[index];
        }
    }

    private BibleBook? ResolveLocalBibleBook(BibleBookInfo book)
        => _bibleLocalMap.TryGetValue(book.Number, out var local) ? local : null;

    private void SetBibleStatus(string message, bool alsoGlobal = false)
    {
        BibleStatusText.Text = message;
        Services.AppLog.Info("[bíblia] " + message);
        if (alsoGlobal) StatusText.Text = message;
    }

    private void RebuildBibleSources(IReadOnlyList<HolyricsClient.BibleVersionInfo>? holyricsVersions)
    {
        var previous = BibleSourceBox.SelectedItem as BibleSourceOption;
        var options = new List<BibleSourceOption>();
        if (holyricsVersions is not null)
        {
            var arc = holyricsVersions.FirstOrDefault(v => string.Equals(v.Version, "pt_arc", StringComparison.OrdinalIgnoreCase));
            if (arc is not null)
                options.Add(new BibleSourceOption(true, arc.Version, "Almeida Revista e Corrigida (ARC) — Holyrics", ""));
            foreach (var version in holyricsVersions
                         .Where(v => !string.Equals(v.Version, "pt_arc", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(v => v.Title, StringComparer.CurrentCultureIgnoreCase))
                options.Add(new BibleSourceOption(true, version.Version, version.Title + " — Holyrics", ""));
        }

        foreach (var local in BibleReaderService.FreePortugueseVersions)
            options.Add(new BibleSourceOption(false, local.Slug, local.Name + " — local", local.License));

        // Enquanto você não escolher manualmente, a ARC do Holyrics (se instalada) é a versão padrão.
        BibleSourceOption? selected = null;
        if (_bibleSourceChosenByUser && previous is not null)
            selected = options.FirstOrDefault(o => o.IsHolyrics == previous.IsHolyrics && string.Equals(o.Id, previous.Id, StringComparison.Ordinal));
        selected ??= options.FirstOrDefault();

        _bibleUiUpdating = true;
        try
        {
            BibleSourceBox.ItemsSource = options;
            BibleSourceBox.SelectedItem = selected;
        }
        finally
        {
            _bibleUiUpdating = false;
        }

        UpdateBibleSourceNote();

        if (previous is null || selected is null || previous.IsHolyrics != selected.IsHolyrics ||
            !string.Equals(previous.Id, selected.Id, StringComparison.Ordinal))
        {
            var current = SelectedBibleVerses();
            _ = ReloadBibleChapterTextAsync(current.FirstOrDefault()?.Number, current.Count > 1 ? (int?)current[^1].Number : null);
        }
    }

    private void UpdateBibleSourceNote()
    {
        if (BibleSourceBox.SelectedItem is not BibleSourceOption source)
        {
            BibleSourceNoteText.Text = "";
            BibleCopyrightText.Text = "";
            return;
        }

        BibleSourceNoteText.Text = source.IsHolyrics
            ? "Texto original do Holyrics (" + source.Title + "). O Lyravoxen escolhe o versículo e o Holyrics exibe."
            : "Projeção na tela própria do Lyravoxen.";
        BibleCopyrightText.Text = source.IsHolyrics
            ? "A Bíblia precisa estar instalada e licenciada no Holyrics."
            : source.Note;
    }

    private async void RefreshHolyricsBibleVersions_Click(object sender, RoutedEventArgs e)
        => await RefreshBibleHolyricsVersionsAsync(showErrors: true);

    private async Task RefreshBibleHolyricsVersionsAsync(bool showErrors)
    {
        if (!_holyrics.IsConfigured)
        {
            if (showErrors)
                SetBibleStatus("Conecte o Holyrics (aba Ao vivo) para usar a ARC e as demais versões instaladas nele.", true);
            return;
        }

        try
        {
            SetBibleStatus("Consultando as Bíblias instaladas no Holyrics...");
            var versions = await _holyrics.GetBibleVersionsAsync();
            RebuildBibleSources(versions);
            var hasArc = versions.Any(v => string.Equals(v.Version, "pt_arc", StringComparison.OrdinalIgnoreCase));
            SetBibleStatus(hasArc
                ? versions.Count + " versões no Holyrics. ARC pronta para projetar."
                : versions.Count + " versões no Holyrics, mas a ARC (pt_arc) não está instalada nele.", showErrors);
        }
        catch (Exception ex)
        {
            if (showErrors)
                SetBibleStatus("Falha ao consultar o Holyrics: " + ex.Message, true);
            else
                SetBibleStatus("Holyrics não respondeu. Use o botão ↻ Holyrics quando ele estiver aberto.");
        }
    }

    private void BibleSourceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_bibleUiUpdating || BibleSourceBox.SelectedItem is null) return;
        _bibleSourceChosenByUser = true;
        UpdateBibleSourceNote();
        var current = SelectedBibleVerses();
        _ = ReloadBibleChapterTextAsync(current.FirstOrDefault()?.Number, current.Count > 1 ? (int?)current[^1].Number : null);
    }

    private void BibleBooksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_bibleUiUpdating || BibleBooksList.SelectedItem is not BibleBookInfo book) return;
        _bibleTypedReference = null;
        _bibleUiUpdating = true;
        try
        {
            BibleChaptersList.ItemsSource = Enumerable.Range(1, book.Chapters).ToList();
            BibleChaptersList.SelectedItem = 1;
        }
        finally
        {
            _bibleUiUpdating = false;
        }

        _ = ReloadBibleChapterTextAsync();
    }

    private void BibleChaptersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_bibleUiUpdating || BibleChaptersList.SelectedItem is not int) return;
        _bibleTypedReference = null;
        _ = ReloadBibleChapterTextAsync();
    }

    private void BibleVersesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_bibleUiUpdating) return;
        UpdateBiblePreview();
    }

    private async void BibleVersesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => await ProjectBibleSelectionAsync();

    private async void BibleVersesList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        e.Handled = true;
        await ProjectBibleSelectionAsync();
    }

    private async void BibleSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        e.Handled = true;
        ExpandTypedBookPrefix();
        await LocateBibleReferenceAsync(project: true);
    }

    private async void BibleLocate_Click(object sender, RoutedEventArgs e)
        => await LocateBibleReferenceAsync(project: false);

    private async Task LocateBibleReferenceAsync(bool project)
    {
        var text = BibleSearchBox.Text.Trim();
        if (text.Length == 0) return;

        var parsed = BibleReferenceParser.TryParseReference(text, allowBookOnly: true);
        if (parsed is null)
        {
            SetBibleStatus("Não entendi “" + text + "”. Exemplos: João 3:16 · Sl 23 · 1 Co 13:4-7 · Gênesis 1");
            return;
        }

        _bibleTypedReference = parsed.VerseStart.HasValue ? parsed : null;
        _bibleUiUpdating = true;
        try
        {
            BibleBooksList.SelectedItem = parsed.Book;
            BibleBooksList.ScrollIntoView(parsed.Book);
            BibleChaptersList.ItemsSource = Enumerable.Range(1, parsed.Book.Chapters).ToList();
            BibleChaptersList.SelectedItem = parsed.Chapter;
            BibleChaptersList.ScrollIntoView(parsed.Chapter);
            BibleSearchBox.Text = parsed.ToString();
            BibleSearchBox.SelectAll();
        }
        finally
        {
            _bibleUiUpdating = false;
        }

        await ReloadBibleChapterTextAsync(parsed.VerseStart, parsed.VerseEnd);
        if (project && parsed.VerseStart.HasValue) await ProjectBibleSelectionAsync();
    }

    private async Task ReloadBibleChapterTextAsync(int? selectVerseStart = null, int? selectVerseEnd = null)
    {
        if (BibleBooksList.SelectedItem is not BibleBookInfo book || BibleChaptersList.SelectedItem is not int chapter) return;

        var requestId = Interlocked.Increment(ref _bibleLoadVersion);
        var source = BibleSourceBox.SelectedItem as BibleSourceOption;
        // Com a Bíblia do Holyrics, o texto é do Holyrics: aqui só aparece a numeração dos versículos.
        var numbersOnly = source is { IsHolyrics: true };
        ApplyBibleVerseLayout(numbersOnly);
        var previewVersion = source is { IsHolyrics: false }
            ? BibleReaderService.FreePortugueseVersions.FirstOrDefault(v => v.Slug == source.Id) ?? BibleReaderService.FreePortugueseVersions[0]
            : BibleReaderService.FreePortugueseVersions[0];

        _bibleUiUpdating = true;
        try
        {
            _bibleVerses = new List<BibleVerseItem>();
            BibleVersesList.ItemsSource = null;
        }
        finally
        {
            _bibleUiUpdating = false;
        }

        UpdateBiblePreview();

        var local = ResolveLocalBibleBook(book);
        if (local is null)
        {
            SetBibleStatus(_bibleCatalog.Count == 0
                ? "Carregando o catálogo de textos... Pelo Holyrics já dá para projetar digitando a referência."
                : "Texto local indisponível para " + book.Name + ". Digite a referência e projete pelo Holyrics.");
            return;
        }

        SetBibleStatus("Carregando " + book.Name + " " + chapter + "...");
        try
        {
            var result = await _bibleReader.GetChapterAsync(previewVersion.Slug, local, chapter);
            if (requestId != _bibleLoadVersion) return;

            var verses = result.Verses.Select((text, index) => new BibleVerseItem(chapter, index + 1, numbersOnly ? "" : text)).ToList();
            _bibleUiUpdating = true;
            try
            {
                _bibleVerses = verses;
                BibleVersesList.ItemsSource = _bibleVerses;
            }
            finally
            {
                _bibleUiUpdating = false;
            }

            if (selectVerseStart.HasValue) SelectBibleVerses(selectVerseStart.Value, selectVerseEnd);
            else UpdateBiblePreview();

            SetBibleStatus(book.Name + " " + chapter + " · " + verses.Count + " versículos" +
                           (numbersOnly ? " · o texto vem do Holyrics" : " · texto: " + previewVersion.Name));
            if (!string.IsNullOrWhiteSpace(result.Copyright) && source is { IsHolyrics: false })
                BibleCopyrightText.Text = result.Copyright;
        }
        catch (Exception ex)
        {
            if (requestId != _bibleLoadVersion) return;
            SetBibleStatus("Não foi possível carregar o texto de " + book.Name + " " + chapter + " (" + ex.Message +
                           "). Você ainda pode projetar digitando a referência.");
        }
    }

    private void ApplyBibleVerseLayout(bool numbersOnly)
    {
        BibleVersesList.ItemsPanel = (ItemsPanelTemplate)BibleWorkspaceTab.FindResource(numbersOnly ? "BibleVerseGridPanel" : "BibleVerseListPanel");
        BibleVersesList.ItemTemplate = (DataTemplate)BibleWorkspaceTab.FindResource(numbersOnly ? "BibleVerseNumberTemplate" : "BibleVerseTextTemplate");
    }

    private void SelectBibleVerses(int start, int? end)
    {
        _bibleUiUpdating = true;
        try
        {
            BibleVersesList.SelectedItems.Clear();
            if (_bibleVerses.Count > 0)
            {
                var first = start == int.MaxValue ? _bibleVerses.Count : Math.Clamp(start, 1, _bibleVerses.Count);
                var last = end.HasValue ? Math.Clamp(end.Value, first, _bibleVerses.Count) : first;
                foreach (var verse in _bibleVerses.Where(v => v.Number >= first && v.Number <= last))
                    BibleVersesList.SelectedItems.Add(verse);
                var anchor = _bibleVerses.FirstOrDefault(v => v.Number == first);
                if (anchor is not null) BibleVersesList.ScrollIntoView(anchor);
            }
        }
        finally
        {
            _bibleUiUpdating = false;
        }

        UpdateBiblePreview();
    }

    private List<BibleVerseItem> SelectedBibleVerses()
        => BibleVersesList.SelectedItems.OfType<BibleVerseItem>().OrderBy(v => v.Number).ToList();

    private static string ComposeBibleText(IReadOnlyList<BibleVerseItem> verses)
        => string.Join(" ", verses.Select(v => verses.Count > 1 ? v.Number + " " + v.Text : v.Text));

    private BibleReference? GetBibleSelectionReference()
    {
        if (BibleBooksList.SelectedItem is not BibleBookInfo book || BibleChaptersList.SelectedItem is not int chapter) return null;

        var selected = SelectedBibleVerses();
        if (selected.Count > 0)
        {
            var end = selected[^1].Number > selected[0].Number ? (int?)selected[^1].Number : null;
            return new BibleReference(book, chapter, selected[0].Number, end);
        }

        var typed = _bibleTypedReference;
        if (typed is not null && typed.Book.Number == book.Number && typed.Chapter == chapter) return typed;
        return new BibleReference(book, chapter, null, null);
    }

    private void UpdateBiblePreview()
    {
        var reference = GetBibleSelectionReference();
        if (reference is null)
        {
            BiblePreviewReferenceText.Text = "Nenhum versículo selecionado";
            BiblePreviewVerseText.Text = "Digite uma referência ou escolha livro, capítulo e versículo.";
            return;
        }

        BiblePreviewReferenceText.Text = reference.ToString();
        var selected = SelectedBibleVerses();
        if (BibleSourceBox.SelectedItem is BibleSourceOption { IsHolyrics: true } holyricsSource)
            BiblePreviewVerseText.Text = reference.VerseStart.HasValue
                ? "O Holyrics vai exibir este versículo em " + holyricsSource.Title + "."
                : "Escolha um versículo na lista ao lado.";
        else if (selected.Count > 0)
            BiblePreviewVerseText.Text = ComposeBibleText(selected);
        else if (reference.VerseStart.HasValue)
            BiblePreviewVerseText.Text = "Prévia do texto indisponível agora. O telão ainda pode projetar esta referência.";
        else
            BiblePreviewVerseText.Text = "Escolha um versículo na lista ao lado.";
    }

    private async void BiblePrevious_Click(object sender, RoutedEventArgs e) => await StepBibleVerseAsync(-1);

    private async void BibleNext_Click(object sender, RoutedEventArgs e) => await StepBibleVerseAsync(1);

    // Anterior/Próximo andam um versículo (atravessando capítulos) e já projetam.
    private async Task StepBibleVerseAsync(int direction)
    {
        if (BibleBooksList.SelectedItem is not BibleBookInfo book || BibleChaptersList.SelectedItem is not int chapter) return;
        if (_bibleVerses.Count == 0)
        {
            SetBibleStatus("Sem texto carregado para andar de versículo em versículo. Digite a próxima referência.");
            return;
        }

        var selected = SelectedBibleVerses();
        int target;
        if (selected.Count == 0) target = direction > 0 ? 1 : _bibleVerses.Count;
        else target = (direction > 0 ? selected[^1].Number : selected[0].Number) + direction;

        if (target >= 1 && target <= _bibleVerses.Count)
        {
            SelectBibleVerses(target, null);
            await ProjectBibleSelectionAsync();
            return;
        }

        var nextChapter = chapter + direction;
        if (nextChapter < 1 || nextChapter > book.Chapters)
        {
            SetBibleStatus(direction > 0 ? "Fim de " + book.Name + "." : "Início de " + book.Name + ".");
            return;
        }

        _bibleTypedReference = null;
        _bibleUiUpdating = true;
        try
        {
            BibleChaptersList.SelectedItem = nextChapter;
            BibleChaptersList.ScrollIntoView(nextChapter);
        }
        finally
        {
            _bibleUiUpdating = false;
        }

        await ReloadBibleChapterTextAsync(direction > 0 ? 1 : int.MaxValue, null);
        await ProjectBibleSelectionAsync();
    }

    private async void ProjectBibleSelection_Click(object sender, RoutedEventArgs e) => await ProjectBibleSelectionAsync();

    private async Task ProjectBibleSelectionAsync()
    {
        var reference = GetBibleSelectionReference();
        if (reference is null || !reference.VerseStart.HasValue)
        {
            SetBibleStatus("Escolha um versículo (ou digite a referência) antes de projetar.");
            return;
        }

        if (BibleSourceBox.SelectedItem is not BibleSourceOption source)
        {
            SetBibleStatus("Escolha a versão da Bíblia.");
            return;
        }

        var referenceText = reference.ToString();
        if (source.IsHolyrics)
        {
            if (!_holyricsEnabled)
            {
                SetBibleStatus("Ative a integração com o Holyrics (aba Ao vivo) para projetar a " + source.Title + ".", true);
                return;
            }

            try
            {
                await _holyrics.ShowBibleVerseAsync(referenceText, source.Id);
                SetBibleStatus("Projetado no Holyrics: " + referenceText + " — " + source.Title, true);
                _ = BringToFrontAsync();
            }
            catch (Exception ex)
            {
                SetBibleStatus("Não foi possível projetar no Holyrics: " + ex.Message, true);
            }

            return;
        }

        var selected = SelectedBibleVerses();
        if (selected.Count == 0)
        {
            SetBibleStatus("Esta versão ainda não tem o texto carregado. Conecte-se à internet uma vez para guardá-lo.", true);
            return;
        }

        try
        {
            var outputDevice = EnsureProjectionWindow();
            _projection!.ShowBibleVerse(referenceText, ComposeBibleText(selected));
            _projection.Activate();
            SetBibleStatus("Enviado para a projeção própria: " + referenceText +
                           (string.IsNullOrWhiteSpace(outputDevice) ? "" : " | Monitor: " + outputDevice), true);
        }
        catch (Exception ex)
        {
            SetBibleStatus("Não foi possível abrir a tela de projeção: " + ex.Message, true);
        }
    }

    private void ClearBibleProjection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Fecha a tela própria de verdade: some o fundo e aparece a área de trabalho do computador.
            if (_projection is { IsVisible: true }) _projection.Close();
        }
        catch (Exception ex)
        {
            SetBibleStatus("Falha ao fechar a projeção própria: " + ex.Message);
        }

        if (_holyricsEnabled && _holyrics.IsConfigured)
            _ = ObserveHolyricsQueueAsync(QueueHolyricsCloseProjectionAsync());
        SetBibleStatus("Telão limpo.", true);
    }

    private void OpenLocalProjection_Click(object sender, RoutedEventArgs e)
    {
        if (_projection is { IsVisible: true })
        {
            _projection.Close();
            LocalProjectionToggleButton.Content = "Abrir projeção própria";
            StatusText.Text = "Projeção própria fechada. O Lyravoxen continua aberto.";
            return;
        }

        EnsureProjectionWindow();
        LocalProjectionToggleButton.Content = "Fechar projeção própria";
        StatusText.Text = "Projeção própria aberta. Pressione ESC ou use Fechar projeção própria para sair.";
    }

    private string? EnsureProjectionWindow()
    {
        if (_projection is null || !_projection.IsVisible)
        {
            _projection = new ProjectionWindow();
            _projection.Closed += (_, _) =>
            {
                if (LocalProjectionToggleButton is not null)
                    LocalProjectionToggleButton.Content = "Abrir projeção própria";
            };
        }
        var outputDevice = _projection.ShowOnOutputDisplay();
        _projection.SetMaxLines(_maxProjectionLines);
        _projection.SetBackgroundImage(_selectedProjectionBackgroundPath);
        _projection.ShowLyric(_currentProjectedLyric);
        return outputDevice;
    }

    private void SelectProjectionBackground_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Escolha um fundo de projeção",
            Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            if (extension is not ".jpg" and not ".jpeg" and not ".png")
                throw new InvalidDataException("Use uma imagem JPG ou PNG.");
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lyravoxen", "ProjectionBackgrounds");
            Directory.CreateDirectory(folder);
            var target = Path.Combine(folder, "background-" + Guid.NewGuid().ToString("N") + extension);
            File.Copy(dialog.FileName, target, false);
            _selectedProjectionBackgroundPath = target;
            EnsureProjectionWindow();
            _projection!.SetBackgroundImage(target);
            SaveProjectionBackgroundPreference(target);
            StatusText.Text = "Fundo personalizado aplicado à projeção.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível aplicar o fundo: " + ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Fundo de projeção", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveProjectionBackgroundPreference(string path)
    {
        try
        {
            var current = File.Exists(_preferencesFile)
                ? JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile)) ?? new()
                : new ProjectionPreferences();
            current.ProjectionBackgroundPath = path;
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencesFile)!);
            var temp = _preferencesFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _preferencesFile, true);
        }
        catch (Exception ex) { StatusText.Text = "Fundo aplicado, mas a preferência não pôde ser salva: " + ex.Message; }
    }

    private Microsoft.Web.WebView2.Wpf.WebView2? ActiveBrowserView => (BrowserTabs.SelectedItem as TabItem)?.Content as Microsoft.Web.WebView2.Wpf.WebView2;

    private async Task InitializeBrowserViewAsync(Microsoft.Web.WebView2.Wpf.WebView2 view)
    {
        await BrowserAdBlocker.InitializeAsync();
        await view.EnsureCoreWebView2Async();
        BrowserAdBlocker.Attach(view.CoreWebView2);
        RefreshBrowserProtectionStatus();
        view.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        view.CoreWebView2.Settings.IsStatusBarEnabled = true;
        view.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            if (ReferenceEquals(ActiveBrowserView, view) && view.Source is not null)
                Dispatcher.Invoke(() => YoutubeSearchBox.Text = view.Source.ToString());
        };
    }

    private void RefreshBrowserProtectionStatus()
    {
        BrowserProtectionToggleButton.Content = BrowserAdBlocker.Enabled
            ? "🛡 Proteção ligada"
            : "Proteção desligada";
        BrowserProtectionStatusText.Text = BrowserAdBlocker.Enabled
            ? $"Proteção ativa · {BrowserAdBlocker.RuleCount:N0} regras · {BrowserAdBlocker.BlockedCount:N0} solicitações bloqueadas"
            : "Proteção desativada para todas as abas";
    }

    private void BrowserProtectionToggle_Click(object sender, RoutedEventArgs e)
    {
        BrowserAdBlocker.Enabled = !BrowserAdBlocker.Enabled;
        RefreshBrowserProtectionStatus();
    }

    private void BrowserAllowSite_Click(object sender, RoutedEventArgs e)
    {
        BrowserAdBlocker.AllowCurrentSite(ActiveBrowserView?.Source?.ToString());
        StatusText.Text = "Exceção registrada para o domínio desta aba.";
        RefreshBrowserProtectionStatus();
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
        _nextOfflineVideo = null;
        SaveNextVideoPreference();
        NextTrackText.Text = "Próxima faixa preparada (" + videoId + "). A música atual continua tocando até você selecionar Tocar próxima.";
        PlayNextButton.IsEnabled = true;
        StatusText.Text = "Próxima música preparada sem interromper a reprodução atual.";
        RefreshNextLyricsPreview();
    }

    private async void PlayNextPrepared_Click(object sender, RoutedEventArgs e)
    {
        if (_nextOfflineVideo is { } offline)
        {
            if (string.IsNullOrWhiteSpace(offline.LocalPath) || !File.Exists(offline.LocalPath) ||
                (offline.OfflineExpiresAt.HasValue && offline.OfflineExpiresAt.Value <= DateTime.Now))
            {
                _nextOfflineVideo = null;
                PlayNextButton.IsEnabled = !string.IsNullOrWhiteSpace(_nextVideoUrl);
                StatusText.Text = "A cópia offline preparada venceu ou não está disponível. Os dados da música continuam na biblioteca.";
                return;
            }
            _nextOfflineVideo = null;
            _nextVideoUrl = null;
            SaveNextVideoPreference();
            PlayNextButton.IsEnabled = false;
            WorkspaceTabs.SelectedIndex = 0;
            PlayLocalAudio(offline);
            StatusText.Text = "Reproduzindo a cópia local autorizada: " + (offline.TrackName.Length > 0 ? offline.TrackName : offline.Title);
            return;
        }
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
            if (_liveRecognition is not null)
                await StopAutoRecognitionAsync(savePartial: true);
            _playerSessionId = Guid.NewGuid().ToString("N");
            _playbackEnded = false;
            _autoSyncPending = false;
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
                Title = _history.FirstOrDefault(v => v.VideoId == videoId)?.Title ?? "Vídeo do YouTube",
                TrackName = _history.FirstOrDefault(v => v.VideoId == videoId)?.TrackName ?? "",
                Composer = _history.FirstOrDefault(v => v.VideoId == videoId)?.Composer ?? "",
                Artist = _history.FirstOrDefault(v => v.VideoId == videoId)?.Artist ?? ""
            };
            _currentVideo.Url = url;
            var historyMetadata = _history.FirstOrDefault(v => v.VideoId == videoId);
            if (string.IsNullOrWhiteSpace(_currentVideo.TrackName)) _currentVideo.TrackName = historyMetadata?.TrackName ?? "";
            if (string.IsNullOrWhiteSpace(_currentVideo.Composer)) _currentVideo.Composer = historyMetadata?.Composer ?? "";
            if (string.IsNullOrWhiteSpace(_currentVideo.Artist)) _currentVideo.Artist = historyMetadata?.Artist ?? "";
            MusicTrackBox.Text = _currentVideo.TrackName;
            MusicArtistBox.Text = _currentVideo.Artist;
            ManualLyricsBox.Text = _currentVideo.FullLyrics ?? "";
            ManualSyncProgressText.Text = string.IsNullOrWhiteSpace(_currentVideo.FullLyrics)
                ? "Esta música ainda não tem letra completa salva."
                : "Letra completa carregada da biblioteca. Edite se necessário e salve para atualizar.";
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
        StatusText.Text = "Cadastro de letra aberto. Cole a letra completa, salve e inicie o reconhecimento automático com a música tocando.";
    }

    private async void SearchLyrics_Click(object sender, RoutedEventArgs e)
    {
        var track = MusicTrackBox.Text.Trim();
        var artist = MusicArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track))
        {
            System.Windows.MessageBox.Show("Informe o título da música. O intérprete pode ficar em branco; nesse caso a busca será feita pelo título e você confirmará a versão encontrada.", "Buscar letra", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await TryLoadAutomaticLyricsAsync(track, artist);
    }

    private async Task OpenLyricsSourceSearchesAsync(string track, string artist)
    {
        var searchTerms = string.IsNullOrWhiteSpace(artist) ? "\"" + track + "\"" : "\"" + track + "\" \"" + artist + "\"";
        var sources = new[]
        {
            (Name: "Cifra Club", Domain: "cifraclub.com.br"),
            (Name: "Vagalume", Domain: "vagalume.com.br"),
            (Name: "Letras.mus.br", Domain: "letras.mus.br"),
            (Name: "Ouvir Música", Domain: "ouvirmusica.com.br"),
            (Name: "Letras.com", Domain: "letras.com"),
            (Name: "Harpa Cristã — O Gospel", Domain: "ogospel.com.br/harpa-crista"),
            (Name: "Harpa Cristã — mBíblia", Domain: "mbiblia.com.br/harpa"),
            (Name: "Harpa Cristã — Hinos da Harpa", Domain: "hinosdaharpa.com.br"),
            (Name: "Hinoteca", Domain: "hinoteca.com.br"),
            (Name: "Letra Sonora", Domain: "letrasonora.com.br"),
            (Name: "Glow Bible — Hinário", Domain: "glowbible.org/hinario"),
            (Name: "Busca ampla", Domain: "")
        };

        StatusText.Text = "Abrindo fontes alternativas. Confira a autoria e a versão antes de usar qualquer letra.";
        TabItem? firstSourceTab = null;
        foreach (var source in sources)
        {
            try
            {
                var query = source.Name.StartsWith("Harpa Cristã", StringComparison.Ordinal)
                    ? "site:" + source.Domain + " " + searchTerms + " letra hino"
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
        StatusText.Text = "Não encontrei uma letra sincronizada no catálogo. Abri pesquisas internas em Cifra Club, Vagalume, Letras.mus.br, Ouvir Música, Letras.com e catálogos de hinos. Copie a letra encontrada e use Cadastrar letra para salvar e sincronizar.";
    }

    private async void SaveFullLyrics_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo para vincular e salvar esta letra.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }
        var fullLyrics = ManualLyricsBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fullLyrics))
        {
            StatusText.Text = "Cole ou digite a letra completa antes de salvar.";
            return;
        }
        var lyricsChanged = !string.Equals(_currentVideo.FullLyrics?.Trim(), fullLyrics, StringComparison.Ordinal);
        _currentVideo.FullLyrics = fullLyrics;
        SaveCurrentFullLyrics();
        ManualSyncProgressText.Text = "Letra completa salva e vinculada a esta música. Você poderá reutilizá-la nas próximas reproduções.";
        StatusText.Text = "Letra completa salva na biblioteca local para: " + MusicTrackBox.Text;

        // Letra nova (ou música ainda sem tempos): sincroniza sozinha, sem outro clique.
        if (lyricsChanged || _cues.Count == 0)
        {
            try { await BeginAutomaticSyncAfterSaveAsync(lyricsChanged); }
            catch (Exception ex) { StatusText.Text = "Letra salva, mas a sincronização automática não pôde iniciar: " + ex.Message; }
        }
    }

    private async Task BeginAutomaticSyncAfterSaveAsync(bool lyricsChanged)
    {
        if (_liveRecognition is not null)
        {
            // Reconhecimento já ativo com a mesma letra: nada a refazer.
            if (!lyricsChanged) return;
            await StopAutoRecognitionAsync(savePartial: false);
        }

        var isPlaying = _isLocalAudioPlaying || _lastPlayerState == 1;
        if (!isPlaying)
        {
            _autoSyncPending = true;
            ManualSyncProgressText.Text = "Letra salva. A sincronização começa sozinha assim que a música tocar.";
            StatusText.Text = "Letra salva. Dê play na música: a sincronização e a projeção começam automaticamente.";
            return;
        }

        if (!_isLocalAudioPlaying && _lastKnownPlaybackSeconds > 15)
        {
            var restart = System.Windows.MessageBox.Show(
                "A música já está em " + TimeSpan.FromSeconds(_lastKnownPlaybackSeconds).ToString(@"mm\:ss") +
                ".\n\nPara sincronizar a letra inteira, é melhor começar do início.\n\nReiniciar a música agora?",
                "Sincronizar letra", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (restart == MessageBoxResult.Yes)
            {
                try
                {
                    await YoutubePlayer.CoreWebView2.ExecuteScriptAsync(
                        "typeof player !== 'undefined' && player && typeof player.seekTo === 'function' ? player.seekTo(0, true) : null");
                    _lastKnownPlaybackSeconds = 0;
                    await Task.Delay(400);
                }
                catch (Exception ex) { StatusText.Text = "Não foi possível reiniciar a música: " + ex.Message; }
            }
        }

        await StartAutoRecognitionAsync();
    }

    private void SaveCurrentFullLyrics()
    {
        if (_currentVideo is null) return;
        _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
        // PersistCurrentCues also creates the synchronized-library entry when this is the first save.
        PersistCurrentCues();
    }

    private async void ToggleAutoRecognition_Click(object sender, RoutedEventArgs e)
    {
        if (_liveRecognition is not null)
        {
            await StopAutoRecognitionAsync(savePartial: true);
            return;
        }

        _autoSyncPending = false;
        await StartAutoRecognitionAsync();
    }

    private async Task StartAutoRecognitionAsync()
    {
        if (_liveRecognition is not null) return;

        if (_currentVideo is null || (!_isLocalAudioPlaying && _lastPlayerState != 1))
        {
            StatusText.Text = "Carregue e inicie a música na aba Ao vivo antes de ativar o reconhecimento automático.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }

        var lines = ManualLyricsBox.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
        if (lines.Count == 0)
        {
            StatusText.Text = "Cole a letra completa, separando cada frase por linha, antes de iniciar o reconhecimento automático.";
            return;
        }

        _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
        SaveCurrentFullLyrics();
        _manualDraftLines = lines;
        _manualDraftCues.Clear();
        _liveTranscriptBuffer.Clear();
        _unmatchedTranscriptBlocks = 0;
        _manualDraftIndex = 0;
        _automaticLyricsActive = true;
        _manualDraftActive = false;
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        ManualCurrentLineText.Text = _manualDraftLines[0];
        ManualSyncProgressText.Text = "Letra salva. Reconhecimento preparado: aguardando o áudio cantar a primeira frase.";
        ProjectionPreviewText.Text = "Aguardando reconhecimento da primeira frase...";
        NextProjectionPreviewText.Text = "Próxima: " + _manualDraftLines[0];
        _ = ClearHolyricsForLiveDraftAsync();

        var service = new LiveAudioTranscriptionService(GetPlaybackSeconds);
        service.TranscriptReceived += LiveRecognition_TranscriptReceived;
        service.StatusChanged += LiveRecognition_StatusChanged;
        _liveRecognition = service;
        AutoRecognitionButton.IsEnabled = false;
        try
        {
            await service.StartAsync();
            AutoRecognitionButton.Content = "Parar reconhecimento automático";
            StatusText.Text = "Reconhecimento ativo. O Lyravoxen está reconhecendo a música localmente, sem enviar áudio pela internet.";
        }
        catch (Exception ex)
        {
            await StopAutoRecognitionAsync(savePartial: false);
            StatusText.Text = "Não foi possível iniciar o reconhecimento automático: " + ex.Message;
        }
        finally
        {
            AutoRecognitionButton.IsEnabled = true;
        }
    }

    private void LiveRecognition_StatusChanged(object? sender, string message)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_automaticLyricsActive)
                StatusText.Text = message;
        }));
    }

    private void LiveRecognition_TranscriptReceived(object? sender, LiveTranscriptEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_automaticLyricsActive || _manualDraftIndex >= _manualDraftLines.Count)
                return;

            if (!e.IsFinal) return;
            if (_liveTranscriptBuffer.Length > 0) _liveTranscriptBuffer.Append(' ');
            _liveTranscriptBuffer.Append(e.Transcript);

            // Aceita a próxima frase ou uma das duas seguintes quando a transcrição vier agrupada.
            // Se o reconhecimento ficar preso (várias transcrições sem casar), a busca é alargada
            // gradualmente, com exigência de confiança maior para frases mais distantes.
            // O avanço é sempre para frente; nunca volta a projetar uma frase anterior.
            var lookAhead = _unmatchedTranscriptBlocks >= 6 ? 12 : _unmatchedTranscriptBlocks >= 3 ? 6 : 2;
            var match = LiveLyricsMatcher.FindNextLine(
                _manualDraftLines, _manualDraftIndex, _liveTranscriptBuffer.ToString(),
                out var confidence, 0.78, lookAhead);

            if (match < _manualDraftIndex)
            {
                _unmatchedTranscriptBlocks++;
                if (_liveTranscriptBuffer.Length > 600)
                    _liveTranscriptBuffer.Remove(0, _liveTranscriptBuffer.Length - 400);
                ManualSyncProgressText.Text = $"Comparando o áudio com a letra salva... confiança {confidence:P0}.";
                return;
            }

            _unmatchedTranscriptBlocks = 0;
            _liveTranscriptBuffer.Clear();
            var matchedIndex = match;
            var estimatedSeconds = Math.Max(0, e.AudioStartSeconds + 0.5);
            double? lastCueSeconds = _manualDraftCues.Count > 0 ? _manualDraftCues[^1].Seconds : null;

            // Frases puladas pelo reconhecimento continuam na letra sincronizada (com tempos estimados).
            var plannedCues = AutoSyncPlanner.CreateCuesForMatch(
                _manualDraftLines, _manualDraftIndex, matchedIndex, lastCueSeconds, estimatedSeconds);
            if (plannedCues.Count == 0) return;
            _manualDraftCues.AddRange(plannedCues);
            var cue = plannedCues[^1];
            estimatedSeconds = cue.Seconds;
            _manualDraftIndex = matchedIndex + 1;

            // Mesmo caminho da reprodução normal: prévia, janela de projeção própria e Holyrics.
            var block = CueTimeline.BuildProjectionBlock(_manualDraftCues, _manualDraftCues.Count - 1, _maxProjectionLines);
            PublishProjectionOnly(block);
            QueueHolyricsUpdate(block);

            SecondsBox.Text = estimatedSeconds.ToString("0.00", CultureInfo.InvariantCulture);
            if (_manualDraftIndex < _manualDraftLines.Count)
            {
                ManualCurrentLineText.Text = _manualDraftLines[_manualDraftIndex];
                NextProjectionPreviewText.Text = "Próxima: " + _manualDraftLines[_manualDraftIndex];
                ManualSyncProgressText.Text = $"Reconhecidas {_manualDraftIndex} de {_manualDraftLines.Count} frases. Última: {confidence:P0} de confiança.";
                StatusText.Text = "Frase reconhecida e enviada ao Holyrics: " + cue.Text;
            }
            else
            {
                ManualCurrentLineText.Text = "Todas as frases foram reconhecidas. Sincronização concluída.";
                NextProjectionPreviewText.Text = "Próxima: —";
                ManualSyncProgressText.Text = $"Concluído: {_manualDraftCues.Count} trechos identificados e sincronizados. Letra e tempos salvos na biblioteca.";
                _ = StopAutoRecognitionAsync(savePartial: true);
            }
        }));
    }

    private async Task StopAutoRecognitionAsync(bool savePartial)
    {
        _automaticLyricsActive = false;
        var service = _liveRecognition;
        _liveRecognition = null;
        if (service is not null)
        {
            service.TranscriptReceived -= LiveRecognition_TranscriptReceived;
            service.StatusChanged -= LiveRecognition_StatusChanged;
            await service.DisposeAsync();
        }

        if (savePartial && _manualDraftCues.Count > 0)
        {
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(_manualDraftCues));
            RefreshCueList();
            PersistCurrentCues();
            if (_currentVideo is not null)
            {
                _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
                SaveCurrentFullLyrics();
            }
            _lastAutomaticCue = -1;
        }

        if (AutoRecognitionButton is not null)
        {
            AutoRecognitionButton.Content = "Reconhecer automaticamente (gratuito e offline)";
            AutoRecognitionButton.IsEnabled = true;
        }
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
        _manualDraftCues.Clear();
        _manualDraftActive = true;
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        LyricTextBox.Text = _manualDraftLines[0];
        SecondsBox.Text = "0.00";
        ProjectionPreviewText.Text = "Aguardando a primeira marcação desta nova sincronização...";
        NextProjectionPreviewText.Text = "Próxima: " + _manualDraftLines[0];
        ManualSyncProgressText.Text = "Sincronização ao vivo iniciada: frase 1 de " + _manualDraftLines.Count + ". A projeção será atualizada somente com as frases desta nova letra.";
        ManualCurrentLineText.Text = _manualDraftLines[0];
        _ = ClearHolyricsForLiveDraftAsync();
        StatusText.Text = "Nova sincronização iniciada. A projeção anterior foi separada desta letra; marque a primeira frase quando ela for cantada.";
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
        if (string.IsNullOrWhiteSpace(track))
        {
            StatusText.Text = "Informe o título da música na aba Ao vivo. O intérprete pode ficar em branco.";
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
            var localMatch = _library.SelectMany(folder => folder.Videos)
                .Where(video => video.Cues.Count > 0 && !string.IsNullOrWhiteSpace(video.TrackName))
                .FirstOrDefault(video =>
                    NormalizeMusicText(video.TrackName) == normalizedTrack &&
                    (string.IsNullOrWhiteSpace(normalizedArtist) ||
                     NormalizeMusicText(video.Artist) == normalizedArtist ||
                     (expectedLyrics is not null && CalculateLyricMatch(expectedLyrics, video.Cues) >= 0.72d)) &&
                    (expectedLyrics is null || CalculateLyricMatch(expectedLyrics, video.Cues) >= 0.72d));

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
                StatusText.Text = "O catálogo automático não encontrou a letra. Abrindo outras fontes de pesquisa...";
                await OpenLyricsSourceSearchesAsync(trackName, artistName);
                return;
            }

            var wantedTrack = NormalizeMusicText(trackName);
            var wantedArtist = NormalizeMusicText(artistName);
            var candidates = new List<(double Score, double LyricMatch, string Track, string Artist, string Album, List<LyricCue> Cues)>();
            // Letras do catálogo que existem só como texto, sem tempos (usadas se não houver versão sincronizada).
            var plainCandidates = new List<(double Score, string Track, string Artist, string Album, string Text)>();
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
                if (!trackMatches)
                    continue;
                if (expectedLyrics is null &&
                    item.TryGetProperty("plainLyrics", out var plainElement) && plainElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(plainElement.GetString()))
                {
                    var plainText = plainElement.GetString()!.Replace("\r\n", "\n").Trim();
                    if (plainText.Count(ch => ch == '\n') >= 3)
                        plainCandidates.Add((100d - (normTrack == wantedTrack ? 0 : 12) - (artistMatches ? 0 : 35),
                            foundTrack, foundArtist, foundAlbum, plainText));
                }
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
                var score = 100d - (normTrack == wantedTrack ? 0 : 12) - (artistMatches ? 0 : 35);
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
                        await OpenLyricsSourceSearchesAsync(trackName, artistName);
                    else
                        StatusText.Text = "A sincronização atual foi preservada. Você pode iniciar a marcação ao vivo quando quiser.";
                    return;
                }

                if (plainCandidates.Count > 0)
                {
                    var plain = plainCandidates.OrderByDescending(candidate => candidate.Score).First();
                    var plainPreview = string.Join(Environment.NewLine, plain.Text.Split('\n').Take(10));
                    var plainAlbum = string.IsNullOrWhiteSpace(plain.Album) ? "" : "\nÁlbum: " + plain.Album;
                    var plainDialog = new MusicMatchDialog(
                        "Encontrei a letra, mas sem tempos",
                        "Vídeo carregado: " + (_currentVideo?.Title ?? "") +
                        "\nFaixa encontrada: " + plain.Track + "\nAutor / intérprete: " + plain.Artist + plainAlbum +
                        "\n\nPrévia da letra:\n" + plainPreview +
                        "\n\nSim: colocar esta letra na caixa, salvar e sincronizar com a música.\nNão: não usar.\nCriar: só colocar a letra na caixa para eu revisar antes.") { Owner = this };
                    plainDialog.ShowDialog();
                    if (plainDialog.Choice == MusicMatchChoice.Yes)
                    {
                        OpenManualLyricsEditor(plain.Text);
                        SaveFullLyrics_Click(this, new RoutedEventArgs());
                    }
                    else if (plainDialog.Choice == MusicMatchChoice.Create)
                    {
                        OpenManualLyricsEditor(plain.Text);
                    }
                    else
                    {
                        StatusText.Text = "Letra do catálogo não aplicada.";
                    }
                    return;
                }

                StatusText.Text = "Não encontrei uma letra sincronizada para esta música. Abrindo automaticamente os catálogos alternativos...";
                await OpenLyricsSourceSearchesAsync(trackName, artistName);
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
            _currentRawTitle = title;
            _currentRawChannel = author;
            // Nomes digitados/corrigidos pelo usuário são preservados. Só o que ainda é o título inteiro do YouTube
            // ("Intérprete - Música (Playback)") é separado automaticamente em intérprete e título da música.
            ParsedTrack? pendingNames = null;
            if (!title.StartsWith("Vídeo do YouTube", StringComparison.Ordinal))
            {
                if (TrackTitleParser.LooksUncorrected(_currentVideo.TrackName, title))
                {
                    var parsed = TrackTitleParser.Parse(title, author, KnownArtists());
                    _currentVideo.TrackName = parsed.Track;
                    _currentVideo.Artist = parsed.Artist;
                    if (!parsed.Confident) pendingNames = parsed;
                }
                else if (string.IsNullOrWhiteSpace(_currentVideo.Artist))
                    _currentVideo.Artist = TrackTitleParser.CleanChannel(author);
            }
            MusicTrackBox.Text = _currentVideo.TrackName;
            MusicArtistBox.Text = _currentVideo.Artist;
            _urlHintFor = videoId;
            if (pendingNames is not null) _ = RefineNamesAsync(videoId, pendingNames);
            else _ = CheckCurrentVideoLyricsAsync(videoId);
        }
        if (!saveToHistory) return;

        var existing = _history.FirstOrDefault(item => item.VideoId == videoId);
        if (existing is not null)
        {
            existing.Title = displayTitle;
            existing.TrackName = MusicTrackBox.Text.Trim();
            existing.Artist = MusicArtistBox.Text.Trim();
            existing.Url = url;
            existing.LastUsed = DateTime.Now;
        }
        else _history.Insert(0, new VideoHistoryItem { VideoId = videoId, Title = displayTitle, TrackName = MusicTrackBox.Text.Trim(), Artist = MusicArtistBox.Text.Trim(), Url = url, LastUsed = DateTime.Now });
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
        var filtered = string.IsNullOrWhiteSpace(search) ? _history : _history.Where(item => item.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || item.TrackName.Contains(search, StringComparison.OrdinalIgnoreCase) || item.Composer.Contains(search, StringComparison.OrdinalIgnoreCase) || item.Artist.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
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
                        RefreshNextLyricsPreview();
                        PromoteQueue(); // a primeira da fila vira a próxima
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
                    if (playerState == 0)
                    {
                        var firstEnd = !_playbackEnded;
                        EndPlayback("Reprodução do YouTube finalizada. Projeção encerrada automaticamente.");
                        if (firstEnd) MaybeAutoAdvance();
                    }
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
                if (state == 1 && _autoSyncPending)
                {
                    _autoSyncPending = false;
                    Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try { await StartAutoRecognitionAsync(); }
                        catch (Exception ex) { StatusText.Text = "Não foi possível iniciar a sincronização automática: " + ex.Message; }
                    }));
                }
                if (state == 1) Dispatcher.Invoke(() => StatusText.Text = !_playerReady ? "YouTube tocando; aguardando confirmação do player." : _cues.Count > 0 ? "YouTube tocando. Sincronização de letras ativa." : "YouTube tocando. Nenhuma letra sincronizada foi carregada.");
                else if (state == 2) Dispatcher.Invoke(() => StatusText.Text = "YouTube pausado.");
                else if (state == 0) Dispatcher.Invoke(() =>
                {
                    var firstEnd = !_playbackEnded;
                    EndPlayback("Reprodução do YouTube finalizada. Projeção encerrada automaticamente.");
                    if (firstEnd) MaybeAutoAdvance();
                });
            }
            if (state != 1 || _cues.Count == 0 || _manualProjectionMode || _automaticLyricsActive) return;
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
        if (!_holyricsEnabled) return;
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

    private Task QueueHolyricsCloseProjectionAsync()
    {
        var version = Interlocked.Increment(ref _holyricsUpdateVersion);
        lock (_holyricsQueueSync)
        {
            _holyricsSendQueue = _holyricsSendQueue.ContinueWith(async _ =>
            {
                if (version != Interlocked.Read(ref _holyricsUpdateVersion)) return;
                await _holyrics.CloseProjectionAsync();
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            return _holyricsSendQueue;
        }
    }

    private Task QueueHolyricsCloseQuickAsync()
    {
        var version = Interlocked.Increment(ref _holyricsUpdateVersion);
        lock (_holyricsQueueSync)
        {
            _holyricsSendQueue = _holyricsSendQueue.ContinueWith(async _ =>
            {
                if (version != Interlocked.Read(ref _holyricsUpdateVersion)) return;
                await _holyrics.CloseQuickPresentationAsync();
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            _ = ObserveHolyricsQueueAsync(_holyricsSendQueue);
            return _holyricsSendQueue;
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


    private async void HolyricsEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingPreferences || HolyricsEnabledBox is null) return;
        var enabled = HolyricsEnabledBox.IsChecked == true;
        if (_holyricsEnabled == enabled) return;
        _holyricsEnabled = enabled;
        Interlocked.Increment(ref _holyricsUpdateVersion);
        SaveHolyricsEnabledPreference(enabled);

        if (!enabled)
        {
            try
            {
                if (_holyrics.IsConfigured)
                    await QueueHolyricsCloseQuickAsync();
                StatusText.Text = "Integração Holyrics desativada. A apresentação rápida foi fechada; o programa Holyrics continua aberto.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Integração desativada, mas o fechamento não foi confirmado: " + ex.Message;
            }
            return;
        }

        StatusText.Text = "Integração Holyrics ativada.";
        if (!string.IsNullOrWhiteSpace(_currentProjectedLyric))
            QueueHolyricsUpdate(_currentProjectedLyric);
    }

    private void SaveHolyricsEnabledPreference(bool enabled)
    {
        try
        {
            var current = File.Exists(_preferencesFile)
                ? JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile)) ?? new()
                : new ProjectionPreferences();
            current.HolyricsEnabled = enabled;
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencesFile)!);
            var temp = _preferencesFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _preferencesFile, true);
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível salvar o estado do Holyrics: " + ex.Message; }
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
        _projection?.SetMaxLines(_maxProjectionLines);
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
            _holyricsEnabled = preferences.HolyricsEnabled ?? true;
            _selectedProjectionBackgroundPath = preferences.ProjectionBackgroundPath;
            HolyricsEnabledBox.IsChecked = _holyricsEnabled;
            _autoAdvance = preferences.AutoAdvance ?? false;
            AutoAdvanceBox.IsChecked = _autoAdvance;
            _maxProjectionLines = Math.Clamp(preferences.MaxLines ?? 4, 1, 10);
            SelectComboItem(LineCountBox, _maxProjectionLines.ToString(CultureInfo.InvariantCulture));
            _nextVideoUrl = YouTubeLink.TryGetVideoId(preferences.NextVideoUrl) is { } queuedId
                ? "https://www.youtube.com/watch?v=" + queuedId : null;
            _nextOfflineVideo = null;
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
        public bool? HolyricsEnabled { get; set; } = true;
        public string? ProjectionBackgroundPath { get; set; }
        public string? NextVideoUrl { get; set; }
        public bool? AutoAdvance { get; set; }
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
        // A saída local nunca depende de o Holyrics estar conectado ou responder.
        try
        {
            if (_projection is { IsVisible: true }) _projection.Close();
            LocalProjectionToggleButton.Content = "Abrir projeção própria";
            await QueueHolyricsCloseProjectionAsync();
            StatusText.Text = "Projeção própria e apresentação do Holyrics encerradas. O Lyravoxen permanece aberto.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Projeção própria encerrada. O Holyrics não confirmou o fechamento: " + ex.Message;
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
        if (_projection is { IsVisible: true }) _projection.ShowLyric(safeText);
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
        if (string.IsNullOrWhiteSpace(lyric))
        {
            StatusText.Text = "Digite uma frase ou selecione uma linha antes de marcar.";
            return;
        }

        try
        {
            double currentSeconds;
            if (_isLocalAudioPlaying)
                currentSeconds = LocalAudioPlayer.Position.TotalSeconds;
            else
            {
                var encoded = await YoutubePlayer.CoreWebView2.ExecuteScriptAsync(
                    "typeof player !== 'undefined' && player && typeof player.getCurrentTime === 'function' ? player.getCurrentTime() : null");
                if (!double.TryParse(encoded, NumberStyles.Float, CultureInfo.InvariantCulture, out currentSeconds) || currentSeconds < 0)
                {
                    StatusText.Text = "O player ainda não informou o tempo atual. Inicie a música e tente novamente.";
                    return;
                }
            }

            var cueSeconds = Math.Max(0, currentSeconds);
            var addedCue = new LyricCue { Seconds = cueSeconds, Text = lyric };

            if (_manualDraftActive)
            {
                // A sincronização ao vivo trabalha numa linha do tempo isolada:
                // nenhuma frase antiga da música anterior pode entrar no bloco enviado.
                if (_manualDraftCues.Count > 0 && cueSeconds <= _manualDraftCues[^1].Seconds)
                {
                    StatusText.Text = "O tempo atual precisa ser posterior à última frase marcada. Retome a música e marque novamente.";
                    return;
                }

                _manualDraftCues.Add(addedCue);
                var draftBlock = CueTimeline.BuildProjectionBlock(_manualDraftCues, _manualDraftCues.Count - 1, _maxProjectionLines);
                _currentProjectedLyric = draftBlock;
                ProjectionPreviewText.Text = string.IsNullOrWhiteSpace(draftBlock) ? "Aguardando a próxima frase..." : draftBlock;
                QueueHolyricsUpdate(draftBlock);

                SecondsBox.Text = cueSeconds.ToString("0.00", CultureInfo.InvariantCulture);
                _manualDraftIndex++;
                if (_manualDraftIndex < _manualDraftLines.Count)
                {
                    LyricTextBox.Text = _manualDraftLines[_manualDraftIndex];
                    ManualCurrentLineText.Text = _manualDraftLines[_manualDraftIndex];
                    NextProjectionPreviewText.Text = "Próxima: " + _manualDraftLines[_manualDraftIndex];
                    ManualSyncProgressText.Text = $"Sincronização ao vivo: frase {_manualDraftIndex + 1} de {_manualDraftLines.Count}. Última marcação: {cueSeconds:0.00}s.";
                    StatusText.Text = $"Frase {_manualDraftIndex} marcada em {cueSeconds:0.00}s. Próxima frase pronta.";
                }
                else
                {
                    _manualDraftActive = false;
                    _cues.Clear();
                    _cues.AddRange(CueTimeline.Normalize(_manualDraftCues));
                    RefreshCueList();
                    PersistCurrentCues();
                    _lastAutomaticCue = -1;
                    LyricTextBox.Clear();
                    ManualCurrentLineText.Text = "Sincronização concluída. A nova letra foi salva na biblioteca local.";
                    ManualSyncProgressText.Text = $"Concluído: {_manualDraftCues.Count} frases sincronizadas e salvas para {MusicTrackBox.Text}.";
                    NextProjectionPreviewText.Text = "Próxima: —";
                    StatusText.Text = $"Letra concluída: {_manualDraftCues.Count} frases foram sincronizadas e salvas. A projeção utilizou somente esta nova letra.";
                }
                return;
            }

            _cues.RemoveAll(c => string.Equals(c.Text, lyric, StringComparison.OrdinalIgnoreCase));
            _cues.Add(addedCue);
            _cues.Sort((a,b) => a.Seconds.CompareTo(b.Seconds));
            var markedCueIndex = _cues.IndexOf(addedCue);
            _lastAutomaticCue = markedCueIndex;
            PublishCueBlock(markedCueIndex);
            RefreshCueList(addedCue);
            PersistCurrentCues();
            SecondsBox.Text = cueSeconds.ToString("0.00", CultureInfo.InvariantCulture);
            LyricTextBox.Clear();
            StatusText.Text = "Frase registrada em " + cueSeconds.ToString("0.00", CultureInfo.InvariantCulture) + "s.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Não foi possível marcar a frase: " + ex.Message;
        }
    }

    private async Task ClearHolyricsForLiveDraftAsync()
    {
        try { await QueueHolyricsCloseQuickAsync(); }
        catch (Exception ex)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                StatusText.Text = "Sincronização iniciada, mas não foi possível limpar a projeção anterior: " + ex.Message));
        }
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
        // Esc limpa somente a projeção; não encerra a música nem fecha o Lyravoxen.
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            PublishProjectionOnly("");
            _manualProjectionMode = false;
            _manualCueIndex = -1;
            _lastAutomaticCue = -1;
            if (_projection is { IsVisible: true }) _projection.ShowLyric("");
            if (_holyricsEnabled && _holyrics.IsConfigured)
                _ = ObserveHolyricsQueueAsync(QueueHolyricsCloseProjectionAsync());
            StatusText.Text = "Projeção limpa. A reprodução continua; o aplicativo permanece aberto.";
            e.Handled = true;
            return;
        }

        // Voz (F4), confirmação da Bíblia e marcação por toque têm prioridade sobre os demais atalhos.
        if (HandleVoiceAndTapKeys(e)) return;

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

    private void MusicMetadata_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null) return;
        _currentVideo.TrackName = MusicTrackBox.Text.Trim();
        _currentVideo.Artist = MusicArtistBox.Text.Trim();
        UpdateCurrentHistoryMetadata();
        SaveCurrentVideoMetadata();
    }

    private void UpdateCurrentHistoryMetadata()
    {
        if (_currentVideo is null) return;
        var item = _history.FirstOrDefault(entry => entry.VideoId == _currentVideo.VideoId);
        if (item is null) return;
        item.TrackName = MusicTrackBox.Text.Trim();
        item.Artist = MusicArtistBox.Text.Trim();
        item.LastUsed = DateTime.Now;
        _history.Sort((a, b) => b.LastUsed.CompareTo(a.LastUsed));
        SaveHistory();
        RefreshHistory();
    }

    private void SaveCurrentVideoMetadata()
    {
        if (_currentVideo is null) return;
        foreach (var saved in _library.SelectMany(folder => folder.Videos)
            .Where(item => item.IsOffline == _currentVideo.IsOffline &&
                (item.IsOffline
                    ? string.Equals(item.LocalPath, _currentVideo.LocalPath, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(item.VideoId, _currentVideo.VideoId, StringComparison.Ordinal))))
        {
            saved.Title = _currentVideo.Title;
            saved.TrackName = _currentVideo.TrackName;
            saved.Composer = _currentVideo.Composer;
            saved.Artist = _currentVideo.Artist;
            saved.Url = _currentVideo.Url;
            saved.FullLyrics = _currentVideo.FullLyrics;
        }
        SaveLibrary();
        RefreshLibrarySongs();
    }

    private void PersistCurrentCues()
    {
        if (_currentVideo is null) return;
        _currentVideo.TrackName = MusicTrackBox.Text.Trim();
        _currentVideo.Artist = MusicArtistBox.Text.Trim();
        UpdateCurrentHistoryMetadata();
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
                Composer = _currentVideo.Composer,
                Artist = _currentVideo.Artist,
                Url = _currentVideo.Url,
                IsOffline = _currentVideo.IsOffline,
                LocalPath = _currentVideo.LocalPath,
                Format = _currentVideo.Format,
                FullLyrics = _currentVideo.FullLyrics,
                SavedAt = DateTime.Now,
                Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList()
            });
        }
        else
        {
            foreach (var saved in savedItems)
            {
                saved.SavedAt = DateTime.Now;
                saved.Title = _currentVideo.Title;
                saved.TrackName = _currentVideo.TrackName;
                saved.Composer = _currentVideo.Composer;
                saved.Artist = _currentVideo.Artist;
                saved.Url = _currentVideo.Url;
                saved.FullLyrics = _currentVideo.FullLyrics;
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
        CleanupExpiredOfflineCopies();
        RefreshFolders();
    }

    private void CleanupExpiredOfflineCopies()
    {
        var now = DateTime.Now;
        var changed = false;
        var expiredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var video in _library.SelectMany(folder => folder.Videos)
                     .Where(video => video.IsOffline && video.OfflineExpiresAt.HasValue && video.OfflineExpiresAt.Value <= now))
        {
            if (!string.IsNullOrWhiteSpace(video.LocalPath)) expiredPaths.Add(video.LocalPath);
            video.LocalPath = null;
            video.OfflineExpiresAt = null;
            changed = true;
        }

        foreach (var request in _requests.Where(item => !string.IsNullOrWhiteSpace(item.OfflineLocalPath) &&
                     item.OfflineExpiresAt.HasValue && item.OfflineExpiresAt.Value <= now))
        {
            expiredPaths.Add(request.OfflineLocalPath);
            request.OfflineLocalPath = "";
            request.OfflineExpiresAt = null;
            request.OfflineFormat = "";
            changed = true;
        }

        // Só remove os bytes quando nenhum item da biblioteca ou pedido ainda referencia o mesmo arquivo.
        foreach (var path in expiredPaths)
        {
            var libraryReferences = _library.SelectMany(folder => folder.Videos)
                .Where(video => video.IsOffline).Select(video => video.LocalPath);
            var requestReferences = _requests
                .Where(request => !request.OfflineExpiresAt.HasValue || request.OfflineExpiresAt.Value > now)
                .Select(request => request.OfflineLocalPath);
            if (OfflineExpiryPolicy.CanDelete(path, libraryReferences, requestReferences))
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception ex) { AppLog.Error("Não foi possível remover uma cópia offline vencida", ex); }
            }
        }

        if (changed)
        {
            SaveLibrary();
            SongRequestStore.Save(_requests);
            RefreshRequestsWindow();
        }
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
        if (_selectedLibraryVideo is not null && !filtered.Contains(_selectedLibraryVideo))
            _selectedLibraryVideo = null;
        _librarySelectionChanging = true;
        LibrarySongList.ItemsSource = null;
        LibrarySongList.ItemsSource = filtered;
        _librarySelectionChanging = false;
    }

    private void LibraryFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedLibraryVideo = null;
        RefreshLibrarySongs();
    }
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
                FullLyrics = _currentVideo.FullLyrics,
                Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList()
            });
        else
        {
            video.Title = _currentVideo.Title;
            video.TrackName = MusicTrackBox.Text.Trim();
            video.Artist = MusicArtistBox.Text.Trim();
            video.Url = _currentVideo.Url;
            video.FullLyrics = _currentVideo.FullLyrics;
            video.Cues = _currentVideo.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
        }
        SaveLibrary();
        RefreshLibrarySongs();
        StatusText.Text = $"Vídeo adicionado à pasta '{folder.Name}'.";
    }

    private async void LibrarySong_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_librarySelectionChanging || LibrarySongList.SelectedItem is not LibraryVideo video) return;
        _selectedLibraryVideo = video;
        LibrarySongList.SelectedItem = null;
        if (video.IsOffline)
        {
            if (!string.IsNullOrWhiteSpace(video.LocalPath) && File.Exists(video.LocalPath)) { PlayLocalAudio(video); return; }
            System.Windows.MessageBox.Show(this, "A cópia offline desta música expirou ou não está disponível. Os dados e a letra continuam salvos na biblioteca; importe novamente um arquivo autorizado para ouvir sem internet.", "Cópia offline indisponível", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        await LoadVideoAsync(video.Url, true);
    }


    private async void ImportAudio_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryFolderList.SelectedItem is not LibraryFolder folder)
        {
            System.Windows.MessageBox.Show("Crie ou selecione uma pasta primeiro.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var retention = PromptForText("Validade da cópia offline", "Por quantos dias manter esta cópia local? (5, 7, 14 ou 30)", "30");
        if (retention is null) return;
        if (!int.TryParse(retention.Trim(), out var days) || days is not (5 or 7 or 14 or 30))
        {
            System.Windows.MessageBox.Show("Escolha 5, 7, 14 ou 30 dias.", "Validade offline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var source = PromptForText("Preparar áudio offline", "Cole o endereço direto de um arquivo MP3/WAV autorizado. Deixe vazio para importar um arquivo do computador:");
        if (source is null) return;
        try
        {
            OfflineMediaResult result;
            if (!string.IsNullOrWhiteSpace(source))
            {
                StatusText.Text = "Baixando e validando o áudio autorizado...";
                result = await _offlineMedia.DownloadAsync(source.Trim(), OfflineMediaFolder);
            }
            else
            {
                using var dialog = new System.Windows.Forms.OpenFileDialog
                {
                    Title = "Selecione um áudio MP3 ou WAV que você tem direito de usar",
                    Filter = "Áudios MP3 ou WAV (*.mp3;*.wav)|*.mp3;*.wav",
                    Multiselect = true
                };
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                var expires = DateTime.Now.AddDays(days);
                foreach (var file in dialog.FileNames)
                {
                    var imported = await _offlineMedia.ImportLocalAsync(file, OfflineMediaFolder);
                    AddOrUpdateOfflineLibraryItem(folder, imported, Path.GetFileNameWithoutExtension(file), expires);
                }
                SaveLibrary();
                RefreshFolders(folder.Id);
                StatusText.Text = "Áudio(s) importado(s) e validados para uso offline. Validade até " + expires.ToString("dd/MM/yyyy HH:mm") + ".";
                return;
            }

            var title = Uri.TryCreate(source.Trim(), UriKind.Absolute, out var uri)
                ? Path.GetFileNameWithoutExtension(uri.AbsolutePath)
                : "Áudio offline";
            if (string.IsNullOrWhiteSpace(title)) title = "Áudio offline";
            AddOrUpdateOfflineLibraryItem(folder, result, title, DateTime.Now.AddDays(days));
            SaveLibrary();
            RefreshFolders(folder.Id);
            StatusText.Text = "Áudio baixado, validado e salvo na Biblioteca Offline. Validade até " + DateTime.Now.AddDays(days).ToString("dd/MM/yyyy HH:mm") + ".";
        }
        catch (Exception ex)
        {
            AppLog.Error("Falha ao preparar áudio na Biblioteca Offline", ex);
            StatusText.Text = "Não foi possível preparar o áudio: " + ex.Message;
            System.Windows.MessageBox.Show(this, ex.Message, "Biblioteca Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddOrUpdateOfflineLibraryItem(LibraryFolder folder, OfflineMediaResult result, string title, DateTime expires)
    {
        var existing = _library.SelectMany(f => f.Videos)
            .FirstOrDefault(v => v.IsOffline && string.Equals(v.LocalPath, result.FilePath, StringComparison.OrdinalIgnoreCase));
        var inFolder = folder.Videos.FirstOrDefault(v => v.IsOffline &&
            string.Equals(v.LocalPath, result.FilePath, StringComparison.OrdinalIgnoreCase));
        if (inFolder is not null)
        {
            inFolder.OfflineExpiresAt = expires;
            inFolder.SavedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(inFolder.Title)) inFolder.Title = title;
            return;
        }
        folder.Videos.Add(new LibraryVideo
        {
            VideoId = existing?.VideoId ?? "",
            Title = string.IsNullOrWhiteSpace(existing?.Title) ? title : existing.Title,
            TrackName = string.IsNullOrWhiteSpace(existing?.TrackName) ? title : existing.TrackName,
            Artist = existing?.Artist ?? "",
            Composer = existing?.Composer ?? "",
            Url = existing?.Url ?? "",
            IsOffline = true, LocalPath = result.FilePath,
            Format = result.Format, FullLyrics = existing?.FullLyrics ?? "",
            Cues = existing?.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList() ?? new(),
            SavedAt = DateTime.Now, OfflineExpiresAt = expires
        });
    }

    private void EditLibrarySong_Click(object sender, RoutedEventArgs e)
    {
        var video = _selectedLibraryVideo;
        if (video is null)
        {
            System.Windows.MessageBox.Show(this, "Selecione uma música na lista antes de editar.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var titleBox = new System.Windows.Controls.TextBox { Text = video.TrackName.Length > 0 ? video.TrackName : video.Title, Margin = new Thickness(0, 3, 0, 8), MinHeight = 32 };
        var artistBox = new System.Windows.Controls.TextBox { Text = video.Artist, Margin = new Thickness(0, 3, 0, 8), MinHeight = 32 };
        var lyricsBox = new System.Windows.Controls.TextBox
        {
            Text = video.FullLyrics, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 220, Margin = new Thickness(0, 3, 0, 8)
        };
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Título da música", FontWeight = FontWeights.Bold });
        panel.Children.Add(titleBox);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Intérprete", FontWeight = FontWeights.Bold });
        panel.Children.Add(artistBox);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Letra completa", FontWeight = FontWeights.Bold });
        panel.Children.Add(lyricsBox);
        var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var save = new System.Windows.Controls.Button { Content = "Salvar alterações", MinWidth = 130, Margin = new Thickness(4), Padding = new Thickness(10, 7, 10, 7) };
        var cancel = new System.Windows.Controls.Button { Content = "Cancelar", MinWidth = 90, Margin = new Thickness(4), Padding = new Thickness(10, 7, 10, 7) };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);
        var dialog = new Window
        {
            Title = "Editar música da Biblioteca",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 560, Height = 480, MinWidth = 460, MinHeight = 400,
            ResizeMode = ResizeMode.CanResize,
            Background = Hex("#071A32"),
            Content = panel
        };
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(titleBox.Text))
            {
                System.Windows.MessageBox.Show(dialog, "Informe o título da música.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            video.Title = titleBox.Text.Trim();
            video.TrackName = titleBox.Text.Trim();
            video.Artist = artistBox.Text.Trim();
            video.FullLyrics = lyricsBox.Text.Trim();
            video.SavedAt = DateTime.Now;
            var sameCurrentRecording = _currentVideo is not null &&
                (video.IsOffline
                    ? _currentVideo.IsOffline && string.Equals(_currentVideo.LocalPath, video.LocalPath, StringComparison.OrdinalIgnoreCase)
                    : !_currentVideo.IsOffline && string.Equals(_currentVideo.VideoId, video.VideoId, StringComparison.Ordinal));
            if (sameCurrentRecording && _currentVideo is not null)
            {
                _currentVideo.Title = video.Title;
                _currentVideo.TrackName = video.TrackName;
                _currentVideo.Artist = video.Artist;
                _currentVideo.FullLyrics = video.FullLyrics;
                MusicTrackBox.Text = video.TrackName;
                MusicArtistBox.Text = video.Artist;
                ManualLyricsBox.Text = video.FullLyrics;
            }
            SaveLibrary();
            RefreshLibrarySongs();
            StatusText.Text = "Dados e letra atualizados na Biblioteca.";
            dialog.DialogResult = true;
            dialog.Close();
        };
        cancel.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };
        dialog.ShowDialog();
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
            ManualLyricsBox.Text = video.FullLyrics ?? "";
            ManualSyncProgressText.Text = string.IsNullOrWhiteSpace(video.FullLyrics)
                ? "Esta música ainda não tem letra completa salva."
                : "Letra completa carregada da biblioteca.";
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(video.Cues));
            CueList.ItemsSource = null; CueList.ItemsSource = _cues;
            StatusText.Text = $"Reproduzindo offline: {video.Title} ({video.Format?.ToUpperInvariant()})";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível reproduzir o áudio: " + ex.Message; }
    }

    private void ProcessAudioCue()
    {
        if (!_isLocalAudioPlaying || _cues.Count == 0 || _manualProjectionMode || _automaticLyricsActive) return;
        var seconds = LocalAudioPlayer.Position.TotalSeconds;
        _lastKnownPlaybackSeconds = seconds;
        var cueIndex = FindActiveCue(seconds);
        if (cueIndex == _lastAutomaticCue) return;
        _lastAutomaticCue = cueIndex;
        PublishCueBlock(cueIndex);
    }

    private void LocalAudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        var firstEnd = !_playbackEnded;
        _lastPlayerState = 0; // não deixar estado antigo do YouTube impedir o avanço automático do áudio local
        EndPlayback("Áudio offline finalizado. Projeção encerrada automaticamente.");
        if (firstEnd) MaybeAutoAdvance();
    }

    private void EndPlayback(string status)
    {
        if (_playbackEnded) return;
        _playbackEnded = true;
        _audioTimer.Stop();
        _isLocalAudioPlaying = false;
        if (_liveRecognition is not null) _ = StopAutoRecognitionAsync(savePartial: true);
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        PublishProjectionOnly("");
        StatusText.Text = status;
        _ = CloseHolyricsQuickPresentationAsync();
    }

    private async Task CloseHolyricsQuickPresentationAsync()
    {
        try { await QueueHolyricsCloseQuickAsync(); }
        catch (Exception ex)
        {
            Dispatcher.BeginInvoke(new Action(() => StatusText.Text += " Não foi possível fechar a apresentação rápida do Holyrics: " + ex.Message));
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
        public string TrackName { get; set; } = "";
        public string Composer { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime LastUsed { get; set; }
        public override string ToString()
        {
            var song = string.IsNullOrWhiteSpace(TrackName) ? Title : TrackName;
            var performer = string.IsNullOrWhiteSpace(Artist) ? "" : " — " + Artist;
            var composer = string.IsNullOrWhiteSpace(Composer) ? "" : " | Autor: " + Composer;
            return song + performer + composer;
        }
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        var useLight = !App.IsLightTheme;
        App.SetTheme(useLight);
        ThemeToggleButton.Content = useLight ? "☾" : "☀";
        ThemeToggleButton.ToolTip = useLight ? "Mudar para modo escuro" : "Mudar para modo claro";
    }

}