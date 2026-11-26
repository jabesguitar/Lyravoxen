namespace Lyravoxen.Services;

/// <summary>
/// Formulário de pedido em um único arquivo HTML, para mandar pelo WhatsApp.
/// Funciona em duas camadas: (1) um formulário simples, SEM JavaScript, que sempre abre e envia para o WhatsApp
/// (vários visualizadores de arquivo do celular bloqueiam scripts); (2) se o JavaScript estiver ativo, ele é
/// trocado pelo formulário com um cartão por música, copiar mensagem e envio ao grupo.
/// </summary>
internal static class OfflineRequestForm
{
    /// <param name="sendUrl">Endereço do serviço na nuvem que recebe o pedido (ex.: https://x.workers.dev/enviar ou "/enviar"); vazio = só WhatsApp.</param>
    public static string Build(WhatsAppSettings settings, string? sendUrl = null)
    {
        var cloud = !string.IsNullOrWhiteSpace(sendUrl);
        sendUrl = cloud ? new string(sendUrl!.Trim().Where(c => char.IsAsciiLetterOrDigit(c) || "-._~:/".Contains(c)).ToArray()) : "";
        var number = WhatsAppSettings.DigitsOnly(settings.Number);
        var groupLink = WhatsAppSettings.NormalizeGroupLink(settings.GroupLink);
        var groupName = WhatsAppSettings.SafeName(settings.GroupName);
        var hasGroup = groupLink.Length > 0 || groupName.Length > 0;

        var title = hasGroup && groupName.Length > 0 ? "Pedido de músicas — " + groupName : "Pedido de músicas";
        var directAction = number.Length > 0 ? "https://wa.me/" + number : "https://wa.me/";
        var directLabel = number.Length > 0 ? "Enviar para o operador no WhatsApp" : "Enviar pelo WhatsApp (escolher contato)";

        var plainMain = cloud
            ? "<button class=\"main\" type=\"submit\">Enviar pedido ao operador</button>\n" +
              "<button type=\"submit\" formmethod=\"get\" formaction=\"" + directAction + "\">" + directLabel + "</button>"
            : "<button class=\"main\" type=\"submit\">" + directLabel + "</button>";
        var plainGroupButton = hasGroup
            ? "<button class=\"grp\" type=\"submit\" formmethod=\"get\" formaction=\"https://wa.me/\">Enviar para o grupo" +
              (groupName.Length > 0 ? " " + groupName : "") + " (escolha o grupo na lista)</button>"
            : "";
        var plainGroupOpen = groupLink.Length > 0
            ? "<p class=\"hint\">Ou abra o grupo e cole a mensagem: <a href=\"" + groupLink + "\">abrir o grupo no WhatsApp</a></p>"
            : "";

        return Template
            .Replace("__TITLE__", title)
            .Replace("__WA__", number)
            .Replace("__GL__", groupLink)
            .Replace("__GN__", groupName)
            .Replace("__DIRECT_ACTION__", directAction)
            .Replace("__DIRECT_LABEL__", directLabel)
            .Replace("__PLAIN_METHOD__", cloud ? "post" : "get")
            .Replace("__PLAIN_ACTION__", cloud ? sendUrl : directAction)
            .Replace("__PLAIN_MAIN__", plainMain)
            .Replace("__SEND__", sendUrl)
            .Replace("__PLAIN_GROUP__", plainGroupButton)
            .Replace("__PLAIN_GROUP_OPEN__", plainGroupOpen);
    }

    /// <summary>Texto para o operador colar no grupo, explicando como pedir (as respostas no grupo são lidas pelo Lyravoxen).</summary>
    public static string GroupInvitationText() =>
        "🎵 *Pedido de músicas*\n" +
        "Para pedir uma música, responda aqui no grupo neste formato:\n\n" +
        "Conjunto: nome do seu grupo\n" +
        "1. Nome da música - link do YouTube\n" +
        "2. Nome da música - link do YouTube\n\n" +
        "Um link por linha. Pode mandar até 10 músicas por mensagem.";

