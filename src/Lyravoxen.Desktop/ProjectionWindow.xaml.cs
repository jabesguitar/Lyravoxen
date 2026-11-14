using System.Windows;
using Forms = System.Windows.Forms;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Lyravoxen;

public partial class ProjectionWindow : Window
{
    public ProjectionWindow() => InitializeComponent();

    public bool ShowOnOutputDisplay()
    {
        var externalOutput = Forms.Screen.AllScreens.FirstOrDefault(screen => !screen.Primary);
        var output = externalOutput ?? Forms.Screen.PrimaryScreen;

        if (output is not null)
        {
            Left = output.Bounds.Left;
            Top = output.Bounds.Top;
            Width = output.Bounds.Width;
            Height = output.Bounds.Height;
        }

        Show();
        WindowState = WindowState.Maximized;
        return externalOutput is not null;
    }

    private const double BaseFontSize = 48;
    private string _themeName = "Noturno";
    private int _maxLines = 4;
    private string _lastLyric = "";
    private bool _showingOverlay;

    public void SetTheme(string theme)
    {
        _themeName = theme;
        var background = theme switch
        {
            "Clássico" => System.Windows.Media.Color.FromRgb(0, 0, 0),
            "Âmbar" => System.Windows.Media.Color.FromRgb(24, 17, 10),
            _ => System.Windows.Media.Color.FromRgb(8, 13, 18)
        };
        var foreground = theme == "Âmbar"
            ? System.Windows.Media.Color.FromRgb(255, 221, 160)
            : System.Windows.Media.Colors.White;
        Background = new System.Windows.Media.SolidColorBrush(background);
        LyricDisplay.Foreground = new System.Windows.Media.SolidColorBrush(foreground);
        OverlayTitle.Foreground = new System.Windows.Media.SolidColorBrush(
            theme == "Âmbar" ? System.Windows.Media.Color.FromRgb(255, 184, 77) : System.Windows.Media.Color.FromRgb(38, 198, 184));
        OverlaySubtitle.Foreground = new System.Windows.Media.SolidColorBrush(foreground);
    }

    public void SetMaxLines(int count)
    {
        _maxLines = Math.Clamp(count, 3, 5);
        UpdateTextLayout(LyricDisplay.Text);
    }

    public void ShowLyric(string text)
    {
        _lastLyric = text ?? "";
        _showingOverlay = false;
        LyricDisplay.Visibility = Visibility.Visible;
        OverlayPanel.Visibility = Visibility.Collapsed;
        LyricDisplay.Text = LimitLines(_lastLyric, _maxLines);
        UpdateTextLayout(LyricDisplay.Text);
    }

    public void ShowStandby()
    {
        _showingOverlay = true;
        LyricDisplay.Visibility = Visibility.Collapsed;
        OverlayTitle.Text = "LYRAVOXEN";
        OverlaySubtitle.Text = "A apresentação começará em instantes";
        OverlayPanel.Visibility = Visibility.Visible;
    }

    public void ShowEnding()
    {
        _showingOverlay = true;
        LyricDisplay.Visibility = Visibility.Collapsed;
        OverlayTitle.Text = "Obrigado pela presença";
        OverlaySubtitle.Text = "Que Deus abençoe a todos";
        OverlayPanel.Visibility = Visibility.Visible;
    }

    // Tela limpa sem fechar a saída nem perder a frase atual.
    public void ClearDisplay()
    {
        LyricDisplay.Text = "";
        OverlayPanel.Visibility = Visibility.Collapsed;
        LyricDisplay.Visibility = Visibility.Visible;
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
