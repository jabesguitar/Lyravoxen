using System.Text;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

public sealed record BibleBookInfo(int Number, string Name, int Chapters, string[] Aliases)
{
    public string Testament => Number <= 39 ? "ANTIGO TESTAMENTO" : "NOVO TESTAMENTO";
    public override string ToString() => Name;
}

public sealed record BibleReference(BibleBookInfo Book, int Chapter, int? VerseStart, int? VerseEnd)
{
    public override string ToString()
    {
        var text = Book.Name + " " + Chapter;
        if (!VerseStart.HasValue) return text;
        text += ":" + VerseStart.Value;
        if (VerseEnd.HasValue && VerseEnd.Value > VerseStart.Value) text += "-" + VerseEnd.Value;
        return text;
    }
}

/// <summary>
/// Understands references typed or spoken in Portuguese ("Jo 3:16", "primeiro Samuel capítulo dois
/// versículo quatro", "1Co 13:4-7") and knows the 66 books of the Bible with their chapter counts.
/// </summary>
public static class BibleReferenceParser
{
    private static BibleBookInfo Book(int number, string name, int chapters, params string[] aliases)
        => new(number, name, chapters, aliases);

    // A ordem é a ordem canônica: Number = posição + 1. Os apelidos já vêm sem acentos e em minúsculas.
    public static IReadOnlyList<BibleBookInfo> Books { get; } = new[]
    {
        Book(1, "Gênesis", 50, "genesis", "gn"), Book(2, "Êxodo", 40, "exodo", "ex"),
        Book(3, "Levítico", 27, "levitico", "lv"), Book(4, "Números", 36, "numeros", "nm"),
        Book(5, "Deuteronômio", 34, "deuteronomio", "dt"), Book(6, "Josué", 24, "josue", "js"),
        Book(7, "Juízes", 21, "juizes", "jz"), Book(8, "Rute", 4, "rute", "rt"),
        Book(9, "1 Samuel", 31, "1 samuel", "i samuel", "1sm"), Book(10, "2 Samuel", 24, "2 samuel", "ii samuel", "2sm"),
        Book(11, "1 Reis", 22, "1 reis", "1rs"), Book(12, "2 Reis", 25, "2 reis", "2rs"),
        Book(13, "1 Crônicas", 29, "1 cronicas", "1cr"), Book(14, "2 Crônicas", 36, "2 cronicas", "2cr"),
        Book(15, "Esdras", 10, "esdras", "ed"), Book(16, "Neemias", 13, "neemias", "ne"),
        Book(17, "Ester", 10, "ester", "et"), Book(18, "Jó", 42, "job"),
        Book(19, "Salmos", 150, "salmos", "salmo", "sl"), Book(20, "Provérbios", 31, "proverbios", "pv"),
        Book(21, "Eclesiastes", 12, "eclesiastes", "ec"), Book(22, "Cantares", 8, "cantares", "canticos", "ct"),
        Book(23, "Isaías", 66, "isaias", "is"), Book(24, "Jeremias", 52, "jeremias", "jr"),
        Book(25, "Lamentações", 5, "lamentacoes", "lm"), Book(26, "Ezequiel", 48, "ezequiel", "ez"),
        Book(27, "Daniel", 12, "daniel", "dn"), Book(28, "Oseias", 14, "oseias", "os"),
        Book(29, "Joel", 3, "joel", "jl"), Book(30, "Amós", 9, "amos", "am"),
        Book(31, "Obadias", 1, "obadias", "ob"), Book(32, "Jonas", 4, "jonas", "jn"),
        Book(33, "Miqueias", 7, "miqueias", "mq"), Book(34, "Naum", 3, "naum", "na"),
        Book(35, "Habacuque", 3, "habacuque", "hc"), Book(36, "Sofonias", 3, "sofonias", "sf"),
        Book(37, "Ageu", 2, "ageu", "ag"), Book(38, "Zacarias", 14, "zacarias", "zc"),
        Book(39, "Malaquias", 4, "malaquias", "ml"), Book(40, "Mateus", 28, "mateus", "mt"),
        Book(41, "Marcos", 16, "marcos", "mc"), Book(42, "Lucas", 24, "lucas", "lc"),
        Book(43, "João", 21, "joao", "jo"), Book(44, "Atos", 28, "atos", "at"),
        Book(45, "Romanos", 16, "romanos", "rm"), Book(46, "1 Coríntios", 16, "1 corintios", "1co"),
        Book(47, "2 Coríntios", 13, "2 corintios", "2co"), Book(48, "Gálatas", 6, "galatas", "gl"),
        Book(49, "Efésios", 6, "efesios", "ef"), Book(50, "Filipenses", 4, "filipenses", "fp"),
        Book(51, "Colossenses", 4, "colossenses", "cl"), Book(52, "1 Tessalonicenses", 5, "1 tessalonicenses", "1ts"),
        Book(53, "2 Tessalonicenses", 3, "2 tessalonicenses", "2ts"), Book(54, "1 Timóteo", 6, "1 timoteo", "1tm"),
        Book(55, "2 Timóteo", 4, "2 timoteo", "2tm"), Book(56, "Tito", 3, "tito", "tt"),
        Book(57, "Filemom", 1, "filemom", "fm"), Book(58, "Hebreus", 13, "hebreus", "hb"),
        Book(59, "Tiago", 5, "tiago", "tg"), Book(60, "1 Pedro", 5, "1 pedro", "1pe"),
        Book(61, "2 Pedro", 3, "2 pedro", "2pe"), Book(62, "1 João", 5, "1 joao", "1jo"),
        Book(63, "2 João", 1, "2 joao", "2jo"), Book(64, "3 João", 1, "3 joao", "3jo"),
        Book(65, "Judas", 1, "judas", "jd"), Book(66, "Apocalipse", 22, "apocalipse", "ap")
    };

