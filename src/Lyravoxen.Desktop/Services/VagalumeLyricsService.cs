using System.Net.Http;
using System.Text.Json;

namespace Lyravoxen.Services;

internal sealed record VagalumeResult(bool Found, string Track, string Artist, string Text, string Message);

/// <summary>Texto de letra (sem tempos) pela API oficial do Vagalume: https://api.vagalume.com.br/search.php?art=&amp;mus=&amp;apikey=
/// Precisa de uma chave gratuita e do intérprete + título da música.</summary>
internal static class VagalumeLyricsService
{
    private static readonly HttpClient Http = CreateHttp();

    private static HttpClient CreateHttp()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Lyravoxen/1.0");
        return client;
    }

    private static VagalumeResult Fail(string message) => new(false, "", "", "", message);

    public static async Task<VagalumeResult> FetchAsync(string track, string artist, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return Fail("Cadastre a chave do Vagalume (⚙ Fonte de letras).");
        if (string.IsNullOrWhiteSpace(track) || string.IsNullOrWhiteSpace(artist)) return Fail("Informe o intérprete e o título da música.");
        var url = "https://api.vagalume.com.br/search.php?art=" + Uri.EscapeDataString(artist.Trim()) +
                  "&mus=" + Uri.EscapeDataString(track.Trim()) + "&apikey=" + Uri.EscapeDataString(apiKey.Trim());
        try
        {
            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return Fail("O Vagalume respondeu com erro " + (int)response.StatusCode + " (confira a chave).");
            return Parse(await response.Content.ReadAsStringAsync(), track);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Fail("Sem conexão com o Vagalume.");
        }
    }

    /// <summary>Lê a resposta do Vagalume ("type", "art", "mus"[0].name/text) e só aceita se a música achada tiver o nome pedido.</summary>
    public static VagalumeResult Parse(string json, string typedTrack)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            string Str(JsonElement element, string name) =>
                element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? "" : "";
            var type = Str(root, "type");
            if (!root.TryGetProperty("mus", out var songs) || songs.ValueKind != JsonValueKind.Array || songs.GetArrayLength() == 0)
                return Fail(type is "notfound" or "song_notfound" ? "O Vagalume não tem essa música." : "Resposta inesperada do Vagalume (confira a chave).");

            var first = songs[0];
            var name = Str(first, "name");
            var text = Str(first, "text");
            var foundArtist = root.TryGetProperty("art", out var art) ? Str(art, "name") : "";
            if (string.IsNullOrWhiteSpace(text)) return Fail("O Vagalume achou a música, mas sem o texto da letra.");
            if (LyricsPreviewService.NameMatch(typedTrack, name) < 3)
                return Fail("O Vagalume achou outra música (" + name + "). Confira os nomes.");
            var cleaned = LyricsTextCleaner.Clean(text);
            if (!LyricsTextCleaner.LooksLikeLyrics(cleaned)) return Fail("O texto que o Vagalume devolveu é curto demais para ser uma letra.");
            return new VagalumeResult(true, name, foundArtist, cleaned, "");
        }
        catch (JsonException)
        {
            return Fail("Resposta inesperada do Vagalume (confira a chave).");
        }
    }
}
