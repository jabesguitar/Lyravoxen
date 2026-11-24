using System.Windows;
using Lyravoxen.Models;

namespace Lyravoxen;

// Marcação manual por toque: um toque por estrofe, para músicas playback (sem voz).
public partial class MainWindow
{
    private bool _tapActive;
    private List<List<string>> _tapStanzas = new();
    private readonly List<double> _tapStarts = new();

    private const double TapSecondsPerLineForLast = 3.0;

    private async void TapStart_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo e dê play.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "Dê play na música antes de iniciar a marcação por toque.";
            return;
        }

        var stanzas = TapSyncPlanner.SplitStanzas(ManualLyricsBox.Text);
        if (stanzas.Count == 0)
        {
            StatusText.Text = "Cole a letra completa (estrofes separadas por linha em branco) antes de marcar.";
            return;
        }

        if (_liveRecognition is not null) await StopAutoRecognitionAsync(savePartial: false);
        _autoSyncPending = false;
        _manualDraftActive = false;

        _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
        SaveCurrentFullLyrics();

        _tapStanzas = stanzas;
        _tapStarts.Clear();
        _tapActive = true;
        // Congela a projeção automática de marcações antigas enquanto você marca.
        _manualProjectionMode = true;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        _ = ClearHolyricsForLiveDraftAsync();
        PublishProjectionOnly("");

        UpdateTapUi();
        TapMarkButton.Focus();
        NextProjectionPreviewText.Text = "Próxima estrofe: " + _tapStanzas[0][0];
        StatusText.Text = "Marcação por toque iniciada. Toque (ou aperte Espaço) quando a 1ª estrofe começar.";
    }

    private void TapMark_Click(object sender, RoutedEventArgs e) => TapMarkStanza();

    private void TapMarkStanza()
    {
        if (!_tapActive) return;
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "A música está pausada. Retome a reprodução para marcar.";
            return;
        }
        if (_tapStarts.Count >= _tapStanzas.Count)
        {
            // Todas marcadas: um toque a mais conclui.
            TapFinish(save: true);
            return;
        }

        var now = GetPlaybackSeconds();
        if (_tapStarts.Count > 0 && now <= _tapStarts[^1] + 0.2)
        {
            StatusText.Text = "Toque muito rápido ou música voltou no tempo. Aguarde a próxima estrofe (Backspace desfaz a última marca).";
            return;
        }

        _tapStarts.Add(now);
        var index = _tapStarts.Count - 1;
        ShowTapStanza(index);
        UpdateTapUi();

        if (_tapStarts.Count >= _tapStanzas.Count)
            StatusText.Text = $"Estrofe {_tapStarts.Count} de {_tapStanzas.Count} marcada em {now:0.0}s. Última! Toque de novo ao fim da música (ou clique em Concluir e salvar).";
        else
            StatusText.Text = $"Estrofe {_tapStarts.Count} de {_tapStanzas.Count} marcada em {now:0.0}s.";
    }

    private void ShowTapStanza(int index)
    {
        var marked = _tapStanzas.Take(index + 1).ToList();
        var provisional = TapSyncPlanner.BuildCues(marked, _tapStarts, TapSecondsPerLineForLast);
        var firstLine = TapSyncPlanner.LinesBefore(_tapStanzas, index);
        var block = CueTimeline.BuildProjectionBlock(provisional, firstLine, _maxProjectionLines);
        PublishLyric(block);
        NextProjectionPreviewText.Text = index + 1 < _tapStanzas.Count
            ? "Próxima estrofe: " + _tapStanzas[index + 1][0]
            : "Próxima: —";
    }

    private void TapUndo_Click(object sender, RoutedEventArgs e) => TapUndo();

    private void TapUndo()
    {
        if (!_tapActive) return;
        if (_tapStarts.Count == 0)
        {
            StatusText.Text = "Ainda não há marcas para desfazer.";
            return;
        }

        _tapStarts.RemoveAt(_tapStarts.Count - 1);
        if (_tapStarts.Count > 0)
        {
            ShowTapStanza(_tapStarts.Count - 1);
        }
        else
        {
            PublishProjectionOnly("");
            _ = ClearHolyricsForLiveDraftAsync();
            NextProjectionPreviewText.Text = "Próxima estrofe: " + _tapStanzas[0][0];
        }
        UpdateTapUi();
        StatusText.Text = $"Última marca desfeita. Estrofes marcadas: {_tapStarts.Count} de {_tapStanzas.Count}.";
    }

    private void TapFinish_Click(object sender, RoutedEventArgs e) => TapFinish(save: true);

    private void TapCancel_Click(object sender, RoutedEventArgs e) => TapFinish(save: false);

    private void TapFinish(bool save)
    {
        if (!_tapActive) return;
        var markedCount = _tapStarts.Count;
        var total = _tapStanzas.Count;
        var cues = save && markedCount > 0
            ? TapSyncPlanner.BuildCues(_tapStanzas, _tapStarts, TapSecondsPerLineForLast)
            : new List<LyricCue>();

        _tapActive = false;
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;

        if (save && cues.Count > 0)
        {
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(cues));
            RefreshCueList();
            PersistCurrentCues();
            var partial = markedCount < total ? $" (marcadas {markedCount} de {total} estrofes; as demais ficaram sem tempo)" : "";
            ManualSyncProgressText.Text = $"Marcação por toque concluída: {_cues.Count} frases salvas para {MusicTrackBox.Text}.{partial}";
            ManualCurrentLineText.Text = "Sincronização por toque salva na biblioteca local.";
            StatusText.Text = $"Marcação por toque salva: {_cues.Count} frases em {markedCount} estrofes{partial}.";
        }
        else
        {
            StatusText.Text = save
                ? "Nenhuma estrofe foi marcada; nada foi salvo."
                : "Marcação por toque cancelada. A sincronização anterior foi mantida.";
        }

        NextProjectionPreviewText.Text = "Próxima: —";
        UpdateTapUi();
    }

    private void UpdateTapUi()
    {
        TapStartButton.IsEnabled = !_tapActive;
        TapMarkButton.IsEnabled = _tapActive;
        TapUndoButton.IsEnabled = _tapActive && _tapStarts.Count > 0;
        TapFinishButton.IsEnabled = _tapActive && _tapStarts.Count > 0;
        TapCancelButton.IsEnabled = _tapActive;

        if (!_tapActive)
        {
            TapMarkButton.Content = "TOQUE · próxima estrofe (Espaço)";
            TapProgressText.Text = "Pronto para marcar.";
            return;
        }

        if (_tapStarts.Count < _tapStanzas.Count)
        {
            TapMarkButton.Content = $"TOQUE · estrofe {_tapStarts.Count + 1} de {_tapStanzas.Count} (Espaço)";
            TapProgressText.Text = "Próxima: " + string.Join(" / ", _tapStanzas[_tapStarts.Count].Take(2));
        }
        else
        {
            TapMarkButton.Content = "Concluir (Espaço)";
            TapProgressText.Text = "Todas as estrofes marcadas.";
        }
    }
}
