namespace Lyravoxen.Models;

public sealed class LyricCue
{
    public double Seconds { get; set; }
    public string Text { get; set; } = "";

    public override string ToString() =>
        $"{TimeSpan.FromSeconds(Seconds):mm\\:ss}   {Text}";
}
