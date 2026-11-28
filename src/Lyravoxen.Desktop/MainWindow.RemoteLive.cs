using System.Windows;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

// Aba Ao vivo do celular: limpar telão, descartar/criar letra, marcação por toque e rascunho de tempos pelo celular.
public partial class MainWindow
{
    private string? _nextRejectedVideoId;
    private readonly AudioMonitorService _audioMonitor = new();

    private bool _reopenProjectionAfterClear;

    /// <summary>Mesma ação do "Limpar telão" da Bíblia (fecha a tela própria e limpa o Holyrics), mais congelar a letra automática
    /// para ela não voltar sozinha. "Voltar ao automático" reabre a tela própria e retoma.</summary>
    private string ClearLiveProjectionForRemote()
    {
        if (_tapActive) return "A marcação por toque está ativa. Conclua ou cancele antes de limpar o telão.";
        _reopenProjectionAfterClear = _projection is { IsVisible: true };
        _manualProjectionMode = true;
        _manualCueIndex = -1;
        _lastAutomaticCue = -1;
        PublishProjectionOnly("");
        ClearBibleProjection_Click(this, new RoutedEventArgs());
        return "Telão do PC limpo. Toque em \"Voltar ao automático\" para retomar a letra.";
    }

    private void ReopenProjectionAfterLiveClear()
    {
        if (!_reopenProjectionAfterClear) return;
        _reopenProjectionAfterClear = false;
        EnsureProjectionWindow();
        LocalProjectionToggleButton.Content = "Fechar projeção própria";
    }

    /// <summary>Capítulo anterior/próximo pelo celular: além de trocar o capítulo, projeta o versículo 1 (como o passo de versículo).</summary>
    private async Task ChangeBibleChapterForRemoteAsync(int direction)
    {
        var bookBefore = BibleBooksList.SelectedItem;
        await ChangeBibleChapterAsync(direction);
        if (!ReferenceEquals(bookBefore, BibleBooksList.SelectedItem)) await Task.Delay(700); // mudou de livro: espera carregar o capítulo
        await ProjectBibleSelectionAsync();
    }

