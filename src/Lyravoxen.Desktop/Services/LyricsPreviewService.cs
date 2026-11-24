using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyravoxen.Models;

namespace Lyravoxen.Services;

internal enum LyricsAvailability { Library, Synced, PlainOnly, NotFound, Error }

/// <summary>Resultado da verificação "este vídeo já tem letra?" (sem abrir janelas, sem alterar nada).</summary>
internal sealed record LyricsPreview
{
    public LyricsAvailability Availability { get; init; }
    public string VideoTitle { get; init; } = "";
    public string VideoChannel { get; init; } = "";
    /// <summary>Título da música e intérprete deduzidos do título/canal do vídeo (já sem "Playback", "Legendado"...).</summary>
    public string GuessTrack { get; init; } = "";
    public string GuessArtist { get; init; } = "";
    public string FoundTrack { get; init; } = "";
    public string FoundArtist { get; init; } = "";
    /// <summary>False quando a faixa do catálogo tem o nome certo mas o artista não aparece no título/canal: conferir a versão.</summary>
    public bool ArtistMatches { get; init; } = true;
    public int CueCount { get; init; }
    /// <summary>Quantos resultados o catálogo devolveu na busca (ajuda a entender um "não encontrada").</summary>
    public int CatalogResults { get; init; }
    /// <summary>Primeiras linhas da letra, para o operador conferir se é o mesmo louvor.</summary>
    public string Preview { get; init; } = "";
    /// <summary>Frases com tempo da letra do catálogo (vazio se só houver texto ou se veio da biblioteca).</summary>
    public List<LyricCue> Cues { get; init; } = new();
    /// <summary>Letra completa em texto, sem tempos.</summary>
    public string PlainText { get; init; } = "";
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
        LyricsAvailability.NotFound => "❌ Letra não encontrada no catálogo" + (CatalogResults > 0 ? " (" + CatalogResults + " resultados, nenhum com esse nome)" : "") + ". Será preciso cadastrar.",
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
            var items = await SearchCatalogAsync(BuildQueries(title));
            var guess = TrackTitleParser.Parse(title, author, videos.Select(video => video.Artist));
            if (!guess.Confident)
            {
                // "A - B" sem pistas: os resultados do catálogo mostram qual é a música e qual é o intérprete.
                var order = DecideOrder(items, guess.PartA, guess.PartB);
                if (order == true) guess = new ParsedTrack(guess.PartA, guess.PartB, true);
                else if (order == false) guess = guess with { Confident = true };
            }
            return Store(videoId, Pick(items, title, author) with
            {
                VideoChannel = author,
                GuessTrack = guess.Track,
                GuessArtist = guess.Artist,
                CatalogResults = items.Count
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Store(videoId, new LyricsPreview { Availability = LyricsAvailability.Error, VideoTitle = title, VideoChannel = author });
        }
    }

    /// <summary>Consulta o catálogo (lrclib) em paralelo, uma consulta de texto livre (q=) por termo.</summary>
    private static Task<List<JsonElement>> SearchCatalogAsync(IEnumerable<string> terms) =>
        SearchCatalogQueriesAsync(terms.Select(term => "q=" + Uri.EscapeDataString(term)));

