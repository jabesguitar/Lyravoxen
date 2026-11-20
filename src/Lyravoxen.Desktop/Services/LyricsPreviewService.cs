using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyravoxen.Models;

namespace Lyravoxen.Services;

internal enum LyricsAvailability { Library, Synced, PlainOnly, NotFound, Error }

/// <summary>Resultado da verificação "este vídeo já tem letra?" (sem abrir janelas, sem alterar nada).</summary>
internal sealed class LyricsPreview
{
    public LyricsAvailability Availability { get; init; }
    public string VideoTitle { get; init; } = "";
    public string FoundTrack { get; init; } = "";
    public string FoundArtist { get; init; } = "";
    /// <summary>False quando a faixa do catálogo tem o nome certo mas o artista não aparece no título/canal: conferir a versão.</summary>
    public bool ArtistMatches { get; init; } = true;
    public int CueCount { get; init; }
    /// <summary>Primeiras linhas da letra, para o operador conferir se é o mesmo louvor.</summary>
    public string Preview { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public string Headline => Availability switch
    {
        LyricsAvailability.Library => "✅ Letra já cadastrada na sua biblioteca" +
            (CueCount > 0 ? " (" + CueCount + " frases sincronizadas)." : " (texto sem tempos)."),
        LyricsAvailability.Synced => (ArtistMatches ? "✅" : "🟡") + " Letra sincronizada no catálogo: " + FoundTrack +
            (string.IsNullOrWhiteSpace(FoundArtist) ? "" : " — " + FoundArtist) +
            (ArtistMatches ? "." : ". Confira se é a mesma versão."),
        LyricsAvailability.PlainOnly => "🟡 Letra só em texto (sem tempos) no catálogo: " + FoundTrack +
            (string.IsNullOrWhiteSpace(FoundArtist) ? "" : " — " + FoundArtist) +
            ". Dá para usar com a marcação por toque.",
        LyricsAvailability.NotFound => "❌ Letra não encontrada no catálogo. Será preciso cadastrar.",
        _ => "⚠ Não foi possível verificar agora (sem internet?)."
    };
}

internal static class LyricsPreviewService
{
    private static readonly HttpClient Http = CreateHttp();
    private static readonly Dictionary<string, LyricsPreview> Cache = new();

    private static HttpClient CreateHttp()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Lyravoxen/1.0 (https://github.com/jabesguitar/Lyravoxen)");
        return client;
    }

