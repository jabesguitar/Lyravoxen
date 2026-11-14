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

    public void ShowLyric(string text) => LyricDisplay.Text = text;

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
