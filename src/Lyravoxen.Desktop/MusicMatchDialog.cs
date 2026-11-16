namespace Lyravoxen;

public enum MusicMatchChoice
{
    None,
    Yes,
    No,
    Create
}

public sealed class MusicMatchDialog : System.Windows.Window
{
    public MusicMatchChoice Choice { get; private set; }

    public MusicMatchDialog(string heading, string details)
    {
        Title = "Confirmar música — Lyravoxen";
        Width = 560;
        Height = 470;
        MinWidth = 480;
        MinHeight = 400;
        ResizeMode = System.Windows.ResizeMode.CanResize;
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 29, 40));
        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 245, 248));

        var panel = new System.Windows.Controls.DockPanel { Margin = new System.Windows.Thickness(22) };
        var headingText = new System.Windows.Controls.TextBlock
        {
            Text = heading,
            FontSize = 20,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 198, 184)),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            Margin = new System.Windows.Thickness(0, 0, 0, 14)
        };
        System.Windows.Controls.DockPanel.SetDock(headingText, System.Windows.Controls.Dock.Top);
        panel.Children.Add(headingText);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 16, 0, 0)
        };
        System.Windows.Controls.DockPanel.SetDock(buttons, System.Windows.Controls.Dock.Bottom);
        buttons.Children.Add(CreateButton("Sim", MusicMatchChoice.Yes, true));
        buttons.Children.Add(CreateButton("Não", MusicMatchChoice.No));
        buttons.Children.Add(CreateButton("Criar", MusicMatchChoice.Create));
        panel.Children.Add(buttons);

        var detailText = new System.Windows.Controls.TextBox
        {
            Text = details,
            IsReadOnly = true,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 19, 27)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 236, 241)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(53, 80, 100)),
            Padding = new System.Windows.Thickness(12),
            FontSize = 13
        };
        panel.Children.Add(detailText);
        Content = panel;
    }

    private System.Windows.Controls.Button CreateButton(string label, MusicMatchChoice choice, bool isDefault = false)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = label,
            MinWidth = 92,
            Padding = new System.Windows.Thickness(18, 9, 18, 9),
            Margin = new System.Windows.Thickness(6, 0, 0, 0),
            FontSize = 13,
            FontWeight = System.Windows.FontWeights.SemiBold,
            Background = new System.Windows.Media.SolidColorBrush(choice switch
            {
                MusicMatchChoice.Yes => System.Windows.Media.Color.FromRgb(23, 110, 104),
                MusicMatchChoice.No => System.Windows.Media.Color.FromRgb(53, 75, 91),
                _ => System.Windows.Media.Color.FromRgb(49, 90, 131)
            }),
            Foreground = System.Windows.Media.Brushes.White,
            IsDefault = isDefault
        };
        button.Click += (_, _) =>
        {
            Choice = choice;
            DialogResult = true;
        };
        return button;
    }
}
