using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lyravoxen.Models;

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
        foreach (var occurrence in _lyricsStructureDraft.ExecutionOrder)
        {
            if (!sections.TryGetValue(occurrence.SectionId, out var section)) continue;
            for (var lineIndex = 0; lineIndex < section.Lines.Count; lineIndex++)
            {
                var label = string.IsNullOrWhiteSpace(occurrence.Label) ? section.Label : occurrence.Label;
                _lyricsEditorCards.Add(new LyricsEditorCard
                {
                    SectionId = occurrence.SectionId,
                    SectionLabel = $"{label} · ocorrência {occurrence.OccurrenceIndex}",
                    Text = section.Lines[lineIndex],
                    SourceLineIndex = lineIndex,
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
        StatusText.Text = "Letra organizada em cartões individuais. Repetições identificadas permanecem ligadas ao mesmo texto-fonte.";
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

    private void LyricsLineCardsList_KeyDown(object sender, KeyEventArgs e)
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

        var order = new List<LyricsExecutionOccurrence>();
        (string SectionId, int OccurrenceIndex)? previousKey = null;
        foreach (var card in _lyricsEditorCards)
        {
            var key = (card.SectionId, card.OccurrenceIndex);
            if (previousKey == key) continue;
            var section = _lyricsStructureDraft.Sections.FirstOrDefault(item => item.SectionId == card.SectionId);
            if (section is null) continue;
            order.Add(new LyricsExecutionOccurrence
            {
                SectionId = card.SectionId,
                Label = card.SectionLabel.Split('·')[0].Trim(),
                Kind = section.Kind,
                OccurrenceIndex = card.OccurrenceIndex,
                SourceIndex = card.SourceBlockIndex
            });
            previousKey = key;
        }
        foreach (var section in _currentVideo.Sections)
        {
            section.OccurrenceCount = order.Count(item => item.SectionId == section.SectionId);
        }
        _currentVideo.ExecutionOrder = order;

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
}
