using System.Windows;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Lyravoxen;

public partial class ProjectionWindow : Window
{
    public ProjectionWindow() => InitializeComponent();

    public void ShowOnOutputDisplay()
    {
        var output = Forms.Screen.AllScreens.FirstOrDefault(screen => !screen.Primary)
                     ?? Forms.Screen.PrimaryScreen;

        if (output is not null)
        {
            Left = output.Bounds.Left;
            Top = output.Bounds.Top;
            Width = output.Bounds.Width;
            Height = output.Bounds.Height;
        }

        Show();
        WindowState = WindowState.Maximized;
    }

    private int _maxLines = 4;
    public void SetMaxLines(int count) { _maxLines = Math.Clamp(count, 3, 5); LyricDisplay.MaxHeight = _maxLines * LyricDisplay.FontSize * 1.3; }
    public void ShowLyric(string text) { LyricDisplay.Text = LimitLines(text, _maxLines); }
    private static string LimitLines(string text, int maxLines)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>(); var current = "";
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
            if (candidate.Length > 38 && current.Length > 0) { lines.Add(current); current = word; if (lines.Count >= maxLines) break; }
            else current = candidate;
        }
        if (lines.Count < maxLines && current.Length > 0) lines.Add(current);
        return string.Join(Environment.NewLine, lines);
    }

    protected override void OnKeyDown(WpfKeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            WindowState = WindowState.Normal;
            Close();
        }
        base.OnKeyDown(e);
    }
}
