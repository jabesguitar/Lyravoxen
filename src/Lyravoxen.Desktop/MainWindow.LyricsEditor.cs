using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

public sealed class LyricsEditorCard : INotifyPropertyChanged
{
    private double? _seconds;
    private double? _confidence;
    private bool _needsReview;
    private bool _isManual;

    public string SectionId { get; init; } = "";
    public string SectionLabel { get; init; } = "";
    public string Text { get; init; } = "";
    public int SourceLineIndex { get; init; }
    public int SourceLineCount { get; init; } = 1;
    public int SourceBlockIndex { get; init; }
    public int OccurrenceIndex { get; init; }
    public bool IsRepeat { get; init; }

    public double? Seconds
    {
        get => _seconds;
        set { if (_seconds == value) return; _seconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeLabel)); OnPropertyChanged(nameof(MarkButtonText)); }
    }

    public double? Confidence
    {
        get => _confidence;
        set { if (_confidence == value) return; _confidence = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusLabel)); }
    }

    public bool NeedsReview
    {
        get => _needsReview;
        set { if (_needsReview == value) return; _needsReview = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusLabel)); }
    }

    public bool IsManual
    {
        get => _isManual;
        set { if (_isManual == value) return; _isManual = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusLabel)); }
    }

    public string TimeLabel => Seconds.HasValue
        ? TimeSpan.FromSeconds(Math.Max(0, Seconds.Value)).ToString(@"mm\:ss\.f")
        : "Ainda não marcado";

    public string MarkButtonText => Seconds.HasValue ? "REMARCAR" : "MARCAR AGORA";

    public string StatusLabel => NeedsReview
        ? "REVISAR · tempo estimado ou baixa confiança"
        : IsManual
            ? "MARCADO MANUALMENTE"
            : Confidence.HasValue
                ? $"AUTOMÁTICO · confiança {Confidence.Value:P0}"
                : "AGUARDANDO MARCAÇÃO";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class MainWindow
{
    private readonly ObservableCollection<LyricsEditorCard> _lyricsEditorCards = new();
    private LyricsStructureAnalysis? _lyricsStructureDraft;
    private readonly Stack<(LyricsEditorCard Card, double? Seconds, double? Confidence, bool NeedsReview, bool IsManual)> _lyricsMarkUndo = new();
    private List<LyricsEditorCard> _automaticCardSequence = new();
    private List<(LyricsEditorCard Card, string Line, int CardIndex, int LineIndex)> _automaticCardLines = new();

    private void InitializeLyricsEditor()
    {
        LyricsLineCardsList.ItemsSource = _lyricsEditorCards;
    }

    private void BuildLyricsCards_Click(object sender, RoutedEventArgs e)
    {
        var source = ManualLyricsBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            StatusText.Text = "Cole ou digite a letra completa antes de criar os cartões.";
            ManualLyricsBox.Focus();
            return;
        }

        _lyricsStructureDraft = LyricsStructureAnalyzer.Analyze(source);
        _lyricsEditorCards.Clear();
        _lyricsMarkUndo.Clear();

        var sections = _lyricsStructureDraft.Sections.ToDictionary(section => section.SectionId);
        var savedOrder = _currentVideo?.ExecutionOrder
            .Where(item => sections.ContainsKey(item.SectionId))
            .ToList() ?? new List<LyricsExecutionOccurrence>();
        var execution = savedOrder.Count > 0 &&
            savedOrder.Count == _currentVideo?.ExecutionOrder.Count
                ? savedOrder
                : _lyricsStructureDraft.ExecutionOrder;
        var linesPerSlide = Math.Clamp(_maxProjectionLines, 1, 10);
        foreach (var occurrence in execution)
        {
            if (!sections.TryGetValue(occurrence.SectionId, out var section)) continue;
            var slideStarts = occurrence.LineOrder.Count > 0
                ? occurrence.LineOrder.Where(index => index >= 0 && index < section.Lines.Count).ToList()
                : Enumerable.Range(0, (section.Lines.Count + linesPerSlide - 1) / linesPerSlide)
                    .Select(page => page * linesPerSlide).ToList();
            foreach (var lineStart in slideStarts)
            {
                var slideLines = section.Lines.Skip(lineStart).Take(linesPerSlide).ToList();
                if (slideLines.Count == 0) continue;
                var label = string.IsNullOrWhiteSpace(occurrence.Label) ? section.Label : occurrence.Label;
                _lyricsEditorCards.Add(new LyricsEditorCard
                {
                    SectionId = occurrence.SectionId,
                    SectionLabel = $"{label} · ocorrência {occurrence.OccurrenceIndex}",
                    Text = string.Join(Environment.NewLine, slideLines),
                    SourceLineIndex = lineStart,
                    SourceLineCount = slideLines.Count,
                    SourceBlockIndex = section.SourceIndex,
                    OccurrenceIndex = occurrence.OccurrenceIndex,
                    IsRepeat = occurrence.OccurrenceIndex > 1
                });
            }
        }

        LyricsEditorStatus.Text = $"{_lyricsEditorCards.Count} cartões criados · {_lyricsStructureDraft.Sections.Count} blocos-fonte. Selecione uma linha e marque no instante em que ela começa.";
        ManualSyncProgressText.Text = "Cartões prontos. Toque em MARCAR AGORA no início de cada frase ou use o assistente automático.";
        ManualCurrentLineText.Text = _lyricsEditorCards.FirstOrDefault()?.Text ?? "Nenhuma linha disponível.";
        LyricsLineCardsList.SelectedIndex = _lyricsEditorCards.Count > 0 ? 0 : -1;
        ApplySavedCueTimesToCards();
        StatusText.Text = "Letra organizada em cartões individuais. Repetições identificadas permanecem ligadas ao mesmo texto-fonte.";
    }

    private void ReloadLyricsCardsFromCurrentVideo()
    {
        if (_currentVideo is null)
        {
            _lyricsEditorCards.Clear();
            _lyricsStructureDraft = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(ManualLyricsBox.Text))
            ManualLyricsBox.Text = _currentVideo.FullLyrics ?? "";

        if (string.IsNullOrWhiteSpace(ManualLyricsBox.Text))
        {
            _lyricsEditorCards.Clear();
            _lyricsStructureDraft = null;
            LyricsEditorStatus.Text = "Esta música ainda não tem letra. Pesquise ou cole a letra para criar os cartões.";
            return;
        }

        BuildLyricsCards_Click(this, new RoutedEventArgs());
    }

    private void ApplySavedCueTimesToCards()
    {
        if (_currentVideo is null || _currentVideo.Cues.Count == 0 || _lyricsEditorCards.Count == 0) return;
        var used = new HashSet<LyricCue>();
        foreach (var card in _lyricsEditorCards)
        {
            var cue = _currentVideo.Cues.FirstOrDefault(item =>
                !used.Contains(item) &&
                !string.IsNullOrWhiteSpace(item.SectionId) &&
                item.SectionId == card.SectionId &&
                item.OccurrenceIndex == card.OccurrenceIndex &&
                item.SourceLineIndex == card.SourceLineIndex);
            cue ??= _currentVideo.Cues.FirstOrDefault(item =>
                !used.Contains(item) &&
                (string.Equals(item.Text.Trim(), card.Text.Trim(), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(item.Text.Trim(), card.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim(), StringComparison.OrdinalIgnoreCase)));
            if (cue is null) continue;

            used.Add(cue);
            card.Seconds = cue.Seconds;
            card.Confidence = cue.Confidence;
            card.NeedsReview = cue.NeedsReview;
            card.IsManual = cue.IsManual;
        }
    }

    private void LyricsLineCardsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LyricsLineCardsList.SelectedItem is not LyricsEditorCard card) return;
        ProjectionPreviewText.Text = card.Text;
        var nextIndex = LyricsLineCardsList.SelectedIndex + 1;
        NextProjectionPreviewText.Text = nextIndex >= 0 && nextIndex < _lyricsEditorCards.Count
            ? "Próxima frase: " + _lyricsEditorCards[nextIndex].Text
            : "Fim da sequência cadastrada.";
        ManualCurrentLineText.Text = card.Text;
    }

    private void MarkLyricsLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is LyricsEditorCard card)
            MarkLyricsCard(card);
    }

    private void MarkSelectedLyricsLine_Click(object sender, RoutedEventArgs e)
    {
        if (LyricsLineCardsList.SelectedItem is LyricsEditorCard card)
            MarkLyricsCard(card);
        else
            StatusText.Text = "Selecione o cartão da frase que deseja marcar.";
    }

    private void MarkLyricsCard(LyricsEditorCard card)
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue a música na aba Ao vivo antes de marcar os cartões.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "Dê play na música e marque o cartão exatamente quando a frase começar.";
            return;
        }

        _lyricsMarkUndo.Push((card, card.Seconds, card.Confidence, card.NeedsReview, card.IsManual));
        card.Seconds = GetPlaybackSeconds();
        card.Confidence = 1.0;
        card.NeedsReview = false;
        card.IsManual = true;
        LyricsEditorStatus.Text = $"Marcado em {card.TimeLabel} · {card.SectionLabel}";
        StatusText.Text = $"Frase marcada em {card.TimeLabel}: {card.Text}";
        var index = _lyricsEditorCards.IndexOf(card);
        if (index >= 0 && index + 1 < _lyricsEditorCards.Count)
            LyricsLineCardsList.SelectedIndex = index + 1;
    }

    private void UndoLyricsMark_Click(object sender, RoutedEventArgs e)
    {
        if (_lyricsMarkUndo.Count == 0)
        {
            StatusText.Text = "Ainda não há marcação para desfazer.";
            return;
        }
        var previous = _lyricsMarkUndo.Pop();
        previous.Card.Seconds = previous.Seconds;
        previous.Card.Confidence = previous.Confidence;
        previous.Card.NeedsReview = previous.NeedsReview;
        previous.Card.IsManual = previous.IsManual;
        LyricsLineCardsList.SelectedItem = previous.Card;
        StatusText.Text = "Última marcação desfeita.";
    }

    private void RepeatLyricsBlock_Click(object sender, RoutedEventArgs e)
    {
        if (LyricsLineCardsList.SelectedItem is not LyricsEditorCard selected)
        {
            StatusText.Text = "Selecione uma linha do bloco que deseja repetir.";
            return;
        }

        var sourceRows = _lyricsEditorCards
            .Where(card => card.SectionId == selected.SectionId && card.OccurrenceIndex == selected.OccurrenceIndex)
            .ToList();
        if (sourceRows.Count == 0) return;

        var nextOccurrence = _lyricsEditorCards
            .Where(card => card.SectionId == selected.SectionId)
            .Select(card => card.OccurrenceIndex)
            .DefaultIfEmpty(0).Max() + 1;

        foreach (var source in sourceRows)
        {
            _lyricsEditorCards.Add(new LyricsEditorCard
            {
                SectionId = source.SectionId,
                SectionLabel = source.SectionLabel.Split('·')[0].Trim() + $" · ocorrência {nextOccurrence}",
                Text = source.Text,
                SourceLineIndex = source.SourceLineIndex,
                SourceBlockIndex = source.SourceBlockIndex,
                OccurrenceIndex = nextOccurrence,
                IsRepeat = true
            });
        }

        LyricsLineCardsList.SelectedItem = _lyricsEditorCards[^sourceRows.Count];
        LyricsEditorStatus.Text = $"Bloco repetido como ocorrência {nextOccurrence}. O texto-fonte não foi duplicado.";
        StatusText.Text = "Repetição adicionada à sequência. Marque os novos cartões no momento em que forem cantados.";
    }

    private void MoveLyricsCardUp_Click(object sender, RoutedEventArgs e) => MoveSelectedLyricsCard(-1);
    private void MoveLyricsCardDown_Click(object sender, RoutedEventArgs e) => MoveSelectedLyricsCard(1);

    private void MoveSelectedLyricsCard(int direction)
    {
        if (LyricsLineCardsList.SelectedItem is not LyricsEditorCard selected) return;
        var index = _lyricsEditorCards.IndexOf(selected);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= _lyricsEditorCards.Count) return;
        _lyricsEditorCards.Move(index, target);
        LyricsLineCardsList.SelectedItem = selected;
    }

    private void LyricsLineCardsList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space && LyricsLineCardsList.SelectedItem is LyricsEditorCard card)
        {
            MarkLyricsCard(card);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            MoveSelectedLyricsCard(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            MoveSelectedLyricsCard(1);
            e.Handled = true;
        }
    }

    private void SaveLyricsEditor_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }
        if (_lyricsEditorCards.Count == 0 || string.IsNullOrWhiteSpace(ManualLyricsBox.Text))
        {
            BuildLyricsCards_Click(sender, e);
            if (_lyricsEditorCards.Count == 0) return;
        }

        _lyricsStructureDraft ??= LyricsStructureAnalyzer.Analyze(ManualLyricsBox.Text);
        _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
        _currentVideo.Sections = CloneLyricsSections(_lyricsStructureDraft.Sections);

        _currentVideo.ExecutionOrder = BuildExecutionOrderFromCards();

        var marked = _lyricsEditorCards
            .Where(card => card.Seconds.HasValue)
            .Select(card => new LyricCue
            {
                Seconds = card.Seconds!.Value,
                Text = card.Text,
                SectionId = card.SectionId,
                OccurrenceIndex = card.OccurrenceIndex,
                SourceLineIndex = card.SourceLineIndex,
                Confidence = card.Confidence,
                NeedsReview = card.NeedsReview,
                IsManual = card.IsManual
            })
            .OrderBy(cue => cue.Seconds)
            .ToList();

        _cues.Clear();
        _cues.AddRange(CueTimeline.Normalize(marked));
        RefreshCueList();
        PersistCurrentCues();

        var unmarked = _lyricsEditorCards.Count - marked.Count;
        var reviewed = marked.Count(cue => cue.NeedsReview);
        LyricsEditorStatus.Text = $"{marked.Count}/{_lyricsEditorCards.Count} cartões sincronizados · {unmarked} sem marcação · {reviewed} para revisar.";
        ManualSyncProgressText.Text = unmarked == 0
            ? "Sincronização salva. A letra-fonte e as ocorrências estão preservadas separadamente."
            : $"Sincronização parcial salva. {unmarked} cartões ainda não têm tempo; marque-os depois.";
        StatusText.Text = $"Cadastro salvo para {MusicTrackBox.Text}: {marked.Count} frases com tempo. A letra original foi preservada.";
    }


    private List<LyricsExecutionOccurrence> BuildExecutionOrderFromCards()
    {
        var order = new List<LyricsExecutionOccurrence>();
        if (_lyricsStructureDraft is null) return order;

        var index = 0;
        while (index < _lyricsEditorCards.Count)
        {
            var first = _lyricsEditorCards[index];
            var key = (first.SectionId, first.OccurrenceIndex);
            var end = index + 1;
            while (end < _lyricsEditorCards.Count &&
                   _lyricsEditorCards[end].SectionId == key.SectionId &&
                   _lyricsEditorCards[end].OccurrenceIndex == key.OccurrenceIndex)
                end++;

            var section = _lyricsStructureDraft.Sections.FirstOrDefault(item => item.SectionId == first.SectionId);
            if (section is not null)
            {
                order.Add(new LyricsExecutionOccurrence
                {
                    SectionId = first.SectionId,
                    Label = first.SectionLabel.Split('·')[0].Trim(),
                    Kind = section.Kind,
                    OccurrenceIndex = first.OccurrenceIndex,
                    SourceIndex = first.SourceBlockIndex,
                    LineOrder = _lyricsEditorCards.Skip(index).Take(end - index)
                        .Select(card => card.SourceLineIndex).ToList()
                });
            }
            index = end;
        }

        foreach (var section in _lyricsStructureDraft.Sections)
            section.OccurrenceCount = order.Count(item => item.SectionId == section.SectionId);
        return order;
    }

    private static LyricCue CloneLyricCue(LyricCue cue) => new()
    {
        Seconds = cue.Seconds,
        Text = cue.Text,
        SectionId = cue.SectionId,
        OccurrenceIndex = cue.OccurrenceIndex,
        SourceLineIndex = cue.SourceLineIndex,
        Confidence = cue.Confidence,
        NeedsReview = cue.NeedsReview,
        IsManual = cue.IsManual
    };

    private sealed record LyricsCatalogResult(
        string Track, string Artist, string Album, double Duration,
        string PlainLyrics, List<LyricCue> TimedCues)
    {
        public string Display => string.IsNullOrWhiteSpace(Artist) ? Track : Track + " — " + Artist;
        public string Preview => !string.IsNullOrWhiteSpace(PlainLyrics)
            ? PlainLyrics
            : string.Join(Environment.NewLine, TimedCues.Select(cue => cue.Text));
    }

    private async void OpenLyricsWebSearches_Click(object sender, RoutedEventArgs e)
    {
        var track = MusicTrackBox.Text.Trim();
        var artist = MusicArtistBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(track))
        {
            StatusText.Text = "Informe o título ou selecione uma música antes de abrir outras fontes.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }

        await OpenLyricsSourceSearchesAsync(track, artist);
    }

    private async void SearchLyricsCatalog_Click(object sender, RoutedEventArgs e)
    {
        var query = LyricsCatalogSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            query = (MusicTrackBox.Text + " " + MusicArtistBox.Text).Trim();
            LyricsCatalogSearchBox.Text = query;
        }
        if (query.Length == 0)
        {
            StatusText.Text = "Digite o título, intérprete ou um trecho da letra para pesquisar.";
            LyricsCatalogSearchBox.Focus();
            return;
        }

        try
        {
            StatusText.Text = "Pesquisando no catálogo gratuito de letras e sincronizações...";
            var url = "https://lrclib.net/api/search?q=" + Uri.EscapeDataString(query);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Lyravoxen/1.0 (https://github.com/jabesguitar/Lyravoxen)");
            using var response = await MetadataHttp.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("O catálogo respondeu em um formato inesperado.");

            var results = new List<LyricsCatalogResult>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var track = item.TryGetProperty("trackName", out var trackNode) ? trackNode.GetString() ?? "" : "";
                var artist = item.TryGetProperty("artistName", out var artistNode) ? artistNode.GetString() ?? "" : "";
                var album = item.TryGetProperty("albumName", out var albumNode) ? albumNode.GetString() ?? "" : "";
                var duration = item.TryGetProperty("duration", out var durationNode) && durationNode.ValueKind == JsonValueKind.Number
                    ? durationNode.GetDouble() : 0;
                var plain = item.TryGetProperty("plainLyrics", out var plainNode) && plainNode.ValueKind == JsonValueKind.String
                    ? plainNode.GetString() ?? "" : "";
                var synced = item.TryGetProperty("syncedLyrics", out var syncedNode) && syncedNode.ValueKind == JsonValueKind.String
                    ? syncedNode.GetString() ?? "" : "";
                var timed = string.IsNullOrWhiteSpace(synced)
                    ? new List<LyricCue>()
                    : ParseTimedLyrics(synced, ".lrc").OrderBy(cue => cue.Seconds).ToList();
                if (string.IsNullOrWhiteSpace(plain) && timed.Count == 0) continue;
                results.Add(new LyricsCatalogResult(track, artist, album, duration, plain.Trim(), timed));
            }

            if (results.Count == 0)
            {
                StatusText.Text = "O catálogo gratuito não encontrou resultados. Vou abrir as fontes alternativas de pesquisa.";
                await OpenLyricsSourceSearchesAsync(query, "");
                return;
            }

            var window = new Window
            {
                Title = "Pesquisa de letras — Lyravoxen",
                Owner = this,
                Width = 920,
                Height = 650,
                MinWidth = 720,
                MinHeight = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Hex("#07172C"),
                Foreground = Hex("#F2F6F8")
            };
            var root = new Grid { Margin = new Thickness(16) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var heading = new TextBlock
            {
                Text = $"RESULTADOS PARA “{query}” · {results.Count} encontradas",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = Hex("#00D9F5"), Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetColumnSpan(heading, 2);
            root.Children.Add(heading);

            var list = new ListBox
            {
                ItemsSource = results,
                DisplayMemberPath = nameof(LyricsCatalogResult.Display),
                Background = Hex("#0B2450"),
                Foreground = Hex("#F2F6F8"),
                BorderBrush = Hex("#245B88"),
                FontSize = 13,
                Margin = new Thickness(0, 0, 9, 0)
            };
            Grid.SetRow(list, 1);
            root.Children.Add(list);

            var previewPanel = new DockPanel();
            var previewInfo = new TextBlock
            {
                Text = "Selecione uma versão para conferir a letra e os tempos.",
                Foreground = Hex("#B8CAD5"), FontSize = 11, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 0, 4, 8)
            };
            DockPanel.SetDock(previewInfo, Dock.Top);
            previewPanel.Children.Add(previewInfo);
            var preview = new TextBox
            {
                IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 13, Padding = new Thickness(10),
                Background = Hex("#020A18"), Foreground = Hex("#F2F6F8"),
                BorderBrush = Hex("#245B88")
            };
            previewPanel.Children.Add(preview);
            Grid.SetRow(previewPanel, 1);
            Grid.SetColumn(previewPanel, 1);
            root.Children.Add(previewPanel);

            LyricsCatalogResult? selected = null;
            list.SelectionChanged += (_, _) =>
            {
                selected = list.SelectedItem as LyricsCatalogResult;
                preview.Text = selected?.Preview ?? "";
                previewInfo.Text = selected is null
                    ? "Selecione uma versão."
                    : selected.Display +
                      (string.IsNullOrWhiteSpace(selected.Album) ? "" : " · " + selected.Album) +
                      (selected.Duration > 0 ? " · " + TimeSpan.FromSeconds(selected.Duration).ToString(@"mm\:ss") : "") +
                      (selected.TimedCues.Count > 0 ? $" · {selected.TimedCues.Count} tempos encontrados" : " · letra sem tempos");
            };
            if (results.Count > 0) list.SelectedIndex = 0;

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var useTimed = new Button
            {
                Content = "Usar letra + tempos",
                MinWidth = 150, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(5, 12, 0, 0),
                Background = Hex("#087F9E"), Foreground = Hex("#FFFFFF"), FontWeight = FontWeights.Bold,
                IsEnabled = false
            };
            var useText = new Button
            {
                Content = "Usar texto para revisar",
                MinWidth = 150, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(5, 12, 0, 0),
                Background = Hex("#153B69"), Foreground = Hex("#FFFFFF"), IsEnabled = false
            };
            var cancel = new Button
            {
                Content = "Cancelar", MinWidth = 95, Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(5, 12, 0, 0), Background = Hex("#153B69"), Foreground = Hex("#FFFFFF"), IsCancel = true
            };
            void RefreshButtons()
            {
                useText.IsEnabled = selected is not null && !string.IsNullOrWhiteSpace(selected.Preview);
                useTimed.IsEnabled = selected is not null && selected.TimedCues.Count > 1;
            }
            list.SelectionChanged += (_, _) => RefreshButtons();
            var importTimed = false;
            useTimed.Click += (_, _) => { importTimed = true; window.DialogResult = true; };
            useText.Click += (_, _) => { importTimed = false; window.DialogResult = true; };
            buttons.Children.Add(cancel);
            buttons.Children.Add(useText);
            buttons.Children.Add(useTimed);
            Grid.SetRow(buttons, 2);
            Grid.SetColumnSpan(buttons, 2);
            root.Children.Add(buttons);
            window.Content = root;

            if (window.ShowDialog() != true || selected is null) return;
            if (_currentVideo is null)
            {
                StatusText.Text = "Letra selecionada. Carregue primeiro o vídeo da música na aba Ao vivo para vincular e salvar.";
                return;
            }

            MusicTrackBox.Text = selected.Track;
            MusicArtistBox.Text = selected.Artist;
            _currentVideo.TrackName = selected.Track;
            _currentVideo.Artist = selected.Artist;
            var importedCues = importTimed ? selected.TimedCues : new List<LyricCue>();
            var sourceText = importTimed && importedCues.Count > 1
                ? string.Join(Environment.NewLine, importedCues.Select(cue => cue.Text))
                : !string.IsNullOrWhiteSpace(selected.PlainLyrics)
                    ? selected.PlainLyrics
                    : string.Join(Environment.NewLine, importedCues.Select(cue => cue.Text));
            ManualLyricsBox.Text = sourceText;
            _currentVideo.FullLyrics = sourceText;
            _currentVideo.Sections.Clear();
            _currentVideo.ExecutionOrder.Clear();
            _currentVideo.Cues = importedCues.Select(cue => new LyricCue
            {
                Seconds = cue.Seconds, Text = cue.Text, Confidence = 1.0,
                NeedsReview = false, IsManual = false
            }).ToList();
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(_currentVideo.Cues));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            ReloadLyricsCardsFromCurrentVideo();
            SaveLyricsEditor_Click(this, new RoutedEventArgs());
            LyricsEditorStatus.Text = importTimed
                ? $"Letra importada com {selected.TimedCues.Count} marcações do catálogo. Confira e ajuste os cartões."
                : "Texto importado sem tempos. Marque as frases manualmente ou use o reconhecimento durante a reprodução.";
            StatusText.Text = "Letra encontrada no catálogo gratuito. Confira título, intérprete, autoria e permissões antes de projetar.";
            WorkspaceTabs.SelectedItem = ManualSyncTab;
        }
        catch (TaskCanceledException)
        {
            StatusText.Text = "A pesquisa de letras foi cancelada por tempo limite.";
        }
        catch (HttpRequestException ex)
        {
            StatusText.Text = "Não foi possível acessar o catálogo gratuito de letras: " + ex.Message;
        }
        catch (JsonException)
        {
            StatusText.Text = "O catálogo respondeu com dados inválidos.";
        }
        catch (Exception ex)
        {
            AppLog.Error("Pesquisa integrada de letras falhou", ex);
            StatusText.Text = "Falha ao pesquisar letras: " + ex.Message;
        }
    }

}
