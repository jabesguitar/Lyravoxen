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

    private const double BaseFontSize = 48;
    private int _maxLines = 4;
    private string _lastLyric = "";

    public void SetMaxLines(int count)
    {
        _maxLines = Math.Clamp(count, 3, 5);
        UpdateTextLayout(LyricDisplay.Text);
    }

    public void ShowLyric(string text)
    {
        _lastLyric = text ?? "";
        LyricDisplay.Text = LimitLines(_lastLyric, _maxLines);
        UpdateTextLayout(LyricDisplay.Text);
    }

    // Tela limpa sem fechar a saída nem perder a frase atual.
    public void ClearDisplay()
    {
        LyricDisplay.Text = "";
        UpdateTextLayout("");
    }

    public void RestoreDisplay() => ShowLyric(_lastLyric);

    private void UpdateTextLayout(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            LyricDisplay.FontSize = BaseFontSize;
            return;
        }

        // O tamanho da fonte é ajustado sem alterar propriedades de altura do TextBlock.
        var visualLines = text.Split('\n').Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / 38d)));
        var scale = Math.Min(1d, Math.Sqrt(_maxLines / (double)Math.Max(_maxLines, visualLines)));
        LyricDisplay.FontSize = Math.Clamp(BaseFontSize * scale, 20, BaseFontSize);
    }
    private static string LimitLines(string text, int maxLines)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>(); var current = "";
        var index = 0;
        for (; index < words.Length; index++)
        {
            var word = words[index];
            var candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
            if (candidate.Length > 38 && current.Length > 0)
            {
                lines.Add(current);
                current = word;
                if (lines.Count >= maxLines - 1)
                {
                    index++;
                    break;
                }
            }
            else current = candidate;
        }

        if (index < words.Length)
        {
            // Nunca descartar o restante da frase: textos maiores podem ocupar mais linhas visuais.
            current = string.Join(" ", new[] { current }.Concat(words.Skip(index)).Where(part => !string.IsNullOrWhiteSpace(part)));
        }
        if (current.Length > 0) lines.Add(current);
        return string.Join(Environment.NewLine, lines);
    }

    protected override void OnKeyDown(WpfKeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            ClearDisplay();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