    private static readonly List<(BibleBookInfo Book, Regex Pattern)> AliasPatterns = BuildAliasPatterns();

    // "Jó" (com acento) é o livro de Jó; "Jo" sem acento é a abreviação usual de João.
    private static readonly Regex JobWithAccent =
        new(@"(?<![\p{L}\p{N}])j\u00f3(?![\p{L}\p{N}])", RegexOptions.CultureInvariant);

    private static readonly Regex NumericReference = new(
        @"(?:capitulo\s*)?(?<chapter>\d{1,3})(?:\s*(?::|,|\.|\s+)?\s*(?:versiculos?\s*)?(?<verse>\d{1,3})(?:\s*(?:[-\u2013\u2014]|ao|ate|a)\s*(?<end>\d{1,3}))?)?",
        RegexOptions.CultureInvariant);

    private static List<(BibleBookInfo Book, Regex Pattern)> BuildAliasPatterns()
    {
        var list = new List<(BibleBookInfo Book, Regex Pattern)>();
        foreach (var book in Books)
            foreach (var alias in book.Aliases)
                list.Add((book, new Regex(@"(?<![a-z0-9])" + AliasToPattern(alias) + @"(?![a-z0-9])", RegexOptions.CultureInvariant)));
        return list;
    }

    private static string AliasToPattern(string alias)
    {
        var trimmed = alias.Trim();
        // "1jo", "1 jo" e "1  jo" são a mesma coisa.
        if (trimmed.Length > 1 && char.IsDigit(trimmed[0]) && char.IsLetter(trimmed[1]))
            return trimmed[0] + @"\s*" + Regex.Escape(trimmed.Substring(1));
        return Regex.Escape(trimmed).Replace(@"\ ", @"\s*");
    }

    private static string NormalizeText(string text)
    {
        var normalized = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return Regex.Replace(normalized, @"\p{Mn}", "")
            .Replace("primeiro ", "1 ").Replace("primeira ", "1 ")
            .Replace("segundo ", "2 ").Replace("segunda ", "2 ")
            .Replace("terceiro ", "3 ").Replace("terceira ", "3 ")
            .Replace("\u00ba", "").Replace("\u00aa", "");
    }

    public static string? TryParse(string spokenText) => TryParseReference(spokenText)?.ToString();

