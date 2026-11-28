using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

public enum LyricsSectionKind
{
    Unknown,
    Verse,
    Chorus,
    Bridge,
    Intro,
    Outro
}

public sealed class LyricsSectionBlock
{
    public string SectionId { get; init; } = "";
    public string Label { get; init; } = "";
    public LyricsSectionKind Kind { get; init; }
    public int SourceIndex { get; init; }
    public List<string> Lines { get; init; } = new();
    public int RepeatCount { get; init; } = 1;
    public int OccurrenceCount { get; set; } = 1;
    public bool ExplicitlyLabeled { get; init; }
    public bool NeedsReview { get; set; }
}

public sealed class LyricsExecutionOccurrence
{
    public string SectionId { get; init; } = "";
    public string Label { get; init; } = "";
    public LyricsSectionKind Kind { get; init; }
    public int OccurrenceIndex { get; init; }
    public int SourceIndex { get; init; }
    // Starting source-line index for each projection slide, in the user's saved order.
    public List<int> LineOrder { get; init; } = new();
}

public sealed class LyricsStructureAnalysis
{
    public List<LyricsSectionBlock> Sections { get; init; } = new();
    public List<LyricsExecutionOccurrence> ExecutionOrder { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Fast, deterministic first-pass parser for pasted lyrics.
/// It preserves source blocks and builds a separate execution list so repeated choruses
/// can point to one source block without duplicating the lyric text.
/// Audio-based section detection is intentionally a separate later stage.
/// </summary>
public static class LyricsStructureAnalyzer
{
    private sealed record ParsedBlock(List<string> Lines, LyricsSectionKind Kind, string Label,
        bool Explicit, int RepeatCount);

    private static readonly Regex RepeatSuffix = new(@"(?:\s*\(?\s*(?<a>\d+)\s*x\s*\)?|\s*x\s*(?<b>\d+))\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static LyricsStructureAnalysis Analyze(string? lyrics)
    {
        var result = new LyricsStructureAnalysis();
        if (string.IsNullOrWhiteSpace(lyrics)) return result;

        var parsed = ParseBlocks(lyrics);
        if (parsed.Count == 0) return result;

        var fingerprints = parsed.Select(block => Fingerprint(block.Lines)).ToList();
        var counts = fingerprints.GroupBy(value => value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < parsed.Count; i++)
        {
            var block = parsed[i];
            var fingerprint = fingerprints[i];
            var repeated = counts[fingerprint] > 1;
            var kind = block.Kind;
            var label = block.Label;

            if (!block.Explicit && repeated)
            {
                kind = LyricsSectionKind.Chorus;
                label = "Coro / Refrão";
            }
            else if (!block.Explicit && kind == LyricsSectionKind.Unknown)
            {
                label = "Parte " + (i + 1);
            }

            var identityKey = (int)kind + "|" + fingerprint;
            if (!ids.TryGetValue(identityKey, out var sectionId))
            {
                sectionId = CreateStableId(identityKey);
                ids[identityKey] = sectionId;
                result.Sections.Add(new LyricsSectionBlock
                {
                    SectionId = sectionId,
                    Label = label,
                    Kind = kind,
                    SourceIndex = i,
                    Lines = new List<string>(block.Lines),
                    RepeatCount = block.RepeatCount,
                    OccurrenceCount = 0,
                    ExplicitlyLabeled = block.Explicit,
                    NeedsReview = !block.Explicit && !repeated
                });
            }

            var section = result.Sections.First(item => item.SectionId == sectionId);
            section.OccurrenceCount += block.RepeatCount;
            if (section.NeedsReview && (block.Explicit || repeated))
                section.NeedsReview = false;

            var nextOccurrence = occurrences.TryGetValue(sectionId, out var seen) ? seen : 0;
            for (var repeat = 0; repeat < block.RepeatCount; repeat++)
            {
                result.ExecutionOrder.Add(new LyricsExecutionOccurrence
                {
                    SectionId = sectionId,
                    Label = section.Label,
                    Kind = section.Kind,
                    OccurrenceIndex = nextOccurrence + repeat + 1,
                    SourceIndex = i,
                    LineOrder = new List<int>()
                });
            }
            occurrences[sectionId] = nextOccurrence + block.RepeatCount;
        }

        if (result.Sections.Any(section => section.NeedsReview))
            result.Warnings.Add("Há partes sem rótulo explícito e sem repetição textual. A classificação como estrofe, ponte ou outra seção precisa de confirmação ou análise do áudio.");
        if (result.Sections.Any(section => section.Kind == LyricsSectionKind.Chorus))
            result.Warnings.Add("Coro identificado por rótulo ou repetição textual. Confirme as entradas na gravação: o mesmo coro pode ocorrer em vários momentos.");
        return result;
    }

    private static List<ParsedBlock> ParseBlocks(string lyrics)
    {
        var blocks = new List<ParsedBlock>();
        var currentLines = new List<string>();
        var currentKind = LyricsSectionKind.Unknown;
        var currentLabel = "";
        var currentExplicit = false;
        var currentRepeat = 1;

        void Flush()
        {
            if (currentLines.Count == 0) return;
            blocks.Add(new ParsedBlock(new List<string>(currentLines), currentKind, currentLabel,
                currentExplicit, currentRepeat));
            currentLines.Clear();
            currentKind = LyricsSectionKind.Unknown;
            currentLabel = "";
            currentExplicit = false;
            currentRepeat = 1;
        }

        var normalized = lyrics.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var raw in normalized.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                Flush();
                continue;
            }

            var marker = ParseHeader(line);
            if (marker is not null)
            {
                Flush();
                currentKind = marker.Value.Kind;
                currentLabel = marker.Value.Label;
                currentExplicit = true;
                currentRepeat = marker.Value.RepeatCount;
                continue;
            }

            currentLines.Add(line);
        }
        Flush();
        return blocks;
    }

    private static (LyricsSectionKind Kind, string Label, int RepeatCount)? ParseHeader(string line)
    {
        var candidate = line.Trim();
        if (candidate.StartsWith("[", StringComparison.Ordinal) &&
            candidate.EndsWith("]", StringComparison.Ordinal))
            candidate = candidate[1..^1].Trim();
        else if (candidate.EndsWith(":", StringComparison.Ordinal))
            candidate = candidate[..^1].Trim();

        var repeat = 1;
        var repeatMatch = RepeatSuffix.Match(candidate);
        if (repeatMatch.Success)
        {
            var value = repeatMatch.Groups["a"].Success ? repeatMatch.Groups["a"].Value : repeatMatch.Groups["b"].Value;
            if (int.TryParse(value, out var parsedRepeat))
            {
                repeat = Math.Clamp(parsedRepeat, 1, 8);
                candidate = candidate[..repeatMatch.Index].Trim();
            }
        }

        var key = Normalize(candidate);
        if (key.Length == 0) return null;

        if (key is "coro" or "refrao" or "refrao final" or "chorus" or "refrain")
            return (LyricsSectionKind.Chorus, "Coro / Refrão", repeat);
        if (key.StartsWith("estrofe") || key.StartsWith("verso") || key.StartsWith("verse"))
            return (LyricsSectionKind.Verse, candidate, repeat);
        if (key is "ponte" or "bridge")
            return (LyricsSectionKind.Bridge, "Ponte", repeat);
        if (key is "intro" or "introducao")
            return (LyricsSectionKind.Intro, "Introdução", repeat);
        if (key is "outro" or "final" or "encerramento")
            return (LyricsSectionKind.Outro, "Final", repeat);

        return null;
    }

    private static string Fingerprint(IEnumerable<string> lines) =>
        string.Join(" ", lines.Select(Normalize).Where(line => line.Length > 0));

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static string CreateStableId(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
