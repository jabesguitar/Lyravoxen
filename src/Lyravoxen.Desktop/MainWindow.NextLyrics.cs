using System.Windows;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

// 🔎 Prévia de letra: confere, ainda na espera, se a próxima música (e cada pedido) já tem letra,
// para o operador antecipar o cadastro/sincronização antes de a música entrar.
public partial class MainWindow
{
    private LyricsPreview? _nextLyrics;
    private int _nextLyricsToken;
    private readonly HashSet<string> _lyricsQueued = new();
    private readonly SemaphoreSlim _lyricsGate = new(1, 1);

    private void InitializeNextLyrics() => Loaded += (_, _) =>
    {
        RefreshNextLyricsPreview();
        PrecheckRequestLyrics();
    };

    private void VerifyNextLyrics_Click(object sender, RoutedEventArgs e) => RefreshNextLyricsPreview(force: true);

    /// <summary>Atualiza a prévia da letra da música preparada como próxima (limpa se não houver próxima).</summary>
    private async void RefreshNextLyricsPreview(bool force = false)
    {
        try
        {
            var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
            var token = ++_nextLyricsToken;
            if (videoId is null)
            {
                _nextLyrics = null;
                NextLyricsText.Text = "";
                return;
            }

            NextLyricsText.Text = "🔎 Verificando se esta música já tem letra…";
            var preview = await LyricsPreviewService.CheckAsync(videoId, _library, force);
            if (token != _nextLyricsToken) return; // outra música foi preparada enquanto verificava
            _nextLyrics = preview;
            NextLyricsText.Text = preview.Headline +
                (string.IsNullOrWhiteSpace(preview.VideoTitle) ? "" : "\nVídeo: " + preview.VideoTitle) +
                (preview.Preview.Length > 0 ? "\n\nPrévia da letra:\n" + preview.Preview : "");
        }
        catch (Exception ex)
        {
            AppLog.Error("Prévia da letra da próxima música falhou", ex);
            NextLyricsText.Text = "⚠ Não foi possível verificar a letra agora.";
        }
    }

    private string RequestLyricsLine(SongRequest request)
    {
        var videoId = YouTubeLink.TryGetVideoId(request.Url);
        if (videoId is null) return "Letra: não dá para verificar (o link não é de um vídeo do YouTube).";
        var cached = LyricsPreviewService.Cached(videoId);
        if (cached is null)
        {
            QueueLyricsCheck(videoId);
            return "🔎 Verificando se tem letra…";
        }
        return cached.Headline + (cached.Preview.Length > 0 ? "\n" + cached.Preview.Replace("\n", " / ") : "");
    }

    /// <summary>Verifica a letra de todos os pedidos da lista, um de cada vez (poupa o catálogo e a internet).</summary>
    private void PrecheckRequestLyrics()
    {
        foreach (var request in _requests.ToList())
            if (YouTubeLink.TryGetVideoId(request.Url) is { } videoId && LyricsPreviewService.Cached(videoId) is null)
                QueueLyricsCheck(videoId);
    }

    private async void QueueLyricsCheck(string videoId)
    {
        if (!_lyricsQueued.Add(videoId)) return;
        try
        {
            await _lyricsGate.WaitAsync();
            try { await LyricsPreviewService.CheckAsync(videoId, _library); }
            finally { _lyricsGate.Release(); }
        }
        catch (Exception ex) { AppLog.Error("Verificação de letra do pedido falhou", ex); }
        finally { _lyricsQueued.Remove(videoId); }
        // O resultado (inclusive "erro", guardado por 2 minutos) já está no cache: redesenha sem repetir a consulta.
        RefreshRequestsWindow();
    }

    // ----- Celular: nome/autor da próxima música, verificação e confirmação da letra -----

    private (string VideoId, string Track, string Artist, LyricsPreview Preview)? _remoteLyricsCheck;

    /// <summary>Aviso curto para quando se toca a próxima sem letra que role sozinha.</summary>
    private string NextLyricsWarning() => _nextLyrics switch
    {
        null => "",
        { Availability: LyricsAvailability.NotFound } => " (sem letra cadastrada: não vai projetar)",
        { Availability: LyricsAvailability.PlainOnly } => " (letra sem tempos: não roda sozinha)",
        { Availability: LyricsAvailability.Library, CueCount: 0 } => " (letra sem tempos: não roda sozinha)",
        _ => ""
    };

    /// <summary>Procura a letra da música preparada como próxima, pelo nome/autor digitados no celular (ou pelo título do vídeo).</summary>
    private async Task<string> CheckNextLyricsFromRemoteAsync(string argument)
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Prepare uma próxima música antes (por um pedido ou pela aba de pesquisa).";
        var parts = (argument ?? "").Split('\n');
        var track = parts[0].Trim();
        var artist = parts.Length > 1 ? parts[1].Trim() : "";

