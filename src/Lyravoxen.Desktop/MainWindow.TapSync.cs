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
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música na aba Ao vivo e dê play.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }
        if (!_isLocalAudioPlaying && _lastPlayerState != 1)
        {
            StatusText.Text = "Dê play na música antes de iniciar a marcação sequencial.";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }

        if (_lyricsEditorCards.Count == 0)
            BuildLyricsCards_Click(this, new RoutedEventArgs());
        if (_lyricsEditorCards.Count == 0)
        {
            StatusText.Text = "Cole ou pesquise a letra e crie os cartões antes de iniciar a marcação.";
            return;
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

        var nextIndex = _lyricsEditorCards
            .Select((card, index) => (card, index))
            .Where(item => !item.card.Seconds.HasValue)
            .Select(item => item.index)
            .DefaultIfEmpty(-1)
            .First();
        if (nextIndex >= 0) LyricsLineCardsList.SelectedIndex = nextIndex;
        LyricsLineCardsList.Focus();

        UpdateTapUi();
        StatusText.Text = "Marcação sequencial iniciada. Pressione Espaço ou clique em Marcar próxima no começo de cada slide.";
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
