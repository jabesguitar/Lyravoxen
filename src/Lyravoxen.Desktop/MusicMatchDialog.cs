using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        buttons.Children.Add(CreateButton("Sim", MusicMatchChoice.Yes, "#176E68", true));
        buttons.Children.Add(CreateButton("Não", MusicMatchChoice.No, "#354B5B"));
        buttons.Children.Add(CreateButton("Criar", MusicMatchChoice.Create, "#315A83"));
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

    private Button CreateButton(string label, MusicMatchChoice choice, string background, bool isDefault = false)
    {
        var button = new Button
        {
            Content = label,
            MinWidth = 92,
            Padding = new Thickness(18, 9, 18, 9),
            Margin = new Thickness(6, 0, 0, 0),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background)),
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
