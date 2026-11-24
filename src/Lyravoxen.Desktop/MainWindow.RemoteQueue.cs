using System.Text.Json;
using System.Windows;
using Lyravoxen.Services;

namespace Lyravoxen;

// Fila de próximas músicas, link colado no celular, tocar a próxima sozinho ao terminar e texto automático da letra (Vagalume).
public partial class MainWindow
{
    private readonly List<(string VideoId, string Label)> _queue = new();
    private bool _autoAdvance;
    private int _autoAdvanceToken;
    private LyricsSourceSettings _lyricSources = LyricsSourceSettings.Load();

    // ----- link do YouTube colado no celular -----

    /// <summary>Argumento "1|link" (troca a próxima já preparada) ou "0|link" (só se não houver próxima).</summary>
    private string SetNextFromUrlForRemote(string argument)
    {
        var cut = (argument ?? "").IndexOf('|');
        var replace = cut > 0 && argument![..cut] == "1";
        var url = cut >= 0 ? argument![(cut + 1)..] : argument ?? "";
        var videoId = YouTubeLink.TryGetVideoId(url.Trim());
        if (videoId is null) return "Esse link não é de um vídeo do YouTube.";
        return PrepareNextForRemote(videoId, "", replace).Message;
    }

    // ----- fila -----

    private string QueueLabel((string VideoId, string Label) item)
    {
        if (item.Label.Length > 0) return item.Label;
        var cached = LyricsPreviewService.Cached(item.VideoId);
        if (cached is null) return "Vídeo " + item.VideoId;
        if (cached.GuessTrack.Length > 0) return cached.GuessArtist.Length > 0 ? cached.GuessTrack + " — " + cached.GuessArtist : cached.GuessTrack;
        return string.IsNullOrWhiteSpace(cached.VideoTitle) ? "Vídeo " + item.VideoId : cached.VideoTitle;
    }

    /// <summary>Argumento "tipo|valor": req|id do pedido, lib|id do vídeo na biblioteca, url|link.</summary>
    private string QueueAddForRemote(string argument)
    {
        var cut = (argument ?? "").IndexOf('|');
        if (cut <= 0) return "Comando inválido.";
        var kind = argument![..cut];
        var value = argument[(cut + 1)..].Trim();
        string? videoId;
        var label = "";
        SongRequest? request = null;
        switch (kind)
        {
            case "req":
                request = _requests.FirstOrDefault(item => item.Id == value);
                if (request is null) return "Esse pedido não está mais na lista.";
                videoId = YouTubeLink.TryGetVideoId(request.Url);
                label = request.Name;
                break;
            case "lib":
                var video = _library.SelectMany(folder => folder.Videos).FirstOrDefault(item => !item.IsOffline && item.VideoId == value);
                if (video is null) return "Música não encontrada na biblioteca.";
                videoId = video.VideoId;
                label = LibraryLabel(video);
                break;
            default:
                videoId = YouTubeLink.TryGetVideoId(value);
                break;
        }
        if (videoId is null) return "Esse link não é de um vídeo do YouTube.";

        // Sem próxima preparada: a música já vira a próxima.
        if (string.IsNullOrWhiteSpace(_nextVideoUrl))
        {
            var prepared = PrepareNextForRemote(videoId, label, replace: false);
            if (prepared.Ok && request is not null) RemoveRequest(request);
            return prepared.Message;
        }
        if (YouTubeLink.TryGetVideoId(_nextVideoUrl) == videoId || _queue.Any(item => item.VideoId == videoId))
            return "Essa música já é a próxima ou já está na fila.";
        if (_queue.Count >= 20) return "A fila está cheia (20 músicas).";
        _queue.Add((videoId, label));
        if (request is not null) RemoveRequest(request);
        QueueLyricsCheckIfNeeded(videoId);
        return "Na fila, posição " + _queue.Count + ": " + (label.Length > 0 ? label : "vídeo " + videoId) + ".";
    }

    private string QueueRemoveForRemote(string argument)
    {
        if (!int.TryParse(argument, out var index) || index < 0 || index >= _queue.Count) return "Essa música não está mais na fila.";
        var removed = _queue[index];
        _queue.RemoveAt(index);
        return "Tirada da fila: " + QueueLabel(removed) + ".";
    }

    private string QueueClearForRemote()
    {
        var count = _queue.Count;
        _queue.Clear();
        return count == 0 ? "A fila já está vazia." : "Fila limpa (" + count + (count == 1 ? " música)." : " músicas).");
    }

