using System.Text.RegularExpressions;

namespace Lyravoxen.Services;

/// <summary>Resultado: Track = título da música, Artist = intérprete. PartA/PartB só vêm preenchidos quando o título era "A - B"
/// e não deu para saber qual dos dois é o intérprete (Confident = false).</summary>
internal sealed record ParsedTrack(string Track, string Artist, bool Confident, string PartA = "", string PartB = "");

/// <summary>Separa, a partir do título e do canal de um vídeo do YouTube, o intérprete e o título da música,
/// já sem "Playback", "Legendado", "Clipe Oficial", "(Ao Vivo)" etc.</summary>
internal static class TrackTitleParser
{
    private static readonly Regex PartSplit = new(@"\s+[-–—|]\s+", RegexOptions.Compiled);

    private static readonly Regex ChannelSuffix = new(
        @"\s*-\s*topic$|\s*vevo$|\s+(?:oficial|official)(?:\s+channel)?$|\s+music$|\s+records?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Canais de playback/letra/karaokê não são o intérprete.
    private static readonly Regex NotAnArtistChannel = new(
        @"\b(?:play\s?back|karaok[eê]|legendad[oa]s?|lyrics?|letras?|instrumental|cifras?|covers?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string CleanChannel(string? channel)
    {
        var name = channel ?? "";
        string previous;
        do { previous = name; name = ChannelSuffix.Replace(name, "").Trim(); } while (name != previous);
        name = Regex.Replace(name, @"\s+", " ");
        return NotAnArtistChannel.IsMatch(name) ? "" : name;
    }

    public static List<string> SplitParts(string cleanedTitle) =>
        PartSplit.Split(cleanedTitle ?? "").Select(part => part.Trim()).Where(part => part.Length > 0).ToList();

    private static bool Same(string x, string y) =>
        x.Length >= 3 && y.Length >= 3 && (x == y || LyricsPreviewService.ContainsPhrase(x, y) || LyricsPreviewService.ContainsPhrase(y, x));

    public static ParsedTrack Parse(string? videoTitle, string? channel, IEnumerable<string>? knownArtists = null)
    {
        var channelName = CleanChannel(channel);
        var parts = SplitParts(LyricsPreviewService.CleanTitle(videoTitle));
        // Numeração de faixa no começo ("01 - Santo").
        if (parts.Count >= 2 && Regex.IsMatch(parts[0], @"^\d{1,3}$")) parts.RemoveAt(0);

        if (parts.Count == 0) return new ParsedTrack("", channelName, channelName.Length > 0);
        if (parts.Count == 1) return new ParsedTrack(parts[0], channelName, true);

        var a = parts[0];
        var b = parts[1];
        var normalizedA = LyricsPreviewService.Normalize(a);
        var normalizedB = LyricsPreviewService.Normalize(b);
        var normalizedChannel = LyricsPreviewService.Normalize(channelName);

        // 1) O nome do canal costuma ser o do intérprete.
        var aIsChannel = Same(normalizedA, normalizedChannel);
        var bIsChannel = Same(normalizedB, normalizedChannel);
        if (aIsChannel && !bIsChannel) return new ParsedTrack(b, a, true);
        if (bIsChannel && !aIsChannel) return new ParsedTrack(a, b, true);

        // 2) Intérpretes que você já cadastrou.
        var known = new HashSet<string>((knownArtists ?? Enumerable.Empty<string>())
            .Select(LyricsPreviewService.Normalize).Where(name => name.Length >= 3));
        var aKnown = known.Contains(normalizedA);
        var bKnown = known.Contains(normalizedB);
        if (aKnown && !bKnown) return new ParsedTrack(b, a, true);
        if (bKnown && !aKnown) return new ParsedTrack(a, b, true);

        // 3) Sem pistas: o padrão do YouTube é "Intérprete - Música"; o catálogo pode corrigir depois (ResolveOrderAsync).
        return new ParsedTrack(b, a, false, a, b);
    }

    /// <summary>True quando o campo "título da música" parece ter ficado com o título inteiro do vídeo
    /// (tem "Intérprete - Música", "(Playback)", "Legendado"...), ou seja, ainda não foi corrigido.</summary>
    public static bool LooksUncorrected(string? track, string? rawVideoTitle)
    {
        var text = (track ?? "").Trim();
        if (text.Length == 0) return true;
        if (LyricsPreviewService.Normalize(text) == LyricsPreviewService.Normalize(rawVideoTitle)) return true;
        if (LyricsPreviewService.CleanTitle(text) != text) return true;
        return SplitParts(text).Count >= 2;
    }
}
