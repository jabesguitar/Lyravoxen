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
        Closed += (_, _) => { _remote?.Dispose(); _audioMonitor.Dispose(); };
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
            _remote = new RemoteControlServer(LoadOrCreatePin())
            {
                Handler = HandleRemoteCommandAsync,
                RequestHandler = HandleSongRequestAsync,
                IsRequestBlocked = IsRequestSenderBlocked,
                AudioStreamer = _audioMonitor.StreamAsync,
                QrPng = MakeQrPngBytes,
                OnlineLink = () => _whatsApp.OnlineRequestLink
            };
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
                    case "nextconfirm": return ConfirmNextLyrics();
                    case "livejson": return BuildLiveJson();
                    case "onlinelink": return _whatsApp.OnlineRequestLink;
                    case "audiolevel":
                        _audioMonitor.EnsureStarted();
                        return _audioMonitor.Running ? _audioMonitor.Level.ToString() : "ERR:" + (_audioMonitor.LastError.Length > 0 ? _audioMonitor.LastError : "captura desligada");
                    case "clearlive": return ClearLiveProjectionForRemote();
                    case "setnext": return SetNextFromUrlForRemote(argument);
                    case "qadd": return QueueAddForRemote(argument);
                    case "qremove": return QueueRemoveForRemote(argument);
                    case "qclear": return QueueClearForRemote();
                    case "autoadvance": return ToggleAutoAdvanceForRemote();
                    case "lyricsfetch": return await LyricsFetchForRemoteAsync(argument);
                    case "nextreject": return RejectNextLyricsForRemote();
                    case "fixnames": return await FixNextNamesForRemoteAsync();
                    case "lyricsnext": return LyricsTextForNextFromRemote(argument);
                    case "lyricsnow": return LyricsTextForNowFromRemote(argument);
                    case "estimate": return EstimateTimings();
                    case "tapstart":
                    case "tapmark":
                    case "tapundo":
                    case "tapfinish":
                    case "tapcancel": return await TapCommandForRemoteAsync(command);
                    case "cueprev":
                        PreviousCue_Click(this, new RoutedEventArgs());
                        return StatusText.Text;
                    case "cuenext":
                        NextCue_Click(this, new RoutedEventArgs());
                        return StatusText.Text;
                    case "auto":
                        ReopenProjectionAfterLiveClear();
                        ResumeAutomaticProjection_Click(this, new RoutedEventArgs());
                        return "Projeção automática retomada.";
                    case "vol":
                    case "seek": return await RemotePlayerActionAsync(command, argument);
                    case "reqpick": return RequestPickForRemote(argument);
                    case "libsearch": return LibrarySearchJson(argument);
                    case "libpick": return LibraryPickForRemote(argument);
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
                    case "chnext": await ChangeBibleChapterForRemoteAsync(1); break;
                    case "chprev": await ChangeBibleChapterForRemoteAsync(-1); break;
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

    /// <summary>Volume ("+10", "-10", "mute") e avançar/voltar (segundos) no player do YouTube, pelo celular.</summary>
    private async Task<string> RemotePlayerActionAsync(string command, string argument)
    {
        if (_isLocalAudioPlaying) return "Volume e avançar/voltar pelo celular só funcionam com música do YouTube.";
        if (YoutubePlayer.CoreWebView2 is null) return "Nenhuma música carregada.";
        const string guard = "if(typeof player==='undefined'||!player||typeof player.getVolume!=='function')return 'sem player';";
        string script;
        if (command == "seek")
        {
            if (!double.TryParse(argument, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds) || Math.Abs(seconds) > 120)
                return "Valor inválido.";
            script = "(function(){" + guard + "var t=Math.max(0,player.getCurrentTime()+(" +
                     seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "));player.seekTo(t,true);return 'ok';})()";
        }
        else if (argument == "mute")
        {
            script = "(function(){" + guard + "if(player.isMuted()){player.unMute();return 'Som ligado.';}player.mute();return 'Som desligado (mudo).';})()";
        }
        else
        {
            if (!int.TryParse(argument, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var step) || Math.Abs(step) > 50)
                return "Valor inválido.";
            script = "(function(){" + guard + "var v=Math.max(0,Math.min(100,player.getVolume()+(" + step +
                     ")));player.setVolume(v);if(v>0&&player.isMuted()){player.unMute();}return 'Volume '+Math.round(v)+'%';})()";
        }

        var result = (await YoutubePlayer.CoreWebView2.ExecuteScriptAsync(script)).Trim('"');
        if (result == "sem player") return "O player ainda não está pronto.";
        return command == "seek" ? "Posição ajustada (" + argument + " s)." : result;
    }

    /// <summary>QR code em PNG de um endereço (null se não der para gerar).</summary>
    private static byte[]? MakeQrPngBytes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            return new PngByteQRCode(data).GetGraphic(8);
        }
        catch (Exception ex)
        {
            AppLog.Error("Falha ao gerar o QR code", ex);
            return null;
        }
    }

    /// <summary>QR code de um endereço como imagem para a tela do computador.</summary>
    private static BitmapImage? MakeQrImage(string url)
    {
        var png = MakeQrPngBytes(url);
        if (png is null) return null;
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(png);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
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
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

        System.Windows.Controls.TextBlock Text(string value, double size = 14, bool bold = false, string color = "#EAF4FF") => new()
        {
            Text = value,
            FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Hex(color),
            Margin = new Thickness(0, 0, 0, 8)
        };

        System.Windows.Controls.Border Card(System.Windows.UIElement content) => new()
        {
            Background = Hex("#0B2B50"),
            BorderBrush = Hex("#167FA8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 12),
            Child = content
        };

        System.Windows.Controls.Button ActionButton(string label) => new()
        {
            Content = label,
            Padding = new Thickness(14, 8, 14, 8),
            MinHeight = 38,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Hex("#FFFFFF"),
            Background = Hex("#123E68"),
            BorderBrush = Hex("#168EB8"),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Thickness(0, 4, 8, 0)
        };

        panel.Children.Add(Text("CONTROLE PELO CELULAR", 21, true, "#35DDF5"));
        panel.Children.Add(Text("Conecte seu celular ao Lyravoxen para controlar a projeção e funções compatíveis pela rede local.", 13, false, "#B8D5EA"));

        var steps = new System.Windows.Controls.StackPanel();
        steps.Children.Add(Text("1. MESMA REDE WI-FI", 12, true, "#35DDF5"));
        steps.Children.Add(Text("Conecte o celular ao mesmo Wi-Fi deste computador (ou ao ponto de acesso do celular).", 13));
        steps.Children.Add(Text("2. LEIA O QR CODE", 12, true, "#35DDF5"));
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
                var qrFrame = new System.Windows.Controls.Border
                {
                    Background = Hex("#FFFFFF"),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(10),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Child = new System.Windows.Controls.Image { Source = bitmap, Width = 230, Height = 230, Stretch = System.Windows.Media.Stretch.Uniform }
                };
                steps.Children.Add(qrFrame);
            }
            catch (Exception ex) { AppLog.Error("Falha ao gerar o QR code", ex); }
        }
        else
        {
            steps.Children.Add(Text("Nenhum endereço de rede local foi encontrado. Confira se o computador e o celular estão conectados à rede.", 13, false, "#FFD27A"));
        }
        steps.Children.Add(Text("Se preferir, digite o endereço abaixo no navegador do celular:", 12, false, "#B8D5EA"));

        var addressBox = new System.Windows.Controls.TextBox
        {
            Text = urls.Count == 0 ? "(sem rede encontrada)" : string.Join("\n", urls),
            IsReadOnly = true,
            FontSize = 14,
            Padding = new Thickness(10),
            Foreground = Hex("#DDF5FF"),
            Background = Hex("#06172F"),
            BorderBrush = Hex("#245B88"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        steps.Children.Add(addressBox);
        var copy = ActionButton("Copiar endereço");
        copy.Click += (_, _) =>
        {
            try { System.Windows.Clipboard.SetText(addressBox.Text); }
            catch (Exception ex) { System.Windows.MessageBox.Show(this, "Não foi possível copiar o endereço: " + ex.Message, "Controle pelo celular", MessageBoxButton.OK, MessageBoxImage.Warning); }
        };
        steps.Children.Add(copy);
        panel.Children.Add(Card(steps));

        var pinPanel = new System.Windows.Controls.StackPanel();
        pinPanel.Children.Add(Text("3. DIGITE O PIN", 12, true, "#35DDF5"));
        var pinText = Text(_remote.Pin, 30, true, "#FFFFFF");
        pinText.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        pinPanel.Children.Add(pinText);
        pinPanel.Children.Add(Text("Digite este código na página aberta pelo celular. O PIN protege o acesso ao controle.", 12, false, "#B8D5EA"));
        var renew = ActionButton("Gerar novo PIN");
        renew.Click += (_, _) =>
        {
            _remote.Pin = LoadOrCreatePin(renew: true);
            pinText.Text = _remote.Pin;
        };
        pinPanel.Children.Add(renew);
        panel.Children.Add(Card(pinPanel));

        var help = new System.Windows.Controls.StackPanel();
        help.Children.Add(Text("O QUE VOCÊ PODE FAZER", 12, true, "#35DDF5"));
        help.Children.Add(Text("Usar os controles compatíveis da página móvel, navegar pela Bíblia, projetar referências e controlar funções disponíveis da reprodução e dos pedidos.", 13));
        help.Children.Add(Text("Na primeira conexão, permita o acesso do Lyravoxen no Firewall do Windows em Redes privadas. O controle funciona enquanto o aplicativo estiver aberto.", 12, false, "#B8D5EA"));
        panel.Children.Add(Card(help));

        var window = new Window
        {
            Title = "Lyravoxen — Controle pelo celular",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 560,
            MinWidth = 480,
            MaxHeight = SystemParameters.WorkArea.Height * 0.9,
            SizeToContent = SizeToContent.Height,
            Background = Hex("#06172F"),
            Content = new System.Windows.Controls.ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled
            }
        };
        window.Show();
    }
}
