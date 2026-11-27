using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Lyravoxen.Models;
using Lyravoxen.Offline;
using Lyravoxen.Services;

namespace Lyravoxen;

// 📥 Pedidos: músicas enviadas pelas pessoas pela página de pedidos (/pedido) ficam numa lista de espera.
public partial class MainWindow
{
    private List<SongRequest> _requests = new();
    private readonly Dictionary<string, string> _blockedRequestSenders = LoadBlockedRequestSenders();
    private static string BlockedSendersFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "pedidos-bloqueados.json");
    private static Dictionary<string, string> LoadBlockedRequestSenders()
    {
        try
        {
            var loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(BlockedSendersFile));
            return loaded is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
        }
        catch { return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
    }
    private bool IsRequestSenderBlocked(string ip) => !string.IsNullOrWhiteSpace(ip) && _blockedRequestSenders.ContainsKey(ip);
    private void SaveBlockedRequestSenders()
    {
        try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BlockedSendersFile)!); System.IO.File.WriteAllText(BlockedSendersFile, System.Text.Json.JsonSerializer.Serialize(_blockedRequestSenders, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { AppLog.Error("Não foi possível salvar os remetentes bloqueados", ex); }
    }
    private async void ManageBlockedSenders()
    {
        var ip = PromptForText("Bloquear remetente", "IP do remetente (local ou público):");
        if (string.IsNullOrWhiteSpace(ip)) return;
        if (!IPAddress.TryParse(ip.Trim(), out var parsedIp))
        {
            System.Windows.MessageBox.Show(_requestsWindow ?? this, "Informe um endereço IPv4 ou IPv6 válido.", "Bloquear remetente", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ip = parsedIp.ToString();
        var label = PromptForText("Identificar remetente", "Nome para identificar este IP:", ip) ?? ip;
        _blockedRequestSenders[ip] = label.Trim(); SaveBlockedRequestSenders();
        if (CloudConfigured)
        {
            var cloud = await CloudRequestClient.SetBlockedAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken, ip, label.Trim(), true);
            StatusText.Text = cloud.Ok ? "Remetente bloqueado localmente e na internet: " + label : "Bloqueado localmente. Bloqueio na internet não sincronizado: " + cloud.Message;
        }
        else StatusText.Text = "Remetente bloqueado localmente: " + label + ". Configure pedidos na nuvem para bloquear o link público.";
        RefreshRequestsWindow();
    }
    private async void BlockSender(string ip, string name)
    {
        if (!IPAddress.TryParse(ip, out var parsed)) return;
        ip = parsed.ToString();
        _blockedRequestSenders[ip] = name;
        SaveBlockedRequestSenders();
        if (CloudConfigured)
        {
            var cloud = await CloudRequestClient.SetBlockedAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken, ip, name, true);
            StatusText.Text = cloud.Ok ? "IP bloqueado na internet e neste computador: " + ip : "IP bloqueado neste computador, mas a nuvem não confirmou: " + cloud.Message;
        }
        else StatusText.Text = "IP bloqueado apenas neste computador. Configure a conexão com a nuvem para bloquear o formulário público.";
        RefreshRequestsWindow();
    }

    private async void UnblockSender(string ip)
    {
        var cloudOk = !CloudConfigured;
        if (CloudConfigured)
        {
            var cloud = await CloudRequestClient.SetBlockedAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken, ip, "", false);
            cloudOk = cloud.Ok;
        }
        _blockedRequestSenders.Remove(ip);
        SaveBlockedRequestSenders();
        StatusText.Text = cloudOk ? "Bloqueio removido: " + ip : "Bloqueio removido localmente, mas ainda pode estar ativo no Worker: " + ip;
        RefreshRequestsWindow();
    }

    private async Task RefreshCloudBlockedSendersAsync()
    {
        if (!CloudConfigured) return;
        var result = await CloudRequestClient.GetBlockedAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken);
        if (!result.Ok)
        {
            StatusText.Text = "Não foi possível atualizar bloqueios do Worker: " + result.Message;
            return;
        }
        foreach (var sender in result.Items)
            if (IPAddress.TryParse(sender.Ip, out var parsed))
                _blockedRequestSenders[parsed.ToString()] = sender.Name;
        SaveBlockedRequestSenders();
        RefreshRequestsWindow();
    }


    private Window? _requestsWindow;
    private System.Windows.Controls.StackPanel? _requestsPanel;
    private WhatsAppSettings _whatsApp = new();
    private DispatcherTimer? _clipTimer;
    private string _lastClip = "";

    private void InitializeRequests()
    {
        _requests = SongRequestStore.Load();
        _whatsApp = WhatsAppSettings.Load();
        Loaded += (_, _) =>
        {
            UpdateRequestsButton();
            StartClipboardWatch();
            StartCloudPolling();
        };
    }

    // Pedidos que chegam por um grupo do WhatsApp: o operador seleciona e copia as mensagens no WhatsApp Web/Desktop
    // e o Lyravoxen importa sozinho (só textos que parecem pedido; links soltos copiados para outro fim são ignorados).
    private void StartClipboardWatch()
    {
        try { _lastClip = System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : ""; } catch { _lastClip = ""; }
        _clipTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _clipTimer.Tick += (_, _) => ClipboardTick();
        _clipTimer.Start();
    }

    private void ClipboardTick()
    {
        if (!_whatsApp.WatchClipboard) return;
        string? text;
        try { text = System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null; }
        catch { return; } // a área de transferência pode estar em uso por outro programa; tenta no próximo ciclo
        if (string.IsNullOrEmpty(text) || text == _lastClip) return;
        _lastClip = text;
        if (!SongRequestStore.LooksLikeRequest(text)) return;
        var (added, _) = ImportMessageText(text);
        if (added > 0)
        {
            StatusText.Text = "📥 Pedido copiado do WhatsApp importado: " + added + (added == 1 ? " música." : " músicas.");
            AppLog.Info("Pedido importado da área de transferência: " + added);
        }
    }

    // ----- Pedidos pela internet (serviço gratuito na nuvem; ver CloudWorker) -----
    private DispatcherTimer? _cloudTimer;
    private string _cloudVersion = "";
    private bool _cloudBusy;
    private bool _cloudFailureLogged;
    private readonly HashSet<string> _cloudSeenIds = new();

    private void StartCloudPolling()
    {
        _cloudTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _cloudTimer.Tick += async (_, _) => await CloudTickAsync();
        _cloudTimer.Start();
        _ = CloudTickAsync();
    }

    private bool CloudConfigured => _whatsApp.CloudUrl.Length > 0 && _whatsApp.CloudToken.Length > 0;

    /// <summary>Busca os pedidos que chegaram ao serviço e importa os que este computador ainda não viu.</summary>
    private async Task CloudTickAsync(bool force = false)
    {
        if (_cloudBusy || !CloudConfigured || (!_whatsApp.CloudAuto && !force)) return;
        _cloudBusy = true;
        try
        {
            var result = await CloudRequestClient.FetchAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken, force ? "" : _cloudVersion);
            if (!result.Ok)
            {
                _cloudVersion = "";
                if (!_cloudFailureLogged) { AppLog.Info("Pedidos pela internet: " + result.Message); _cloudFailureLogged = true; }
                return;
            }
            _cloudFailureLogged = false;
            if (result.Items.Count == 0) { _cloudVersion = result.Version; return; }

            // Não apaga nada do serviço: assim vários computadores (casa, igreja) recebem os mesmos pedidos.
            // O serviço descarta sozinho depois de algumas horas; cada computador lembra o que já importou.
            var total = 0;
            foreach (var item in result.Items)
                if (_cloudSeenIds.Add(item.Id) && !IsRequestSenderBlocked(item.SenderIp)) total += ImportMessageText(item.Text, item.SenderIp).Added;
            _cloudVersion = result.Version;
            if (total > 0)
            {
                StatusText.Text = "📥 Pedido recebido pela internet: " + total + (total == 1 ? " música." : " músicas.");
                AppLog.Info("Pedidos pela internet: " + total + " música(s) de " + result.Items.Count + " mensagem(ns).");
            }
        }
        catch (Exception ex) { AppLog.Error("Pedidos pela internet falharam", ex); }
        finally { _cloudBusy = false; }
    }

    /// <summary>Importa pedidos de um texto (mensagem, várias mensagens ou conversa inteira). Devolve quantas músicas entraram.</summary>
    private (int Added, string Message) ImportMessageText(string text, string senderIp = "")
    {
        var jsons = SongRequestStore.MessagesToJson(text);
        if (jsons.Count == 0) return (0, "Não encontrei links nessa mensagem.");
        var total = 0;
        var lastMessage = "";
        foreach (var json in jsons)
        {
            var (added, ok, message) = SongRequestStore.Parse(json, _requests, DateTime.Now);
            if (ok) { foreach (var request in added) request.SenderIp = senderIp; _requests.AddRange(added); total += added.Count; }
            else lastMessage = message;
        }
        if (total > 0)
        {
            SongRequestStore.Save(_requests);
            UpdateRequestsButton();
            RefreshRequestsWindow();
            PrecheckRequestLyrics();
        }
        return (total, total > 0 ? "" : lastMessage);
    }

    private void UpdateRequestsButton() => RequestsButton.Content = "📥 Pedidos (" + _requests.Count + ")";

    private Task<(bool Ok, string Message)> HandleSongRequestAsync(string json, string ip)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            var (added, ok, message) = SongRequestStore.Parse(json, _requests, DateTime.Now);
            if (ok)
            {
                foreach (var request in added) request.SenderIp = ip;
                _requests.AddRange(added);
                SongRequestStore.Save(_requests);
                UpdateRequestsButton();
                var who = string.IsNullOrWhiteSpace(added[0].Group) ? "alguém" : added[0].Group;
                StatusText.Text = "📥 Novo pedido de " + who + ": " + added.Count + (added.Count == 1 ? " música." : " músicas.");
                AppLog.Info("Pedido recebido de " + who + " (" + ip + "): " + added.Count);
                RefreshRequestsWindow();
                PrecheckRequestLyrics();
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

        _requestsPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(18) };
        var window = new Window
        {
            Title = "Lyravoxen — Pedidos recebidos",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 920,
            Height = 760,
            MinWidth = 720,
            MinHeight = 560,
            Background = Hex("#06172F"),
            Content = new System.Windows.Controls.ScrollViewer
            {
                Content = _requestsPanel,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled
            }
        };
        window.Closed += (_, _) => { _requestsWindow = null; _requestsPanel = null; };
        _requestsWindow = window;
        RefreshRequestsWindow();
        window.Show();
        _ = RefreshCloudBlockedSendersAsync();
    }

    private System.Windows.Controls.TextBlock RequestText(string value, double size, bool bold = false, string color = "#EAF4FF") => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Hex(color),
        Margin = new Thickness(0, 0, 0, 5)
    };

    private System.Windows.Controls.Button RequestButton(string label, Action action)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = label,
            Padding = new Thickness(13, 7, 13, 7),
            Margin = new Thickness(0, 5, 8, 0),
            MinHeight = 36,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Hex("#FFFFFF"),
            Background = Hex("#123E68"),
            BorderBrush = Hex("#168EB8"),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void RefreshRequestsWindow()
    {
        if (_requestsPanel is null) return;
        _requestsPanel.Children.Clear();

        var header = new System.Windows.Controls.Border
        {
            Background = Hex("#0B2B50"),
            BorderBrush = Hex("#087F9E"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var headerGrid = new System.Windows.Controls.Grid();
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.Controls.ColumnDefinition().Width = new GridLength(1, GridUnitType.Star) });
        var headerInfo = new System.Windows.Controls.StackPanel();
        headerInfo.Children.Add(RequestText("PEDIDOS RECEBIDOS", 21, true, "#FFFFFF"));
        headerInfo.Children.Add(RequestText("Confira os pedidos, prepare a próxima música ou salve uma cópia offline.", 12, false, "#B8D5EA"));
        headerInfo.Children.Add(RequestText(_requests.Count + (_requests.Count == 1 ? " música aguardando" : " músicas aguardando"), 13, true, "#35DDF5"));
        header.Child = headerInfo;
        _requestsPanel.Children.Add(header);

        var toolbar = new System.Windows.Controls.WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        toolbar.Children.Add(RequestButton("🛡 IPs bloqueados", ManageBlockedSenders));
        toolbar.Children.Add(RequestButton("↻ Atualizar bloqueios", () => _ = RefreshCloudBlockedSendersAsync()));
        _requestsPanel.Children.Add(toolbar);

        var settings = new System.Windows.Controls.StackPanel { Margin = new Thickness(8, 5, 8, 8) };
        var urls = _remote?.LocalUrls().Select(u => u + "pedido").ToList() ?? new List<string>();
        var link = urls.FirstOrDefault(u => u.Contains("//192.168.", StringComparison.Ordinal)) ?? urls.FirstOrDefault();
        settings.Children.Add(RequestText(link is null
            ? "Sem rede local encontrada. Conecte o computador ao Wi-Fi para receber pedidos na mesma rede."
            : "LINK DA REDE LOCAL — compartilhe com quem estiver no mesmo Wi-Fi", 12, true, "#35DDF5"));
        if (link is not null)
        {
            settings.Children.Add(new System.Windows.Controls.TextBox { Text = string.Join("\n", urls), IsReadOnly = true, FontSize = 13, Margin = new Thickness(0, 2, 0, 6), Padding = new Thickness(8) });
            settings.Children.Add(RequestButton("Copiar link da rede local", () =>
            {
                try { System.Windows.Clipboard.SetText(link); StatusText.Text = "Link de pedidos copiado: " + link; }
                catch (Exception ex) { StatusText.Text = "Não foi possível copiar: " + ex.Message; }
            }));
        }

        var cloudLink = _whatsApp.OnlineRequestLink;
        settings.Children.Add(RequestText("PEDIDOS PELA INTERNET — funciona em qualquer rede", 12, true, "#35DDF5"));
        var qrImage = MakeQrImage(cloudLink);
        if (qrImage is not null)
            settings.Children.Add(new System.Windows.Controls.Image { Source = qrImage, Width = 130, Height = 130, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Margin = new Thickness(0, 3, 0, 4) });
        settings.Children.Add(new System.Windows.Controls.TextBox { Text = cloudLink, IsReadOnly = true, FontSize = 13, Margin = new Thickness(0, 2, 0, 6), Padding = new Thickness(8) });
        settings.Children.Add(RequestButton("Copiar link da internet", () =>
        {
            try { System.Windows.Clipboard.SetText(cloudLink); StatusText.Text = "Link copiado: " + cloudLink; }
            catch (Exception ex) { StatusText.Text = "Não foi possível copiar: " + ex.Message; }
        }));

        settings.Children.Add(RequestText("WHATSAPP — " + (!string.IsNullOrWhiteSpace(_whatsApp.GroupName) ? _whatsApp.GroupName : "configuração de grupo"), 12, true, "#35DDF5"));
        settings.Children.Add(RequestText("Copie as mensagens no WhatsApp Web/Desktop (Ctrl+C) para importar automaticamente " +
            (_whatsApp.WatchClipboard ? "— recurso ligado." : "— importação automática desligada nas configurações."), 11, false, "#B8D5EA"));
        var setupButtons = new System.Windows.Controls.WrapPanel();
        setupButtons.Children.Add(RequestButton("Configurar internet", CloudSettingsDialog));
        setupButtons.Children.Add(RequestButton("Configurar WhatsApp", WhatsAppSettingsDialog));
        setupButtons.Children.Add(RequestButton("Colar pedido", ImportRequestDialog));
        setupButtons.Children.Add(RequestButton("Importar conversa .txt", ImportChatFile));
        settings.Children.Add(setupButtons);

        var settingsExpander = new System.Windows.Controls.Expander
        {
            Header = "⚙ Configurar recebimento de pedidos e links",
            IsExpanded = false,
            Foreground = Hex("#35DDF5"),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Background = Hex("#0A2342"),
            BorderBrush = Hex("#245B88"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 14),
            Content = settings
        };
        _requestsPanel.Children.Add(settingsExpander);

        if (_blockedRequestSenders.Count > 0)
        {
            var blockedPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(8, 4, 8, 8) };
            foreach (var blocked in _blockedRequestSenders.ToList())
            {
                var row = new System.Windows.Controls.WrapPanel();
                row.Children.Add(RequestText("Bloqueado: " + blocked.Value + " · " + blocked.Key, 12, false, "#FF9BAE"));
                row.Children.Add(RequestButton("Desbloquear", () => UnblockSender(blocked.Key)));
                blockedPanel.Children.Add(row);
            }
            _requestsPanel.Children.Add(new System.Windows.Controls.Expander
            {
                Header = "Remetentes bloqueados (" + _blockedRequestSenders.Count + ")",
                Foreground = Hex("#FF9BAE"),
                Content = blockedPanel,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        _requestsPanel.Children.Add(RequestText("LISTA DE PEDIDOS", 14, true, "#35DDF5"));
        if (_requests.Count == 0)
        {
            var empty = new System.Windows.Controls.Border
            {
                Background = Hex("#0B2341"),
                BorderBrush = Hex("#245B88"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 8, 0, 0),
                Child = RequestText("Nenhum pedido aguardando no momento.", 14, false, "#B8D5EA")
            };
            _requestsPanel.Children.Add(empty);
            return;
        }

        foreach (var request in _requests.OrderByDescending(r => r.ReceivedAt).ToList())
        {
            var card = new System.Windows.Controls.Border
            {
                Background = Hex("#0B2548"),
                BorderBrush = Hex("#167BA3"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(13),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 7, 0, 7)
            };
            var content = new System.Windows.Controls.StackPanel();
            content.Children.Add(RequestText(string.IsNullOrWhiteSpace(request.Name) ? "MÚSICA SEM TÍTULO INFORMADO" : request.Name, 19, true, "#FFFFFF"));
            content.Children.Add(RequestText("CONJUNTO / CANTOR", 10, true, "#35DDF5"));
            content.Children.Add(RequestText(string.IsNullOrWhiteSpace(request.Group) ? "Não informado" : request.Group, 15, true, "#F3C969"));
            content.Children.Add(RequestText("LINK ENVIADO", 10, true, "#35DDF5"));
            var urlText = new System.Windows.Controls.TextBox
            {
                Text = request.Url,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = Hex("#9DD9F5"),
                Background = Hex("#06172F"),
                BorderBrush = Hex("#245B88"),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 1, 0, 6)
            };
            content.Children.Add(urlText);
            content.Children.Add(RequestText("Recebido em " + request.ReceivedAt.ToString("dd/MM/yyyy 'às' HH:mm") +
                (string.IsNullOrWhiteSpace(request.SenderIp) ? "" : " · IP " + request.SenderIp), 11, false, "#AFC8DF"));
            content.Children.Add(RequestText(RequestLyricsLine(request), 12, false, "#EAF4FF"));
            if (!string.IsNullOrWhiteSpace(request.OfflineLocalPath))
                content.Children.Add(RequestText(File.Exists(request.OfflineLocalPath) && (!request.OfflineExpiresAt.HasValue || request.OfflineExpiresAt.Value > DateTime.Now)
                    ? "✓ CÓPIA OFFLINE PRONTA · válida até " + request.OfflineExpiresAt?.ToString("dd/MM/yyyy")
                    : "⚠ CÓPIA OFFLINE EXPIRADA OU AUSENTE", 11, true, "#43E0B1"));

            if (!string.IsNullOrWhiteSpace(request.SenderIp))
            {
                var ipAction = _blockedRequestSenders.ContainsKey(request.SenderIp)
                    ? RequestButton("Desbloquear IP", () => UnblockSender(request.SenderIp))
                    : RequestButton("Bloquear IP", () => BlockSender(request.SenderIp, request.Group));
                ipAction.FontSize = 10;
                ipAction.MinHeight = 30;
                content.Children.Add(ipAction);
            }

            var buttons = new System.Windows.Controls.WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
            var item = request;
            buttons.Children.Add(RequestButton("▶ Preparar próxima", () => PrepareRequestAsNext(item)));
            buttons.Children.Add(RequestButton("⬇ Baixar offline", () => ImportOfflineForRequest(item)));
            buttons.Children.Add(RequestButton("Abrir link", () => OpenRequest(item)));
            buttons.Children.Add(RequestButton("Descartar pedido", () => RemoveRequest(item)));
            content.Children.Add(buttons);
            card.Child = content;
            _requestsPanel.Children.Add(card);
        }
    }

    private bool EnsureYoutubeDownloadConsent()
    {
        var consentFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "youtube-audio-consent.json");
        try
        {
            if (File.Exists(consentFile) &&
                System.Text.Json.JsonDocument.Parse(File.ReadAllText(consentFile)).RootElement.TryGetProperty("authorizedUseConfirmed", out var saved) &&
                saved.ValueKind == System.Text.Json.JsonValueKind.True) return true;
        }
        catch { /* consentimento corrompido: solicita novamente */ }

        var answer = System.Windows.MessageBox.Show(_requestsWindow ?? this,
            "O download integrado usa o yt-dlp oficial para obter a faixa de áudio pública em M4A. Use somente em gravações próprias, de domínio público ou para as quais você tenha autorização. O Lyravoxen não usa cookies, não acessa contas, não contorna DRM, paywalls nem anúncios; a disponibilidade depende do vídeo e do YouTube. O componente é gratuito e terá seu SHA-256 verificado antes do uso.\n\nVocê confirma que tem autorização para baixar e armazenar o áudio deste tipo de conteúdo?",
            "Uso autorizado de áudio", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (answer != MessageBoxResult.Yes) return false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(consentFile)!);
            File.WriteAllText(consentFile, "{ \"authorizedUseConfirmed\": true }");
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(_requestsWindow ?? this, "Não foi possível registrar a confirmação: " + ex.Message, "Download offline", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private LibraryFolder GetOrCreateRequestsOfflineFolder()
    {
        var folder = _library.FirstOrDefault(f => string.Equals(f.Name, "Pedidos recebidos", StringComparison.OrdinalIgnoreCase));
        if (folder is not null) return folder;
        folder = new LibraryFolder { Name = "Pedidos recebidos" };
        _library.Add(folder);
        if (!SaveLibrary())
        {
            _library.Remove(folder);
            throw new IOException("Não foi possível criar a pasta Pedidos recebidos na Biblioteca Offline.");
        }
        RefreshFolders(folder.Id);
        return folder;
    }

    /// <summary>Importa um arquivo autorizado e o associa ao pedido e ao ID exato do vídeo quando disponível.</summary>
    private async void ImportOfflineForRequest(SongRequest request)
    {
        var retention = PromptForText("Validade da cópia offline", "Por quantos dias manter esta cópia local? (5, 7, 14 ou 30)", "30");
        if (retention is null) return;
        if (!int.TryParse(retention.Trim(), out var days) || days is not (5 or 7 or 14 or 30))
        {
            System.Windows.MessageBox.Show(_requestsWindow ?? this, "Escolha 5, 7, 14 ou 30 dias.", "Validade offline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            OfflineMediaResult result = null!;
            var isYouTube = YouTubeLink.TryGetVideoId(request.Url) is not null;
            var importLocal = false;
            if (isYouTube)
            {
                if (!EnsureYoutubeDownloadConsent()) return;
                try
                {
                    StatusText.Text = "Preparando componente gratuito e baixando áudio autorizado do YouTube...";
                    result = await _youtubeAudio.DownloadAuthorizedAudioAsync(request.Url, OfflineMediaFolder);
                }
                catch (Exception downloadError)
                {
                    var fallback = System.Windows.MessageBox.Show(_requestsWindow ?? this,
                        "Não foi possível obter a faixa de áudio M4A deste vídeo.\n\nMotivo: " + downloadError.Message +
                        "\n\nDeseja associar um arquivo MP3/WAV que você já possui e tem autorização para usar?",
                        "Download indisponível", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (fallback != MessageBoxResult.Yes) return;
                    importLocal = true;
                }
            }
            else
            {
                try
                {
                    StatusText.Text = "Baixando o arquivo de áudio direto do endereço do pedido...";
                    result = await _offlineMedia.DownloadAsync(request.Url, OfflineMediaFolder);
                }
                catch (Exception downloadError)
                {
                    var fallback = System.Windows.MessageBox.Show(_requestsWindow ?? this,
                        "O endereço do pedido não entregou um arquivo MP3/WAV direto.\n\nMotivo: " + downloadError.Message +
                        "\n\nDeseja escolher um MP3/WAV autorizado no computador?",
                        "Download direto indisponível", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (fallback != MessageBoxResult.Yes) return;
                    importLocal = true;
                }
            }
            if (importLocal)
            {
                using var dialog = new System.Windows.Forms.OpenFileDialog
                {
                    Title = "Selecione um áudio autorizado para este pedido",
                    Filter = "Áudios MP3 ou WAV (*.mp3;*.wav)|*.mp3;*.wav",
                    Multiselect = false
                };
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                result = await _offlineMedia.ImportLocalAsync(dialog.FileName, OfflineMediaFolder);
            }

            var videoId = YouTubeLink.TryGetVideoId(request.Url) ?? "";
            var expires = DateTime.Now.AddDays(days);
            var title = string.IsNullOrWhiteSpace(request.Name) ? Path.GetFileNameWithoutExtension(result.FilePath) : request.Name;
            var folder = GetOrCreateRequestsOfflineFolder();
            var existing = _library.SelectMany(f => f.Videos)
                .FirstOrDefault(v => v.IsOffline && string.Equals(v.LocalPath, result.FilePath, StringComparison.OrdinalIgnoreCase));
            var inFolder = folder.Videos.FirstOrDefault(v => v.IsOffline &&
                string.Equals(v.LocalPath, result.FilePath, StringComparison.OrdinalIgnoreCase));
            if (inFolder is null)
            {
                inFolder = new LibraryVideo
                {
                    VideoId = videoId,
                    Title = string.IsNullOrWhiteSpace(existing?.Title) ? title : existing.Title,
                    TrackName = string.IsNullOrWhiteSpace(existing?.TrackName) ? title : existing.TrackName,
                    Artist = existing?.Artist ?? "",
                    Composer = existing?.Composer ?? "",
                    Url = string.IsNullOrWhiteSpace(existing?.Url) ? request.Url : existing.Url,
                    IsOffline = true, LocalPath = result.FilePath, Format = result.Format,
                    FullLyrics = existing?.FullLyrics ?? "",
                    Cues = existing?.Cues.Select(cue => new LyricCue { Seconds = cue.Seconds, Text = cue.Text }).ToList() ?? new(),
                    SavedAt = DateTime.Now, OfflineExpiresAt = expires
                };
                folder.Videos.Add(inFolder);
            }
            else
            {
                inFolder.OfflineExpiresAt = expires;
                if (string.IsNullOrWhiteSpace(inFolder.Url)) inFolder.Url = request.Url;
            }
            request.OfflineLocalPath = result.FilePath;
            request.OfflineVideoId = videoId;
            request.OfflineExpiresAt = expires;
            request.OfflineFormat = result.Format;
            if (!SaveLibrary()) throw new IOException("A Biblioteca não confirmou a gravação no disco. O arquivo de áudio foi preservado na pasta de mídia; tente salvar novamente.");
            SongRequestStore.Save(_requests);
            RefreshFolders(folder.Id);
            StatusText.Text = "Cópia offline validada e associada ao pedido. Validade até " + expires.ToString("dd/MM/yyyy HH:mm") + ".";
            RefreshRequestsWindow();
        }
        catch (Exception ex)
        {
            AppLog.Error("Falha ao preparar áudio offline para pedido", ex);
            System.Windows.MessageBox.Show(_requestsWindow ?? this, "Não foi possível preparar o áudio: " + ex.Message, "Biblioteca Offline", MessageBoxButton.OK, MessageBoxImage.Error);
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
        var requestedVideoId = YouTubeLink.TryGetVideoId(request.Url) ?? "";
        var offlineMatch = !string.IsNullOrWhiteSpace(request.OfflineLocalPath) && File.Exists(request.OfflineLocalPath) &&
            (!request.OfflineExpiresAt.HasValue || request.OfflineExpiresAt.Value > DateTime.Now)
            ? _library.SelectMany(folder => folder.Videos).FirstOrDefault(video =>
                video.IsOffline && video.LocalPath == request.OfflineLocalPath &&
                (requestedVideoId.Length == 0 || video.VideoId == requestedVideoId))
            : null;
        // A request without its own offline file must never be matched to another recording by title alone.
        if (offlineMatch is not null)
        {
            if ((!string.IsNullOrWhiteSpace(_nextVideoUrl) || _nextOfflineVideo is not null) &&
                System.Windows.MessageBox.Show(_requestsWindow ?? this,
                    "Já existe uma música preparada como próxima. Substituir por esta cópia offline?",
                    "Lyravoxen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _nextVideoUrl = null;
            _nextOfflineVideo = offlineMatch;
            SaveNextVideoPreference();
            NextLyricsText.Text = "";
            NextLyricsText.Visibility = Visibility.Collapsed;
            NextTrackText.Text = "Próxima faixa offline preparada: " + (offlineMatch.TrackName.Length > 0 ? offlineMatch.TrackName : offlineMatch.Title) +
                                 ". Será reproduzida localmente, sem internet, quando você tocar em Tocar próxima.";
            PlayNextButton.IsEnabled = true;
            StatusText.Text = "Pedido associado a uma cópia offline válida. A música atual continua tocando.";
            RemoveRequest(request);
            return;
        }

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
        RefreshNextLyricsPreview();
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

    private void ImportRequestDialog()
    {
        var box = new System.Windows.Controls.TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Height = 240,
            Margin = new Thickness(0, 6, 0, 8)
        };
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                var clip = System.Windows.Clipboard.GetText();
                if (SongRequestStore.LooksLikeRequest(clip)) box.Text = clip;
            }
        }
        catch { /* sem acesso à área de transferência: o campo fica vazio */ }

        var import = new System.Windows.Controls.Button { Content = "Importar", Padding = new Thickness(18, 6, 18, 6), HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(RequestText("Cole aqui a mensagem do WhatsApp — de uma pessoa, várias mensagens ou o trecho do grupo (do formulário ou texto com links):", 13));
        panel.Children.Add(box);
        panel.Children.Add(import);
        var dialog = new Window
        {
            Title = "Importar pedido",
            Owner = _requestsWindow ?? this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 540,
            SizeToContent = SizeToContent.Height,
            Background = Hex("#F5F7FA"),
            Content = panel
        };
        import.Click += (_, _) =>
        {
            var (added, message) = ImportMessageText(box.Text);
            if (added > 0)
            {
                StatusText.Text = "📥 Pedido importado: " + added + (added == 1 ? " música." : " músicas.");
                dialog.Close();
            }
            else System.Windows.MessageBox.Show(dialog, message, "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        dialog.Show();
        box.Focus();
    }

    private void ImportChatFile()
    {
        var open = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Conversa exportada do WhatsApp (sem mídia)",
            Filter = "Conversa do WhatsApp (*.txt)|*.txt|Todos os arquivos (*.*)|*.*"
        };
        if (open.ShowDialog(_requestsWindow ?? this) != true) return;
        try
        {
            var text = System.IO.File.ReadAllText(open.FileName, System.Text.Encoding.UTF8);
            var (added, message) = ImportMessageText(text);
            StatusText.Text = added > 0 ? "📥 Conversa importada: " + added + (added == 1 ? " música." : " músicas.") : message;
            System.Windows.MessageBox.Show(_requestsWindow ?? this,
                added > 0
                    ? added + (added == 1 ? " música entrou" : " músicas entraram") + " na lista de espera (lidas as últimas 40 mensagens com pedidos; links já recebidos nos últimos 2 dias são ignorados)."
                    : message,
                "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível ler a conversa: " + ex.Message; }
    }

    private void WhatsAppSettingsDialog()
    {
        System.Windows.Controls.TextBox Field(string value) => new() { Text = value, Margin = new Thickness(0, 4, 0, 10), FontSize = 14 };
        var number = Field(_whatsApp.Number);
        var groupName = Field(_whatsApp.GroupName);
        var groupLink = Field(_whatsApp.GroupLink);
        var watch = new System.Windows.Controls.CheckBox
        {
            Content = "Importar sozinho os pedidos copiados do WhatsApp Web/Desktop",
            IsChecked = _whatsApp.WatchClipboard,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var status = RequestText("", 12, false, "#B3261E");

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(RequestText("WhatsApp: número e/ou grupo", 18, true));
        panel.Children.Add(RequestText("Seu número, com país e DDD (ex.: 5582999999999). Deixe em branco se só for usar o grupo:", 12));
        panel.Children.Add(number);
        panel.Children.Add(RequestText("Nome do grupo (aparece no formulário):", 12));
        panel.Children.Add(groupName);
        panel.Children.Add(RequestText("Link de convite do grupo (no WhatsApp: Dados do grupo → Convidar via link). Opcional, só serve para abrir o grupo:", 12));
        panel.Children.Add(groupLink);
        panel.Children.Add(watch);
        panel.Children.Add(status);

        var dialog = new Window
        {
            Title = "WhatsApp — número e grupo",
            Owner = _requestsWindow ?? this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 560,
            SizeToContent = SizeToContent.Height,
            Background = Hex("#F5F7FA"),
            Content = panel
        };

        bool Apply()
        {
            var link = WhatsAppSettings.NormalizeGroupLink(groupLink.Text);
            if (!string.IsNullOrWhiteSpace(groupLink.Text) && link.Length == 0)
            {
                status.Text = "Esse link não é de convite de grupo. Deve ser parecido com https://chat.whatsapp.com/AbCdEfGhIjK";
                return false;
            }
            _whatsApp.Number = WhatsAppSettings.DigitsOnly(number.Text);
            _whatsApp.GroupName = WhatsAppSettings.SafeName(groupName.Text);
            _whatsApp.GroupLink = link;
            _whatsApp.WatchClipboard = watch.IsChecked == true;
            _whatsApp.Save();
            RefreshRequestsWindow();
            return true;
        }

        var buttons = new System.Windows.Controls.WrapPanel();
        buttons.Children.Add(RequestButton("Salvar", () =>
        {
            if (Apply()) { StatusText.Text = "Configuração do WhatsApp salva."; dialog.Close(); }
        }));
        buttons.Children.Add(RequestButton("📄 Gerar formulário (.html)", () =>
        {
            if (!Apply()) return;
            var save = new Microsoft.Win32.SaveFileDialog { FileName = "Pedido-de-musicas.html", Filter = "Página HTML (*.html)|*.html" };
            if (save.ShowDialog(dialog) != true) return;
            try
            {
                System.IO.File.WriteAllText(save.FileName, OfflineRequestForm.Build(_whatsApp, _whatsApp.SendUrl), new System.Text.UTF8Encoding(false));
                StatusText.Text = "Formulário salvo em " + save.FileName + ". Envie esse arquivo pelo WhatsApp.";
                status.Foreground = Hex("#1A6B3A");
                status.Text = "Salvo. No celular, se o arquivo abrir num visualizador, use ⋮ → Abrir com → Chrome. O formulário simples funciona mesmo sem isso.";
            }
            catch (Exception ex) { status.Text = "Não foi possível salvar o formulário: " + ex.Message; }
        }));
        buttons.Children.Add(RequestButton("💬 Copiar texto de convite para o grupo", () =>
        {
            try
            {
                System.Windows.Clipboard.SetText(OfflineRequestForm.GroupInvitationText());
                status.Foreground = Hex("#1A6B3A");
                status.Text = "Copiado. Cole no grupo: as pessoas respondem ali e você copia as respostas para o Lyravoxen importar.";
            }
            catch (Exception ex) { status.Text = "Não foi possível copiar: " + ex.Message; }
        }));
        panel.Children.Add(buttons);
        dialog.Show();
    }

    /// <summary>Usado pelo celular: prepara o pedido mais antigo com link do YouTube como próxima música, sem janelas no PC.</summary>
    private string PrepareOldestRequestForRemote(bool replace)
    {
        var request = _requests.OrderBy(r => r.ReceivedAt).FirstOrDefault(r => YouTubeLink.TryGetVideoId(r.Url) is not null);
        if (request is null) return _requests.Count == 0 ? "Nenhum pedido na lista." : "Os pedidos da lista não têm link de vídeo do YouTube.";
        return RequestPickForRemote(request.Id + "|" + (replace ? "1" : "0"));
    }

    private void CloudSettingsDialog()
    {
        System.Windows.Controls.TextBox Field(string value) => new() { Text = value, Margin = new Thickness(0, 4, 0, 10), FontSize = 14 };
        var url = Field(_whatsApp.CloudUrl);
        var token = Field(_whatsApp.CloudToken.Length > 0 ? _whatsApp.CloudToken : WhatsAppSettings.NewToken());
        var auto = new System.Windows.Controls.CheckBox { Content = "Buscar pedidos da internet sozinho (a cada 20 s)", IsChecked = _whatsApp.CloudAuto, Margin = new Thickness(0, 0, 0, 10) };
        var status = RequestText("", 12, false, "#B3261E");

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(RequestText("☁ Pedidos pela internet", 18, true));
        panel.Children.Add(RequestText(
            "Um serviço gratuito (Cloudflare) guarda os pedidos enviados de qualquer rede até o Lyravoxen buscá-los. " +
            "Faça uma vez: 1) clique em \"Gerar código do serviço\" e siga o LEIA-ME que ele cria junto; 2) cole abaixo o endereço que o Cloudflare der; 3) Testar conexão.", 12));
        panel.Children.Add(RequestText("Endereço do serviço (ex.: https://lyravoxen-pedidos.seunome.workers.dev):", 12));
        panel.Children.Add(url);
        panel.Children.Add(RequestText("Senha (só o Lyravoxen e o serviço conhecem; não envie a ninguém):", 12));
        panel.Children.Add(token);
        panel.Children.Add(auto);
        panel.Children.Add(status);

        var dialog = new Window
        {
            Title = "Pedidos pela internet",
            Owner = _requestsWindow ?? this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 600,
            SizeToContent = SizeToContent.Height,
            Background = Hex("#F5F7FA"),
            Content = panel
        };

        bool Apply()
        {
            var normalized = WhatsAppSettings.NormalizeCloudUrl(url.Text);
            if (!string.IsNullOrWhiteSpace(url.Text) && normalized.Length == 0)
            {
                status.Foreground = Hex("#B3261E");
                status.Text = "Endereço inválido. Deve começar com https:// e ser o endereço do serviço (termina em .workers.dev).";
                return false;
            }
            var newToken = WhatsAppSettings.SafeToken(token.Text);
            if (newToken.Length < 12)
            {
                status.Foreground = Hex("#B3261E");
                status.Text = "A senha precisa ter pelo menos 12 letras/números (use \"Nova senha\").";
                return false;
            }
            if (normalized != _whatsApp.CloudUrl || newToken != _whatsApp.CloudToken) _cloudVersion = "";
            _whatsApp.CloudUrl = normalized;
            _whatsApp.CloudToken = newToken;
            _whatsApp.CloudAuto = auto.IsChecked == true;
            _whatsApp.Save();
            RefreshRequestsWindow();
            return true;
        }

        var buttons = new System.Windows.Controls.WrapPanel();
        buttons.Children.Add(RequestButton("Salvar", () =>
        {
            if (!Apply()) return;
            StatusText.Text = "Pedidos pela internet: configuração salva.";
            dialog.Close();
        }));
        buttons.Children.Add(RequestButton("🧪 Testar conexão", async () =>
        {
            if (!Apply()) return;
            if (!CloudConfigured) { status.Foreground = Hex("#B3261E"); status.Text = "Cole o endereço do serviço primeiro."; return; }
            status.Foreground = Hex("#4A5A6B");
            status.Text = "Testando…";
            var (ok, message) = await CloudRequestClient.PingAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken);
            status.Foreground = Hex(ok ? "#1A6B3A" : "#B3261E");
            status.Text = ok ? "✅ Conectado. Os pedidos enviados de qualquer rede chegarão aqui." : message;
            if (ok) _ = CloudTickAsync(force: true);
        }));
        buttons.Children.Add(RequestButton("📄 Gerar código do serviço (worker.js)", () =>
        {
            if (!Apply()) return;
            var save = new Microsoft.Win32.SaveFileDialog { FileName = "worker.js", Filter = "Código do serviço (*.js)|*.js" };
            if (save.ShowDialog(dialog) != true) return;
            try
            {
                var folder = System.IO.Path.GetDirectoryName(save.FileName)!;
                System.IO.File.WriteAllText(save.FileName, CloudWorker.Build(_whatsApp), new System.Text.UTF8Encoding(false));
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "LEIA-ME-nuvem.txt"),
                    CloudWorker.Readme(System.IO.Path.GetFileName(save.FileName)), new System.Text.UTF8Encoding(true));
                status.Foreground = Hex("#1A6B3A");
                status.Text = "Salvei " + System.IO.Path.GetFileName(save.FileName) + " e LEIA-ME-nuvem.txt em " + folder + ". Siga o LEIA-ME.";
            }
            catch (Exception ex) { status.Foreground = Hex("#B3261E"); status.Text = "Não foi possível salvar: " + ex.Message; }
        }));
        buttons.Children.Add(RequestButton("🔑 Nova senha", () => { token.Text = WhatsAppSettings.NewToken(); status.Foreground = Hex("#4A5A6B"); status.Text = "Nova senha gerada. Salve e coloque a mesma no Cloudflare (ADMIN_TOKEN)."; }));
        buttons.Children.Add(RequestButton("Copiar senha", () =>
        {
            try { System.Windows.Clipboard.SetText(token.Text); status.Foreground = Hex("#1A6B3A"); status.Text = "Senha copiada."; }
            catch (Exception ex) { status.Foreground = Hex("#B3261E"); status.Text = "Não foi possível copiar: " + ex.Message; }
        }));
        panel.Children.Add(buttons);
        dialog.Show();
    }
}
