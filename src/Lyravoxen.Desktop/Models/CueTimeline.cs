namespace Lyravoxen.Models;

public static class CueTimeline
{
    public static List<LyricCue> Normalize(IEnumerable<LyricCue>? cues) =>
        (cues ?? Enumerable.Empty<LyricCue>())
            .Where(cue => cue is not null && double.IsFinite(cue.Seconds) && cue.Seconds >= 0)
            .OrderBy(cue => cue.Seconds)
            .Select(cue => new LyricCue
            {
                Seconds = cue.Seconds,
                Text = cue.Text ?? "",
                SectionId = cue.SectionId ?? "",
                OccurrenceIndex = cue.OccurrenceIndex,
                SourceLineIndex = cue.SourceLineIndex,
                Confidence = cue.Confidence,
                NeedsReview = cue.NeedsReview,
                IsManual = cue.IsManual
            })
            .ToList();

    // Stateless upper-bound search: seeking backward immediately selects the earlier phrase.
    public static int FindActiveIndex(IReadOnlyList<LyricCue> sortedCues, double playbackSeconds, double leadSeconds)
    {
        if (sortedCues.Count == 0 || !double.IsFinite(playbackSeconds)) return -1;
        var target = Math.Max(0, playbackSeconds) + Math.Max(0, leadSeconds);
        var low = 0;
        var high = sortedCues.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (sortedCues[middle].Seconds <= target) low = middle + 1;
            else high = middle;
        }
        return low - 1;
    }
    public static double ClampForwardOffset(IReadOnlyList<double?> executionTimes, int anchorIndex, double requestedDelta, double minimumGapSeconds = 0.05)
    {
        if (anchorIndex < 0 || anchorIndex >= executionTimes.Count || !double.IsFinite(requestedDelta)) return 0;
        if (!executionTimes[anchorIndex].HasValue) return requestedDelta;
        double? previous = null;
        for (var i = anchorIndex - 1; i >= 0; i--)
            if (executionTimes[i].HasValue) { previous = executionTimes[i]; break; }
        var minimum = previous.HasValue ? previous.Value + Math.Max(0, minimumGapSeconds) : 0;
        return Math.Max(requestedDelta, minimum - executionTimes[anchorIndex]!.Value);
    }

    /// <summary>
    /// Ajusta o intervalo entre os tempos a partir de uma frase âncora.
    /// Fator abaixo de 1 acelera a sequência; acima de 1 desacelera.
    /// </summary>
    public static double?[] ScaleAfterAnchor(
        IReadOnlyList<double?> executionTimes,
        int anchorIndex,
        double rateMultiplier,
        double minimumGapSeconds = 0.05)
    {
        var result = executionTimes.ToArray();
        if (anchorIndex < 0 || anchorIndex >= result.Length ||
            !result[anchorIndex].HasValue ||
            !double.IsFinite(rateMultiplier) || rateMultiplier <= 0)
            return result;

        var anchorSeconds = result[anchorIndex]!.Value;
        var previousSeconds = anchorSeconds;
        var minimumGap = Math.Max(0, minimumGapSeconds);
        for (var i = anchorIndex + 1; i < result.Length; i++)
        {
            if (!result[i].HasValue || !double.IsFinite(result[i]!.Value))
                continue;

            var scaledSeconds = anchorSeconds +
                (result[i]!.Value - anchorSeconds) * rateMultiplier;
            result[i] = Math.Max(scaledSeconds, previousSeconds + minimumGap);
            previousSeconds = result[i]!.Value;
        }
        return result;
    }

    /// <summary>
    /// Cria uma prévia não destrutiva do deslocamento de uma frase e das frases marcadas seguintes.
    /// O resultado nunca altera a lista recebida e respeita o espaço mínimo em relação à frase anterior.
    /// </summary>
    public static double?[] PreviewShiftAfterAnchor(
        IReadOnlyList<double?> executionTimes,
        int anchorIndex,
        double requestedAnchorSeconds,
        double minimumGapSeconds = 0.05)
    {
        var preview = executionTimes.ToArray();
        if (anchorIndex < 0 || anchorIndex >= preview.Length ||
            !preview[anchorIndex].HasValue ||
            !double.IsFinite(requestedAnchorSeconds))
            return preview;

        var delta = ClampForwardOffset(
            executionTimes, anchorIndex,
            requestedAnchorSeconds - executionTimes[anchorIndex]!.Value,
            minimumGapSeconds);

        for (var i = anchorIndex; i < preview.Length; i++)
            if (preview[i].HasValue)
                preview[i] = Math.Max(0, preview[i]!.Value + delta);

        return preview;
    }

    // Build a rolling page from consecutive visual lyric lines.
    public static string BuildProjectionBlock(IReadOnlyList<LyricCue> cues, int activeCueIndex, int lineCount)
    {
        if (cues.Count == 0 || activeCueIndex < 0 || activeCueIndex >= cues.Count)
            return string.Empty;

        var maxLines = Math.Clamp(lineCount, 1, 10);
        static string[] Lines(string? text) => (text ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();

        // Cues created by the new registration editor carry a section/occurrence key.
        // Never let a projection page bleed into the previous or next stanza/chorus.
        var active = cues[activeCueIndex];
        if (!string.IsNullOrWhiteSpace(active.SectionId))
        {
            var first = activeCueIndex;
            while (first > 0 &&
                   cues[first - 1].SectionId == active.SectionId &&
                   cues[first - 1].OccurrenceIndex == active.OccurrenceIndex)
                first--;
            var last = activeCueIndex + 1;
            while (last < cues.Count &&
                   cues[last].SectionId == active.SectionId &&
                   cues[last].OccurrenceIndex == active.OccurrenceIndex)
                last++;
            var sectionLines = cues.Skip(first).Take(last - first).SelectMany(cue => Lines(cue.Text)).ToArray();
            var linesBeforeActive = cues.Skip(first).Take(activeCueIndex - first).Sum(cue => Lines(cue.Text).Length);
            var sectionBlockStart = (linesBeforeActive / maxLines) * maxLines;
            return string.Join(Environment.NewLine, sectionLines.Skip(sectionBlockStart).Take(maxLines));
        }

        var allLines = cues.SelectMany(cue => Lines(cue.Text)).ToArray();
        var linesBeforeActiveCue = 0;
        for (var i = 0; i < activeCueIndex; i++)
            linesBeforeActiveCue += Lines(cues[i].Text).Length;

        var blockStart = (linesBeforeActiveCue / maxLines) * maxLines;
        return string.Join(Environment.NewLine, allLines.Skip(blockStart).Take(maxLines));
    }

}