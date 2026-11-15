namespace Lyravoxen.Models;

public static class CueTimeline
{
    public static List<LyricCue> Normalize(IEnumerable<LyricCue>? cues) =>
        (cues ?? Enumerable.Empty<LyricCue>())
            .Where(cue => cue is not null && double.IsFinite(cue.Seconds) && cue.Seconds >= 0)
            .OrderBy(cue => cue.Seconds)
            .Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text ?? "" })
            .ToList();

    // Fixed-size projection blocks: lines accumulate within the current block,
    // and the entire previous block is replaced when the next block begins.
    public static string BuildProjectionBlock(IReadOnlyList<LyricCue> cues, int activeIndex, int lineCount)
    {
        if (cues is null || activeIndex < 0 || activeIndex >= cues.Count) return "";
        var count = Math.Clamp(lineCount, 1, 10);
        var firstIndex = (activeIndex / count) * count;
        return string.Join(Environment.NewLine, cues.Skip(firstIndex).Take(activeIndex - firstIndex + 1)
            .Select(cue => cue.Text?.Trim() ?? "")
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

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
}