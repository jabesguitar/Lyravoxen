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
                timeout.CancelAfter(TimeSpan.FromSeconds(20));
                var stream = client.GetStream();
                var ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";

                var buffer = new byte[16384];
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
:root{color-scheme:dark}
body{margin:0;font-family:system-ui,sans-serif;background:#0b1117;color:#e8eef4;padding:14px}
h1{font-size:18px;margin:0 0 12px;color:#6fd6c8}
input{width:100%;box-sizing:border-box;font-size:20px;padding:14px;border-radius:12px;border:1px solid #2b3a4a;background:#131b24;color:#e8eef4}
button{font-size:18px;padding:16px;border:0;border-radius:12px;background:#1d2a38;color:#e8eef4;width:100%}
button.main{background:#176e68}button.warn{background:#8c3b3b}
.row{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:10px 0}
#st{margin-top:12px;font-size:14px;color:#8fa1b3;min-height:40px;white-space:pre-wrap}
</style></head><body>
<h1>Lyravoxen · Controle</h1>
<div id="pinbox"><input id="pin" inputmode="numeric" placeholder="PIN mostrado no computador" autocomplete="off">
<div class="row"><button class="main" onclick="savePin()">Entrar</button></div></div>
<div id="app" style="display:none">
<input id="ref" placeholder="Ex.: João 3 16 (use o microfone do teclado)" autocomplete="off" autocapitalize="none" onkeydown="if(event.key==='Enter')go()">
<div class="row"><button class="main" onclick="go()">Projetar</button><button class="warn" onclick="cmd('clear')">Limpar telão</button></div>
<div class="row"><button onclick="cmd('prev')">◀ Versículo</button><button onclick="cmd('next')">Versículo ▶</button></div>
<div class="row"><button onclick="cmd('chprev')">◀ Capítulo</button><button onclick="cmd('chnext')">Capítulo ▶</button></div>
<div id="st"></div></div>
<script>
var pin='';try{pin=localStorage.getItem('lyra-pin')||'';}catch(e){}
function $(id){return document.getElementById(id);}
function show(){$('pinbox').style.display=pin?'none':'block';$('app').style.display=pin?'block':'none';}
function savePin(){pin=$('pin').value.trim();if(pin){try{localStorage.setItem('lyra-pin',pin);}catch(e){}show();cmd('state');}}
async function cmd(c,a){
 try{
  var r=await fetch('/api?pin='+encodeURIComponent(pin)+'&c='+c+'&a='+encodeURIComponent(a||''));
  var j=await r.json();
  if(r.status===401){try{localStorage.removeItem('lyra-pin');}catch(e){}pin='';show();}
  $('st').textContent=j.status||j.error||'';
 }catch(e){$('st').textContent='Sem conexão com o computador.';}
}
function go(){var v=$('ref').value.trim();if(v){cmd('ref',v);$('ref').select();}}
show();if(pin)cmd('state');
setInterval(function(){if(pin)cmd('state');},4000);
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
