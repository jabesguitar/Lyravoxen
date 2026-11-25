using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Lyravoxen.Services;

/// <summary>Servidor HTTP mínimo na rede local: o celular abre uma página e controla a aba Bíblia (protegido por PIN).</summary>
internal sealed class RemoteControlServer : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private TcpListener? _listener;

    public RemoteControlServer(string pin) => Pin = pin;

    public string Pin { get; set; }
    public int Port { get; private set; }
    public Func<string, string, Task<string>>? Handler { get; set; }
    public Func<string, string, Task<(bool Ok, string Message)>>? RequestHandler { get; set; }
    /// <summary>Transmite o som do PC ao celular (PCM 16 bits, mono, 24 kHz) até a conexão fechar.</summary>
    public Func<Stream, CancellationToken, Task>? AudioStreamer { get; set; }
    /// <summary>Gera o QR code (PNG) de um texto/endereço; usado pela página do celular.</summary>
    public Func<string, byte[]?>? QrPng { get; set; }
    /// <summary>Link da página de pedidos pela internet, mostrado no celular junto do QR code.</summary>
    public Func<string>? OnlineLink { get; set; }

    private readonly Dictionary<string, Queue<DateTime>> _submissions = new();

    /// <summary>No máximo 6 envios por minuto por aparelho.</summary>
    private bool AllowSubmission(string ip)
    {
        lock (_submissions)
        {
            if (!_submissions.TryGetValue(ip, out var times)) _submissions[ip] = times = new Queue<DateTime>();
            var now = DateTime.UtcNow;
            while (times.Count > 0 && now - times.Peek() > TimeSpan.FromMinutes(1)) times.Dequeue();
            if (times.Count >= 6) return false;
            times.Enqueue(now);
            return true;
        }
    }

    public void Start(int firstPort = 5280)
    {
        for (var port = firstPort; port < firstPort + 10; port++)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                _listener = listener;
                Port = port;
                break;
            }
            catch (SocketException) { }
        }
        if (_listener is null) throw new InvalidOperationException("Nenhuma porta livre para o controle remoto.");
        _ = Task.Run(AcceptLoopAsync);
    }

    public IEnumerable<string> LocalUrls() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => "http://" + a.Address + ":" + Port + "/")
            .Distinct();

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleAsync(client));
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                AppLog.Error("Controle remoto: falha ao aceitar conexão", ex);
                await Task.Delay(500);
            }
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(45)); // a verificação de letra pode levar alguns segundos
                var stream = client.GetStream();
                var ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";

                var buffer = new byte[65536]; // letras coladas no celular vão na URL
                var length = 0;
                while (length < buffer.Length)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(length), timeout.Token);
                    if (read == 0) break;
                    length += read;
                    if (Encoding.ASCII.GetString(buffer, 0, length).Contains("\r\n\r\n", StringComparison.Ordinal)) break;
                }

                var request = Encoding.ASCII.GetString(buffer, 0, length);
                var firstLine = request.Split("\r\n")[0].Split(' ');
                if (firstLine.Length < 2 || firstLine[0] != "GET")
                {
                    await WriteAsync(stream, 405, "text/plain; charset=utf-8", "Método não permitido", timeout.Token);
                    return;
                }

                var target = firstLine[1];
                var queryIndex = target.IndexOf('?');
                var path = queryIndex < 0 ? target : target[..queryIndex];
                var query = ParseQuery(queryIndex < 0 ? "" : target[(queryIndex + 1)..]);

                if (path == "/")
                {
                    await WriteAsync(stream, 200, "text/html; charset=utf-8", PageHtml, timeout.Token);
                    return;
                }

                if (path == "/brand.png")
                {
                    var brandPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Lyravoxen_Icone_App_1024.png");
                    if (!File.Exists(brandPath))
                    {
                        await WriteAsync(stream, 404, "text/plain; charset=utf-8", "Logomarca indisponível", timeout.Token);
                        return;
                    }
                    var brandBytes = await File.ReadAllBytesAsync(brandPath, timeout.Token);
                    var brandHead = "HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nCache-Control: no-store\r\nContent-Length: " + brandBytes.Length + "\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(brandHead), timeout.Token);
                    await stream.WriteAsync(brandBytes, timeout.Token);
                    return;
                }

                if (path == "/pedido")
                {
                    await WriteAsync(stream, 200, "text/html; charset=utf-8", RequestPageHtml, timeout.Token);
                    return;
                }

                if (path == "/pedido/enviar")
                {
                    if (!AllowSubmission(ip))
                    {
                        await WriteAsync(stream, 429, "application/json; charset=utf-8",
                            JsonSerializer.Serialize(new { ok = false, status = "Muitos envios seguidos. Aguarde um minuto." }), timeout.Token);
                        return;
                    }
                    query.TryGetValue("d", out var payload);
                    var outcome = RequestHandler is null
                        ? (Ok: false, Message: "Pedidos indisponíveis no momento.")
                        : await RequestHandler(payload ?? "", ip);
                    await WriteAsync(stream, 200, "application/json; charset=utf-8",
                        JsonSerializer.Serialize(new { ok = outcome.Ok, status = outcome.Message }), timeout.Token);
                    return;
                }

                if (path == "/qr.png")
                {
                    if (!query.TryGetValue("pin", out var qrPin) || qrPin != Pin || QrPng is null)
                    {
                        await WriteAsync(stream, 401, "text/plain; charset=utf-8", "PIN incorreto", timeout.Token);
                        return;
                    }
                    var png = QrPng(OnlineLink?.Invoke() ?? "");
                    if (png is null)
                    {
                        await WriteAsync(stream, 500, "text/plain; charset=utf-8", "Sem QR", timeout.Token);
                        return;
                    }
                    var qrHead = "HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nCache-Control: no-store\r\nContent-Length: " + png.Length + "\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(qrHead), timeout.Token);
                    await stream.WriteAsync(png, timeout.Token);
                    return;
                }
                if (path == "/audio")
                {
                    if (!query.TryGetValue("pin", out var audioPin) || audioPin != Pin)
                    {
                        await WriteAsync(stream, 401, "application/json; charset=utf-8",
                            JsonSerializer.Serialize(new { ok = false, error = "PIN incorreto." }), timeout.Token);
                        return;
                    }
                    if (AudioStreamer is null)
                    {
                        await WriteAsync(stream, 404, "text/plain; charset=utf-8", "Áudio indisponível", timeout.Token);
                        return;
                    }
                    var head = "HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nCache-Control: no-store\r\nX-Audio-Format: s16le;rate=24000;channels=1\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(head), _cts.Token);
                    await AudioStreamer(stream, _cts.Token); // fluxo longo: não usa o limite de 45 s das outras rotas
                    return;
                }
                if (path == "/api")
                {
                    if (!query.TryGetValue("pin", out var pin) || pin != Pin)
                    {
                        await WriteAsync(stream, 401, "application/json; charset=utf-8",
                            JsonSerializer.Serialize(new { ok = false, error = "PIN incorreto." }), timeout.Token);
                        return;
                    }

                    query.TryGetValue("c", out var command);
                    query.TryGetValue("a", out var argument);
                    var status = Handler is null ? "Controle indisponível." : await Handler(command ?? "state", argument ?? "");
                    await WriteAsync(stream, 200, "application/json; charset=utf-8",
                        JsonSerializer.Serialize(new { ok = true, status }), timeout.Token);
                    return;
                }

                await WriteAsync(stream, 404, "text/plain; charset=utf-8", "Não encontrado", timeout.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AppLog.Error("Controle remoto: erro numa requisição", ex);
            }
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var key = separator < 0 ? part : part[..separator];
            var value = separator < 0 ? "" : part[(separator + 1)..];
            result[Uri.UnescapeDataString(key.Replace('+', ' '))] = Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        return result;
    }

    private static async Task WriteAsync(NetworkStream stream, int code, string contentType, string body, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var reason = code switch { 200 => "OK", 401 => "Unauthorized", 404 => "Not Found", 405 => "Method Not Allowed", 429 => "Too Many Requests", _ => "Error" };
        var head = "HTTP/1.1 " + code + " " + reason + "\r\nContent-Type: " + contentType +
                   "\r\nContent-Length: " + bytes.Length + "\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head), token);
        await stream.WriteAsync(bytes, token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener?.Stop(); } catch { }
        _cts.Dispose();
    }

    private const string PageHtml = """
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1">
<title>Lyravoxen</title>
<style> 
:root{color-scheme:dark;--navy:#06152f;--panel:#0b2450;--panel2:#0a1d3d;--cyan:#00d9f5;--cyan2:#24bfe5;--violet:#873cff;--gold:#f3c928;--text:#f2f6fb;--muted:#a9bfdc;--line:#214c79;--danger:#a83d58}
*{box-sizing:border-box}
body{margin:0;min-height:100vh;font-family:system-ui,-apple-system,"Segoe UI",sans-serif;background:radial-gradient(ellipse at 50% -15%,#123c72 0%,#081d40 38%,var(--navy) 78%);color:var(--text);padding:16px 14px 130px}
.brandbar{position:relative;overflow:hidden;max-width:760px;margin:0 auto 18px;display:flex;align-items:center;gap:12px;padding:18px 20px;min-height:94px;border:1px solid rgba(0,217,245,.28);border-radius:18px;background:linear-gradient(135deg,rgba(10,39,81,.96),rgba(6,21,47,.94));box-shadow:0 12px 34px rgba(0,0,0,.24)}
.brandbar:before{content:"";position:absolute;right:14px;top:50%;transform:translateY(-50%);width:190px;height:190px;background:url('/brand.png') center/contain no-repeat;opacity:.105;pointer-events:none}
.brandbar>*{position:relative;z-index:1}
body:before{content:"";position:fixed;right:-90px;bottom:-110px;width:min(72vw,520px);height:min(72vw,520px);background:url('/brand.png') center/contain no-repeat;opacity:.035;pointer-events:none;z-index:0}
body>*{position:relative;z-index:1}
.brandname{font-size:20px;font-weight:800;letter-spacing:.035em;line-height:1.1;color:#fff}
.brandname span{color:var(--cyan)}
.brandtag{display:block;margin-top:5px;font-size:10px;letter-spacing:.18em;font-weight:700;color:var(--muted)}
.networktag{margin-left:auto;align-self:center;padding:6px 9px;border:1px solid rgba(0,217,245,.3);border-radius:999px;color:var(--cyan);font-size:9px;font-weight:800;letter-spacing:.12em;white-space:nowrap}
input{width:100%;font-size:17px;padding:13px 14px;border-radius:12px;border:1px solid #28517d;background:#071a38;color:var(--text);margin:6px 0;outline:none}
input:focus,textarea:focus{border-color:var(--cyan);box-shadow:0 0 0 3px rgba(0,217,245,.12)}
input::placeholder,textarea::placeholder{color:#829bbd}
button{font:600 15px system-ui,sans-serif;padding:13px 14px;border:1px solid #28517d;border-radius:12px;background:linear-gradient(180deg,#153b69,#102d55);color:var(--text);width:100%;min-height:46px;box-shadow:0 4px 12px rgba(0,0,0,.16);transition:filter .15s,border-color .15s,transform .15s}
button:active{transform:translateY(1px);filter:brightness(1.12)}
button.main{background:linear-gradient(135deg,#00bcd9,#0879bd);border-color:#31dff5;color:#04172d;font-weight:800}
button.warn{background:linear-gradient(135deg,#8f3150,#61243f);border-color:#b44b68;color:#fff}
button.small{font-size:13px;padding:9px 10px;width:auto;min-height:36px}
.row{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:10px 0}
.row3{display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px;margin:10px 0}
body{padding-bottom:120px}
#st{position:fixed;left:10px;right:10px;bottom:10px;z-index:5;font-size:14px;color:var(--text);background:rgba(7,26,56,.97);border:1px solid rgba(0,217,245,.35);border-radius:14px;padding:12px 14px;max-height:38vh;overflow:auto;white-space:pre-wrap;box-shadow:0 8px 28px rgba(0,0,0,.45)}
#st:empty{display:none}
.card{background:linear-gradient(145deg,rgba(12,39,79,.96),rgba(7,26,56,.97));border:1px solid rgba(49,113,164,.55);border-radius:17px;padding:13px;margin:12px 0;box-shadow:0 8px 24px rgba(0,0,0,.17)}
.card h2{font-size:13px;margin:0 0 10px;color:#8feaff;font-weight:750;letter-spacing:.035em}
.slide{background:#020a18;border:1px solid #28517d;border-radius:12px;display:flex;align-items:center;justify-content:center;text-align:center;padding:12px;color:#fff;line-height:1.25;white-space:pre-wrap;overflow:hidden}
.slide.big{aspect-ratio:16/9;font-size:22px;font-weight:700}
.slide.mini{min-height:96px;max-height:150px;font-size:15px;color:#dfeeff}
.dim{font-size:13px;color:var(--muted);margin:6px 0}
textarea{width:100%;font-size:16px;padding:12px;border-radius:12px;border:1px solid #28517d;background:#071a38;color:var(--text);margin:5px 0}
.tapbig{font-size:30px;padding:38px 0;background:linear-gradient(135deg,#00cfe9,#0879bd);border-color:#46e9f7;color:#04172d;margin:8px 0;font-weight:850}
.gap{margin-top:10px}
.vubox{height:14px;background:#06152f;border:1px solid #28517d;border-radius:8px;overflow:hidden;margin:8px 0}
#vu{height:100%;width:0;background:linear-gradient(90deg,#00d9f5,#287cff,#873cff);transition:width .25s}
.mini-head{display:flex;gap:10px;align-items:center;margin-bottom:8px}
.mini-head img{width:96px;height:54px;object-fit:cover;border-radius:8px;background:#020a18}
.mini-head b{font-size:16px}
.suggest{background:rgba(0,174,210,.09);border:1px solid rgba(0,217,245,.32);border-radius:13px;padding:10px;margin:8px 0}
.item{display:flex;gap:8px;align-items:center;justify-content:space-between;border-top:1px solid rgba(49,113,164,.35);padding:9px 0}
.item:first-child{border-top:0}
.item .t{flex:1;font-size:15px}
.item .t small{display:block;color:var(--muted);font-size:12px}
.tabs{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:0 0 8px}
.tabs button{font-weight:750}
#pinbox{max-width:760px;margin:0 auto 14px;padding:16px;border:1px solid rgba(0,217,245,.28);border-radius:17px;background:rgba(8,29,62,.9)}
#pinbox:before{content:"ACESSO PROTEGIDO";display:block;color:var(--cyan);font-size:10px;letter-spacing:.18em;font-weight:800;margin-bottom:7px}
#app{max-width:760px;margin:0 auto}
@media(min-width:760px){body{padding-top:24px}.card{padding:16px}.brandbar{margin-bottom:22px}}
@media(max-width:390px){.brandbar{gap:9px;padding:12px;min-height:76px}.brandbar:before{width:130px;height:130px;right:2px}.brandname{font-size:17px}.networktag{font-size:8px;padding:5px 7px}.row3{gap:6px}.row3 button{font-size:13px;padding:11px 5px}}
</style></head><body>
<header class="brandbar">
 <div><div class="brandname">Lyra<span>voxen</span></div><small class="brandtag">CONTROLE REMOTO</small></div>
 <span class="networktag">REDE LOCAL</span>
</header>
<div id="pinbox"><input id="pin" inputmode="numeric" placeholder="PIN mostrado no computador" autocomplete="off">
<div class="row"><button class="main" onclick="savePin()">Entrar</button></div></div>
<div id="app" style="display:none">
<div class="tabs"><button id="t-bible" class="main" onclick="tab('bible')">📖 Bíblia</button><button id="t-live" onclick="tab('live')">▶ Ao vivo</button></div>
<button class="gap" style="margin-bottom:4px" onclick="showQr()">📲 QR code dos pedidos online</button>

<div id="p-live" style="display:none">
 <div class="card"><h2>📚 Buscar na biblioteca</h2>
  <input id="lq" placeholder="Nome da música ou autor" autocomplete="off" onkeydown="if(event.key==='Enter')libSearch()">
  <button onclick="libSearch()">🔍 Buscar</button>
  <div id="liblist" class="dim"></div>
 </div>
 <div class="card"><h2>🕘 Últimas músicas salvas</h2><div id="recentlist" class="dim">Nenhuma música salva ainda.</div></div>
 <div class="card"><h2>🎬 No telão agora</h2>
  <div class="slide big" id="nowline">Nenhuma música tocando</div>
  <div class="dim" id="nowinfo"></div>
  <div class="dim" id="nownext"></div>
  <div class="row"><button onclick="act('cueprev')">◀ Frase</button><button onclick="act('cuenext')">Frase ▶</button></div>
  <button class="gap" onclick="ask('clearlive','Limpar o telão? A letra só volta quando você tocar em Voltar ao automático.')">🧹 Limpar telão</button>
  <button id="autobtn" class="main gap" style="display:none" onclick="act('auto')">🔄 Voltar ao automático</button>
  <div class="row"><button onclick="act('seek','-10')">⏪ −10 s</button><button onclick="act('seek','10')">+10 s ⏩</button></div>
  <div class="row3"><button onclick="act('vol','-10')">🔉 −</button><button onclick="act('vol','mute')">🔇 Mudo</button><button onclick="act('vol','10')">🔊 +</button></div>
  <div class="row"><button onclick="act('toggle')">⏯ Pausar / continuar</button><button class="warn" onclick="ask('endlive','Encerrar a música e limpar a projeção?')">⏹ Encerrar</button></div>
  <div class="suggest" id="nolyrics" style="display:none">
   <div class="dim" id="nolyricstxt"></div>
   <button class="main" id="b-paste-now" onclick="openPaste('now')">✎ Colar a letra desta música</button>
   <button id="b-fetch-now" class="gap" style="display:none" onclick="fetchText('now')">📝 Buscar o texto automaticamente</button>
   <div class="row"><button id="b-estimate" onclick="act('estimate')">⏱ Estimar tempos</button><button class="main" id="b-tapstart" onclick="act('tapstart')">👆 Marcar por toque</button></div>
  </div>
  <div class="suggest" id="tappanel" style="display:none">
   <div class="dim" id="taptxt"></div>
   <button class="tapbig" id="tapbig" onclick="act('tapmark')">TOQUE</button>
   <div class="row3"><button onclick="act('tapundo')">↩ Desfazer</button><button onclick="act('tapfinish')">✔ Concluir</button><button class="warn" onclick="act('tapcancel')">✖ Cancelar</button></div>
  </div>
 </div>
 <div class="card" id="pastepanel" style="display:none"><h2>✎ Colar a letra</h2>
  <div class="dim" id="pastehint"></div>
  <textarea id="pastebox" rows="7" placeholder="Cole aqui a letra completa (copie no Letras.mus.br, Cifra Club...). Uma linha em branco entre as estrofes."></textarea>
  <div class="row"><button class="main" onclick="sendPaste()">💾 Salvar letra</button><button onclick="closePaste()">✖ Cancelar</button></div>
 </div>

 <div class="card"><h2>🎧 Som do computador</h2>
  <div class="vubox"><div id="vu"></div></div>
  <div class="dim" id="vutxt">Medindo o som do PC…</div>
  <button id="audiobtn" onclick="toggleAudio()">🔈 Ouvir a música do PC: DESLIGADO</button>
 </div>
 <div class="card"><h2>⏭ Próxima música</h2>
  <div id="nextempty" class="dim">Nenhuma preparada. Escolha um pedido ou busque na biblioteca abaixo.</div>
  <div id="nextbody" style="display:none">
   <div class="mini-head"><img id="thumb" alt=""><div><b id="ntitle"></b><div class="dim" id="nstatus"></div></div></div>
   <div class="slide mini" id="nextslide"></div>
   <div class="suggest" id="confirmbox" style="display:none">
    <div id="conftxt" class="dim"></div>
    <div class="row"><button class="main" onclick="act('nextconfirm',undefined,true)">✅ Confirmar letra</button><button class="warn" onclick="act('nextreject',undefined,true)">✖ Não usar</button></div>
   </div>
   <div class="dim" id="rejectedtxt" style="display:none">Sugestão descartada. Corrija os nomes abaixo e verifique de novo, ou crie a letra.</div>
   <button id="createbtn" class="gap" onclick="openPaste('next')">✎ Criar letra (colar o texto)</button>
   <button id="fetchbtn" class="gap" style="display:none" onclick="fetchText('next')">📝 Buscar o texto automaticamente</button>
   <input id="nt" placeholder="Música (use o microfone do teclado)" autocomplete="off">
   <input id="na" placeholder="Autor / intérprete" autocomplete="off">
   <div class="row"><button onclick="swapNames()">⇄ Trocar campos</button><button onclick="fixNames()">✨ Corrigir nomes</button></div>
   <button onclick="nextCheck()">🔎 Verificar com estes nomes</button>
   <div class="row"><button class="main" id="playbtn" onclick="playNext()">⏭ Tocar próxima</button><button onclick="act('reqnext')">📥 Pedido → próxima</button></div>
   <button id="advbtn" class="gap" onclick="act('autoadvance')">⏩ Tocar a próxima sozinha ao terminar: DESLIGADO</button>
  </div>
 </div>

 <div class="card"><h2>🔗 Colar link do YouTube</h2>
  <input id="nl" placeholder="Cole o link do vídeo" inputmode="url" autocapitalize="none" autocomplete="off">
  <div class="row"><button class="main" onclick="useLink(false)">▶ Como próxima</button><button onclick="useLink(true)">➕ Na fila</button></div>
 </div>
 <div class="card" id="queuecard" style="display:none"><h2 id="queuehead">📋 Fila</h2><div id="queuelist"></div>
  <button class="gap" onclick="ask('qclear','Limpar a fila inteira?')">🗑 Limpar fila</button>
 </div>
 <div class="card"><h2 id="reqhead">📥 Pedidos</h2><div id="reqlist" class="dim">Nenhum pedido na lista.</div></div>

</div>

<div id="p-bible">
<input id="ref" placeholder="Ex.: João 3 16 (use o microfone do teclado)" autocomplete="off" autocapitalize="none" onkeydown="if(event.key==='Enter')go()">
<div class="row"><button class="main" onclick="go()">Projetar</button><button class="warn" onclick="cmd('clear')">Limpar telão</button></div>
<div class="row"><button onclick="cmd('prev')">◀ Versículo</button><button onclick="cmd('next')">Versículo ▶</button></div>
<div class="row"><button onclick="cmd('chprev')">◀ Capítulo</button><button onclick="cmd('chnext')">Capítulo ▶</button></div>
</div>
<div id="st"></div></div>
<div id="qrover" style="display:none;position:fixed;left:0;top:0;right:0;bottom:0;background:rgba(0,0,0,.88);z-index:20;text-align:center;padding:6vh 14px 0" onclick="this.style.display='none'">
 <div style="color:#fff;font-size:18px;margin-bottom:12px">Aponte a câmera de outro celular para este QR code</div>
 <img id="qrimg" alt="QR code" style="width:78vw;max-width:420px;background:#fff;padding:12px;border-radius:12px">
 <div id="qrlink" style="color:#9fd9d1;font-size:14px;margin-top:14px;word-break:break-all"></div>
 <div style="color:#8fa1b3;font-size:13px;margin-top:10px">Toque em qualquer lugar para fechar</div>
</div>
<script>
var pin='';try{pin=localStorage.getItem('lyra-pin')||'';}catch(e){}
var curTab='bible',holdUntil=0,lastNextId='',lastNamesRev=-1,live=null,pasteTarget='next',sigs={},aud={on:false,ctx:null,ctl:null,next:0,rate:24000,chunks:0,dropped:0};
function $(id){return document.getElementById(id);}
function el(tag,cls,text){var e=document.createElement(tag);if(cls){e.className=cls;}if(text!==undefined){e.textContent=text;}return e;}
function show(){$('pinbox').style.display=pin?'none':'block';$('app').style.display=pin?'block':'none';}
function savePin(){pin=$('pin').value.trim();if(pin){try{localStorage.setItem('lyra-pin',pin);}catch(e){}show();cmd('state');}}
async function api(c,a){
 var r=await fetch('/api?pin='+encodeURIComponent(pin)+'&c='+c+'&a='+encodeURIComponent(a||''));
 var j=await r.json();
 if(r.status===401){try{localStorage.removeItem('lyra-pin');}catch(e){}pin='';show();}
 return j;
}
async function cmd(c,a,poll){
 try{var j=await api(c,a);if(poll&&Date.now()<holdUntil){return;}$('st').textContent=j.status||j.error||'';}
 catch(e){$('st').textContent='Sem conexão com o computador.';}
}
async function act(c,a,hold){
 if(hold){holdUntil=Date.now()+60000;}
 await cmd(c,a);
 pollLive();
}
function ask(c,q,a){if(confirm(q)){act(c,a);}}
function tab(t){curTab=t;$('p-bible').style.display=t==='bible'?'block':'none';$('p-live').style.display=t==='live'?'block':'none';$('t-bible').className=t==='bible'?'main':'';$('t-live').className=t==='live'?'main':'';if(t==='live'){pollLive();}}
function go(){var v=$('ref').value.trim();if(v){cmd('ref',v);$('ref').select();}}
function nextCheck(){holdUntil=Date.now()+90000;$('st').textContent='Verificando a letra…';cmd('nextcheck',$('nt').value.trim()+'\n'+$('na').value.trim()).then(pollLive);}
function playNext(){
 var q='Tocar a próxima música agora?';
 if(live&&live.next&&live.next.warning){q+='\n\nAtenção: '+live.next.warning+'.';}
 if(live&&live.now&&live.now.state!=='parada'){q+='\n\nA música atual será trocada.';}
 if(confirm(q)){act('playnext');}
}
function updAudioBtn(){var b=$('audiobtn');b.textContent=aud.on?'🔊 Ouvindo o PC: LIGADO (toque para desligar)':'🔈 Ouvir a música do PC: DESLIGADO';b.className=aud.on?'main':'';}
function toggleAudio(){if(aud.on){audioOff();}else{audioOn();}}
function audioOff(){
 aud.on=false;
 try{if(aud.ctl){aud.ctl.abort();}}catch(e){}
 try{if(aud.ctx){aud.ctx.close();}}catch(e){}
 aud.ctx=null;aud.ctl=null;updAudioBtn();
}
async function audioOn(){
 if(aud.on){return;}
 var Ctx=window.AudioContext||window.webkitAudioContext;
 if(!Ctx||!window.fetch){$('st').textContent='Este navegador não toca o áudio ao vivo. Tente o Chrome.';return;}
 aud.ctx=new Ctx();try{await aud.ctx.resume();}catch(e){}
 aud.ctl=new AbortController();aud.on=true;aud.next=0;aud.chunks=0;aud.dropped=0;updAudioBtn();
 var left=new Uint8Array(0),problem='';
 try{
  var r=await fetch('/audio?pin='+encodeURIComponent(pin),{signal:aud.ctl.signal});
  if(!r.ok||!r.body){throw new Error(r.status===401?'PIN incorreto.':'o computador não conseguiu capturar o som.');}
  var reader=r.body.getReader();
  while(aud.on){
   var part=await reader.read();
   if(part.done){break;}
   var data=new Uint8Array(left.length+part.value.length);data.set(left,0);data.set(part.value,left.length);
   var usable=data.length-(data.length%2);left=data.slice(usable);
   if(usable===0){continue;}
   var now=aud.ctx.currentTime;
   if(aud.next>now+1.5){aud.dropped++;continue;} // atraso demais: descarta para voltar ao vivo
   var n=usable/2,buf=aud.ctx.createBuffer(1,n,aud.rate),ch=buf.getChannelData(0),dv=new DataView(data.buffer,data.byteOffset,usable);
   for(var i=0;i<n;i++){ch[i]=dv.getInt16(i*2,true)/32768;}
   var src=aud.ctx.createBufferSource();src.buffer=buf;src.connect(aud.ctx.destination);
   if(aud.next<now+0.05){aud.next=now+0.3;} // folga de 0,3 s para não picotar
   src.start(aud.next);aud.next+=buf.duration;aud.chunks++;
  }
 }catch(e){if(aud.on){problem=(e&&e.message)?e.message:'sem conexão';}}
 var wasOn=aud.on;
 audioOff();
 if(problem){$('st').textContent='O áudio parou: '+problem;}
 else if(wasOn){$('st').textContent='O áudio do PC parou (o computador fechou a conexão).';}
}
async function pollLevel(){
 if(!pin||curTab!=='live'){return;}
 try{
  var j=await api('audiolevel'),t=String(j.status||'');
  if(t.indexOf('ERR:')===0){$('vu').style.width='0%';$('vutxt').textContent='Não consegui medir o som do PC: '+t.slice(4);return;}
  var v=parseInt(t,10)||0;
  $('vu').style.width=v+'%';
  $('vutxt').textContent=v>3?'🔊 Está saindo som no PC.':'🔇 Silêncio no PC (nada tocando ou volume zerado).';
 }catch(e){}
}
setInterval(pollLevel,1000);
function fetchText(t){act('lyricsfetch',t+'|'+(t==='next'?($('nt').value.trim()+'\n'+$('na').value.trim()):''),true);}
async function useLink(queue){
 var url=$('nl').value.trim();
 if(!url){$('st').textContent='Cole o link do vídeo primeiro.';return;}
 if(queue){await act('qadd','url|'+url);}
 else{
  var has=live&&live.next;
  if(has&&!confirm('Trocar a próxima música preparada por este vídeo?')){return;}
  await act('setnext',(has?'1':'0')+'|'+url);
 }
 if(($('st').textContent||'').indexOf('Esse link')!==0){$('nl').value='';}
}
async function showQr(){
 try{
  var j=await api('onlinelink');
  if(!j.ok||!j.status){$('st').textContent=j.error||'Não foi possível abrir o QR code.';return;}
  $('qrlink').textContent=j.status;
  $('qrimg').src='/qr.png?pin='+encodeURIComponent(pin)+'&t='+Date.now();
  $('qrover').style.display='block';
 }catch(e){$('st').textContent='Não foi possível abrir o QR code.';}
}
function swapNames(){var a=$('nt').value;$('nt').value=$('na').value;$('na').value=a;$('st').textContent='Campos trocados. Toque em Verificar para procurar a letra.';}
function fixNames(){act('fixnames',undefined,true);}
function openPaste(t){pasteTarget=t;$('pastehint').textContent=t==='now'?'Cole a letra da música que está tocando agora.':'Cole a letra da próxima música.';$('pastepanel').style.display='block';$('pastebox').focus();}
function closePaste(){$('pastepanel').style.display='none';}
async function sendPaste(){
 var t=$('pastebox').value;
 if(t.trim().length<20){$('st').textContent='Cole a letra completa primeiro.';return;}
 await act(pasteTarget==='now'?'lyricsnow':'lyricsnext',t,true);
 if(($('st').textContent||'').indexOf('✅')===0){closePaste();$('pastebox').value='';}
}
function fillRows(box,key,list,emptyText,build){
 var sig=JSON.stringify(list||[]);
 if(sigs[key]===sig){return;}
 sigs[key]=sig;box.textContent='';
 if(!list||!list.length){box.textContent=emptyText;return;}
 list.forEach(function(m){box.appendChild(build(m));});
}
function libRow(m){
 var row=el('div','item'),t=el('div','t',m.title);
 t.appendChild(el('small','',m.lyrics==='sync'?'✅ letra sincronizada':m.lyrics==='text'?'🟡 letra só em texto':'❌ sem letra'));
 var b1=el('button','small','Próxima'),bq=el('button','small','➕ Fila'),b2=el('button','small main','▶ Tocar já');
 b1.onclick=function(){pickLib(m.id,false);};bq.onclick=function(){act('qadd','lib|'+m.id);};b2.onclick=function(){pickLib(m.id,true);};
 row.appendChild(t);row.appendChild(b1);row.appendChild(bq);row.appendChild(b2);return row;
}
function pickReq(id){
 var has=live&&live.next;
 if(has&&!confirm('Trocar a próxima música preparada por este pedido?')){return;}
 act('reqpick',id+'|'+(has?'1':'0'));
}
function pickLib(id,play){
 var has=live&&live.next,playing=live&&live.now&&live.now.state!=='parada';
 if(play){if(!confirm('Tocar esta música agora?'+(playing?'\n\nA música atual será trocada.':''))){return;}}
 else if(has&&!confirm('Trocar a próxima música preparada por esta?')){return;}
 act('libpick',id+'|'+(play?'play':'next'));
}
async function libSearch(){
 try{
  var j=await api('libsearch',$('lq').value.trim()),list=JSON.parse(j.status||'[]'),box=$('liblist');
  box.textContent='';
  if(!list.length){box.textContent='Nada encontrado na biblioteca.';return;}
  list.forEach(function(m){box.appendChild(libRow(m));});
 }catch(e){$('liblist').textContent='Não foi possível buscar agora.';}
}
function render(s){
 live=s;
 var now=s.now||{};
 $('nowline').textContent=now.line||(now.state==='parada'?'Nenhuma música tocando':'…');
 $('nowinfo').textContent=now.title?(now.title+' · '+now.state+(now.manual?' · modo manual':'')):'';
 $('autobtn').style.display=now.manual?'block':'none';
 $('nownext').textContent=now.next?('Próxima frase: '+now.next):'';
 var nx=s.next;
 if(!nx){$('nextempty').style.display='block';$('nextbody').style.display='none';lastNextId='';}
 else{
  $('nextempty').style.display='none';$('nextbody').style.display='block';
  $('ntitle').textContent=nx.title||'Próxima música';
  var th=$('thumb');
  if(th.getAttribute('data-id')!==nx.videoId){th.setAttribute('data-id',nx.videoId);th.style.display='block';th.onerror=function(){th.style.display='none';};th.src='https://i.ytimg.com/vi/'+nx.videoId+'/mqdefault.jpg';}
  $('nstatus').textContent=nx.headline||'';
  $('nextslide').textContent=(nx.preview&&nx.preview.length)?nx.preview.join('\n'):'(sem prévia da letra)';
  $('confirmbox').style.display=nx.canConfirm?'block':'none';
  if(nx.canConfirm){$('conftxt').textContent='Encontrei esta letra: '+(nx.track||'')+(nx.artist?' — '+nx.artist:'')+'. Se for a música certa, confirme: ela é salva e roda sozinha no telão quando você tocar.';}
  if(lastNextId!==nx.videoId||lastNamesRev!==nx.namesRev){lastNextId=nx.videoId;lastNamesRev=nx.namesRev;$('nt').value=nx.track||'';$('na').value=nx.artist||'';}
  $('rejectedtxt').style.display=nx.rejected?'block':'none';
  $('createbtn').style.display=nx.status==='library'?'none':'block';
  $('playbtn').textContent=nx.warning?'⏭ Tocar (sem letra automática)':'⏭ Tocar próxima';
 }
 $('reqhead').textContent='📥 Pedidos ('+(s.requestCount||0)+')';
 fillRows($('reqlist'),'req',s.requests,'Nenhum pedido na lista.',function(r){
  var row=el('div','item'),t=el('div','t',r.name+(r.group?' — '+r.group:''));
  t.appendChild(el('small','',r.lyrics||''));
  var b=el('button','small main','Preparar'),bq=el('button','small','➕ Fila');b.onclick=function(){pickReq(r.id);};bq.onclick=function(){act('qadd','req|'+r.id);};
  row.appendChild(t);row.appendChild(b);row.appendChild(bq);return row;
 });
 fillRows($('recentlist'),'recent',s.recent,'Nenhuma música salva ainda.',libRow);
 var q=s.queue||[];
 $('queuecard').style.display=q.length?'block':'none';
 $('queuehead').textContent='📋 Fila ('+q.length+')';
 fillRows($('queuelist'),'queue',q,'',function(m){
  var row=el('div','item'),t=el('div','t',(m.i+1)+'. '+m.title);
  t.appendChild(el('small','',m.lyrics||''));
  var b=el('button','small warn','✖');b.onclick=function(){act('qremove',String(m.i));};
  row.appendChild(t);row.appendChild(b);return row;
 });
 $('advbtn').textContent='⏩ Tocar a próxima sozinha ao terminar: '+(s.autoAdvance?'LIGADO':'DESLIGADO');
 $('advbtn').className=s.autoAdvance?'main gap':'gap';
 $('b-fetch-now').style.display=(s.canFetchText&&!now.hasText&&now.state!=='parada')?'block':'none';
 $('fetchbtn').style.display=(s.canFetchText&&nx&&nx.status!=='library')?'block':'none';
 // música tocando sem letra com tempos: colar / estimar / marcar por toque
 var tap=s.tap||{},playing=now.state!=='parada';
 $('nolyrics').style.display=(playing&&!now.hasCues&&!tap.active)?'block':'none';
 if(playing&&!now.hasCues){
  $('nolyricstxt').textContent=now.hasText?'Esta música tem a letra em texto, sem tempos. Estime os tempos (rascunho em segundos) ou marque por toque.':'Esta música ainda não tem letra. Cole a letra para cadastrar.';
  $('b-paste-now').style.display=now.hasText?'none':'block';
  $('b-estimate').style.display=now.hasText?'block':'none';
  $('b-tapstart').style.display=now.hasText?'block':'none';
 }
 $('tappanel').style.display=tap.active?'block':'none';
 if(tap.active){
  var done=tap.marked>=tap.total;
  $('taptxt').textContent=done?'Todas as estrofes marcadas. Toque para concluir e salvar.':'Estrofe '+(tap.marked+1)+' de '+tap.total+(tap.nextLine?' — “'+tap.nextLine+'”':'');
  $('tapbig').textContent=done?'✔ CONCLUIR':'TOQUE';
 }
}
var polling=false;
async function pollLive(){
 if(!pin||polling){return;}
 polling=true;
 try{
  if(curTab==='live'){var j=await api('livejson');if(j.status){render(JSON.parse(j.status));}}
  else{await cmd('state','',true);}
 }catch(e){}
 polling=false;
}
show();if(pin){cmd('state');}
setInterval(pollLive,3000);
</script></body></html>
""";

    private const string RequestPageHtml = """
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1">
<title>Pedido de músicas</title>
<style>
:root{color-scheme:dark}
body{margin:0;font-family:system-ui,sans-serif;background:#0b1117;color:#e8eef4;padding:14px}
h1{font-size:18px;margin:0 0 6px;color:#6fd6c8}
p{font-size:14px;color:#8fa1b3;margin:0 0 12px}
label{font-size:13px;color:#8fa1b3}
input{width:100%;box-sizing:border-box;font-size:17px;padding:12px;border-radius:12px;border:1px solid #2b3a4a;background:#131b24;color:#e8eef4;margin:4px 0 8px}
button{font-size:17px;padding:14px;border:0;border-radius:12px;background:#1d2a38;color:#e8eef4;width:100%;margin:6px 0}
button.main{background:#176e68}
.card{background:#0f171f;border:1px solid #1f2d3b;border-radius:14px;padding:10px;margin:10px 0}
#st{margin-top:12px;font-size:15px;color:#6fd6c8;white-space:pre-wrap}
</style></head><body>
<h1>Pedido de músicas</h1>
<p>Informe o nome do conjunto e cole o link de cada música. Os pedidos chegam ao operador do som.</p>
<label>Nome do conjunto / grupo (opcional)</label>
<input id="g" maxlength="60" autocomplete="off" placeholder="Ex.: Grupo de Louvor Jovem">
<div id="list"></div>
<button onclick="add()">+ Adicionar outra música</button>
<button class="main" onclick="send()">Enviar</button>
<div id="st"></div>
<script>
var n=0;
function $(id){return document.getElementById(id);}
function add(){
 if(n>=10){return;}
 n++;
 var d=document.createElement('div');d.className='card';
 d.innerHTML='<b>Música '+n+'</b><input class="nm" maxlength="80" placeholder="Nome da música (opcional)" autocomplete="off"><input class="lk" placeholder="Cole o link aqui" inputmode="url" autocapitalize="none" autocomplete="off">';
 $('list').appendChild(d);
}
async function send(){
 var cards=document.querySelectorAll('.card'),items=[];
 for(var i=0;i<cards.length;i++){
  var link=cards[i].querySelector('.lk').value.trim();
  if(link){items.push({nome:cards[i].querySelector('.nm').value.trim(),link:link});}
 }
 if(!items.length){$('st').textContent='Cole pelo menos um link.';return;}
 var d=JSON.stringify({grupo:$('g').value.trim(),itens:items});
 try{
  var r=await fetch('/pedido/enviar?d='+encodeURIComponent(d));
  var j=await r.json();
  $('st').textContent=j.status||'';
  if(j.ok){$('list').innerHTML='';n=0;add();}
 }catch(e){$('st').textContent='Não foi possível enviar. Confira se está na mesma rede (Wi-Fi) do computador.';}
}
add();
</script></body></html>
""";
}