        LyricsPreview preview;
        if (LyricsPreviewService.LibraryMatch(videoId, _library) is { } saved) preview = saved;
        else if (track.Length == 0) preview = await LyricsPreviewService.CheckAsync(videoId, _library, force: true);
        else preview = await LyricsPreviewService.SearchByNamesAsync(track, artist);

        if (YouTubeLink.TryGetVideoId(_nextVideoUrl) != videoId)
            return "A próxima música mudou durante a verificação. Verifique de novo.";

        _remoteLyricsCheck = (videoId, track, artist, preview);
        _nextLyrics = preview;
        NextLyricsText.Text = preview.Headline + (preview.Preview.Length > 0 ? "\n\nPrévia da letra:\n" + preview.Preview : "");

        var text = new System.Text.StringBuilder(preview.Headline);
        if (track.Length > 0) text.Append("\nProcurado: ").Append(track).Append(artist.Length > 0 ? " — " + artist : "");
        if (preview.Preview.Length > 0) text.Append("\n\nPrévia da letra:\n").Append(preview.Preview);
        text.Append(preview.Availability switch
        {
            LyricsAvailability.Synced => "\n\nÉ esta? Toque em ✅ Confirmar letra. Os tempos são da versão do catálogo: se o vídeo for outra gravação (ao vivo, playback), a letra pode adiantar ou atrasar.",
            LyricsAvailability.PlainOnly => "\n\nSó há o texto, sem tempos. Dá para confirmar e salvar, mas ele não roda sozinho: use a marcação por toque no computador.",
            LyricsAvailability.Library => preview.CueCount > 0 ? "\n\nJá está salva com tempos. É só tocar." : "\n\nSó há o texto salvo, sem tempos: não roda sozinho.",
            LyricsAvailability.NotFound => "\n\nTente outro nome ou autor. Sem letra, a música toca sem projeção.",
            _ => "\n\nTente de novo em instantes."
        });
        return text.ToString();
    }

    /// <summary>Guarda na biblioteca a letra verificada para o vídeo da próxima música; ao tocar, o carregamento já usa essas frases.</summary>
    private string ConfirmNextLyricsFromRemote()
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Não há próxima música preparada.";
        if (_remoteLyricsCheck is not { } check || check.VideoId != videoId) return "Primeiro toque em 🔎 Verificar letra.";
        var preview = check.Preview;
        if (preview.Availability == LyricsAvailability.Library) return "Esta música já tem letra salva. É só tocar.";
        if (preview.Availability is LyricsAvailability.NotFound or LyricsAvailability.Error || string.IsNullOrWhiteSpace(preview.PlainText))
            return "Não há letra encontrada para confirmar.";

        var track = check.Track.Length > 0 ? check.Track : preview.FoundTrack;
        var artist = check.Artist.Length > 0 ? check.Artist : preview.FoundArtist;

        var folder = _library.FirstOrDefault(item => string.Equals(item.Name, "Músicas sincronizadas", StringComparison.OrdinalIgnoreCase));
        if (folder is null)
        {
            folder = new LibraryFolder { Name = "Músicas sincronizadas" };
            _library.Add(folder);
            RefreshFolders(folder.Id);
        }
        var video = _library.SelectMany(item => item.Videos).FirstOrDefault(item => !item.IsOffline && item.VideoId == videoId);
        if (video is null)
        {
            video = new LibraryVideo
            {
                VideoId = videoId,
                Url = _nextVideoUrl ?? "",
                Title = string.IsNullOrWhiteSpace(preview.VideoTitle) ? track : preview.VideoTitle
            };
            folder.Videos.Add(video);
        }
        video.TrackName = track;
        video.Artist = artist;
        video.FullLyrics = preview.PlainText;
        video.Cues = preview.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
        SaveLibrary();
        RefreshLibrarySongs();
        _remoteLyricsCheck = null;
        RefreshNextLyricsPreview(force: true);
        AppLog.Info("Letra confirmada pelo celular para " + videoId + ": " + track + " — " + artist + " (" + video.Cues.Count + " frases)");
        StatusText.Text = "Letra confirmada pelo celular: " + track + (artist.Length > 0 ? " — " + artist : "") + ".";

        return video.Cues.Count > 0
            ? "✅ Letra confirmada e salva: " + track + (artist.Length > 0 ? " — " + artist : "") + " (" + video.Cues.Count + " frases). Ao tocar a próxima, ela roda no telão."
            : "Letra salva só como texto (sem tempos). Ao tocar, ela não roda sozinha.";
    }
}
