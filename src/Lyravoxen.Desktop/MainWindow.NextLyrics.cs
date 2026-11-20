using System.Windows;
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
}
