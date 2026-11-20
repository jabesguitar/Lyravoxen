namespace Lyravoxen.Services;

/// <summary>Formulário de pedido em um único arquivo HTML, que funciona sem rede e gera uma mensagem para o WhatsApp.</summary>
internal static class OfflineRequestForm
{
    public static string Build(string whatsappNumber)
    {
        var digits = new string((whatsappNumber ?? "").Where(char.IsDigit).ToArray());
        return Template.Replace("__WA__", digits);
    }

    private const string Template = """
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1">
<title>Pedido de músicas</title>
<style>
:root{color-scheme:dark}
body{margin:0;font-family:system-ui,sans-serif;background:#0b1117;color:#e8eef4;padding:14px}
h1{font-size:18px;margin:0 0 6px;color:#6fd6c8}
p{font-size:14px;color:#8fa1b3;margin:0 0 12px}
label{font-size:13px;color:#8fa1b3}
input,textarea{width:100%;box-sizing:border-box;font-size:17px;padding:12px;border-radius:12px;border:1px solid #2b3a4a;background:#131b24;color:#e8eef4;margin:4px 0 8px}
textarea{font-size:13px;height:150px}
button{font-size:17px;padding:14px;border:0;border-radius:12px;background:#1d2a38;color:#e8eef4;width:100%;margin:6px 0}
button.main{background:#176e68}
.card{background:#0f171f;border:1px solid #1f2d3b;border-radius:14px;padding:10px;margin:10px 0}
#out{display:none}
#st{margin-top:8px;font-size:15px;color:#6fd6c8}
</style></head><body>
<h1>Pedido de músicas</h1>
<p>Informe o nome do conjunto e cole o link de cada música. Depois envie a mensagem pelo WhatsApp.</p>
<div id="form">
<label>Nome do conjunto / grupo (opcional)</label>
<input id="g" maxlength="60" autocomplete="off" placeholder="Ex.: Grupo de Louvor Jovem">
<div id="list"></div>
<button onclick="add()">+ Adicionar outra música</button>
<button class="main" onclick="build()">Gerar mensagem</button>
</div>
<div id="out">
<label>Mensagem pronta:</label>
<textarea id="msg" readonly></textarea>
<button class="main" onclick="wa()">Enviar pelo WhatsApp</button>
<button onclick="copyMsg()">Copiar mensagem</button>
<button onclick="back()">Voltar e editar</button>
</div>
<div id="st"></div>
<script>
var n=0,WA='__WA__';
function $(id){return document.getElementById(id);}
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
  var link=cards[i].querySelector('.lk').value.trim(),name=cards[i].querySelector('.nm').value.trim();
  if(link){items.push({nome:name,link:link});lines.push((items.length)+'. '+(name?name+' — ':'')+link);}
 }
 if(!items.length){$('st').textContent='Cole pelo menos um link.';return;}
 var g=$('g').value.trim();
 var json=JSON.stringify({grupo:g,itens:items});
 var token=btoa(unescape(encodeURIComponent(json)));
 var text='🎵 Pedido de músicas'+(g?'\nConjunto: '+g:'')+'\n\n'+lines.join('\n')+'\n\n#LYRA:'+token;
 $('msg').value=text;$('form').style.display='none';$('out').style.display='block';$('st').textContent='';
}
function back(){$('out').style.display='none';$('form').style.display='block';}
function wa(){
 var base=WA?'https://wa.me/'+WA:'https://wa.me/';
 window.location.href=base+'?text='+encodeURIComponent($('msg').value);
}
function copyMsg(){
 var t=$('msg');t.removeAttribute('readonly');t.select();t.setSelectionRange(0,99999);
 var ok=false;try{ok=document.execCommand('copy');}catch(e){}
 t.setAttribute('readonly','');
 $('st').textContent=ok?'Mensagem copiada. Cole no WhatsApp.':'Segure o texto acima, copie e cole no WhatsApp.';
}
add();
</script></body></html>
""";
}
