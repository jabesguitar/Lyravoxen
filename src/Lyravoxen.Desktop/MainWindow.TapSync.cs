using System.Windows;

namespace Lyravoxen;

// Marcação manual sequencial por cartão/slide.
// Cada cartão representa o bloco de projeção que o usuário visualiza e marca no áudio.
public partial class MainWindow
{
    private bool _tapActive;
    private readonly List<LyricsEditorCard> _tapMarkedCards = new();

    private async void TapStart_Click(object sender, RoutedEventArgs e)
    {
        var selected = LyricsLineCardsList.SelectedItem as LyricsEditorCard;
        if (selected?.Seconds.HasValue == true) selected = null;
        await StartTapSequenceAsync(selected, markImmediately: false);
    }

    private async void TapContinueFromHere_Click(object sender, RoutedEventArgs e)
    {
        if (LyricsLineCardsList.SelectedItem is not LyricsEditorCard selected)
        {
            StatusText.Text = "Selecione o cartão da frase que está sendo cantada e clique em Continuar daqui.";
            return;
        }
        await StartTapSequenceAsync(selected, markImmediately: true);
    }

    private async Task<bool> StartTapSequenceAsync(LyricsEditorCard? preferredCard, bool markImmediately)
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo e dê play.";
            WorkspaceTabs.SelectedIndex = 0;
            return false;
        }
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "A música precisa estar tocando para registrar a posição atual.";
            WorkspaceTabs.SelectedIndex = 0;
            return false;
        }
        if (_lyricsEditorCards.Count == 0)
            BuildLyricsCards_Click(this, new RoutedEventArgs());
        if (_lyricsEditorCards.Count == 0)
        {
            StatusText.Text = "Cole ou pesquise a letra e crie os cartões antes de iniciar a marcação.";
            return false;
        }
        if (_liveRecognition is not null)
            await StopAutoRecognitionAsync(savePartial: false);

        _autoSyncPending = false;
        _manualDraftActive = false;
        _tapMarkedCards.Clear();
        _tapActive = true;
        _manualProjectionMode = true;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;

        var start = preferredCard is not null && _lyricsEditorCards.Contains(preferredCard)
            ? preferredCard
            : _lyricsEditorCards.FirstOrDefault(item => !item.Seconds.HasValue);
        if (start is null)
        {
            _tapActive = false;
            UpdateTapUi();
            StatusText.Text = "Todos os cartões já têm tempo. Selecione um cartão específico para remarcar.";
            return false;
        }
        LyricsLineCardsList.SelectedItem = start;
        LyricsLineCardsList.Focus();
        UpdateTapUi();
        if (markImmediately)
        {
            MarkLyricsCard(start);
            if (start.Seconds.HasValue) _tapMarkedCards.Add(start);
            var next = _lyricsEditorCards.Skip(_lyricsEditorCards.IndexOf(start) + 1)
                .FirstOrDefault(item => !item.Seconds.HasValue);
            if (next is not null) LyricsLineCardsList.SelectedItem = next;
            UpdateTapUi();
            StatusText.Text = $"Posição atual registrada em {start.TimeLabel}. A marcação continuará a partir das próximas frases.";
        }
        else
            StatusText.Text = "Marcação sequencial iniciada deste cartão. Pressione Espaço ou Marcar próxima no começo das frases.";
        return true;
    }

    private void TapMark_Click(object sender, RoutedEventArgs e) => TapMarkStanza();

    private void TapMarkStanza()
    {
        if (!_tapActive) return;
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "A música está pausada. Retome a reprodução para marcar o próximo cartão.";
            return;
        }

        var card = LyricsLineCardsList.SelectedItem as LyricsEditorCard;
        if (card is null || card.Seconds.HasValue)
            card = _lyricsEditorCards.FirstOrDefault(item => !item.Seconds.HasValue);

        if (card is null)
        {
            StatusText.Text = "Todos os cartões já têm marcação. Conclua e salve a sequência.";
            UpdateTapUi();
            return;
        }

        LyricsLineCardsList.SelectedItem = card;
        MarkLyricsCard(card);
        if (!card.Seconds.HasValue) return;

        _tapMarkedCards.Add(card);
        ProjectionPreviewText.Text = card.Text;
        var next = _lyricsEditorCards.FirstOrDefault(item => !item.Seconds.HasValue);
        NextProjectionPreviewText.Text = next is null ? "Fim da sequência." : "Próximo cartão: " + next.Text;
        UpdateTapUi();
        StatusText.Text = $"Cartão {card.SectionLabel} marcado em {card.TimeLabel}. Continue com Espaço ou clique em Marcar próxima.";
    }

    private void TapUndo() => TapUndo_Click(this, new RoutedEventArgs());

    private void TapUndo_Click(object sender, RoutedEventArgs e)
    {
        if (!_tapActive)
        {
            UndoLyricsMark_Click(this, new RoutedEventArgs());
            return;
        }
        if (_tapMarkedCards.Count == 0)
        {
            StatusText.Text = "Não há marcação desta sequência para desfazer.";
            return;
        }
        UndoLyricsMark_Click(this, new RoutedEventArgs());
    }

    private void TapFinish_Click(object sender, RoutedEventArgs e) => TapFinish(save: true);
    private void TapCancel_Click(object sender, RoutedEventArgs e) => TapFinish(save: false);

    private void TapFinish(bool save)
    {
        if (!_tapActive) return;

        var markedCount = _tapMarkedCards.Count;
        if (!save)
        {
            while (_tapMarkedCards.Count > 0)
                UndoLyricsMark_Click(this, new RoutedEventArgs());
            StatusText.Text = "Marcação sequencial cancelada. Os tempos feitos nesta sessão foram desfeitos.";
        }
        else
        {
            SaveLyricsEditor_Click(this, new RoutedEventArgs());
            StatusText.Text = $"Marcação sequencial concluída: {markedCount} cartões marcados nesta sessão.";
        }

        _tapMarkedCards.Clear();
        _tapActive = false;
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        UpdateTapUi();
    }

    private void UpdateTapUi()
    {
        if (!_tapActive)
        {
            LyricsEditorStatus.Text = "Editor pronto. Selecione os cartões e marque cada bloco quando começar.";
            return;
        }

        var total = _lyricsEditorCards.Count;
        var completed = _lyricsEditorCards.Count(card => card.Seconds.HasValue);
        var next = _lyricsEditorCards.FirstOrDefault(card => !card.Seconds.HasValue);
        LyricsEditorStatus.Text = $"MARCAÇÃO SEQUENCIAL · {completed} de {total} cartões com tempo.";
        ManualSyncProgressText.Text = next is null
            ? "Todos os cartões estão marcados. Clique em Concluir sequência e salvar."
            : "Próximo cartão: " + next.SectionLabel + " — " + next.Text + " · Espaço marca o selecionado.";
    }
}