    private string RejectNextLyricsForRemote()
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Não há próxima música preparada.";
        _nextRejectedVideoId = videoId;
        NextLyricsText.Text = "Sugestão de letra descartada. Verifique com outros nomes ou crie a letra.";
        return "Sugestão descartada. Digite música e autor e toque em Verificar, ou toque em Criar letra.";
    }

    private (string Track, string Artist) NextNames(string videoId)
    {
        var typedTrack = _nextTyped is { } typed && typed.VideoId == videoId ? typed.Track : "";
        var typedArtist = _nextTyped is { } typedBoth && typedBoth.VideoId == videoId ? typedBoth.Artist : "";
        if (typedTrack.Length > 0) return (typedTrack, typedArtist);
        var preview = _nextLyricsVideoId == videoId ? _nextLyrics : null;
        var track = FirstFilled(preview?.FoundTrack, preview?.GuessTrack, _nextHint is { } hint && hint.VideoId == videoId ? hint.Label : "");
        var artist = FirstFilled(preview?.FoundArtist, preview?.GuessArtist);
        return (track, artist);
    }

    private LibraryFolder EnsureSyncedFolder()
    {
        var folder = _library.FirstOrDefault(item => string.Equals(item.Name, "Músicas sincronizadas", StringComparison.OrdinalIgnoreCase));
        if (folder is null)
        {
            folder = new LibraryFolder { Name = "Músicas sincronizadas" };
            _library.Add(folder);
            RefreshFolders(folder.Id);
        }
        return folder;
    }

    private static string? ValidateLyricsText(string? raw, out string cleaned)
    {
        cleaned = "";
        if (string.IsNullOrWhiteSpace(raw)) return "Cole a letra completa primeiro.";
        if (raw.Length > 20000) return "Texto grande demais para ser uma letra.";
        cleaned = LyricsTextCleaner.Clean(raw);
        return LyricsTextCleaner.LooksLikeLyrics(cleaned) ? null : "Isso não parece uma letra (poucas linhas). Cole a letra completa.";
    }

    private static string DescribeLyrics(string cleaned) =>
        cleaned.Split('\n').Count(line => line.Trim().Length > 0) + " linhas em " + TapSyncPlanner.SplitStanzas(cleaned).Count + " estrofes";

    /// <summary>Letra colada no celular para a PRÓXIMA música: fica salva (sem tempos) na biblioteca.</summary>
    private string LyricsTextForNextFromRemote(string text)
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Prepare uma próxima música antes.";
        var problem = ValidateLyricsText(text, out var cleaned);
        if (problem is not null) return problem;

        var (track, artist) = NextNames(videoId);
        var folder = EnsureSyncedFolder();
        var video = _library.SelectMany(item => item.Videos).FirstOrDefault(item => !item.IsOffline && item.VideoId == videoId);
        if (video is null)
        {
            video = new LibraryVideo
            {
                VideoId = videoId,
                Url = _nextVideoUrl ?? "",
                Title = artist.Length > 0 ? track + " — " + artist : track
            };
            folder.Videos.Add(video);
        }
        if (track.Length > 0) video.TrackName = track;
        if (artist.Length > 0) video.Artist = artist;
        video.FullLyrics = cleaned;
        video.SavedAt = DateTime.Now;
        SaveLibrary();
        RefreshLibrarySongs();
        _nextRejectedVideoId = null;
        RefreshNextLyricsPreview(force: true);
        AppLog.Info("Letra colada pelo celular para a próxima (" + videoId + "): " + DescribeLyrics(cleaned));
        return "✅ Letra salva para a próxima (" + DescribeLyrics(cleaned) + "), sem tempos. Toque em Tocar próxima e depois em Estimar tempos ou Marcar por toque.";
    }

    /// <summary>Letra colada no celular para a música que está TOCANDO agora.</summary>
    private string LyricsTextForNowFromRemote(string text)
    {
        if (_currentVideo is null) return "Nenhuma música carregada.";
        var problem = ValidateLyricsText(text, out var cleaned);
        if (problem is not null) return problem;
        ManualLyricsBox.Text = cleaned;
        _currentVideo.FullLyrics = cleaned;
        SaveCurrentFullLyrics();
        return "✅ Letra colada (" + DescribeLyrics(cleaned) + "). Agora toque em Estimar tempos ou Marcar por toque.";
    }

    /// <summary>Rascunho de tempos em segundos (ver TapSyncPlanner.EstimateStarts). Se a música está tocando e já passou dos
    /// 3 s, esse instante é usado como o começo da 1ª estrofe.</summary>
    private string EstimateTimings()
    {
        if (_currentVideo is null) return "Carregue uma música primeiro.";
        var text = ManualLyricsBox.Text.Trim();
        if (text.Length == 0) return "Cole a letra primeiro.";
        var stanzas = TapSyncPlanner.SplitStanzas(text);
        if (stanzas.Count == 0) return "Não encontrei estrofes na letra.";
        if (_playerDurationSeconds <= 0) return "A duração da música ainda não chegou. Dê play, espere alguns segundos e tente de novo.";

        var playing = _isLocalAudioPlaying || _lastPlayerState == 1;
        var position = GetPlaybackSeconds();
        var first = playing && position >= 3 ? position : -1;
        var starts = TapSyncPlanner.EstimateStarts(stanzas, _playerDurationSeconds, first);
        var cues = TapSyncPlanner.BuildCues(stanzas, starts, 3.0);
        if (cues.Count == 0) return "Não foi possível estimar.";

        if (_tapActive) TapFinish(save: false);
        _currentVideo.FullLyrics = text;
        _cues.Clear();
        _cues.AddRange(CueTimeline.Normalize(cues));
        RefreshCueList();
        _lastAutomaticCue = -1;
        _manualProjectionMode = false;
        _manualCueIndex = -1;
        PersistCurrentCues();
        var origin = first >= 0 ? "a 1ª estrofe começa em " + TimeSpan.FromSeconds(first).ToString(@"m\:ss") : "introdução estimada";
        var message = "⏱ Tempos estimados (rascunho): " + _cues.Count + " frases, " + origin + ". Acompanhe e corrija com Frase ◀/▶ ou marque por toque.";
        StatusText.Text = message;
        return message;
    }

    private void EstimateTimings_Click(object sender, RoutedEventArgs e) => StatusText.Text = EstimateTimings();

    private async Task<string> TapCommandForRemoteAsync(string command)
    {
        switch (command)
        {
            case "tapstart":
                if (_currentVideo is null) return "Nenhuma música carregada.";
                if (string.IsNullOrWhiteSpace(ManualLyricsBox.Text)) return "Cole a letra primeiro.";
                TapStart_Click(this, new RoutedEventArgs());
                await Task.Delay(250);
                return _tapActive ? "Marcação por toque iniciada. Toque no botão quando cada estrofe começar." : StatusText.Text;
            case "tapmark":
                TapMarkStanza();
                return StatusText.Text;
            case "tapundo":
                TapUndo();
                return StatusText.Text;
            case "tapfinish":
                TapFinish(save: true);
                return StatusText.Text;
            default:
                TapFinish(save: false);
                return StatusText.Text;
        }
    }

    /// <summary>Estado extra enviado ao celular: marcação por toque, músicas salvas recentemente e o que falta na música atual.</summary>
    private object BuildTapState() => new
    {
        active = _tapActive,
        marked = _tapMarkedCards.Count,
        total = _lyricsEditorCards.Count,
        nextLine = _tapActive
            ? _lyricsEditorCards.FirstOrDefault(card => !card.Seconds.HasValue)?.Text ?? ""
            : ""
    };

    private object BuildRecentSaved() => _library.SelectMany(folder => folder.Videos)
        .Where(video => !video.IsOffline && !string.IsNullOrWhiteSpace(video.VideoId) && (video.Cues.Count > 0 || !string.IsNullOrWhiteSpace(video.FullLyrics)))
        .GroupBy(video => video.VideoId)
        .Select(group => group.First())
        .OrderByDescending(video => video.SavedAt)
        .ThenBy(video => LibraryLabel(video), StringComparer.CurrentCultureIgnoreCase)
        .Take(8)
        .Select(video => new { id = video.VideoId, title = LibraryLabel(video), lyrics = video.Cues.Count > 0 ? "sync" : "text" })
        .ToList();
}