    /// <summary>Quando a próxima vira a música atual, a primeira da fila assume o lugar de próxima.</summary>
    private void PromoteQueue()
    {
        if (_queue.Count == 0 || !string.IsNullOrWhiteSpace(_nextVideoUrl)) return;
        var first = _queue[0];
        _queue.RemoveAt(0);
        PrepareNextForRemote(first.VideoId, first.Label, replace: true);
    }

    private void QueueLyricsCheckIfNeeded(string videoId)
    {
        if (LyricsPreviewService.Cached(videoId) is null) QueueLyricsCheck(videoId);
    }

    private object BuildQueueJson() => _queue.Select((item, index) =>
    {
        QueueLyricsCheckIfNeeded(item.VideoId);
        return new
        {
            i = index,
            id = item.VideoId,
            title = QueueLabel(item),
            lyrics = LyricsPreviewService.Cached(item.VideoId)?.Headline ?? "🔎 Verificando se tem letra…"
        };
    }).ToList();

    // ----- tocar a próxima sozinho -----

    private void AutoAdvanceBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingPreferences) return;
        _autoAdvance = AutoAdvanceBox.IsChecked == true;
        SaveAutoAdvancePreference();
        StatusText.Text = _autoAdvance ? "A próxima música tocará sozinha quando a atual terminar." : "Tocar a próxima sozinho: desligado.";
    }

    private string ToggleAutoAdvanceForRemote()
    {
        _autoAdvance = !_autoAdvance;
        AutoAdvanceBox.IsChecked = _autoAdvance;
        SaveAutoAdvancePreference();
        return _autoAdvance ? "Ligado: a próxima música toca sozinha quando a atual terminar." : "Desligado: a próxima só toca quando você mandar.";
    }

    private void SaveAutoAdvancePreference()
    {
        try
        {
            var current = File.Exists(_preferencesFile)
                ? JsonSerializer.Deserialize<ProjectionPreferences>(File.ReadAllText(_preferencesFile)) ?? new()
                : new ProjectionPreferences();
            current.AutoAdvance = _autoAdvance;
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencesFile)!);
            var temp = _preferencesFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _preferencesFile, true);
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível salvar a preferência: " + ex.Message; }
    }

    /// <summary>Chamado quando o player avisa que a música terminou sozinha (não quando você encerra).</summary>
    private void MaybeAutoAdvance()
    {
        if (!_autoAdvance || string.IsNullOrWhiteSpace(_nextVideoUrl)) return;
        var token = ++_autoAdvanceToken;
        StatusText.Text = "A próxima música começa em 3 segundos…";
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            await Task.Delay(3000);
            if (token != _autoAdvanceToken || !_autoAdvance || string.IsNullOrWhiteSpace(_nextVideoUrl) || _lastPlayerState == 1) return;
            PlayNextPrepared_Click(this, new RoutedEventArgs());
        }));
    }

    // ----- texto automático da letra (Vagalume) -----

    private async Task<VagalumeResult> FetchVagalumeTextAsync(string track, string artist) =>
        await VagalumeLyricsService.FetchAsync(LyricsPreviewService.CleanTitle(track), LyricsPreviewService.CleanTitle(artist), _lyricSources.VagalumeKey);

    /// <summary>Argumento "next|música\nintérprete" ou "now|": busca o texto e salva como no ✎ Criar letra.</summary>
    private async Task<string> LyricsFetchForRemoteAsync(string argument)
    {
        if (_lyricSources.VagalumeKey.Length == 0)
            return "Falta a chave do Vagalume. No computador: aba Cadastrar letra → ⚙ Fonte de letras.";
        var cut = (argument ?? "").IndexOf('|');
        var target = cut > 0 ? argument![..cut] : "next";
        var names = cut >= 0 ? argument![(cut + 1)..].Split('\n') : Array.Empty<string>();
        var track = names.Length > 0 ? names[0].Trim() : "";
        var artist = names.Length > 1 ? names[1].Trim() : "";

        string videoId;
        if (target == "now")
        {
            if (_currentVideo is null) return "Nenhuma música carregada.";
            videoId = _currentVideo.VideoId;
            if (track.Length == 0) { track = MusicTrackBox.Text.Trim(); artist = MusicArtistBox.Text.Trim(); }
        }
        else
        {
            videoId = YouTubeLink.TryGetVideoId(_nextVideoUrl) ?? "";
            if (videoId.Length == 0) return "Prepare uma próxima música antes.";
            if (track.Length == 0) (track, artist) = NextNames(videoId);
            else _nextTyped = (videoId, track, artist);
        }
        if (track.Length == 0 || artist.Length == 0) return "Informe música e intérprete e tente de novo.";

        var result = await FetchVagalumeTextAsync(track, artist);
        if (!result.Found) return result.Message;
        var saved = target == "now" ? LyricsTextForNowFromRemote(result.Text) : LyricsTextForNextFromRemote(result.Text);
        return saved.StartsWith("✅", StringComparison.Ordinal)
            ? "📝 Texto encontrado no Vagalume: " + result.Track + " — " + result.Artist + ". " + saved
            : saved;
    }

    private async void FetchLyricsText_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_lyricSources.VagalumeKey.Length == 0) { LyricSourceDialog(); return; }
            var track = MusicTrackBox.Text.Trim();
            var artist = MusicArtistBox.Text.Trim();
            if (track.Length == 0 || artist.Length == 0)
            {
                StatusText.Text = "Preencha o intérprete e o título da música (aba Sincronização) para buscar o texto.";
                return;
            }
            StatusText.Text = "🔎 Buscando o texto da letra no Vagalume…";
            var result = await FetchVagalumeTextAsync(track, artist);
            if (!result.Found) { StatusText.Text = result.Message; return; }
            if (!string.IsNullOrWhiteSpace(ManualLyricsBox.Text) &&
                System.Windows.MessageBox.Show(this, "Substituir a letra que já está na caixa pelo texto do Vagalume?", "Buscar texto",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            ManualLyricsBox.Text = result.Text;
            StatusText.Text = "📝 Texto encontrado no Vagalume (" + result.Track + " — " + result.Artist + "): " + DescribeLyrics(result.Text) +
                              ". Use ⚡ Cadastro rápido ou ⏱ Estimar tempos.";
        }
        catch (Exception ex)
        {
            AppLog.Error("Busca do texto da letra falhou", ex);
            StatusText.Text = "Não foi possível buscar o texto: " + ex.Message;
        }
    }

    private void LyricSource_Click(object sender, RoutedEventArgs e) => LyricSourceDialog();

    private void LyricSourceDialog()
    {
        var key = new System.Windows.Controls.TextBox { Text = _lyricSources.VagalumeKey, Margin = new Thickness(0, 4, 0, 10), FontSize = 14 };
        var status = RequestText("", 12, false, "#B3261E");
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(RequestText("⚙ Fonte de letras: Vagalume", 18, true));
        panel.Children.Add(RequestText(
            "O Vagalume tem uma API gratuita que devolve o TEXTO da letra (sem os tempos), em português. Você precisa de uma chave: no site vagalume.com.br " +
            "crie uma conta e peça a chave da API (procure por \"API\"). Cole a chave abaixo. Com ela, a busca de texto automática funciona no computador e no celular.", 12));
        panel.Children.Add(RequestText("Chave da API do Vagalume:", 12));
        panel.Children.Add(key);
        panel.Children.Add(status);

        var dialog = new Window
        {
            Title = "Fonte de letras",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 560,
            SizeToContent = SizeToContent.Height,
            Background = Hex("#F5F7FA"),
            Content = panel
        };
        var buttons = new System.Windows.Controls.WrapPanel();
        buttons.Children.Add(RequestButton("Salvar", () =>
        {
            _lyricSources.VagalumeKey = LyricsSourceSettings.SafeKey(key.Text);
            _lyricSources.Save();
            StatusText.Text = _lyricSources.VagalumeKey.Length > 0 ? "Chave do Vagalume salva." : "Chave do Vagalume removida.";
            dialog.Close();
        }));
        buttons.Children.Add(RequestButton("🧪 Testar chave", async () =>
        {
            var testKey = LyricsSourceSettings.SafeKey(key.Text);
            if (testKey.Length == 0) { status.Foreground = Hex("#B3261E"); status.Text = "Cole a chave primeiro."; return; }
            status.Foreground = Hex("#4A5A6B");
            status.Text = "Testando com Legião Urbana — Pais e Filhos…";
            var result = await VagalumeLyricsService.FetchAsync("Pais e Filhos", "Legião Urbana", testKey);
            status.Foreground = Hex(result.Found ? "#1A6B3A" : "#B3261E");
            status.Text = result.Found ? "✅ A chave funciona." : result.Message;
        }));
        panel.Children.Add(buttons);
        dialog.Show();
    }
}
