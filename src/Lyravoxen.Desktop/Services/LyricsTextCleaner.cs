using System.Text.RegularExpressions;

namespace Lyravoxen.Services;

/// <summary>Limpa uma letra copiada de um site (Letras.mus.br, Cifra Club, Vagalume...): tira cifras, rótulos de parte
/// ([Refrão], Intro), créditos e links, e deixa estrofes separadas por uma linha em branco.</summary>
internal static class LyricsTextCleaner
{
    private const string Labels =
        @"intro(?:du[cç][aã]o)?|refr[aã]o|coro|verso|ponte|estrofe|pr[eé][\s\-]?refr[aã]o|pr[eé][\s\-]?coro|final|outro|solo|interl[uú]dio|chorus|verse|bridge|instrumental";

    // "[Refrão]", "(Intro 2x)", "{Ponte}"
    private static readonly Regex BracketLabel = new(@"^[\[\(\{]\s*(?:" + Labels + @")\b[^\]\)\}]*[\]\)\}]\s*:?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // "Refrão:", "Refrão 2", "Intro" sozinhos na linha
    private static readonly Regex PlainLabel = new(@"^(?:" + Labels + @")\s*\d{0,2}\s*:?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Chord = new(
        @"^[A-G](?:#|b)?(?:m|maj|min|dim|aug|sus|add)?\d{0,2}(?:\([^)]*\))?(?:/[A-G](?:#|b)?)?$", RegexOptions.Compiled);

    private static readonly Regex Credit = new(
        @"^(?:(?:compositor(?:es)?|composi[cç][aã]o|autor(?:es)?|fonte|enviad[ao] por|letra de|letra e m[uú]sica de)\s*[:\-–].*|letra de\s+[""“].*|©.*|copyright\b.*|https?://\S+|www\.\S+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Repeat = new(@"\s*[\(\[]?\s*(?<![\p{L}\p{N}])(?:\d\s*x|x\s*\d|bis|\d\s*vezes)\s*[\)\]]?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OnlySymbols = new(@"^[\W_]+$", RegexOptions.Compiled);

    private static bool IsChordLine(string line)
    {
        var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;
        if (tokens.Length == 1 && tokens[0].Length < 2) return false; // "E" ou "A" sozinhos podem ser letra
        return tokens.All(token => Chord.IsMatch(token) || token is "|" or "-" or "/" || Regex.IsMatch(token, @"^\(?x\d\)?$", RegexOptions.IgnoreCase));
    }

    public static string Clean(string? text)
    {
        var lines = new List<string>();
        var previousBlank = true;
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var line = Regex.Replace(raw.Replace('\u00a0', ' ').Trim(), @"\s+", " ");
            if (line.Length > 0 && (BracketLabel.IsMatch(line) || PlainLabel.IsMatch(line) || Credit.IsMatch(line) || IsChordLine(line) || OnlySymbols.IsMatch(line)))
                continue;
            line = Repeat.Replace(line, "").Trim();
            if (line.Length == 0)
            {
                if (!previousBlank) lines.Add("");
                previousBlank = true;
                continue;
            }
            lines.Add(line);
            previousBlank = false;
        }
        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Parece uma letra (e não um link ou frase solta)? Usado para aceitar o que está na área de transferência.</summary>
    public static bool LooksLikeLyrics(string? text)
    {
        var cleaned = Clean(text);
        return cleaned.Split('\n').Count(line => line.Trim().Length > 0) >= 4 && cleaned.Length >= 80;
    }
}
