using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Lyravoxen.Services;
using QRCoder;

namespace Lyravoxen;

// 📱 Controle pelo celular: página na rede local (Wi-Fi) protegida por PIN.
public partial class MainWindow
{
    private RemoteControlServer? _remote;

    private static readonly string RemotePinFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "remote-pin.txt");

    private void InitializeRemote()
    {
        Loaded += (_, _) => StartRemote();
        Closed += (_, _) => _remote?.Dispose();
    }

    private static string LoadOrCreatePin(bool renew = false)
    {
        try
        {
            if (!renew && File.Exists(RemotePinFile))
            {
                var saved = File.ReadAllText(RemotePinFile).Trim();
                if (saved.Length >= 4) return saved;
            }
            var pin = Random.Shared.Next(1000, 10000).ToString();
            Directory.CreateDirectory(Path.GetDirectoryName(RemotePinFile)!);
            File.WriteAllText(RemotePinFile, pin);
            return pin;
        }
        catch { return "1234"; }
    }

    private void StartRemote()
    {
        try
        {
            _remote = new RemoteControlServer(LoadOrCreatePin()) { Handler = HandleRemoteCommandAsync, RequestHandler = HandleSongRequestAsync };
            _remote.Start();
            AppLog.Info("Controle remoto ativo na porta " + _remote.Port);
        }
        catch (Exception ex)
        {
            _remote = null;
            AppLog.Error("Controle remoto indisponível", ex);
        }
    }

    private Task<string> HandleRemoteCommandAsync(string command, string argument)
    {
        return Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                switch (command)
                {
                    case "playnext":
                        if (string.IsNullOrWhiteSpace(_nextVideoUrl)) return "Não há próxima música preparada." + "\n" + LiveStateText();
                        var lyricsWarning = NextLyricsWarning();
                        PlayNextPrepared_Click(this, new RoutedEventArgs());
                        return "Tocando a próxima música…" + lyricsWarning + "\n" + LiveStateText();
                    case "nextcheck": return await CheckNextLyricsFromRemoteAsync(argument);
                    case "nextconfirm": return ConfirmNextLyricsFromRemote();
                    case "toggle": return await ToggleLivePlaybackAsync() + "\n" + LiveStateText();
                    case "endlive":
                        EndPlaybackManually_Click(this, new RoutedEventArgs());
                        return "Música encerrada e projeção limpa." + "\n" + LiveStateText();
                    case "reqnext": return PrepareOldestRequestForRemote(replace: false) + "\n" + LiveStateText();
                    case "reqreplace": return PrepareOldestRequestForRemote(replace: true) + "\n" + LiveStateText();
                    case "live": return LiveStateText();
                    case "ref":
                        if (string.IsNullOrWhiteSpace(argument)) return "Digite uma referência.";
                        BibleSearchBox.Text = argument.Trim();
                        ExpandTypedBookPrefix();
                        await LocateBibleReferenceAsync(project: true);
                        break;
                    case "next": await StepBibleVerseAsync(1); break;
                    case "prev": await StepBibleVerseAsync(-1); break;
                    case "chnext": await ChangeBibleChapterAsync(1); break;
                    case "chprev": await ChangeBibleChapterAsync(-1); break;
                    case "project": await ProjectBibleSelectionAsync(); break;
                    case "clear": ClearBibleProjection_Click(this, new RoutedEventArgs()); break;
                    case "state": break;
                    default: return "Comando desconhecido.";
                }
                return BibleStatusText.Text + "\n" + LiveStateText();
            }
            catch (Exception ex)
            {
                AppLog.Error("Comando remoto falhou", ex);
                return "Erro: " + ex.Message;
            }
        }).Task.Unwrap();
    }

    private string LiveStateText() =>
        "▶ Próxima: " + (string.IsNullOrWhiteSpace(_nextVideoUrl) ? "nenhuma preparada" : "preparada") +
        " · 📥 " + _requests.Count + (_requests.Count == 1 ? " pedido" : " pedidos") +
        (string.IsNullOrWhiteSpace(_nextVideoUrl) || _nextLyrics is null ? "" : "\n" + _nextLyrics.Headline);

    private async Task<string> ToggleLivePlaybackAsync()
    {
        if (_isLocalAudioPlaying) return "Pausar pelo celular só funciona com música do YouTube.";
        if (YoutubePlayer.CoreWebView2 is null) return "Nenhuma música carregada.";
        var result = await YoutubePlayer.CoreWebView2.ExecuteScriptAsync(
            "typeof player !== 'undefined' && player && typeof player.getPlayerState === 'function' ? (player.getPlayerState()===1 ? (player.pauseVideo(),'pausada') : (player.playVideo(),'tocando')) : 'sem player'");
        return result.Trim('"') switch
        {
            "pausada" => "Música pausada.",
            "tocando" => "Música tocando.",
            _ => "O player ainda não está pronto."
        };
    }

    private static System.Windows.Media.Brush Hex(string color) =>
        (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color)!;

    private void Remote_Click(object sender, RoutedEventArgs e)
    {
        if (_remote is null)
        {
            System.Windows.MessageBox.Show(this, "O controle pelo celular não conseguiu iniciar. Use o botão 📋 Relatório e envie o texto para análise.",
                "Lyravoxen — Celular", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var urls = _remote.LocalUrls().ToList();
        var url = urls.FirstOrDefault(u => u.Contains("//192.168.", StringComparison.Ordinal)) ?? urls.FirstOrDefault();

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(18) };
        System.Windows.Controls.TextBlock Text(string value, double size = 14, bool bold = false) => new()
        {
            Text = value,
            FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Hex("#1A2430"),
            Margin = new Thickness(0, 0, 0, 8)
        };

        panel.Children.Add(Text("📱 Controle pelo celular", 20, true));
        panel.Children.Add(Text("1. Conecte o celular no MESMO Wi-Fi deste computador (ou no ponto de acesso do celular).\n" +
                                "2. Aponte a câmera para o QR code (ou digite o endereço no navegador).\n" +
                                "3. Digite o PIN abaixo uma vez. Depois é só usar: campo de referência (com o microfone do teclado), Projetar, versículo/capítulo e Limpar telão."));
        if (url is not null)
        {
            try
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                var png = new PngByteQRCode(data).GetGraphic(8);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(png);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                panel.Children.Add(new System.Windows.Controls.Image { Source = bitmap, Width = 220, Height = 220, Margin = new Thickness(0, 6, 0, 6) });
            }
            catch (Exception ex) { AppLog.Error("Falha ao gerar o QR code", ex); }
        }

        var addressBox = new System.Windows.Controls.TextBox
        {
            Text = urls.Count == 0 ? "(sem rede encontrada)" : string.Join("\n", urls),
            IsReadOnly = true,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(addressBox);
        var pinText = Text("PIN: " + _remote.Pin, 24, true);
        panel.Children.Add(pinText);
        panel.Children.Add(Text("Na primeira vez o Windows pergunta sobre o firewall: permita em \"Redes privadas\". " +
                                "Só funciona enquanto o Lyravoxen estiver aberto.", 12));

        var renew = new System.Windows.Controls.Button { Content = "Trocar PIN", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 0, 6) };
        renew.Click += (_, _) =>
        {
            _remote.Pin = LoadOrCreatePin(renew: true);
            pinText.Text = "PIN: " + _remote.Pin;
        };
        panel.Children.Add(renew);

        var window = new Window
        {
            Title = "Lyravoxen — Celular",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            Background = Hex("#F5F7FA"),
            Content = new System.Windows.Controls.ScrollViewer { Content = panel, VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto }
        };
        window.Show();
    }
}