    private const string Template = """
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>__TITLE__</title>
<style>
:root{color-scheme:dark}
body{margin:0;font-family:system-ui,sans-serif;background:#0b1117;color:#e8eef4;padding:14px}
h1{font-size:18px;margin:0 0 6px;color:#6fd6c8}
p{font-size:14px;color:#8fa1b3;margin:0 0 12px}
p.hint{font-size:13px}
a{color:#6fd6c8}
label{font-size:13px;color:#8fa1b3}
input,textarea{width:100%;box-sizing:border-box;font-size:17px;padding:12px;border-radius:12px;border:1px solid #2b3a4a;background:#131b24;color:#e8eef4;margin:4px 0 8px}
textarea{font-size:15px;height:190px}
button{font-size:17px;padding:14px;border:0;border-radius:12px;background:#1d2a38;color:#e8eef4;width:100%;margin:6px 0}
button.main{background:#176e68}
button.grp{background:#25508a}
.card{background:#0f171f;border:1px solid #1f2d3b;border-radius:14px;padding:10px;margin:10px 0}
#fancy{display:none}
#out{display:none}
#st{margin-top:8px;font-size:15px;color:#6fd6c8}
</style></head><body>
<h1>__TITLE__</h1>
<p class="hint">Para prevenir abusos, o serviço registra o IP público e o horário do pedido. O endereço MAC não é coletado.</p>

<div id="plain">
<p>Escreva uma música por linha, com o nome e o link (do YouTube, por exemplo). Depois toque no botão para enviar pelo WhatsApp.</p>
<form method="__PLAIN_METHOD__" action="__PLAIN_ACTION__" accept-charset="utf-8">
<textarea name="text" aria-label="Pedido de músicas">🎵 Pedido de músicas
Conjunto: 

1. Nome da música - link
2. Nome da música - link
3. Nome da música - link</textarea>
__PLAIN_MAIN__
__PLAIN_GROUP__
</form>
__PLAIN_GROUP_OPEN__
</div>

<div id="fancy">
<div id="form">
<p>Informe o nome do conjunto e cole o link de cada música. Depois envie a mensagem pelo WhatsApp.</p>
<label>Nome do conjunto / grupo (opcional)</label>
<input id="g" maxlength="60" autocomplete="off" placeholder="Ex.: Grupo de Louvor Jovem">
<div id="list"></div>
<button type="button" id="b-add">+ Adicionar outra música</button>
<button type="button" class="main" id="b-build">Gerar mensagem</button>
</div>
<div id="out">
<label>Mensagem pronta:</label>
<textarea id="msg" readonly></textarea>
<button type="button" class="main" id="b-send" style="display:none">Enviar pedido ao operador</button>
<button type="button" class="main" id="b-wa">__DIRECT_LABEL__</button>
<button type="button" class="grp" id="b-grp" style="display:none">Enviar para o grupo __GN__</button>
<button type="button" id="b-open" style="display:none">Copiar e abrir o grupo __GN__</button>
<button type="button" id="b-copy">Copiar mensagem</button>
<button type="button" id="b-back">Voltar e editar</button>
</div>
</div>
<div id="st"></div>
<script>
(function(){
var n=0,WA='__WA__',GL='__GL__',GN='__GN__',SEND='__SEND__';
function $(id){return document.getElementById(id);}
function say(t){$('st').textContent=t;}
try{
 // Se este script está rodando, troca o formulário simples pelo completo.
 $('plain').style.display='none';
 $('fancy').style.display='block';
 if(SEND){$('b-send').style.display='block';}
 if(GL||GN){$('b-grp').style.display='block';}
 if(GL){$('b-open').style.display='block';}
 function add(){
  if(n>=10){return;}
  n++;
  var d=document.createElement('div');d.className='card';
  d.innerHTML='<b>Música '+n+'</b><input class="nm" maxlength="80" placeholder="Nome da música (opcional)" autocomplete="off"><input class="lk" placeholder="Cole o link aqui" inputmode="url" autocapitalize="none" autocomplete="off">';
  $('list').appendChild(d);
 }
 function build(){
  var cards=document.querySelectorAll('.card'),items=[],lines=[];
  for(var i=0;i<cards.length;i++){
   var link=cards[i].querySelector('.lk').value.replace(/^\s+|\s+$/g,''),name=cards[i].querySelector('.nm').value.replace(/^\s+|\s+$/g,'');
   if(link){items.push({nome:name,link:link});lines.push((items.length)+'. '+(name?name+' - ':'')+link);}
  }
  if(!items.length){say('Cole pelo menos um link.');return;}
  var g=$('g').value.replace(/^\s+|\s+$/g,'');
  var text='🎵 Pedido de músicas'+(g?'\nConjunto: '+g:'')+'\n\n'+lines.join('\n');
  try{var json=JSON.stringify({grupo:g,itens:items});text+='\n\n#LYRA:'+btoa(unescape(encodeURIComponent(json)));}catch(e){}
  $('msg').value=text;$('form').style.display='none';$('out').style.display='block';say('');
 }
 function back(){$('out').style.display='none';$('form').style.display='block';}
 function go(url){window.location.href=url;}
 function copyText(){
  var t=$('msg');t.removeAttribute('readonly');t.focus();t.select();
  try{t.setSelectionRange(0,99999);}catch(e){}
  var ok=false;try{ok=document.execCommand('copy');}catch(e){}
  t.setAttribute('readonly','');
  return ok;
 }
 $('b-send').onclick=function(){
  say('Enviando…');
  var x=new XMLHttpRequest();
  x.open('POST',SEND,true);
  x.setRequestHeader('Content-Type','application/json');
  x.onreadystatechange=function(){
   if(x.readyState!==4){return;}
   var r=null;try{r=JSON.parse(x.responseText);}catch(e){}
   if(x.status===200&&r&&r.ok){say('✅ '+r.status);$('b-send').style.display='none';}
   else{say((r&&r.status)?r.status:'Não foi possível enviar agora. Use o botão do WhatsApp.');}
  };
  x.send(JSON.stringify({text:$('msg').value}));
 };
 $('b-add').onclick=add;
 $('b-build').onclick=build;
 $('b-back').onclick=back;
 $('b-wa').onclick=function(){go((WA?'https://wa.me/'+WA:'https://wa.me/')+'?text='+encodeURIComponent($('msg').value));};
 $('b-copy').onclick=function(){say(copyText()?'Mensagem copiada. Cole no WhatsApp.':'Segure o texto acima, copie e cole no WhatsApp.');};
 $('b-grp').onclick=function(){
  // O WhatsApp não permite preencher texto num grupo pelo link: abre a lista de conversas com a mensagem pronta.
  say('Escolha o grupo '+GN+' na lista do WhatsApp.');
  go('https://wa.me/?text='+encodeURIComponent($('msg').value));
 };
 $('b-open').onclick=function(){
  var ok=copyText();
  say(ok?'Mensagem copiada. No grupo, cole e envie.':'Não consegui copiar: segure o texto, copie e cole no grupo.');
  setTimeout(function(){go(GL);},ok?700:2500);
 };
 add();
}catch(e){
 // Qualquer falha de script deixa o formulário simples visível.
 try{$('plain').style.display='block';$('fancy').style.display='none';}catch(e2){}
}
})();
</script></body></html>
""";
}
