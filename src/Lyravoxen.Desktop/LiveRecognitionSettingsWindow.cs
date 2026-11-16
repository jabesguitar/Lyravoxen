using System.Windows;
using System.Windows.Controls;
using Lyravoxen.Services;

namespace Lyravoxen;

public sealed class LiveRecognitionSettingsWindow : Window
{
    private readonly PasswordBox _apiKey = new() { Margin = new Thickness(0, 6, 0, 12), MinWidth = 360 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };

    public LiveRecognitionSettingsWindow()
    {
        Title = "Reconhecimento automático de letras";
        Width = 520;
        Height = 300;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = System.Windows.Media.Brushes.White;
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = "Conectar reconhecimento de voz", FontSize = 19, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });
        panel.Children.Add(new TextBlock { Text = "Chave da API OpenAI (não é a senha do ChatGPT)" });
        panel.Children.Add(_apiKey);
        panel.Children.Add(new TextBlock
        {
            Text = "A chave será protegida no Windows para este usuário. O áudio que sai do computador será enviado à API durante a sincronização. O uso é cobrado separadamente da assinatura ChatGPT.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.DimGray
        });
        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var save = new System.Windows.Controls.Button { Content = "Salvar chave", Padding = new Thickness(14, 7, 14, 7), IsDefault = true };
        save.Click += (_, _) =>
        {
            try
            {
                LiveRecognitionSettings.SaveApiKey(_apiKey.Password);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        };
        var cancel = new System.Windows.Controls.Button { Content = "Cancelar", Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        panel.Children.Add(_status);
        Content = panel;
    }
}
