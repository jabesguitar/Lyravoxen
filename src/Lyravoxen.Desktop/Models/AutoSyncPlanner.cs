namespace Lyravoxen.Models;

/// <summary>
/// Turns one recognised lyric line into the timed cues that must be saved.
/// When the recogniser jumps ahead, the skipped lines are kept (with estimated times)
/// instead of silently disappearing from the synchronised lyric.
/// </summary>
public static class AutoSyncPlanner
{
    private const double SecondsPerSkippedLine = 3.0;
    private const double MinimumStep = 0.05;

    // Without a previous anchor, only a couple of skipped lines can be placed safely
    // (the recogniser simply started mid-line). More than that means the song was
    // already playing when recognition began, so those lines have no known time.
    public const int MaxLeadingLinesWithoutAnchor = 2;

    public static List<LyricCue> CreateCuesForMatch(
        IReadOnlyList<string> lines,
        int nextIndex,
        int matchedIndex,
        double? lastCueSeconds,
        double recognizedSeconds)
    {
        var result = new List<LyricCue>();
        if (lines is null || nextIndex < 0 || matchedIndex < nextIndex || matchedIndex >= lines.Count)
            return result;

        var matchedSeconds = double.IsFinite(recognizedSeconds) ? Math.Max(0, recognizedSeconds) : 0;
        if (lastCueSeconds.HasValue)
            matchedSeconds = Math.Max(matchedSeconds, lastCueSeconds.Value + MinimumStep);

        var skipped = matchedIndex - nextIndex;
        if (skipped > 0 && (lastCueSeconds.HasValue || skipped <= MaxLeadingLinesWithoutAnchor))
        {
            var from = lastCueSeconds ?? Math.Max(0, matchedSeconds - SecondsPerSkippedLine * skipped);
            for (var k = 0; k < skipped; k++)
            {
                var seconds = from + (matchedSeconds - from) * (k + 1) / (skipped + 1);
                result.Add(new LyricCue { Seconds = seconds, Text = lines[nextIndex + k] });
            }
        }

        result.Add(new LyricCue { Seconds = matchedSeconds, Text = lines[matchedIndex] });
        return result;
    }
}