    /// <summary>Cada item é a query-string pronta ("q=..." ou "track_name=...&amp;artist_name=...").</summary>
    private static async Task<List<JsonElement>> SearchCatalogQueriesAsync(IEnumerable<string> queryStrings)
    {
        var tasks = queryStrings.Distinct(StringComparer.Ordinal).Select(async queryString =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://lrclib.net/api/search?" + queryString);
            using var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<JsonElement>();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.ValueKind == JsonValueKind.Array
                ? json.RootElement.EnumerateArray().Select(item => item.Clone()).ToList()
                : new List<JsonElement>();
        }).ToList();
        return (await Task.WhenAll(tasks)).SelectMany(list => list).ToList();
    }

    /// <summary>Letra já salva na biblioteca para este vídeo (ou null). Chamar da thread da tela.</summary>
    public static LyricsPreview? LibraryMatch(string videoId, IEnumerable<LibraryFolder> library)
    {
        var video = library.SelectMany(folder => folder.Videos)
            .FirstOrDefault(item => !item.IsOffline && item.VideoId == videoId && HasLyrics(item));
        return video is null ? null : FromLibrary(video, video.Title);
    }

    /// <summary>Procura a letra pelo nome da música e do intérprete digitados (tolera "Ao Vivo", ordem trocada e pequenas diferenças).</summary>
    public static async Task<LyricsPreview> SearchByNamesAsync(string track, string artist)
    {
        track = CleanTitle(track);
        artist = CleanTitle(artist);
        if (track.Length == 0) return new LyricsPreview { Availability = LyricsAvailability.NotFound };
        try
        {
            var queryStrings = new List<string>();
            if (artist.Length > 0)
            {
                queryStrings.Add("q=" + Uri.EscapeDataString(track + " " + artist));
                queryStrings.Add("track_name=" + Uri.EscapeDataString(track) + "&artist_name=" + Uri.EscapeDataString(artist));
            }
            queryStrings.Add("q=" + Uri.EscapeDataString(track));
            queryStrings.Add("track_name=" + Uri.EscapeDataString(track));
            return PickByNames(await SearchCatalogQueriesAsync(queryStrings), track, artist);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new LyricsPreview { Availability = LyricsAvailability.Error };
        }
    }

    private static double WordOverlap(string a, string b)
    {
        var wa = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var wb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (wa.Count == 0 || wb.Count == 0) return 0;
        return (double)wa.Intersect(wb).Count() / Math.Min(wa.Count, wb.Count);
    }

    /// <summary>Quanto o nome encontrado combina com o digitado, de 0 a 4 (3 ou mais = é a mesma música).</summary>
    public static int NameMatch(string typed, string found) => NameScore(Normalize(CleanTitle(typed)), Normalize(found));

    /// <summary>0 a 4: quanto o nome encontrado combina com o digitado (4 = igual, 3 = um contém o outro, 1 = palavras em comum).</summary>
    private static int NameScore(string typed, string found)
    {
        if (typed.Length == 0 || found.Length == 0) return 0;
        if (typed == found) return 4;
        if (ContainsPhrase(found, typed) || ContainsPhrase(typed, found)) return 3;
        return WordOverlap(typed, found) >= 0.6 ? 1 : 0;
    }

    /// <summary>Escolhe a letra pelos nomes digitados: aceita a música com sufixos ("Reconstrução - Ao Vivo"), o intérprete
    /// parecido e até os dois campos trocados. Prefere letra sincronizada.</summary>
    public static LyricsPreview PickByNames(IEnumerable<JsonElement> items, string track, string artist)
    {
        var list = items as IList<JsonElement> ?? items.ToList();
        var typedTrack = Normalize(track);
        var typedArtist = Normalize(artist);
        var candidates = new List<(int Score, bool Synced, bool ArtistOk, string Track, string Artist, string Text, List<LyricCue> Cues)>();
        foreach (var item in list)
        {
            string Str(string name) => item.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
            var foundTrack = Str("trackName");
            var foundArtist = Str("artistName");
            var normalizedTrack = Normalize(foundTrack);
            var normalizedArtist = Normalize(foundArtist);

            // Normal (música = campo da música) e trocado (o usuário pôs o intérprete no campo da música).
            var trackScore = NameScore(typedTrack, normalizedTrack);
            var artistScore = typedArtist.Length == 0 ? 1 : NameScore(typedArtist, normalizedArtist);
            var swappedTrackScore = typedArtist.Length == 0 ? 0 : NameScore(typedArtist, normalizedTrack);
            var swappedArtistScore = NameScore(typedTrack, normalizedArtist);
            var typedForTrack = typedTrack;
            if (swappedTrackScore > trackScore && swappedTrackScore >= 3 && swappedArtistScore >= 1)
            {
                trackScore = swappedTrackScore;
                artistScore = swappedArtistScore;
                typedForTrack = typedArtist;
            }
            // Nome curto ("Deus", "Santo") só vale se for igual ou se o intérprete combinar bem.
            if (trackScore < 3 || (typedForTrack.Length < 6 && trackScore < 4 && artistScore < 3)) continue;

            var syncedText = Str("syncedLyrics");
            var plain = Str("plainLyrics");
            var cues = string.IsNullOrWhiteSpace(syncedText) ? new List<LyricCue>() : ParseLrc(syncedText);
            var isSynced = cues.Count >= 2;
            var text = isSynced ? syncedText : (!string.IsNullOrWhiteSpace(plain) ? plain : "");
            if (string.IsNullOrWhiteSpace(text)) continue;
            candidates.Add((trackScore * 2 + artistScore * 2, isSynced, artistScore >= 2 || typedArtist.Length == 0, foundTrack, foundArtist, text, cues));
        }

        if (candidates.Count == 0)
            return new LyricsPreview { Availability = LyricsAvailability.NotFound, CatalogResults = list.Count };
        var pick = candidates.OrderByDescending(candidate => candidate.Synced).ThenByDescending(candidate => candidate.Score).First();
        var lines = pick.Synced ? LrcTextLines(pick.Text) : pick.Text.Replace("\r", "").Split('\n');
        return new LyricsPreview
        {
            Availability = pick.Synced ? LyricsAvailability.Synced : LyricsAvailability.PlainOnly,
            FoundTrack = pick.Track,
            FoundArtist = pick.Artist,
            ArtistMatches = pick.ArtistOk,
            Preview = FirstLines(lines),
            Cues = pick.Synced ? pick.Cues : new List<LyricCue>(),
            PlainText = string.Join("\n", lines).Trim(),
            CatalogResults = list.Count
        };
    }

    /// <summary>Para "A - B" sem pistas: o catálogo confirma se A é a música (true) ou se B é a música (false). null = não deu para decidir.</summary>
    public static async Task<bool?> ResolveOrderAsync(string partA, string partB)
    {
        try { return DecideOrder(await SearchCatalogAsync(new[] { partA + " " + partB }), partA, partB); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return null; }
    }

    public static bool? DecideOrder(IEnumerable<JsonElement> items, string partA, string partB)
    {
        var a = Normalize(partA);
        var b = Normalize(partB);
        var aTrack = 0;
        var bTrack = 0;
        foreach (var item in items)
        {
            string Str(string name) => item.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
            var track = Normalize(Str("trackName"));
            var artist = Normalize(Str("artistName"));
            bool Near(string x, string y) => x.Length >= 3 && y.Length >= 3 && (x == y || ContainsPhrase(x, y) || ContainsPhrase(y, x));
            if (track == a && Near(artist, b)) aTrack++;
            if (track == b && Near(artist, a)) bTrack++;
        }
        if (aTrack > 0 && bTrack == 0) return true;
        if (bTrack > 0 && aTrack == 0) return false;
        return null;
    }

    /// <summary>Converte uma letra .lrc ("[01:23.45] texto") em frases com tempo.</summary>
    public static List<LyricCue> ParseLrc(string? lrc)
    {
        var cues = new List<LyricCue>();
        foreach (var raw in (lrc ?? "").Replace("\r", "").Split('\n'))
        {
            var tags = Regex.Matches(raw, @"\[(\d{1,3}):(\d{1,2}(?:[.,]\d{1,3})?)\]");
            if (tags.Count == 0) continue;
            var text = Regex.Replace(raw, @"^(\s*\[\d[^\]]*\])+\s*", "").Trim();
            if (text.Length == 0) continue;
            foreach (Match tag in tags)
            {
                var seconds = int.Parse(tag.Groups[1].Value, CultureInfo.InvariantCulture) * 60 +
                              double.Parse(tag.Groups[2].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                cues.Add(new LyricCue { Seconds = seconds, Text = text });
            }
        }
        return cues.OrderBy(cue => cue.Seconds).ToList();
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

    /// <summary>Remove marcas comuns de título de vídeo ("Clipe Oficial", "(Ao Vivo)", "Playback", "Legendado", "[HD]", emojis...).</summary>
    public static string CleanTitle(string? title)
    {
        var text = Regex.Replace(title ?? "", @"[\(\[\{][^\)\]\}]*[\)\]\}]", " ");
        text = Regex.Replace(text, @"[\u2600-\u27BF\u266A\u266B]|[\uD83C-\uDBFF][\uDC00-\uDFFF]", " ");
        // "feat. Fulano" / "ft. Fulano" até o próximo separador.
        text = Regex.Replace(text, @"\s+(?:feat|ft|featuring)\.?\s+[^-–—|]+", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text,
            @"\b(?:(?:clipe|v[ií]deo|[aá]udio|videoclipe)\s+oficial|official\s+(?:music\s+|lyric\s+)?(?:video|audio)|lyrics?\s+video|lyrics?|com\s+legendas?|com\s+letras?|legendad[oa]s?|legendas?|letras?|play\s?back|ao\s+vivo|live|karaok[eê]|instrumental|(?:(?:meio|um|uns|1|dois|2|tr[eê]s|3|quatro|4)\s+)?(?:tom|tons)\s+(?:abaixo|acima)|tom\s*(?:de|em|:)\s*[A-G][#b]?m?|(?:full\s+)?hd|hq|4k|1080p|720p|oficial|official|visualizer)\b",
            " ", RegexOptions.IgnoreCase);
        return Regex.Replace(text, @"\s+", " ").Trim(' ', '-', '–', '—', '|', ':', '/', '"', '“', '”', '*');
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

        var best = new List<(double Score, bool Synced, bool ArtistOk, string Track, string Artist, string Text, List<LyricCue> Cues)>();
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
            var syncedText = Str("syncedLyrics");
            var plain = Str("plainLyrics");
            var cues = string.IsNullOrWhiteSpace(syncedText) ? new List<LyricCue>() : ParseLrc(syncedText);
            var isSynced = cues.Count >= 2;
            var text = isSynced ? syncedText : (!string.IsNullOrWhiteSpace(plain) ? plain : "");
            if (string.IsNullOrWhiteSpace(text)) continue;
            var score = (exact ? 4 : 0) + (artistOk ? 3 : 0) + Math.Min(normalizedTrack.Length, 40) / 100d;
            best.Add((score, isSynced, artistOk, foundTrack, foundArtist, text, cues));
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
            Preview = FirstLines(lines),
            Cues = pick.Synced ? pick.Cues : new List<LyricCue>(),
            PlainText = string.Join("\n", pick.Synced ? LrcTextLines(pick.Text) : pick.Text.Replace("\r", "").Split('\n')).Trim()
        };
    }
}
