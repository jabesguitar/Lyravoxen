namespace Lyravoxen.Models;

public sealed class LyricCue
{
    public double Seconds { get; set; }
    public string Text { get; set; } = "";

    // Optional origin metadata. Legacy cues remain valid when these fields are empty.
    public string SectionId { get; set; } = "";
    public int OccurrenceIndex { get; set; }
    public int SourceLineIndex { get; set; } = -1;
    public double? Confidence { get; set; }
    public bool NeedsReview { get; set; }
    public bool IsManual { get; set; }

    public override string ToString() =>
        $"{TimeSpan.FromSeconds(Seconds):mm\\:ss}   {Text}";
}


/// <summary>Correção manual aplicada a partir de um cartão e herdada pelas frases seguintes.</summary>
public sealed class LyricsTimelineAdjustment
{
    public string SectionId { get; set; } = "";
    public int OccurrenceIndex { get; set; }
    public int SourceLineIndex { get; set; } = -1;
    public int AnchorOrderIndex { get; set; }
    public double DeltaSeconds { get; set; }
}
