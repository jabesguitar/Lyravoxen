using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

public static class LyricsCatalogRanking
{
    private static readonly string[] BrazilianMarkers = { "voce", "voces", "ce", "ta", "to", "pra", "pro", "a gente", "ne", "saudade", "bora", "meu bem" };
    private static readonly string[] PortugueseMarkers = { "nao", "coracao", "senhor", "louvor", "minha", "meu", "tua", "teu", "amor", "vida", "mundo", "deus", "porque", "quando" };
    private static readonly string[] SpanishMarkers = { "una y otra vez", "quiero", "eres", "tuyos", "corazon", "ojos", "llorar", "siempre", "noche", "contigo", "mi piel", "los dias" };
    private static readonly string[] EnglishMarkers = { "the", "you", "your", "with", "when", "know", "heart", "love", "tonight", "never", "forever", "without" };

    public static int ScoreBrazilianPortuguese(string? lyrics, string? localeHint = null)
    {
        var hint = Normalize(localeHint ?? "");
        if (hint.Contains("pt br", StringComparison.Ordinal) || hint.Contains("brazil", StringComparison.Ordinal) || hint.Contains("brasil", StringComparison.Ordinal)) return 100;
        var text = " " + Normalize(lyrics ?? "") + " ";
        if (text.Trim().Length == 0) return 0;
        var br = CountDistinctMarkers(text, BrazilianMarkers);
        var pt = CountDistinctMarkers(text, PortugueseMarkers);
        var es = CountDistinctMarkers(text, SpanishMarkers);
        var en = CountDistinctMarkers(text, EnglishMarkers);
        if (br >= 2) return 100;
        if (br == 1) return 75;
        if (pt >= 3 && es == 0 && en <= 1) return 45;
        if (es >= 2 && pt < 2) return -35;
        if (en >= 2 && pt < 2) return -25;
        if (pt >= 2) return 25;
        return 0;
    }

    public static string GetLocaleLabel(int score) => score switch
    {
        >= 70 => "PT-BR provável",
        > 0 => "Português provável",
        < 0 => "Outro idioma provável",
        _ => "Idioma não confirmado"
    };

    public static int GetNameMatchScore(string? expected, string? candidate)
    {
        var left = Normalize(expected ?? "");
        var right = Normalize(candidate ?? "");
        if (left.Length == 0 || right.Length == 0) return 0;
        if (left == right) return 100;
        if (left.Contains(right, StringComparison.Ordinal) || right.Contains(left, StringComparison.Ordinal)) return 65;
        var a = left.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var b = right.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        return a.Count == 0 ? 0 : (int)Math.Round(a.Intersect(b).Count() * 50d / a.Count);
    }

    private static int CountDistinctMarkers(string text, IEnumerable<string> markers) =>
        markers.Select(Normalize).Distinct(StringComparer.Ordinal)
            .Count(marker => Regex.IsMatch(text, @"(?<![a-z0-9])" + Regex.Escape(marker) + @"(?![a-z0-9])"));

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
        }
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }
}
