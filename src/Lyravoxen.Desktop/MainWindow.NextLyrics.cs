using System.Text.Json;
using System.Windows;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

// 🔎 Prévia de letra: confere, ainda na espera, se a próxima música (e cada pedido) já tem letra,
// para o operador antecipar o cadastro/sincronização antes de a música entrar.
public partial class MainWindow
{
    private LyricsPreview? _nextLyrics;
    /// <summary>Vídeo a que _nextLyrics se refere (evita usar a letra de outra música).</summary>
    private string? _nextLyricsVideoId;
    private int _namesRev;
    /// <summary>Nome/autor digitados no celular para a verificação atual.</summary>
    private (string VideoId, string Track, string Artist)? _nextTyped;
    /// <summary>Nome da próxima música quando vem de um pedido ou da biblioteca (para mostrar no celular).</summary>
    private (string VideoId, string Label)? _nextHint;
    private int _nextLyricsToken;
    private readonly HashSet<string> _lyricsQueued = new();
    private readonly SemaphoreSlim _lyricsGate = new(1, 1);

    private void InitializeNextLyrics() => Loaded += (_, _) =>
    {
        RefreshNextLyricsPreview();
        PrecheckRequestLyrics();
    };

    private void VerifyNextLyrics_Click(object sender, RoutedEventArgs e) => RefreshNextLyricsPreview(force: true);

