using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

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

    public void ShowLyric(string text) => LyricDisplay.Text = text;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            WindowState = WindowState.Normal;
            Close();
        }
        base.OnKeyDown(e);
    }
}