    /// <summary>Resultado já conhecido (resultados de erro valem só 2 minutos, para tentar de novo depois).</summary>
    public static LyricsPreview? Cached(string videoId)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(videoId, out var hit)) return null;
            if (hit.Availability == LyricsAvailability.Error && DateTime.Now - hit.CreatedAt > TimeSpan.FromMinutes(2)) return null;
            return hit;
        }
    }

    private static LyricsPreview Store(string videoId, LyricsPreview preview)
    {
        lock (Cache) Cache[videoId] = preview;
        return preview;
    }

    /// <summary>Chamar da thread da tela: a biblioteca é lida antes do primeiro await.</summary>
    public static async Task<LyricsPreview> CheckAsync(string videoId, IEnumerable<LibraryFolder> library, bool force = false)
    {
        if (!force && Cached(videoId) is { } hit) return hit;

        var videos = library.SelectMany(folder => folder.Videos).Where(video => !video.IsOffline).ToList();
        var byId = videos.FirstOrDefault(video => video.VideoId == videoId && HasLyrics(video));
        if (byId is not null) return Store(videoId, FromLibrary(byId, byId.Title));

        string title, author;
        try
        {
            var endpoint = "https://www.youtube.com/oembed?url=" +
                           Uri.EscapeDataString("https://www.youtube.com/watch?v=" + videoId) + "&format=json";
            using var response = await Http.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
                return Store(videoId, new LyricsPreview { Availability = LyricsAvailability.Error });
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            title = json.RootElement.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            author = json.RootElement.TryGetProperty("author_name", out var a) ? a.GetString() ?? "" : "";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Store(videoId, new LyricsPreview { Availability = LyricsAvailability.Error });
        }

        // A mesma música salva a partir de outro vídeo (outra gravação ou playback) também serve de base.
        var normalizedTitle = Normalize(CleanTitle(title));
        var sameSong = videos.FirstOrDefault(video => HasLyrics(video) && Normalize(video.TrackName).Length >= 5 &&
                                                       ContainsPhrase(normalizedTitle, Normalize(video.TrackName)));
        if (sameSong is not null) return Store(videoId, FromLibrary(sameSong, title));

        try
        {
            var items = new List<JsonElement>();
            foreach (var query in BuildQueries(title))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://lrclib.net/api/search?q=" + Uri.EscapeDataString(query));
                using var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode) continue;
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.ValueKind == JsonValueKind.Array)
                    items.AddRange(json.RootElement.EnumerateArray().Select(item => item.Clone()));
            }
            return Store(videoId, Pick(items, title, author));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Store(videoId, new LyricsPreview { Availability = LyricsAvailability.Error, VideoTitle = title });
        }
    }

    private static bool HasLyrics(LibraryVideo video) => video.Cues.Count > 0 || !string.IsNullOrWhiteSpace(video.FullLyrics);

    private static LyricsPreview FromLibrary(LibraryVideo video, string videoTitle)
    {
        var lines = video.Cues.Count > 0
            ? video.Cues.OrderBy(cue => cue.Seconds).Select(cue => cue.Text)
            : video.FullLyrics.Replace("\r", "").Split('\n');
        return new LyricsPreview
        {
            Availability = LyricsAvailability.Library,
            VideoTitle = videoTitle,
            FoundTrack = video.TrackName,
            FoundArtist = video.Artist,
            CueCount = video.Cues.Count,
            Preview = FirstLines(lines)
        };
    }

    public static string FirstLines(IEnumerable<string> lines, int count = 5) =>
        string.Join("\n", lines.Select(line => line.Trim()).Where(line => line.Length > 0).Take(count));

    /// <summary>Tira os tempos "[01:23.45]" de uma letra .lrc e devolve só o texto das linhas.</summary>
    public static IEnumerable<string> LrcTextLines(string lrc) =>
        (lrc ?? "").Replace("\r", "").Split('\n')
            .Select(line => Regex.Replace(line, @"^(\s*\[[^\]]*\])+\s*", ""))
            .Where(line => line.Trim().Length > 0);

    /// <summary>Remove marcas comuns de título de vídeo ("Clipe Oficial", "(Ao Vivo)", "Playback", "[HD]"...).</summary>
    public static string CleanTitle(string? title)
    {
        var text = Regex.Replace(title ?? "", @"[\(\[\{][^\)\]\}]*[\)\]\}]", " ");
        text = Regex.Replace(text,
            @"\b(clipe oficial|v[ií]deo oficial|official (?:music )?video|official audio|[aá]udio oficial|lyric video|lyrics|com letra|letra|playback|ao vivo|live|karaok[eê]|legendado|hd|4k)\b",
            " ", RegexOptions.IgnoreCase);
        return Regex.Replace(text, @"\s+", " ").Trim(' ', '-', '–', '—', '|', ':', '/');
    }

    /// <summary>Consultas ao catálogo: título limpo inteiro e, se houver "A - B", cada parte (a ordem artista/música varia).</summary>
    public static List<string> BuildQueries(string? title)
    {
        var cleaned = CleanTitle(title);
        var queries = new List<string>();
        if (cleaned.Length > 0) queries.Add(cleaned);
        var parts = Regex.Split(cleaned, @"\s+[-–—|]\s+").Select(part => part.Trim()).Where(part => part.Length > 0).ToList();
        if (parts.Count >= 2) queries.AddRange(parts.Take(2));
        return queries.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var chars = value.Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
        return Regex.Replace(new string(chars).ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
    }

    public static bool ContainsPhrase(string text, string phrase) =>
        text.Length > 0 && phrase.Length > 0 && (" " + text + " ").Contains(" " + phrase + " ", StringComparison.Ordinal);

    /// <summary>Escolhe, entre os resultados do catálogo (lrclib), a letra que combina com o título do vídeo.</summary>
    public static LyricsPreview Pick(IEnumerable<JsonElement> items, string title, string author)
    {
        var cleanedTitle = CleanTitle(title);
        var normalizedTitle = Normalize(cleanedTitle);
        var normalizedAuthor = Normalize(Regex.Replace(author ?? "", @"\s*-\s*topic$|vevo$|\s+oficial$|\s+official$", "", RegexOptions.IgnoreCase));
        var titleParts = Regex.Split(cleanedTitle, @"\s+[-–—|]\s+").Select(Normalize).Where(part => part.Length > 0).ToList();

        var best = new List<(double Score, bool Synced, bool ArtistOk, string Track, string Artist, string Text)>();
        foreach (var item in items)
        {
            string Str(string name) => item.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
            var foundTrack = Str("trackName");
            var foundArtist = Str("artistName");
            var normalizedTrack = Normalize(foundTrack);
            var exact = titleParts.Contains(normalizedTrack) || normalizedTitle == normalizedTrack;
            // Nome curto demais ("Deus") aparece em qualquer título: só vale se for a parte inteira do título.
            if (normalizedTrack.Length < 3 || !ContainsPhrase(normalizedTitle, normalizedTrack) || (normalizedTrack.Length < 6 && !exact)) continue;

            var normalizedArtist = Normalize(foundArtist);
            var artistOk = normalizedArtist.Length >= 3 &&
                           (ContainsPhrase(normalizedTitle, normalizedArtist) || ContainsPhrase(normalizedAuthor, normalizedArtist) ||
                            ContainsPhrase(normalizedArtist, normalizedAuthor) && normalizedAuthor.Length >= 3);
            var synced = Str("syncedLyrics");
            var plain = Str("plainLyrics");
            var text = !string.IsNullOrWhiteSpace(synced) ? synced : plain;
            if (string.IsNullOrWhiteSpace(text)) continue;
            var score = (exact ? 4 : 0) + (artistOk ? 3 : 0) + Math.Min(normalizedTrack.Length, 40) / 100d;
            best.Add((score, !string.IsNullOrWhiteSpace(synced), artistOk, foundTrack, foundArtist, text));
        }

        if (best.Count == 0) return new LyricsPreview { Availability = LyricsAvailability.NotFound, VideoTitle = title };
        // Prefere letra sincronizada; entre iguais, a que mais combina com o título/canal.
        var pick = best.OrderByDescending(candidate => candidate.Synced).ThenByDescending(candidate => candidate.Score).First();
        var lines = pick.Synced ? LrcTextLines(pick.Text) : pick.Text.Replace("\r", "").Split('\n');
        return new LyricsPreview
        {
            Availability = pick.Synced ? LyricsAvailability.Synced : LyricsAvailability.PlainOnly,
            VideoTitle = title,
            FoundTrack = pick.Track,
            FoundArtist = pick.Artist,
            ArtistMatches = pick.ArtistOk,
            Preview = FirstLines(lines)
        };
    }
}
