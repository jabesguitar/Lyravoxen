using System.IO;
using System.Windows;
using System.Windows.Threading;
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
        try { return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(BlockedSendersFile)) ?? new(StringComparer.OrdinalIgnoreCase); }
        catch { return new(StringComparer.OrdinalIgnoreCase); }
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
        ip = ip.Trim();
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
    private async void UnblockSender(string ip)
    {
        _blockedRequestSenders.Remove(ip); SaveBlockedRequestSenders();
        if (CloudConfigured) await CloudRequestClient.SetBlockedAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken, ip, "", false);
        StatusText.Text = "Bloqueio removido: " + ip; RefreshRequestsWindow();
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
        _requestsPanel.Children.Add(RequestButton("🛡 Gerenciar IPs bloqueados", ManageBlockedSenders));
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

        // Link dos pedidos pela internet (qualquer rede) com QR code para a pessoa apontar a câmera.
        var cloudLink = _whatsApp.OnlineRequestLink;
        _requestsPanel.Children.Add(RequestText("Pedidos pela internet (funciona em qualquer rede) — aponte a câmera para o QR code:", 13, true));
        var qrImage = MakeQrImage(cloudLink);
        if (qrImage is not null)
            _requestsPanel.Children.Add(new System.Windows.Controls.Image { Source = qrImage, Width = 190, Height = 190, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 4) });
        _requestsPanel.Children.Add(new System.Windows.Controls.TextBox { Text = cloudLink, IsReadOnly = true, FontSize = 15, Margin = new Thickness(0, 0, 0, 4) });
        _requestsPanel.Children.Add(RequestButton("Copiar link da internet", () =>
        {
            try { System.Windows.Clipboard.SetText(cloudLink); StatusText.Text = "Link copiado: " + cloudLink; }
            catch (Exception ex) { StatusText.Text = "Não foi possível copiar: " + ex.Message; }
        }));

        var groupInfo = !string.IsNullOrEmpty(_whatsApp.GroupLink) || !string.IsNullOrEmpty(_whatsApp.GroupName)
            ? "Pelo WhatsApp — grupo: " + (string.IsNullOrEmpty(_whatsApp.GroupName) ? "(sem nome)" : _whatsApp.GroupName)
            : "Pelo WhatsApp (sem rede local):";
        _requestsPanel.Children.Add(RequestText(groupInfo, 13, true));
        _requestsPanel.Children.Add(RequestText(
            "Num grupo: copie as mensagens de pedido no WhatsApp Web/Desktop (Ctrl+C) e elas entram aqui sozinhas" +
            (_whatsApp.WatchClipboard ? "." : " (importação automática DESLIGADA nas configurações)."), 12, false, "#4A5A6B"));
        var offlineButtons = new System.Windows.Controls.WrapPanel();
        offlineButtons.Children.Add(RequestButton("☁ Pedidos pela internet (qualquer rede)", CloudSettingsDialog));
        offlineButtons.Children.Add(RequestButton("⚙ WhatsApp: número e grupo / formulário", WhatsAppSettingsDialog));
        offlineButtons.Children.Add(RequestButton("📋 Importar pedido (colar mensagem)", ImportRequestDialog));
        offlineButtons.Children.Add(RequestButton("🗂 Importar conversa exportada (.txt)", ImportChatFile));
        _requestsPanel.Children.Add(offlineButtons);
        foreach (var blocked in _blockedRequestSenders.ToList())
        {
            var row = new System.Windows.Controls.WrapPanel();
            row.Children.Add(RequestText("Bloqueado: " + blocked.Value + " · " + blocked.Key, 12, false, "#A83D58"));
            row.Children.Add(RequestButton("Desbloquear", () => UnblockSender(blocked.Key)));
            _requestsPanel.Children.Add(row);
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
                                             " · " + request.ReceivedAt.ToString("dd/MM HH:mm") +
                                             (string.IsNullOrWhiteSpace(request.SenderIp) ? "" : " · IP " + request.SenderIp), 12, false, "#4A5A6B"));
            if (!string.IsNullOrWhiteSpace(request.SenderIp))
                content.Children.Add(RequestButton("Bloquear este IP", () => { _blockedRequestSenders[request.SenderIp] = request.Group; SaveBlockedRequestSenders(); if (CloudConfigured) _ = CloudRequestClient.SetBlockedAsync(_whatsApp.CloudUrl, _whatsApp.CloudToken, request.SenderIp, request.Group, true); StatusText.Text = "IP bloqueado: " + request.SenderIp; RefreshRequestsWindow(); }));
            content.Children.Add(RequestText(request.Url, 12, false, "#2D6A9F"));
            content.Children.Add(RequestText(RequestLyricsLine(request), 12, false, "#1A2430"));

            var buttons = new System.Windows.Controls.WrapPanel();
            var item = request;
            buttons.Children.Add(RequestButton("⏭ Preparar como próxima", () => PrepareRequestAsNext(item)));
            buttons.Children.Add(RequestButton("⬇ Baixar offline", () => ImportOfflineForRequest(item)));
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


    /// <summary>Importa um arquivo autorizado e o associa ao pedido e ao ID exato do vídeo quando disponível.</summary>
    private void ImportOfflineForRequest(SongRequest request)
    {
        var retention = PromptForText("Validade da cópia offline", "Por quantos dias manter esta cópia local? (5, 7, 14 ou 30)", "30");
        if (retention is null) return;
        if (!int.TryParse(retention.Trim(), out var days) || days is not (5 or 7 or 14 or 30))
        {
            System.Windows.MessageBox.Show(_requestsWindow ?? this, "Escolha 5, 7, 14 ou 30 dias.", "Validade offline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Selecione um áudio autorizado para este pedido",
            Filter = "Áudios MP3 ou WAV (*.mp3;*.wav)|*.mp3;*.wav",
            Multiselect = false
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        try
        {
            var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            if (ext is not ".mp3" and not ".wav") throw new InvalidOperationException("Escolha um arquivo MP3 ou WAV.");
            var mediaFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lyravoxen", "Media");
            Directory.CreateDirectory(mediaFolder);
            var target = Path.Combine(mediaFolder, Guid.NewGuid().ToString("N") + ext);
            File.Copy(dialog.FileName, target, false);

            var videoId = YouTubeLink.TryGetVideoId(request.Url) ?? "";
            var expires = DateTime.Now.AddDays(days);
            var title = string.IsNullOrWhiteSpace(request.Name) ? Path.GetFileNameWithoutExtension(dialog.FileName) : request.Name;
            var folder = _library.FirstOrDefault(x => x.Name == "Pedidos offline");
            if (folder is null) { folder = new LibraryFolder { Name = "Pedidos offline" }; _library.Add(folder); }
            folder.Videos.Add(new LibraryVideo
            {
                VideoId = videoId, Title = title, TrackName = title, Url = request.Url,
                IsOffline = true, LocalPath = target, Format = ext.TrimStart('.'),
                SavedAt = DateTime.Now, OfflineExpiresAt = expires
            });
            request.OfflineLocalPath = target;
            request.OfflineVideoId = videoId;
            request.OfflineExpiresAt = expires;
            request.OfflineFormat = ext.TrimStart('.');
            SaveLibrary();
            SongRequestStore.Save(_requests);
            RefreshFolders();
            StatusText.Text = "Cópia offline preparada para o pedido. Validade até " + expires.ToString("dd/MM/yyyy HH:mm") + ".";
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
        var requestedName = LyricsPreviewService.Normalize(request.Name);
        var offlineMatch = requestedName.Length == 0 ? null : _library.SelectMany(folder => folder.Videos)
            .FirstOrDefault(video => video.IsOffline &&
                !string.IsNullOrWhiteSpace(video.LocalPath) && File.Exists(video.LocalPath) &&
                (!video.OfflineExpiresAt.HasValue || video.OfflineExpiresAt.Value > DateTime.Now) &&
                (LyricsPreviewService.Normalize(video.TrackName) == requestedName || LyricsPreviewService.Normalize(video.Title) == requestedName));
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