    /// <summary>
    /// Finds the book, chapter and verse(s) in free text. With <paramref name="allowBookOnly"/>,
    /// a text with only the book name opens chapter 1 instead of being rejected.
    /// </summary>
    public static BibleReference? TryParseReference(string text, bool allowBookOnly = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        BibleBookInfo? book = null;
        var tail = "";

        var lowered = text.ToLowerInvariant().Normalize(NormalizationForm.FormC);
        var job = JobWithAccent.Match(lowered);
        if (job.Success)
        {
            book = Books[17];
            tail = NormalizeText(lowered.Substring(job.Index + job.Length));
        }
        else
        {
            var normalized = NormalizeText(text);
            var bestLength = 0;
            var bestIndex = int.MaxValue;
            foreach (var (candidate, pattern) in AliasPatterns)
            {
                var match = pattern.Match(normalized);
                if (!match.Success) continue;
                // Vence o apelido mais longo ("1 joao" antes de "joao"); empate: o que aparece primeiro.
                if (match.Length > bestLength || (match.Length == bestLength && match.Index < bestIndex))
                {
                    bestLength = match.Length;
                    bestIndex = match.Index;
                    book = candidate;
                    tail = normalized.Substring(match.Index + match.Length);
                }
            }
        }

        if (book is null) return null;

        int chapter;
        int? verse = null;
        int? verseEnd = null;
        var numeric = NumericReference.Match(tail);
        if (numeric.Success && int.TryParse(numeric.Groups["chapter"].Value, out chapter))
        {
            if (numeric.Groups["verse"].Success &&
                int.TryParse(numeric.Groups["verse"].Value, out var numericVerse))
                verse = numericVerse;
            if (verse.HasValue && numeric.Groups["end"].Success &&
                int.TryParse(numeric.Groups["end"].Value, out var numericEnd))
                verseEnd = numericEnd;
        }
        else
        {
            var words = Regex.Matches(tail, @"[a-z]+").Select(m => m.Value).ToList();
            var chapterMarker = words.FindIndex(w => w is "capitulo" or "cap");
            var chapterStart = chapterMarker >= 0 ? chapterMarker + 1 : 0;
            if (!TryParsePortugueseNumber(words, chapterStart, out chapter, out var chapterConsumed))
                return allowBookOnly ? new BibleReference(book, 1, null, null) : null;

            var verseMarker = words.FindIndex(chapterStart + chapterConsumed, w => w is "versiculo" or "versiculos");
            if (verseMarker >= 0 && TryParsePortugueseNumber(words, verseMarker + 1, out var spokenVerse, out _))
                verse = spokenVerse;
        }

        if (!verse.HasValue)
        {
            var trailingWords = Regex.Matches(tail, @"[a-z]+").Select(m => m.Value).ToList();
            var spokenVerseMarker = trailingWords.FindIndex(w => w is "versiculo" or "versiculos");
            if (spokenVerseMarker >= 0 &&
                TryParsePortugueseNumber(trailingWords, spokenVerseMarker + 1, out var trailingVerse, out _))
                verse = trailingVerse;
        }

        if (chapter < 1 || chapter > book.Chapters || verse is < 1 or > 176) return null;
        if (verseEnd is < 1 or > 176) verseEnd = null;
        if (verse.HasValue && verseEnd.HasValue && verseEnd.Value <= verse.Value) verseEnd = null;
        return new BibleReference(book, chapter, verse, verseEnd);
    }

    private static bool TryParsePortugueseNumber(IReadOnlyList<string> words, int start, out int value, out int consumed)
    {
        value = 0;
        consumed = 0;
        if (start < 0 || start >= words.Count) return false;
        var units = new Dictionary<string, int>
        {
            ["zero"]=0,["um"]=1,["uma"]=1,["dois"]=2,["duas"]=2,["tres"]=3,["quatro"]=4,["cinco"]=5,
            ["seis"]=6,["sete"]=7,["oito"]=8,["nove"]=9,["dez"]=10,["onze"]=11,["doze"]=12,
            ["treze"]=13,["quatorze"]=14,["catorze"]=14,["quinze"]=15,["dezesseis"]=16,
            ["dezessete"]=17,["dezoito"]=18,["dezenove"]=19,["vinte"]=20,["trinta"]=30,
            ["quarenta"]=40,["cinquenta"]=50,["sessenta"]=60,["setenta"]=70,["oitenta"]=80,
            ["noventa"]=90,["cem"]=100,["cento"]=100
        };
        if (int.TryParse(words[start], out value)) { consumed = 1; return true; }
        if (!units.TryGetValue(words[start], out value)) return false;
        consumed = 1;
        if (value >= 20 && value < 100 && start + 2 < words.Count &&
            words[start + 1] == "e" && units.TryGetValue(words[start + 2], out var unit) && unit < 10)
        {
            value += unit;
            consumed = 3;
        }
        else if (value == 100 && start + 2 < words.Count &&
            words[start + 1] == "e" && units.TryGetValue(words[start + 2], out var remainder) && remainder < 100)
        {
            value += remainder;
            consumed = 3;
        }
        return true;
    }
}
