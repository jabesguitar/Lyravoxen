using System.Globalization;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

public static class BibleReferenceParser
{
    public static string? TryParse(string spokenText)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;
        var normalized = spokenText.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        normalized = Regex.Replace(normalized, @"\p{Mn}", "")
            .Replace("primeiro ", "1 ").Replace("primeira ", "1 ")
            .Replace("segundo ", "2 ").Replace("segunda ", "2 ")
            .Replace("terceiro ", "3 ").Replace("terceira ", "3 ");

        var books = new (string[] Names, string Canonical)[]
        {
            (new[]{"genesis","gn"}, "Gênesis"), (new[]{"exodo","ex"}, "Êxodo"),
            (new[]{"levitico","lv"}, "Levítico"), (new[]{"numeros","nm"}, "Números"),
            (new[]{"deuteronomio","dt"}, "Deuteronômio"), (new[]{"josue","js"}, "Josué"),
            (new[]{"juizes","jz"}, "Juízes"), (new[]{"rute","rt"}, "Rute"),
            (new[]{"1 samuel","i samuel","1sm"}, "1 Samuel"), (new[]{"2 samuel","ii samuel","2sm"}, "2 Samuel"),
            (new[]{"1 reis","1rs"}, "1 Reis"), (new[]{"2 reis","2rs"}, "2 Reis"),
            (new[]{"1 cronicas","1cr"}, "1 Crônicas"), (new[]{"2 cronicas","2cr"}, "2 Crônicas"),
            (new[]{"esdras","ed"}, "Esdras"), (new[]{"neemias","ne"}, "Neemias"),
            (new[]{"ester","et"}, "Ester"), (new[]{"jo","job"}, "Jó"),
            (new[]{"salmos","salmo","sl"}, "Salmos"), (new[]{"proverbios","pv"}, "Provérbios"),
            (new[]{"eclesiastes","ec"}, "Eclesiastes"), (new[]{"cantares","canticos","ct"}, "Cânticos"),
            (new[]{"isaias","is"}, "Isaías"), (new[]{"jeremias","jr"}, "Jeremias"),
            (new[]{"lamentacoes","lm"}, "Lamentações"), (new[]{"ezequiel","ez"}, "Ezequiel"),
            (new[]{"daniel","dn"}, "Daniel"), (new[]{"oseias","os"}, "Oseias"),
            (new[]{"joel","jl"}, "Joel"), (new[]{"amos","am"}, "Amós"),
            (new[]{"obadias","ob"}, "Obadias"), (new[]{"jonas","jn"}, "Jonas"),
            (new[]{"miqueias","mq"}, "Miqueias"), (new[]{"naum","na"}, "Naum"),
            (new[]{"habacuque","hc"}, "Habacuque"), (new[]{"sofonias","sf"}, "Sofonias"),
            (new[]{"ageu","ag"}, "Ageu"), (new[]{"zacarias","zc"}, "Zacarias"),
            (new[]{"malaquias","ml"}, "Malaquias"), (new[]{"mateus","mt"}, "Mateus"),
            (new[]{"marcos","mc"}, "Marcos"), (new[]{"lucas","lc"}, "Lucas"),
            (new[]{"joao","jo"}, "João"), (new[]{"atos","at"}, "Atos"),
            (new[]{"romanos","rm"}, "Romanos"), (new[]{"1 corintios","1co"}, "1 Coríntios"),
            (new[]{"2 corintios","2co"}, "2 Coríntios"), (new[]{"galatas","gl"}, "Gálatas"),
            (new[]{"efesios","ef"}, "Efésios"), (new[]{"filipenses","fp"}, "Filipenses"),
            (new[]{"colossenses","cl"}, "Colossenses"), (new[]{"1 tessalonicenses","1ts"}, "1 Tessalonicenses"),
            (new[]{"2 tessalonicenses","2ts"}, "2 Tessalonicenses"), (new[]{"1 timoteo","1tm"}, "1 Timóteo"),
            (new[]{"2 timoteo","2tm"}, "2 Timóteo"), (new[]{"tito","tt"}, "Tito"),
            (new[]{"filemom","fm"}, "Filemom"), (new[]{"hebreus","hb"}, "Hebreus"),
            (new[]{"tiago","tg"}, "Tiago"), (new[]{"1 pedro","1pe"}, "1 Pedro"),
            (new[]{"2 pedro","2pe"}, "2 Pedro"), (new[]{"1 joao","1jo"}, "1 João"),
            (new[]{"2 joao","2jo"}, "2 João"), (new[]{"3 joao","3jo"}, "3 João"),
            (new[]{"judas","jd"}, "Judas"), (new[]{"apocalipse","ap"}, "Apocalipse")
        };
        var book = books.FirstOrDefault(item => item.Names.Any(name =>
            Regex.IsMatch(normalized, @"(?<![a-z0-9])" + Regex.Escape(name) + @"(?![a-z0-9])")));
        if (string.IsNullOrWhiteSpace(book.Canonical)) return null;

        var tail = normalized;
        foreach (var name in book.Names.OrderByDescending(n => n.Length))
        {
            var match = Regex.Match(normalized, @"(?<![a-z0-9])" + Regex.Escape(name) + @"(?![a-z0-9])");
            if (match.Success) { tail = normalized[(match.Index + match.Length)..]; break; }
        }

        var numeric = Regex.Match(tail,
            @"(?:capitulo\s*)?(?<chapter>\d{1,3})(?:\s*(?::|,|\s+)?\s*(?:versiculo\s*)?(?<verse>\d{1,3}))?");
        int chapter;
        int? verse = null;
        if (numeric.Success && int.TryParse(numeric.Groups["chapter"].Value, out chapter))
        {
            if (numeric.Groups["verse"].Success &&
                int.TryParse(numeric.Groups["verse"].Value, out var numericVerse))
                verse = numericVerse;
        }
        else
        {
            var words = Regex.Matches(tail, @"[a-z]+").Select(m => m.Value).ToList();
            var chapterMarker = words.FindIndex(w => w is "capitulo" or "cap");
            var chapterStart = chapterMarker >= 0 ? chapterMarker + 1 : 0;
            if (!TryParsePortugueseNumber(words, chapterStart, out chapter, out var chapterConsumed))
                return null;

            var verseMarker = words.FindIndex(chapterStart + chapterConsumed, w => w is "versiculo" or "versiculos");
            if (verseMarker >= 0 && TryParsePortugueseNumber(words, verseMarker + 1, out var spokenVerse, out _))
                verse = spokenVerse;
        }

        if (chapter < 1 || chapter > 150 || verse is < 1 or > 176) return null;
        return verse.HasValue ? $"{book.Canonical} {chapter}:{verse.Value}" : $"{book.Canonical} {chapter}";
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