    private void ConfirmNextLyrics_Click(object sender, RoutedEventArgs e)
    {
        var message = ConfirmNextLyrics();
        StatusText.Text = message;
        if (message.StartsWith("✅", StringComparison.Ordinal) || message.StartsWith("Letra salva", StringComparison.Ordinal))
        {
            NextLyricsText.Text = "";
            NextLyricsText.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Letra do catálogo ainda não salva, que o operador pode confirmar.</summary>
    private static bool IsLyricsCandidate(LyricsPreview preview) =>
        preview.Availability is LyricsAvailability.Synced or LyricsAvailability.PlainOnly;

    /// <summary>Atualiza a prévia da letra da música preparada como próxima (limpa se não houver próxima).</summary>
    private async void RefreshNextLyricsPreview(bool force = false)
    {
        try
        {
            var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
            var token = ++_nextLyricsToken;
            _nextLyrics = null;
            _nextLyricsVideoId = null;
            if (videoId is null)
            {
                NextLyricsText.Text = "";
                return;
            }

            NextLyricsText.Visibility = Visibility.Visible;
            NextLyricsText.Text = "🔎 Verificando se esta música já tem letra…";
            var preview = await LyricsPreviewService.CheckAsync(videoId, _library, force);
            if (token != _nextLyricsToken) return; // outra música foi preparada enquanto verificava
            _nextLyrics = preview;
            _nextLyricsVideoId = videoId;
            NextLyricsText.Text = preview.Headline +
                (string.IsNullOrWhiteSpace(preview.VideoTitle) ? "" : "\nVídeo: " + preview.VideoTitle) +
                (preview.Preview.Length > 0 ? "\n\nPrévia da letra:\n" + preview.Preview : "") +
                (IsLyricsCandidate(preview) ? "\n\n✅ Para usar esta letra, clique em Confirmar letra." : "");
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

    // ----- Celular e computador: confirmar a letra, preparar músicas, estado ao vivo -----

    /// <summary>Aviso curto para quando se toca a próxima sem letra que role sozinha.</summary>
    private string NextLyricsWarning() => _nextLyrics switch
    {
        null => "",
        { Availability: LyricsAvailability.NotFound } => " (sem letra cadastrada: não vai projetar)",
        { Availability: LyricsAvailability.PlainOnly } => " (letra sem tempos: não roda sozinha)",
        { Availability: LyricsAvailability.Library, CueCount: 0 } => " (letra sem tempos: não roda sozinha)",
        _ => ""
    };

    /// <summary>Procura a letra da próxima pelo nome/autor digitados no celular (ou pelo título do vídeo, se vazios).</summary>
    private async Task<string> CheckNextLyricsFromRemoteAsync(string argument)
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Prepare uma próxima música antes (escolha um pedido ou busque na biblioteca).";
        var parts = (argument ?? "").Split('\n');
        var track = parts[0].Trim();
        var artist = parts.Length > 1 ? parts[1].Trim() : "";
        var token = ++_nextLyricsToken; // a verificação automática (título) que ainda estiver rodando passa a ser ignorada

        LyricsPreview preview;
        if (LyricsPreviewService.LibraryMatch(videoId, _library) is { } saved) preview = saved;
        else if (track.Length == 0) preview = await LyricsPreviewService.CheckAsync(videoId, _library, force: true);
        else preview = await LyricsPreviewService.SearchByNamesAsync(track, artist);

        if (YouTubeLink.TryGetVideoId(_nextVideoUrl) != videoId || token != _nextLyricsToken)
            return "A próxima música mudou durante a verificação. Verifique de novo.";

        _nextRejectedVideoId = null;
        _nextTyped = track.Length > 0 ? (videoId, track, artist) : null;
        _nextLyrics = preview;
        _nextLyricsVideoId = videoId;
        NextLyricsText.Text = preview.Headline + (preview.Preview.Length > 0 ? "\n\nPrévia da letra:\n" + preview.Preview : "") +
                              (IsLyricsCandidate(preview) ? "\n\n✅ Para usar esta letra, clique em Confirmar letra." : "");

        var text = new System.Text.StringBuilder(preview.Headline);
        if (track.Length > 0) text.Append("\nProcurado: ").Append(track).Append(artist.Length > 0 ? " — " + artist : "");
        if (IsLyricsCandidate(preview)) text.Append("\n\nÉ esta? Toque em ✅ Confirmar letra.");
        else if (preview.Availability == LyricsAvailability.NotFound) text.Append("\n\nTente outro nome ou autor. Sem letra, a música toca sem projeção.");
        return text.ToString();
    }

    /// <summary>Refaz intérprete/título da próxima a partir do título do vídeo (mesma regra do computador) e manda para o celular.</summary>
    private async Task<string> FixNextNamesForRemoteAsync()
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Prepare uma próxima música antes.";
        var preview = _nextLyricsVideoId == videoId ? _nextLyrics : null;
        var title = !string.IsNullOrWhiteSpace(preview?.VideoTitle) ? preview!.VideoTitle : _nextHint is { } hint && hint.VideoId == videoId ? hint.Label : "";
        if (title.Length == 0) return "Ainda não tenho o título do vídeo. Espere a verificação e tente de novo.";
        var parsed = TrackTitleParser.Parse(title, preview?.VideoChannel, KnownArtists());
        var track = parsed.Track;
        var artist = parsed.Artist;
        if (!parsed.Confident)
        {
            var order = await LyricsPreviewService.ResolveOrderAsync(parsed.PartA, parsed.PartB);
            if (order == true) { track = parsed.PartA; artist = parsed.PartB; }
        }
        if (YouTubeLink.TryGetVideoId(_nextVideoUrl) != videoId) return "A próxima música mudou. Tente de novo.";
        _nextTyped = (videoId, track, artist);
        _namesRev++;
        return "Nomes corrigidos: " + (artist.Length > 0 ? artist + " — " : "") + track + ". Toque em Verificar para procurar a letra.";
    }

    /// <summary>Guarda na biblioteca a letra encontrada para a próxima música; ao tocar, o carregamento já usa essas frases.</summary>
    private string ConfirmNextLyrics()
    {
        var videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (videoId is null) return "Não há próxima música preparada.";
        if (_nextLyrics is null || _nextLyricsVideoId != videoId)
            return "Ainda verificando a letra. Espere alguns segundos e toque de novo.";
        var preview = _nextLyrics;
        if (preview.Availability == LyricsAvailability.Library) return "Esta música já tem letra salva. É só tocar.";
        if (!IsLyricsCandidate(preview) || string.IsNullOrWhiteSpace(preview.PlainText))
            return "Não há letra encontrada para confirmar. Digite música e autor e toque em Verificar.";

        var typedTrack = _nextTyped is { } typed && typed.VideoId == videoId ? typed.Track : "";
        var typedArtist = _nextTyped is { } typedBoth && typedBoth.VideoId == videoId ? typedBoth.Artist : "";
        var track = typedTrack.Length > 0 ? typedTrack : preview.FoundTrack;
        var artist = typedTrack.Length > 0 ? typedArtist : preview.FoundArtist;

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
        video.SavedAt = DateTime.Now;
        video.Cues = preview.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList();
        SaveLibrary();
        RefreshLibrarySongs();
        _nextTyped = null;
        RefreshNextLyricsPreview(force: true);
        AppLog.Info("Letra confirmada para " + videoId + ": " + track + " — " + artist + " (" + video.Cues.Count + " frases)");
        var summary = track + (artist.Length > 0 ? " — " + artist : "");
        StatusText.Text = "Letra confirmada: " + summary + ".";

        return video.Cues.Count > 0
            ? "✅ Letra confirmada e salva: " + summary + " (" + video.Cues.Count + " frases). Ao tocar a próxima, ela roda no telão."
            : "Letra salva só como texto (sem tempos). Ao tocar, ela não roda sozinha.";
    }

    /// <summary>Deixa uma música (pedido ou biblioteca) como próxima, sem janelas no computador.</summary>
    private (bool Ok, string Message) PrepareNextForRemote(string videoId, string label, bool replace)
    {
        label = (label ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(_nextVideoUrl) && !replace)
            return (false, "Já existe uma próxima preparada. Escolha \"Trocar\" para substituí-la.");
        _nextVideoUrl = "https://www.youtube.com/watch?v=" + videoId;
        _nextHint = (videoId, label);
        _nextTyped = null;
        _nextRejectedVideoId = null;
        SaveNextVideoPreference();
        NextTrackText.Text = "Próxima faixa preparada (" + videoId + ")" + (label.Length > 0 ? ": " + label : "") +
                             ". A música atual continua tocando até você selecionar Tocar próxima.";
        PlayNextButton.IsEnabled = true;
        RefreshNextLyricsPreview();
        return (true, "Próxima preparada: " + (label.Length > 0 ? label : videoId) + ".");
    }

    private string LibraryLabel(LibraryVideo video)
    {
        var name = string.IsNullOrWhiteSpace(video.TrackName) ? video.Title : video.TrackName;
        return string.IsNullOrWhiteSpace(video.Artist) ? name : name + " — " + video.Artist;
    }

    private string LibrarySearchJson(string query)
    {
        var words = LyricsPreviewService.Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var found = _library.SelectMany(folder => folder.Videos)
            .Where(video => !video.IsOffline && !string.IsNullOrWhiteSpace(video.VideoId))
            .GroupBy(video => video.VideoId)
            .Select(group => group.First())
            .Where(video =>
            {
                var haystack = LyricsPreviewService.Normalize(video.TrackName + " " + video.Artist + " " + video.Title);
                return words.All(word => haystack.Contains(word, StringComparison.Ordinal));
            })
            .OrderBy(video => LibraryLabel(video), StringComparer.CurrentCultureIgnoreCase)
            .Take(8)
            .Select(video => new
            {
                id = video.VideoId,
                title = LibraryLabel(video),
                lyrics = video.Cues.Count > 0 ? "sync" : !string.IsNullOrWhiteSpace(video.FullLyrics) ? "text" : "none"
            });
        return JsonSerializer.Serialize(found);
    }

    /// <summary>Música da biblioteca como próxima ("next") ou tocando já ("play").</summary>
    private string LibraryPickForRemote(string argument)
    {
        var parts = (argument ?? "").Split('|');
        var videoId = parts[0].Trim();
        var play = parts.Length > 1 && parts[1] == "play";
        var video = _library.SelectMany(folder => folder.Videos).FirstOrDefault(item => !item.IsOffline && item.VideoId == videoId);
        if (video is null) return "Música não encontrada na biblioteca.";
        var (ok, message) = PrepareNextForRemote(videoId, LibraryLabel(video), replace: true);
        if (!ok || !play) return message;
        var warning = video.Cues.Count == 0 ? " (sem letra com tempos: não vai projetar sozinha)" : "";
        PlayNextPrepared_Click(this, new RoutedEventArgs());
        return "Tocando: " + LibraryLabel(video) + warning;
    }

    /// <summary>Pedido escolhido no celular ("id|1" troca a próxima já preparada, "id|0" só se não houver).</summary>
    private string RequestPickForRemote(string argument)
    {
        var parts = (argument ?? "").Split('|');
        var request = _requests.FirstOrDefault(item => item.Id == parts[0]);
        if (request is null) return "Esse pedido não está mais na lista.";
        var videoId = YouTubeLink.TryGetVideoId(request.Url);
        if (videoId is null) return "Esse pedido não tem link de vídeo do YouTube.";
        var (ok, message) = PrepareNextForRemote(videoId, request.Name, replace: parts.Length > 1 && parts[1] == "1");
        if (ok) RemoveRequest(request);
        return message + (ok && !string.IsNullOrWhiteSpace(request.Group) ? " (pedido de " + request.Group + ")" : "");
    }

    private static string FirstFilled(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";

    /// <summary>Tudo o que a aba Ao vivo do celular mostra: o que está no telão, a próxima música com prévia da letra, pedidos.</summary>
    private string BuildLiveJson()
    {
        PrecheckRequestLyrics();

        var playing = _isLocalAudioPlaying || _lastPlayerState == 1;
        var paused = !_isLocalAudioPlaying && _lastPlayerState == 2;
        var hasSong = _currentVideo is not null && (playing || paused);
        var nowTitle = hasSong ? LibraryLabel(_currentVideo!) : "";
        var nextLine = hasSong ? (NextProjectionPreviewText?.Text ?? "").Replace("Próxima:", "").Trim(' ', '—') : "";

        object? next = null;
        var nextId = YouTubeLink.TryGetVideoId(_nextVideoUrl);
        if (nextId is not null)
        {
            var preview = _nextLyricsVideoId == nextId ? _nextLyrics : null;
            var status = preview is null ? "checking" : preview.Availability switch
            {
                LyricsAvailability.Library => preview.CueCount > 0 ? "library" : "library-text",
                LyricsAvailability.Synced => "synced",
                LyricsAvailability.PlainOnly => "plain",
                LyricsAvailability.NotFound => "notfound",
                _ => "error"
            };
            var hint = _nextHint is { } h && h.VideoId == nextId ? h.Label : "";
            var typedTrack = _nextTyped is { } typed && typed.VideoId == nextId ? typed.Track : "";
            var typedArtist = _nextTyped is { } typedBoth && typedBoth.VideoId == nextId ? typedBoth.Artist : "";
            next = new
            {
                videoId = nextId,
                title = hint.Length > 0 ? hint : !string.IsNullOrWhiteSpace(preview?.VideoTitle) ? preview!.VideoTitle : "Próxima música",
                status,
                headline = preview?.Headline ?? "🔎 Verificando se tem letra…",
                preview = (preview?.Preview ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries),
                track = typedTrack.Length > 0 ? typedTrack : FirstFilled(preview?.FoundTrack, preview?.GuessTrack),
                artist = typedTrack.Length > 0 ? typedArtist : FirstFilled(preview?.FoundArtist, preview?.GuessArtist),
                canConfirm = preview is not null && IsLyricsCandidate(preview) && _nextRejectedVideoId != nextId,
                rejected = _nextRejectedVideoId == nextId,
                namesRev = _namesRev,
                warning = preview is null ? "" : NextLyricsWarning().Trim(' ', '(', ')')
            };
        }

        var requests = _requests.OrderBy(item => item.ReceivedAt).Take(8).Select(item =>
        {
            var id = YouTubeLink.TryGetVideoId(item.Url);
            var cached = id is null ? null : LyricsPreviewService.Cached(id);
            return new
            {
                id = item.Id,
                name = string.IsNullOrWhiteSpace(item.Name) ? "(sem nome)" : item.Name,
                group = item.Group,
                lyrics = id is null ? "Sem link de vídeo do YouTube" : cached?.Headline ?? "🔎 Verificando se tem letra…"
            };
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            now = new
            {
                title = nowTitle,
                state = hasSong ? (paused ? "pausada" : "tocando") : "parada",
                line = hasSong ? _currentProjectedLyric ?? "" : "",
                next = nextLine,
                manual = hasSong && _manualProjectionMode,
                cues = _cues.Count,
                hasCues = _cues.Count > 0,
                hasText = !string.IsNullOrWhiteSpace(ManualLyricsBox.Text)
            },
            next,
            requestCount = _requests.Count,
            requests,
            tap = BuildTapState(),
            recent = BuildRecentSaved(),
            queue = BuildQueueJson(),
            autoAdvance = _autoAdvance,
            canFetchText = _lyricSources.VagalumeKey.Length > 0
        });
    }
}
