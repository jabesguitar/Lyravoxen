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
                timeout.CancelAfter(TimeSpan.FromSeconds(45)); // a verificação de letra pode levar alguns segundos
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
input{width:100%;box-sizing:border-box;font-size:18px;padding:12px;border-radius:12px;border:1px solid #2b3a4a;background:#131b24;color:#e8eef4;margin:5px 0}
button{font-size:17px;padding:14px;border:0;border-radius:12px;background:#1d2a38;color:#e8eef4;width:100%}
button.main{background:#176e68}button.warn{background:#8c3b3b}button.small{font-size:14px;padding:9px 10px;width:auto}
.row{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:10px 0}
#st{margin-top:12px;font-size:14px;color:#8fa1b3;min-height:40px;white-space:pre-wrap}
.card{background:#0f171f;border:1px solid #1f2d3b;border-radius:14px;padding:10px;margin:10px 0}
.card h2{font-size:14px;margin:0 0 8px;color:#8fa1b3;font-weight:600}
.slide{background:#000;border:1px solid #243342;border-radius:12px;display:flex;align-items:center;justify-content:center;text-align:center;padding:12px;color:#fff;line-height:1.25;white-space:pre-wrap;overflow:hidden}
.slide.big{aspect-ratio:16/9;font-size:22px;font-weight:700}
.slide.mini{min-height:96px;max-height:150px;font-size:15px;color:#dfe9f0}
.dim{font-size:13px;color:#8fa1b3;margin:6px 0}
.mini-head{display:flex;gap:10px;align-items:center;margin-bottom:8px}
.mini-head img{width:96px;height:54px;object-fit:cover;border-radius:8px;background:#000}
.mini-head b{font-size:16px}
.suggest{background:#10261f;border:1px solid #1f5a49;border-radius:12px;padding:10px;margin:8px 0}
.item{display:flex;gap:8px;align-items:center;justify-content:space-between;border-top:1px solid #1f2d3b;padding:8px 0}
.item:first-child{border-top:0}
.item .t{flex:1;font-size:15px}
.item .t small{display:block;color:#8fa1b3;font-size:12px}
.tabs{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-bottom:6px}
</style></head><body>
<h1>Lyravoxen · Controle</h1>
<div id="pinbox"><input id="pin" inputmode="numeric" placeholder="PIN mostrado no computador" autocomplete="off">
<div class="row"><button class="main" onclick="savePin()">Entrar</button></div></div>
<div id="app" style="display:none">
<div class="tabs"><button id="t-bible" class="main" onclick="tab('bible')">📖 Bíblia</button><button id="t-live" onclick="tab('live')">▶ Ao vivo</button></div>

<div id="p-live" style="display:none">
 <div class="card"><h2>🎬 No telão agora</h2>
  <div class="slide big" id="nowline">Nenhuma música tocando</div>
  <div class="dim" id="nowinfo"></div>
  <div class="dim" id="nownext"></div>
  <div class="row"><button onclick="act('toggle')">⏯ Pausar / continuar</button><button class="warn" onclick="ask('endlive','Encerrar a música e limpar a projeção?')">⏹ Encerrar</button></div>
 </div>

 <div class="card"><h2>⏭ Próxima música</h2>
  <div id="nextempty" class="dim">Nenhuma preparada. Escolha um pedido ou busque na biblioteca abaixo.</div>
  <div id="nextbody" style="display:none">
   <div class="mini-head"><img id="thumb" alt=""><div><b id="ntitle"></b><div class="dim" id="nstatus"></div></div></div>
   <div class="slide mini" id="nextslide"></div>
   <div class="suggest" id="confirmbox" style="display:none">
    <div id="conftxt" class="dim"></div>
    <button class="main" onclick="act('nextconfirm',undefined,true)">✅ Confirmar letra</button>
   </div>
   <input id="nt" placeholder="Música (use o microfone do teclado)" autocomplete="off">
   <input id="na" placeholder="Autor / intérprete" autocomplete="off">
   <button onclick="nextCheck()">🔎 Verificar com estes nomes</button>
   <div class="row"><button class="main" id="playbtn" onclick="playNext()">⏭ Tocar próxima</button><button onclick="act('reqnext')">📥 Pedido → próxima</button></div>
  </div>
 </div>

 <div class="card"><h2 id="reqhead">📥 Pedidos</h2><div id="reqlist" class="dim">Nenhum pedido na lista.</div></div>

 <div class="card"><h2>📚 Biblioteca (músicas já salvas)</h2>
  <input id="lq" placeholder="Nome da música ou autor" autocomplete="off" onkeydown="if(event.key==='Enter')libSearch()">
  <button onclick="libSearch()">🔍 Buscar</button>
  <div id="liblist" class="dim"></div>
 </div>
</div>

<div id="p-bible">
<input id="ref" placeholder="Ex.: João 3 16 (use o microfone do teclado)" autocomplete="off" autocapitalize="none" onkeydown="if(event.key==='Enter')go()">
<div class="row"><button class="main" onclick="go()">Projetar</button><button class="warn" onclick="cmd('clear')">Limpar telão</button></div>
<div class="row"><button onclick="cmd('prev')">◀ Versículo</button><button onclick="cmd('next')">Versículo ▶</button></div>
<div class="row"><button onclick="cmd('chprev')">◀ Capítulo</button><button onclick="cmd('chnext')">Capítulo ▶</button></div>
</div>
<div id="st"></div></div>
<script>
var pin='';try{pin=localStorage.getItem('lyra-pin')||'';}catch(e){}
var curTab='bible',holdUntil=0,lastNextId='',live=null;
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
  list.forEach(function(m){
   var row=el('div','item'),t=el('div','t',m.title);
   t.appendChild(el('small','',m.lyrics==='sync'?'✅ letra sincronizada':m.lyrics==='text'?'🟡 letra só em texto':'❌ sem letra'));
   var b1=el('button','small','Próxima'),b2=el('button','small main','▶ Tocar já');
   b1.onclick=function(){pickLib(m.id,false);};b2.onclick=function(){pickLib(m.id,true);};
   row.appendChild(t);row.appendChild(b1);row.appendChild(b2);box.appendChild(row);
  });
 }catch(e){$('liblist').textContent='Não foi possível buscar agora.';}
}
function render(s){
 live=s;
 var now=s.now||{};
 $('nowline').textContent=now.line||(now.state==='parada'?'Nenhuma música tocando':'…');
 $('nowinfo').textContent=now.title?(now.title+' · '+now.state):'';
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
  if(lastNextId!==nx.videoId){lastNextId=nx.videoId;$('nt').value=nx.track||'';$('na').value=nx.artist||'';}
  $('playbtn').textContent=nx.warning?'⏭ Tocar (sem letra automática)':'⏭ Tocar próxima';
 }
 $('reqhead').textContent='📥 Pedidos ('+(s.requestCount||0)+')';
 var rl=$('reqlist');rl.textContent='';
 if(!s.requests||!s.requests.length){rl.textContent='Nenhum pedido na lista.';}
 else{s.requests.forEach(function(r){
  var row=el('div','item'),t=el('div','t',r.name+(r.group?' — '+r.group:''));
  t.appendChild(el('small','',r.lyrics||''));
  var b=el('button','small main','Preparar');b.onclick=function(){pickReq(r.id);};
  row.appendChild(t);row.appendChild(b);rl.appendChild(row);
 });}
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
