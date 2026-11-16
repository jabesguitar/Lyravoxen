using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;

namespace Lyravoxen;

public enum MusicMatchChoice
{
    None,
    Yes,
    No,
    Create
}

public sealed class MusicMatchDialog : Window
{
    public MusicMatchChoice Choice { get; private set; }

    public MusicMatchDialog(string heading, string details)
    {
        Title = "Confirmar música — Lyravoxen";
        Width = 560;
        Height = 470;
        MinWidth = 480;
        MinHeight = 400;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(16, 29, 40));
        Foreground = new SolidColorBrush(Color.FromRgb(238, 245, 248));

        var panel = new DockPanel { Margin = new Thickness(22) };
        var headingText = new TextBlock
        {
            Text = heading,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(38, 198, 184)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };
        DockPanel.SetDock(headingText, Dock.Top);
        panel.Children.Add(headingText);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        buttons.Children.Add(CreateButton("Sim", MusicMatchChoice.Yes, true));
        buttons.Children.Add(CreateButton("Não", MusicMatchChoice.No));
        buttons.Children.Add(CreateButton("Criar", MusicMatchChoice.Create));
        panel.Children.Add(buttons);

        var detailText = new TextBox
        {
            Text = details,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(10, 19, 27)),
            Foreground = new SolidColorBrush(Color.FromRgb(226, 236, 241)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(53, 80, 100)),
            Padding = new Thickness(12),
            FontSize = 13
        };
        panel.Children.Add(detailText);
        Content = panel;
    }

    private WpfButton CreateButton(string label, MusicMatchChoice choice, bool isDefault = false)
    {
        var button = new WpfButton
        {
            Content = label,
            MinWidth = 92,
            Padding = new Thickness(18, 9, 18, 9),
            Margin = new Thickness(6, 0, 0, 0),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(choice switch
            {
                MusicMatchChoice.Yes => Color.FromRgb(23, 110, 104),
                MusicMatchChoice.No => Color.FromRgb(53, 75, 91),
                _ => Color.FromRgb(49, 90, 131)
            }),
            Foreground = Brushes.White,
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
