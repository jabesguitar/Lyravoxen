namespace Lyravoxen.Models;

public static class CueTimeline
{
    public static List<LyricCue> Normalize(IEnumerable<LyricCue>? cues) =>
        (cues ?? Enumerable.Empty<LyricCue>())
            .Where(cue => cue is not null && double.IsFinite(cue.Seconds) && cue.Seconds >= 0)
            .OrderBy(cue => cue.Seconds)
            .Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text ?? "" })
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
    // Build a rolling projection page from consecutive lyric lines.
    // The active cue determines which fixed-size block is visible: 1-4, 5-8, etc.
    public static string BuildProjectionBlock(IReadOnlyList<LyricCue> cues, int activeCueIndex, int lineCount)
    {
        if (cues.Count == 0 || activeCueIndex < 0 || activeCueIndex >= cues.Count)
            return string.Empty;

        var maxLines = Math.Clamp(lineCount, 1, 10);
        static string[] Lines(string? text) => (text ?? string.Empty)
            .Split(new[] { "\\r\\n", "\\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();

        var linesBefore = 0;
        for (var i = 0; i < activeCueIndex; i++)
            linesBefore += Lines(cues[i].Text).Length;

        var blockStart = (linesBefore / maxLines) * maxLines;
        return cues.SelectMany(cue => Lines(cue.Text))
            .Skip(blockStart)
            .Take(maxLines)
            .Aggregate(new List<string>(), (result, line) => { result.Add(line); return result; })
            .Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, cues.SelectMany(cue => Lines(cue.Text)).Skip(blockStart).Take(maxLines));
    }


}