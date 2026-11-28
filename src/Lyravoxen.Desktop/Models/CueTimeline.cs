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