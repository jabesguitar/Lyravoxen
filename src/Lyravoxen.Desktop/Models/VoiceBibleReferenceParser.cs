using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

/// <summary>
/// Interpreta a frase reconhecida pelo microfone (por exemplo "João três dezesseis",
/// "Salmos vinte e três", "Primeira Coríntios 13 versículo 4 ao 7") e devolve a referência.
/// Números por extenso viram dígitos e o resultado passa pelo <see cref="BibleReferenceParser"/>.
/// </summary>
public static class VoiceBibleReferenceParser
{
    private static readonly Dictionary<string, int> Units = new()
    {
        ["um"] = 1, ["uma"] = 1, ["dois"] = 2, ["duas"] = 2, ["tres"] = 3, ["quatro"] = 4,
        ["cinco"] = 5, ["seis"] = 6, ["sete"] = 7, ["oito"] = 8, ["nove"] = 9
    };

    private static readonly Dictionary<string, int> Teens = new()
    {
        ["dez"] = 10, ["onze"] = 11, ["doze"] = 12, ["treze"] = 13, ["quatorze"] = 14, ["catorze"] = 14,
        ["quinze"] = 15, ["dezesseis"] = 16, ["dezasseis"] = 16, ["dezessete"] = 17, ["dezassete"] = 17,
        ["dezoito"] = 18, ["dezenove"] = 19, ["dezanove"] = 19
    };

    private static readonly Dictionary<string, int> Tens = new()
    {
        ["vinte"] = 20, ["trinta"] = 30, ["quarenta"] = 40, ["cinquenta"] = 50, ["sessenta"] = 60,
        ["setenta"] = 70, ["oitenta"] = 80, ["noventa"] = 90
    };

    private static readonly Regex TokenPattern = new(@"[\p{L}\p{N}]+|[^\p{L}\p{N}\s]", RegexOptions.CultureInvariant);

    public static BibleReference? TryParse(string? heard)
    {
        if (string.IsNullOrWhiteSpace(heard)) return null;

        var converted = ConvertNumberWords(heard);
        var parsed = BibleReferenceParser.TryParseReference(converted, allowBookOnly: true)
                     ?? BibleReferenceParser.TryParseReference(heard, allowBookOnly: true);
        if (parsed is null) return null;

        // Só o nome do livro (sem nenhum número) dentro de uma frase longa costuma ser ruído:
        // apelidos curtos como "os" ou "is" coincidem com palavras comuns, e o silêncio gera legendas inventadas.
        var hasNumber = converted.Any(char.IsDigit);
        if (!parsed.VerseStart.HasValue && !hasNumber && Regex.Matches(heard, @"\p{L}+").Count > 3) return null;
        return parsed;
    }

    /// <summary>Troca números por extenso por dígitos, mantendo o restante do texto.</summary>
    public static string ConvertNumberWords(string text)
    {
        var tokens = TokenPattern.Matches(text).Select(match => match.Value).ToList();
        var output = new List<string>(tokens.Count);
        var index = 0;
        while (index < tokens.Count)
        {
            if (TryReadNumber(tokens, index, out var value, out var used))
            {
                output.Add(value.ToString(CultureInfo.InvariantCulture));
                index += used;
            }
            else
            {
                output.Add(tokens[index]);
                index++;
            }
        }
        return string.Join(' ', output);
    }

    private static bool TryReadNumber(IReadOnlyList<string> tokens, int start, out int value, out int used)
    {
        value = 0;
        used = 0;
        var i = start;
        var first = Fold(tokens[i]);

        if (first is "cem" or "cento")
        {
            value = 100;
            i++;
            if (i + 1 < tokens.Count && Fold(tokens[i]) == "e" && IsSmallNumberWord(Fold(tokens[i + 1])))
            {
                i++; // consome o "e" de "cento e vinte"
            }
            else
            {
                used = i - start;
                return true;
            }
        }

        var word = i < tokens.Count ? Fold(tokens[i]) : "";
        if (Teens.TryGetValue(word, out var teen))
        {
            value += teen;
            i++;
        }
        else if (Tens.TryGetValue(word, out var ten))
        {
            value += ten;
            i++;
            if (i + 1 < tokens.Count && Fold(tokens[i]) == "e" && Units.TryGetValue(Fold(tokens[i + 1]), out var unit))
            {
                value += unit;
                i += 2;
            }
        }
        else if (Units.TryGetValue(word, out var single))
        {
            value += single;
            i++;
        }

        used = i - start;
        return used > 0;
    }

    private static bool IsSmallNumberWord(string word)
        => Units.ContainsKey(word) || Teens.ContainsKey(word) || Tens.ContainsKey(word);

    private static string Fold(string word)
    {
        var decomposed = word.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        return builder.ToString();
    }
}
