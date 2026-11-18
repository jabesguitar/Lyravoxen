using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

/// <summary>
/// Aligns incremental speech-to-text output with the next expected lyric lines.
/// It is deliberately monotonic: recognition noise cannot move the projection backwards.
/// </summary>
public static class LiveLyricsMatcher
{
    private const int NearWindow = 2;
    private const double FarMatchConfidence = 0.85;

    public static int FindNextLine(
        IReadOnlyList<string> expectedLines,
        int nextExpectedIndex,
        string transcript,
        out double confidence,
        double minimumConfidence = 0.78,
        int maxLookAhead = 2)
    {
        confidence = 0;
        if (expectedLines.Count == 0 || nextExpectedIndex < 0 || nextExpectedIndex >= expectedLines.Count)
            return -1;

        var spoken = Tokenize(transcript);
        if (spoken.Length == 0) return -1;

        var bestIndex = -1;
        var bestScore = 0d;
        var highestScore = 0d;
        var lastCandidate = Math.Min(expectedLines.Count - 1, nextExpectedIndex + Math.Max(0, maxLookAhead));

        for (var index = nextExpectedIndex; index <= lastCandidate; index++)
        {
            var expected = Tokenize(expectedLines[index]);
            if (expected.Length == 0) continue;

            var score = ScoreExpectedPhrase(expected, spoken);
            if (score > highestScore) highestScore = score;

            // Frases além da janela normal só são aceitas com confiança maior:
            // evita saltar para um refrão parecido quando o reconhecimento falha.
            var required = index > nextExpectedIndex + NearWindow
                ? Math.Max(minimumConfidence, FarMatchConfidence)
                : minimumConfidence;
            if (score >= required && score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        confidence = bestIndex >= 0 ? bestScore : highestScore;
        return bestIndex;
    }

    private static double ScoreExpectedPhrase(string[] expected, string[] spoken)
    {
        // Very short lyric fragments are ambiguous. Require exact contiguous matching.
        if (expected.Length == 1)
            return spoken.Any(word => word == expected[0]) ? 1d : 0d;

        var best = 0d;
        var minWindow = Math.Max(1, expected.Length - 1);
        var maxWindow = Math.Min(spoken.Length, expected.Length + 2);

        for (var windowLength = minWindow; windowLength <= maxWindow; windowLength++)
        {
            for (var start = Math.Max(0, spoken.Length - windowLength - 8); start <= spoken.Length - windowLength; start++)
            {
                var window = spoken.Skip(start).Take(windowLength).ToArray();
                var matched = LongestOrderedMatch(expected, window);
                var coverage = (double)matched / expected.Length;
                var precision = (double)matched / window.Length;
                var score = coverage * 0.8 + precision * 0.2;
                if (score > best) best = score;
            }
        }
        return best;
    }

    private static int LongestOrderedMatch(string[] expected, string[] spoken)
    {
        var previous = new int[spoken.Length + 1];
        var current = new int[spoken.Length + 1];
        for (var i = 1; i <= expected.Length; i++)
        {
            Array.Clear(current);
            for (var j = 1; j <= spoken.Length; j++)
            {
                current[j] = expected[i - 1] == spoken[j - 1]
                    ? previous[j - 1] + 1
                    : Math.Max(previous[j], current[j - 1]);
            }
            (previous, current) = (current, previous);
        }
        return previous[spoken.Length];
    }

    private static string[] Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }
        return Regex.Split(builder.ToString(), @"\s+")
            .Where(token => token.Length > 0)
            .ToArray();
    }
}
