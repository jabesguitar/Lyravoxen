using System.Windows;
using System.Windows.Threading;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

// Ao colar/carregar um link: mostra logo se já há letra (biblioteca ou catálogo), separa intérprete e título da música
// e acelera o cadastro de músicas sem letra (colar e limpar, cadastro rápido + marcação por toque).
public partial class MainWindow
{
    private string _currentRawTitle = "";
    private string _currentRawChannel = "";
    private LyricsPreview? _currentCatalogLyrics;
    private int _linkCheckToken;
    private string _urlHintFor = "";
    private DispatcherTimer? _urlHintTimer;

    // ----- dica de letra enquanto o link é colado -----

    private void YoutubeUrlBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_urlHintTimer is null)
        {
            _urlHintTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _urlHintTimer.Tick += async (_, _) =>
            {
                _urlHintTimer!.Stop();
                await UpdateUrlHintAsync();
            };
        }
        _urlHintTimer.Stop();
        _urlHintTimer.Start();
    }

    private async Task UpdateUrlHintAsync()
    {
        try
        {
            var videoId = YouTubeLink.TryGetVideoId(YoutubeUrlBox.Text);
            if (videoId is null) { UrlLyricsHintText.Text = ""; _urlHintFor = ""; return; }
            if (videoId == _urlHintFor) return;
            _urlHintFor = videoId;
            UrlLyricsHintText.Text = "🔎 Verificando se este vídeo já tem letra…";
            var preview = await LyricsPreviewService.CheckAsync(videoId, _library);
            if (YouTubeLink.TryGetVideoId(YoutubeUrlBox.Text) != videoId) return;
            UrlLyricsHintText.Text = preview.Headline;
        }
        catch (Exception ex) { AppLog.Error("Dica de letra do link falhou", ex); UrlLyricsHintText.Text = ""; }
    }

    // ----- nomes: intérprete e título da música -----

    private IEnumerable<string> KnownArtists() =>
        _library.SelectMany(folder => folder.Videos).Select(video => video.Artist)
            .Concat(_history.Select(item => item.Artist))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>"Intérprete - Música" ou "Música - Intérprete"? Quando não há pistas, o catálogo decide.</summary>
    private async Task RefineNamesAsync(string videoId, ParsedTrack parsed)
    {
        try
        {
            var order = await LyricsPreviewService.ResolveOrderAsync(parsed.PartA, parsed.PartB);
            if (order is not null && _currentVideo?.VideoId == videoId &&
                MusicTrackBox.Text.Trim() == parsed.Track && MusicArtistBox.Text.Trim() == parsed.Artist) // o usuário ainda não mexeu
            {
                var track = order == true ? parsed.PartA : parsed.PartB;
                var artist = order == true ? parsed.PartB : parsed.PartA;
                if (track != parsed.Track || artist != parsed.Artist)
                {
                    MusicTrackBox.Text = track;
                    MusicArtistBox.Text = artist;
                    _currentVideo.TrackName = track;
                    _currentVideo.Artist = artist;
                    UpdateCurrentHistoryMetadata();
                    SaveCurrentVideoMetadata();
                }
            }
        }
        catch (Exception ex) { AppLog.Error("Correção dos nomes pelo catálogo falhou", ex); }
        await CheckCurrentVideoLyricsAsync(videoId);
    }

    private async void FixNames_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null) { StatusText.Text = "Carregue primeiro um vídeo."; return; }
        var title = _currentRawTitle;
        var channel = _currentRawChannel;
        if (string.IsNullOrWhiteSpace(title))
        {
            // Vídeo da biblioteca/histórico: o título salvo é "Título — Canal".
            var saved = _currentVideo.Title ?? "";
            var cut = saved.LastIndexOf(" — ", StringComparison.Ordinal);
            title = cut > 0 ? saved[..cut] : saved;
            channel = cut > 0 ? saved[(cut + 3)..] : "";
        }
        var parsed = TrackTitleParser.Parse(title, channel, KnownArtists());
        MusicTrackBox.Text = parsed.Track;
        MusicArtistBox.Text = parsed.Artist;
        _currentVideo.TrackName = parsed.Track;
        _currentVideo.Artist = parsed.Artist;
        UpdateCurrentHistoryMetadata();
        SaveCurrentVideoMetadata();
        StatusText.Text = "Nomes corrigidos: " + parsed.Artist + " — " + parsed.Track;
        if (!parsed.Confident) await RefineNamesAsync(_currentVideo.VideoId, parsed);
        else await CheckCurrentVideoLyricsAsync(_currentVideo.VideoId);
    }

    // ----- já tem letra? -----

    private async Task CheckCurrentVideoLyricsAsync(string videoId)
    {
        var token = ++_linkCheckToken;
        try
        {
            _currentCatalogLyrics = null;
            UseCatalogLyricsButton.Visibility = Visibility.Collapsed;
            if (_currentVideo is null || _currentVideo.VideoId != videoId) return;
            _urlHintFor = videoId;

            string headline;
            if (_cues.Count > 0)
            {
                headline = "✅ Letra sincronizada já cadastrada (" + _cues.Count + " frases). Pronta para projetar.";
                LinkLyricsStatusText.Text = headline;
                UrlLyricsHintText.Text = headline;
                return;
            }
            if (!string.IsNullOrWhiteSpace(_currentVideo.FullLyrics))
            {
                headline = "🟡 Esta música tem a letra em texto, sem tempos. Use ⚡ Cadastro rápido (aba Cadastrar letra) para marcar por toque.";
                LinkLyricsStatusText.Text = headline;
                UrlLyricsHintText.Text = headline;
                return;
            }

            LinkLyricsStatusText.Text = "🔎 Procurando a letra no catálogo…";
            UrlLyricsHintText.Text = LinkLyricsStatusText.Text;
            var track = MusicTrackBox.Text.Trim();
            var artist = MusicArtistBox.Text.Trim();
            var preview = track.Length > 0
                ? await LyricsPreviewService.SearchByNamesAsync(track, artist)
                : await LyricsPreviewService.CheckAsync(videoId, _library);
            if (token != _linkCheckToken || _currentVideo?.VideoId != videoId) return;

            // Catálogo sem a música: tenta o texto no Vagalume (se houver chave), para pelo menos ter a letra em texto.
            if (preview.Availability is LyricsAvailability.NotFound && _lyricSources.VagalumeKey.Length > 0 && track.Length > 0 && artist.Length > 0)
            {
                LinkLyricsStatusText.Text = "🔎 Procurando o texto no Vagalume…";
                var vagalume = await FetchVagalumeTextAsync(track, artist);
                if (token != _linkCheckToken || _currentVideo?.VideoId != videoId) return;
                if (vagalume.Found)
                    preview = new LyricsPreview
                    {
                        Availability = LyricsAvailability.PlainOnly,
                        FoundTrack = vagalume.Track,
                        FoundArtist = vagalume.Artist,
                        Preview = LyricsPreviewService.FirstLines(vagalume.Text.Split('\n')),
                        PlainText = vagalume.Text
                    };
            }

            var found = preview.Availability is LyricsAvailability.Synced or LyricsAvailability.PlainOnly;
            var lines = preview.Preview.Length > 0 ? "\n" + string.Join(" / ", preview.Preview.Split('\n').Take(3)) : "";
            LinkLyricsStatusText.Text = preview.Availability == LyricsAvailability.NotFound
                ? "❌ Não achei a letra no catálogo. Use ⚡ Cadastro rápido na aba Cadastrar letra."
                : preview.Headline + lines;
            UrlLyricsHintText.Text = preview.Headline;
            if (found && string.IsNullOrWhiteSpace(MusicArtistBox.Text) && !string.IsNullOrWhiteSpace(preview.FoundArtist))
            {
                // Intérprete que o título do vídeo não trazia: usa o do catálogo.
                MusicArtistBox.Text = preview.FoundArtist;
                _currentVideo.Artist = preview.FoundArtist;
                UpdateCurrentHistoryMetadata();
                SaveCurrentVideoMetadata();
            }
            if (found)
            {
                _currentCatalogLyrics = preview;
                UseCatalogLyricsButton.Content = preview.Availability == LyricsAvailability.Synced
                    ? "✅ Usar esta letra sincronizada"
                    : "📝 Usar o texto e marcar por toque";
                UseCatalogLyricsButton.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Verificação de letra do vídeo falhou", ex);
            LinkLyricsStatusText.Text = "⚠ Não foi possível verificar a letra agora.";
        }
    }

    private void UseCatalogLyrics_Click(object sender, RoutedEventArgs e)
    {
        if (_currentVideo is null || _currentCatalogLyrics is not { } found) return;
        UseCatalogLyricsButton.Visibility = Visibility.Collapsed;

        if (found.Cues.Count >= 2)
        {
            _manualDraftActive = false;
            _manualDraftLines.Clear();
            _manualDraftIndex = 0;
            ManualLyricsBox.Text = found.PlainText;
            _currentVideo.FullLyrics = found.PlainText;
            _cues.Clear();
            _cues.AddRange(CueTimeline.Normalize(found.Cues));
            CueList.ItemsSource = null;
            CueList.ItemsSource = _cues;
            _lastAutomaticCue = -1;
            PersistCurrentCues();
            var done = "✅ Letra sincronizada aplicada (" + _cues.Count + " frases). Confira a sincronia com a gravação.";
            LinkLyricsStatusText.Text = done;
            UrlLyricsHintText.Text = done;
            StatusText.Text = done;
            return;
        }

        ManualLyricsBox.Text = LyricsTextCleaner.Clean(found.PlainText);
        _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
        SaveCurrentFullLyrics();
        WorkspaceTabs.SelectedItem = ManualSyncTab;
        StatusText.Text = "Texto da letra colocado na aba Cadastrar letra. Clique em ⚡ Cadastro rápido para marcar por toque.";
    }

    // ----- cadastro rápido de letra -----

    /// <summary>Letra copiada (Ctrl+C) de um site, já limpa; null se o que está copiado não parece uma letra.</summary>
    private static string? ClipboardLyricsOrNull()
    {
        try
        {
            if (!System.Windows.Clipboard.ContainsText()) return null;
            var text = System.Windows.Clipboard.GetText();
            return LyricsTextCleaner.LooksLikeLyrics(text) ? LyricsTextCleaner.Clean(text) : null;
        }
        catch { return null; }
    }

    private void PasteCleanLyrics_Click(object sender, RoutedEventArgs e)
    {
        var text = ClipboardLyricsOrNull();
        if (text is null)
        {
            StatusText.Text = "Não há uma letra copiada. Copie a letra no site (Ctrl+C) e clique de novo.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(ManualLyricsBox.Text) &&
            System.Windows.MessageBox.Show(this, "Substituir a letra que já está na caixa pela letra copiada?", "Colar e limpar letra",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        ManualLyricsBox.Text = text;
        var stanzas = TapSyncPlanner.SplitStanzas(text).Count;
        StatusText.Text = "Letra colada e limpa: " + text.Split('\n').Count(line => line.Trim().Length > 0) + " linhas em " + stanzas + " estrofes. Confira e use ⚡ Cadastro rápido.";
    }

    private async void QuickRegister_Click(object sender, RoutedEventArgs e)
    {
        try { await QuickRegisterAsync(); }
        catch (Exception ex)
        {
            AppLog.Error("Cadastro rápido falhou", ex);
            StatusText.Text = "Cadastro rápido: " + ex.Message;
        }
    }

    private async Task QuickRegisterAsync()
    {
        if (_currentVideo is null)
        {
            StatusText.Text = "Carregue primeiro a música (cole o link na aba Ao vivo).";
            WorkspaceTabs.SelectedIndex = 0;
            return;
        }

        if (string.IsNullOrWhiteSpace(ManualLyricsBox.Text))
        {
            string? text = null;
            if (_currentCatalogLyrics is { } found && !string.IsNullOrWhiteSpace(found.PlainText))
                text = LyricsTextCleaner.Clean(found.PlainText);
            text ??= ClipboardLyricsOrNull();
            if (text is null && _lyricSources.VagalumeKey.Length > 0 && MusicTrackBox.Text.Trim().Length > 0 && MusicArtistBox.Text.Trim().Length > 0)
            {
                StatusText.Text = "🔎 Buscando o texto da letra no Vagalume…";
                var vagalume = await FetchVagalumeTextAsync(MusicTrackBox.Text.Trim(), MusicArtistBox.Text.Trim());
                if (vagalume.Found) text = vagalume.Text;
            }
            if (text is null)
            {
                StatusText.Text = "Ainda sem letra: abri as fontes de pesquisa. Copie a letra (Ctrl+C) e clique de novo em ⚡ Cadastro rápido.";
                await OpenLyricsSourceSearchesAsync(MusicTrackBox.Text.Trim(), MusicArtistBox.Text.Trim());
                return;
            }
            ManualLyricsBox.Text = text;
        }

        WorkspaceTabs.SelectedItem = ManualSyncTab;
        _currentVideo.TrackName = MusicTrackBox.Text.Trim();
        _currentVideo.Artist = MusicArtistBox.Text.Trim();
        _currentVideo.FullLyrics = ManualLyricsBox.Text.Trim();
        SaveCurrentFullLyrics();

        await RestartPlaybackFromStartAsync();
        for (var i = 0; i < 24 && !(_isLocalAudioPlaying || _lastPlayerState == 1); i++) await Task.Delay(250);
        TapStart_Click(this, new RoutedEventArgs());
    }

    private async Task RestartPlaybackFromStartAsync()
    {
        if (_isLocalAudioPlaying)
        {
            LocalAudioPlayer.Position = TimeSpan.Zero;
            return;
        }
        if (YoutubePlayer.CoreWebView2 is null) return;
        await YoutubePlayer.CoreWebView2.ExecuteScriptAsync(
            "(function(){if(typeof player==='undefined'||!player||typeof player.seekTo!=='function')return 'x';player.seekTo(0,true);player.playVideo();return 'ok';})()");
        _lastKnownPlaybackSeconds = 0;
    }

    // ----- corrigir os nomes de todas as músicas já salvas -----

    private static bool FixNamePair(string? savedTitle, string track, string artist, List<string> known, out string newTrack, out string newArtist)
    {
        newTrack = track;
        newArtist = artist;
        // O título salvo é "Título do vídeo — Canal".
        var title = savedTitle ?? "";
        var cut = title.LastIndexOf(" — ", StringComparison.Ordinal);
        var rawTitle = cut > 0 ? title[..cut] : title;
        var channel = cut > 0 ? title[(cut + 3)..] : "";

        if (TrackTitleParser.LooksUncorrected(track, rawTitle))
        {
            var parsed = TrackTitleParser.Parse(rawTitle, channel, known);
            var normalizedArtist = LyricsPreviewService.Normalize(artist);
            var artistUntouched = normalizedArtist.Length == 0 ||
                                  normalizedArtist == LyricsPreviewService.Normalize(channel) ||
                                  normalizedArtist == LyricsPreviewService.Normalize(TrackTitleParser.CleanChannel(channel));
            if (parsed.Track.Length == 0 || (!parsed.Confident && !artistUntouched)) return false; // não adivinha por cima do que foi digitado
            newTrack = parsed.Track;
            newArtist = parsed.Artist.Length > 0 ? parsed.Artist : artist;
        }
        else
        {
            newTrack = LyricsPreviewService.CleanTitle(track);
            newArtist = LyricsPreviewService.CleanTitle(artist);
            if (newTrack.Length == 0) newTrack = track;
        }
        return newTrack != track || newArtist != artist;
    }

    private void FixAllNames_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this,
                "Corrigir o intérprete e o título de TODAS as músicas da biblioteca e do histórico?\n\n" +
                "O programa tira Playback, Legendado, Ao Vivo, \"um tom abaixo\" etc. e coloca cada nome no seu campo. " +
                "Nomes que você já digitou certinho são mantidos.",
                "Corrigir nomes", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var known = KnownArtists().ToList();
        var changedLibrary = 0;
        foreach (var video in _library.SelectMany(folder => folder.Videos).Where(video => !video.IsOffline))
        {
            if (!FixNamePair(video.Title, video.TrackName, video.Artist, known, out var track, out var artist)) continue;
            video.TrackName = track;
            video.Artist = artist;
            changedLibrary++;
        }
        var changedHistory = 0;
        foreach (var item in _history)
        {
            if (!FixNamePair(item.Title, item.TrackName, item.Artist, known, out var track, out var artist)) continue;
            item.TrackName = track;
            item.Artist = artist;
            changedHistory++;
        }
        if (changedLibrary > 0) { SaveLibrary(); RefreshLibrarySongs(); }
        if (changedHistory > 0) { SaveHistory(); RefreshHistory(); }
        if (_currentVideo is not null)
        {
            MusicTrackBox.Text = _currentVideo.TrackName;
            MusicArtistBox.Text = _currentVideo.Artist;
        }
        StatusText.Text = "Nomes corrigidos: " + changedLibrary + " na biblioteca e " + changedHistory + " no histórico.";
    }
}
