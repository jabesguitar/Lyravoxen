using System.Windows;
using System.Windows.Controls;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfButton = System.Windows.Controls.Button;
using Lyravoxen.Integration;

namespace Lyravoxen;

public sealed class HolyricsSettingsWindow : Window
{
    private readonly HolyricsClient _client;
    private readonly WpfTextBox _host = new() { Text = "127.0.0.1", Margin = new Thickness(0, 4, 0, 10) };
    private readonly WpfTextBox _port = new() { Text = "8091", Margin = new Thickness(0, 4, 0, 10) };
    private readonly PasswordBox _token = new() { Margin = new Thickness(0, 4, 0, 12) };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), MinHeight = 42 };
    public bool Saved { get; private set; }

    public HolyricsSettingsWindow(HolyricsClient client)
    {
        _client = client;
        Title = "Conectar ao Holyrics";
        Width = 440; Height = 390; MinHeight = 390; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = System.Windows.Media.Brushes.White;
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = "API Server local do Holyrics", FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,12) });
        panel.Children.Add(new TextBlock { Text = "Endereço (IP ou 127.0.0.1)" }); panel.Children.Add(_host);
        panel.Children.Add(new TextBlock { Text = "Porta" }); panel.Children.Add(_port);
        panel.Children.Add(new TextBlock { Text = "Token (fica protegido neste computador)" }); panel.Children.Add(_token);
        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var test = new WpfButton { Content = "Testar e salvar", Padding = new Thickness(14,7,14,7), IsDefault = true };
        test.Click += async (_, _) => await TestAndSaveAsync(test);
        var cancel = new WpfButton { Content = "Cancelar", Padding = new Thickness(14,7,14,7), Margin = new Thickness(8,0,0,0), IsCancel = true };
        buttons.Children.Add(test); buttons.Children.Add(cancel); panel.Children.Add(buttons); panel.Children.Add(_status);
        Content = panel;
    }

    private async Task TestAndSaveAsync(WpfButton testButton)
    {
        if (!int.TryParse(_port.Text, out var port))
        {
            System.Windows.MessageBox.Show(this, "Informe uma porta válida, por exemplo 8091.", "Lyravoxen — Holyrics", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        testButton.IsEnabled = false;
        _status.Text = "Testando a comunicação com o Holyrics...";
        try
        {
            await _client.ConfigureAndTestAsync(_host.Text, port, _token.Password);
            Saved = true;
            System.Windows.MessageBox.Show(this, "Conexão confirmada e configuração salva neste computador.", "Lyravoxen — Holyrics", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            _status.Text = "Falha no teste de conexão.";
            System.Windows.MessageBox.Show(this,
                "Não foi possível confirmar a conexão com o Holyrics.\n\nDetalhes: " + ex.Message +
                "\n\nConfira se o API Server Local está ativado, se a porta é 8091 e se o token permite ShowQuickPresentation, CloseCurrentQuickPresentation e CloseCurrentPresentation em Local.",
                "Lyravoxen — Erro de conexão", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { testButton.IsEnabled = true; }
    }
}
