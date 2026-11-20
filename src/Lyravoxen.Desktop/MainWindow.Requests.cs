using System.Windows;
using Lyravoxen.Services;

namespace Lyravoxen;

// 📥 Pedidos: músicas enviadas pelas pessoas pela página de pedidos (/pedido) ficam numa lista de espera.
public partial class MainWindow
{
    private List<SongRequest> _requests = new();
    private Window? _requestsWindow;
    private System.Windows.Controls.StackPanel? _requestsPanel;

    private void InitializeRequests()
    {
        _requests = SongRequestStore.Load();
        Loaded += (_, _) => UpdateRequestsButton();
    }

    private void UpdateRequestsButton() => RequestsButton.Content = "📥 Pedidos (" + _requests.Count + ")";

    private Task<(bool Ok, string Message)> HandleSongRequestAsync(string json, string ip)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            var (added, ok, message) = SongRequestStore.Parse(json, _requests, DateTime.Now);
            if (ok)
            {
                _requests.AddRange(added);
                SongRequestStore.Save(_requests);
                UpdateRequestsButton();
                var who = string.IsNullOrWhiteSpace(added[0].Group) ? "alguém" : added[0].Group;
                StatusText.Text = "📥 Novo pedido de " + who + ": " + added.Count + (added.Count == 1 ? " música." : " músicas.");
                AppLog.Info("Pedido recebido de " + who + " (" + ip + "): " + added.Count);
                RefreshRequestsWindow();
            }
            return (ok, message);
        }).Task;
    }

    private void Requests_Click(object sender, RoutedEventArgs e)
    {
        if (_requestsWindow is not null)
        {
            _requestsWindow.Activate();
            RefreshRequestsWindow();
            return;
        }

        _requestsPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(14) };
        var window = new Window
        {
            Title = "Lyravoxen — Pedidos de músicas",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 600,
            Height = 640,
            Background = Hex("#F5F7FA"),
            Content = new System.Windows.Controls.ScrollViewer
            {
                Content = _requestsPanel,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
            }
        };
        window.Closed += (_, _) => { _requestsWindow = null; _requestsPanel = null; };
        _requestsWindow = window;
        RefreshRequestsWindow();
        window.Show();
    }

    private System.Windows.Controls.TextBlock RequestText(string value, double size, bool bold = false, string color = "#1A2430") => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Hex(color),
        Margin = new Thickness(0, 0, 0, 4)
    };

    private System.Windows.Controls.Button RequestButton(string label, Action action)
    {
        var button = new System.Windows.Controls.Button { Content = label, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 4, 6, 0) };
        button.Click += (_, _) => action();
        return button;
    }

    private void RefreshRequestsWindow()
    {
        if (_requestsPanel is null) return;
        _requestsPanel.Children.Clear();

        _requestsPanel.Children.Add(RequestText("📥 Pedidos de músicas", 20, true));
        var urls = _remote?.LocalUrls().Select(u => u + "pedido").ToList() ?? new List<string>();
        var link = urls.FirstOrDefault(u => u.Contains("//192.168.", StringComparison.Ordinal)) ?? urls.FirstOrDefault();
        _requestsPanel.Children.Add(RequestText(link is null
            ? "Sem rede encontrada: conecte o computador ao Wi-Fi para receber pedidos."
            : "Mande este link às pessoas (elas precisam estar no mesmo Wi-Fi):", 13, false, "#4A5A6B"));
        if (link is not null)
        {
            _requestsPanel.Children.Add(new System.Windows.Controls.TextBox { Text = string.Join("\n", urls), IsReadOnly = true, FontSize = 15, Margin = new Thickness(0, 0, 0, 4) });
            _requestsPanel.Children.Add(RequestButton("Copiar link de pedidos", () =>
            {
                try { System.Windows.Clipboard.SetText(link); StatusText.Text = "Link de pedidos copiado: " + link; }
                catch (Exception ex) { StatusText.Text = "Não foi possível copiar: " + ex.Message; }
            }));
        }

        if (_requests.Count == 0)
        {
            _requestsPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Nenhum pedido na lista de espera.", Margin = new Thickness(0, 14, 0, 0), Foreground = Hex("#4A5A6B") });
            return;
        }

        foreach (var request in _requests.OrderBy(r => r.ReceivedAt).ToList())
        {
            var card = new System.Windows.Controls.Border
            {
                Background = Hex("#FFFFFF"),
                BorderBrush = Hex("#D5DCE4"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 12, 0, 0)
            };
            var content = new System.Windows.Controls.StackPanel();
            content.Children.Add(RequestText(string.IsNullOrWhiteSpace(request.Name) ? "(sem nome da música)" : request.Name, 16, true));
            content.Children.Add(RequestText("De: " + (string.IsNullOrWhiteSpace(request.Group) ? "não informado" : request.Group) +
                                             " · " + request.ReceivedAt.ToString("dd/MM HH:mm"), 12, false, "#4A5A6B"));
            content.Children.Add(RequestText(request.Url, 12, false, "#2D6A9F"));

            var buttons = new System.Windows.Controls.WrapPanel();
            var item = request;
            buttons.Children.Add(RequestButton("⏭ Preparar como próxima", () => PrepareRequestAsNext(item)));
            buttons.Children.Add(RequestButton("Abrir", () => OpenRequest(item)));
            buttons.Children.Add(RequestButton("Copiar link", () =>
            {
                try { System.Windows.Clipboard.SetText(item.Url); StatusText.Text = "Link copiado: " + item.Url; }
                catch (Exception ex) { StatusText.Text = "Não foi possível copiar: " + ex.Message; }
            }));
            buttons.Children.Add(RequestButton("Descartar", () => RemoveRequest(item)));
            content.Children.Add(buttons);
            card.Child = content;
            _requestsPanel.Children.Add(card);
        }
    }

    private void RemoveRequest(SongRequest request)
    {
        _requests.RemoveAll(r => r.Id == request.Id);
        SongRequestStore.Save(_requests);
        UpdateRequestsButton();
        RefreshRequestsWindow();
    }

    private void PrepareRequestAsNext(SongRequest request)
    {
        var videoId = YouTubeLink.TryGetVideoId(request.Url);
        if (videoId is null)
        {
            System.Windows.MessageBox.Show(_requestsWindow ?? this,
                "Este link não é de um vídeo do YouTube. Use \"Abrir\" para ver a página e escolher o vídeo.",
                "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_nextVideoUrl) &&
            System.Windows.MessageBox.Show(_requestsWindow ?? this,
                "Já existe uma música preparada como próxima. Substituir por esta?",
                "Lyravoxen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _nextVideoUrl = "https://www.youtube.com/watch?v=" + videoId;
        SaveNextVideoPreference();
        NextTrackText.Text = "Próxima faixa preparada (" + videoId + ")" +
                             (string.IsNullOrWhiteSpace(request.Name) ? "" : ": " + request.Name) +
                             ". A música atual continua tocando até você selecionar Tocar próxima.";
        PlayNextButton.IsEnabled = true;
        StatusText.Text = "Próxima música preparada a partir do pedido" +
                          (string.IsNullOrWhiteSpace(request.Group) ? "." : " de " + request.Group + ".");
        RemoveRequest(request);
    }

    private void OpenRequest(SongRequest request)
    {
        try
        {
            var tab = WorkspaceTabs.Items.OfType<System.Windows.Controls.TabItem>()
                .FirstOrDefault(t => t.Header is string header && header.Contains("Próxima", StringComparison.Ordinal));
            var view = ActiveBrowserView;
            if (tab is not null && view?.CoreWebView2 is not null)
            {
                WorkspaceTabs.SelectedItem = tab;
                view.CoreWebView2.Navigate(request.Url);
                return;
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(request.Url) { UseShellExecute = true });
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível abrir o link: " + ex.Message; }
    }
}
