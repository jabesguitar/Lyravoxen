import { createRequire } from 'module';
const require = createRequire((process.env.PW_MODULES||'/opt/npm-tools/node_modules/'));
const { chromium } = require('playwright');
import http from 'http';
import fs from 'fs';
let pass=0, fail=0; const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };
const page_html=fs.readFileSync('./phone_page.html','utf8');

// ---- computador falso: mesma estrutura JSON do BuildLiveJson (C#)
const S={pin:'1234',now:null,next:null,manual:false,lib:[{id:'LIB00000001',title:'Santo — Fernandinho',lyrics:'sync'},{id:'LIB00000002',title:'Reconstrução — Eliã Oliveira',lyrics:'none'}],
 requests:[{id:'r1',name:'Grande é o Senhor',group:'Jovens',videoId:'REQ00000001',lyrics:'✅ Letra sincronizada no catálogo: Grande é o Senhor — Adhemar.'},
           {id:'r2',name:'<img src=x onerror="window.__xss=1">',group:'<b>x</b>',videoId:'REQ00000002',lyrics:'❌ Letra não encontrada no catálogo. Será preciso cadastrar.'}],
 log:[],checkingUntil:0,tap:{active:false,marked:0,total:0,nextLine:''},recent:[{id:'REC00000001',title:'Aleluia <b>x</b> — Coral',lyrics:'sync'}],hasText:false,hasCues:true,namesRev:0,queue:[],autoAdvance:false,canFetchText:false};
const live=()=>({queue:S.queue.map((q,i)=>({i,id:q.id,title:q.title,lyrics:'🔎 Verificando se tem letra…'})),autoAdvance:S.autoAdvance,canFetchText:S.canFetchText,now:{title:S.now?S.now.title:'',state:S.now?S.now.state:'parada',line:S.now?S.now.line:'',next:S.now?S.now.nextLine:'',manual:!!(S.now&&S.manual),cues:S.now?12:0,hasCues:S.hasCues,hasText:S.hasText},tap:S.tap,recent:S.recent,
 next:S.next?{videoId:S.next.videoId,title:S.next.title,status:Date.now()<S.checkingUntil?'checking':S.next.status,headline:Date.now()<S.checkingUntil?'🔎 Verificando se tem letra…':S.next.headline,preview:Date.now()<S.checkingUntil?[]:S.next.preview,track:S.next.track,artist:S.next.artist,canConfirm:Date.now()>=S.checkingUntil&&S.next.status==='synced'&&!S.next.rejected,rejected:!!S.next.rejected,namesRev:S.namesRev,warning:S.next.warning}:null,
 requestCount:S.requests.length,requests:S.requests.map(r=>({id:r.id,name:r.name,group:r.group,lyrics:r.lyrics}))});
const mkNext=(videoId,title,status)=>({videoId,title,status,headline:status==='synced'?'✅ Letra sincronizada no catálogo: Grande é o Senhor — Adhemar.':status==='library'?'✅ Letra já cadastrada na sua biblioteca (12 frases sincronizadas).':'❌ Letra não encontrada no catálogo. Será preciso cadastrar.',
  preview:status==='notfound'?[]:['Grande é o Senhor','E digno de louvor','Na cidade do nosso Deus','Seu santo monte'],track:status==='notfound'?'':'Grande é o Senhor',artist:status==='notfound'?'':'Adhemar',warning:status==='notfound'?'sem letra cadastrada: não vai projetar':''});
function handle(c,a){
  S.log.push([c,a]);
  switch(c){
   case 'state': return 'Bíblia: pronta\n▶ Próxima: '+(S.next?'preparada':'nenhuma preparada')+' · 📥 '+S.requests.length+' pedidos';
   case 'livejson': return JSON.stringify(live());
   case 'reqpick':{const [id,rep]=a.split('|');const r=S.requests.find(x=>x.id===id);if(!r)return 'Esse pedido não está mais na lista.';
     if(S.next&&rep!=='1')return 'Já existe uma próxima preparada. Escolha "Trocar" para substituí-la.';
     S.next=mkNext(r.videoId,r.name,r.lyrics.startsWith('✅')?'synced':'notfound');S.checkingUntil=Date.now()+1200;S.requests=S.requests.filter(x=>x.id!==id);return 'Próxima preparada: '+r.name+'.';}
   case 'nextconfirm':{if(!S.next)return 'Não há próxima música preparada.';if(Date.now()<S.checkingUntil)return 'Ainda verificando a letra. Espere alguns segundos e toque de novo.';
     if(S.next.status!=='synced')return 'Não há letra encontrada para confirmar. Digite música e autor e toque em Verificar.';S.next=mkNext(S.next.videoId,S.next.title,'library');return '✅ Letra confirmada e salva: Grande é o Senhor — Adhemar (12 frases). Ao tocar a próxima, ela roda no telão.';}
   case 'nextcheck':{if(!S.next)return 'Prepare uma próxima música antes (escolha um pedido ou busque na biblioteca).';const [t,ar]=a.split('\n');S.next=mkNext(S.next.videoId,S.next.title,t?'synced':'notfound');if(t){S.next.track=t;S.next.artist=ar;}return S.next.headline+(t?'\nProcurado: '+t+(ar?' — '+ar:''):'');}
   case 'libsearch':{const w=a.toLowerCase().split(' ').filter(Boolean);return JSON.stringify(S.lib.filter(m=>w.every(x=>m.title.toLowerCase().includes(x))));}
   case 'libpick':{const [id,mode]=a.split('|');const m=S.lib.find(x=>x.id===id);S.next=mkNext(id,m.title,m.lyrics==='sync'?'library':'notfound');if(mode==='play'){S.now={title:m.title,state:'tocando',line:'Santo, santo, santo',nextLine:'Senhor Deus todo-poderoso'};S.next=null;return 'Tocando: '+m.title;}return 'Próxima preparada: '+m.title+'.';}
   case 'playnext':{if(!S.next)return 'Não há próxima música preparada.\n▶ Próxima: nenhuma preparada';S.now={title:S.next.title,state:'tocando',line:'Grande é o Senhor',nextLine:'E digno de louvor'};const w=S.next.warning?' ('+S.next.warning+')':'';S.next=null;return 'Tocando a próxima música…'+w;}
   case 'toggle':{if(!S.now)return 'Nenhuma música carregada.';S.now.state=S.now.state==='tocando'?'pausada':'tocando';return S.now.state==='pausada'?'Música pausada.':'Música tocando.';}
   case 'endlive':S.now=null;S.manual=false;return 'Música encerrada e projeção limpa.';
   case 'cuenext':S.manual=true;if(S.now){S.now.line='E digno de louvor';S.now.nextLine='Na cidade do nosso Deus';}return 'Projeção manual: frase 2 de 12.';
   case 'cueprev':S.manual=true;return 'Projeção manual: frase 1 de 12.';
   case 'auto':S.manual=false;return 'Projeção automática retomada.';
   case 'seek':return 'Posição ajustada ('+a+' s).';
   case 'vol':return a==='mute'?'Som desligado (mudo).':'Volume 80%';
   case 'clearlive':if(S.now){S.now.line='';}S.manual=true;return 'Telão limpo. Toque em "Voltar ao automático" para retomar a letra.';
   case 'nextreject':if(!S.next)return 'Não há próxima música preparada.';S.next.rejected=true;return 'Sugestão descartada. Digite música e autor e toque em Verificar, ou toque em Criar letra.';
   case 'fixnames':if(!S.next)return 'Prepare uma próxima música antes.';{const t=S.next.track;S.next.track=S.next.artist;S.next.artist=t;S.namesRev++;}return 'Nomes corrigidos: '+S.next.artist+' — '+S.next.track+'.';
   case 'lyricsnext':case 'lyricsnow':if(a.trim().length<20)return 'Isso não parece uma letra (poucas linhas). Cole a letra completa.';if(c==='lyricsnow')S.hasText=true;else if(S.next){S.next.status='library';S.next.headline='🟡 Letra só em texto (sem tempos) no catálogo: X.';}S.lastLyrics=a;return '✅ Letra salva ('+a.split('\n').length+' linhas).';
   case 'estimate':S.hasCues=true;return '⏱ Tempos estimados (rascunho): 12 frases.';
   case 'tapstart':S.tap={active:true,marked:0,total:3,nextLine:'Santo, santo, santo'};return 'Marcação por toque iniciada.';
   case 'tapmark':S.tap.marked++;S.tap.nextLine='Senhor Deus';if(S.tap.marked>3){S.tap.active=false;S.hasCues=true;S.recent.unshift({id:'NEW00000001',title:'Música nova — Coral',lyrics:'sync'});return '✅ Salva na biblioteca.';}return 'Estrofe '+S.tap.marked+' marcada.';
   case 'tapundo':S.tap.marked=Math.max(0,S.tap.marked-1);return 'Desfeito.';
   case 'tapfinish':S.tap={active:false,marked:0,total:0,nextLine:''};S.hasCues=true;S.recent.unshift({id:'NEW00000002',title:'Outra nova — Coral',lyrics:'sync'});return '✅ Salva na biblioteca.';
   case 'tapcancel':S.tap={active:false,marked:0,total:0,nextLine:''};return 'Marcação cancelada.';
   case 'audiolevel':return S.level===undefined?'0':S.level;
   case 'setnext':{const [rep,...u]=a.split('|');const url=u.join('|');if(!/youtu/.test(url))return 'Esse link não é de um vídeo do YouTube.';if(S.next&&rep!=='1')return 'Já existe uma próxima preparada.';S.next=mkNext('LNK00000001','Vídeo colado','notfound');return 'Próxima preparada: vídeo LNK00000001.';}
   case 'qadd':{const [kind,...v]=a.split('|');const val=v.join('|');if(kind==='url'&&!/youtu/.test(val))return 'Esse link não é de um vídeo do YouTube.';
     if(!S.next){S.next=mkNext('Q'+Math.random().toString(36).slice(2,12),kind+':'+val,'notfound');return 'Próxima preparada: '+val+'.';}
     S.queue.push({id:'Q'+S.queue.length,title:kind+':'+val});if(kind==='req')S.requests=S.requests.filter(r=>r.id!==val);return 'Na fila, posição '+S.queue.length+': '+val+'.';}
   case 'qremove':S.queue.splice(parseInt(a),1);return 'Tirada da fila.';
   case 'qclear':S.queue=[];return 'Fila limpa.';
   case 'autoadvance':S.autoAdvance=!S.autoAdvance;return S.autoAdvance?'Ligado: a próxima música toca sozinha quando a atual terminar.':'Desligado.';
   case 'lyricsfetch':S.lastFetch=a;return '📝 Texto encontrado no Vagalume: Santo — Fernandinho. ✅ Letra salva (8 linhas).';
   default:return 'Comando desconhecido.';
  }
}
const server=http.createServer((req,res)=>{
  const u=new URL(req.url,'http://x');
  if(u.pathname==='/'){res.writeHead(200,{'content-type':'text/html; charset=utf-8'});return res.end(page_html);}
  if(u.pathname==='/audio'){
    if(u.searchParams.get('pin')!==S.pin){res.writeHead(401,{'content-type':'application/json'});return res.end(JSON.stringify({ok:false,error:'PIN incorreto.'}));}
    res.writeHead(200,{'content-type':'application/octet-stream'}); S.audioOpen=(S.audioOpen||0)+1; S.phase=S.phase||0;
    const t=setInterval(()=>{const b=Buffer.alloc(4800);for(let i=0;i<2400;i++){b.writeInt16LE(Math.round(8000*Math.sin((i+S.phase)*2*Math.PI*440/24000)),i*2);}S.phase+=2400;res.write(b);},100);
    req.on('close',()=>{clearInterval(t);S.audioClosed=(S.audioClosed||0)+1;});
    return;
  }
  if(u.pathname==='/api'){
    if(u.searchParams.get('pin')!==S.pin){res.writeHead(401,{'content-type':'application/json'});return res.end(JSON.stringify({ok:false,error:'PIN incorreto.'}));}
    res.writeHead(200,{'content-type':'application/json; charset=utf-8'});return res.end(JSON.stringify({ok:true,status:handle(u.searchParams.get('c'),u.searchParams.get('a')||'')}));}
  res.writeHead(404);res.end();
}).listen(0);
const base='http://127.0.0.1:'+server.address().port+'/';

const browser=await chromium.launch({args:['--autoplay-policy=no-user-gesture-required']});
const ctx=await browser.newContext({viewport:{width:390,height:900},isMobile:true,hasTouch:true});
const page=await ctx.newPage();
const errors=[]; page.on('pageerror',e=>errors.push(String(e)));
const dialogs=[]; page.on('dialog',d=>{dialogs.push(d.message());d.accept();});
await page.route(/ytimg\.com/,r=>r.fulfill({status:200,contentType:'image/png',body:Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==','base64')}));
const text=async id=>(await page.textContent('#'+id))||'';
const lastCall=c=>[...S.log].reverse().find(x=>x[0]===c);

// 1) PIN errado e certo
await page.goto(base);
ok(await page.isVisible('#pinbox')&&!(await page.isVisible('#app')),'pede o PIN');
await page.fill('#pin','9999'); await page.click('text=Entrar'); await page.waitForTimeout(400);
ok(await page.isVisible('#pinbox'),'PIN errado volta a pedir o PIN');
await page.fill('#pin','1234'); await page.click('text=Entrar'); await page.waitForSelector('#app',{state:'visible'});
ok((await text('st')).includes('Bíblia'),'PIN certo mostra o estado da Bíblia');

// 2) aba Ao vivo vazia
await page.click('#t-live'); await page.waitForFunction(()=>document.getElementById('reqhead').textContent.includes('(2)'));
ok((await text('nowline')).includes('Nenhuma música tocando'),'sem música: mensagem no telão');
ok(await page.isVisible('#nextempty')&&!(await page.isVisible('#nextbody')),'sem próxima: orienta a escolher um pedido');
const reqHtml=await page.innerHTML('#reqlist');
ok(reqHtml.includes('&lt;img')&&!reqHtml.includes('<img src=x'),'nome de pedido malicioso aparece como texto (não vira HTML)');
ok(await page.evaluate(()=>window.__xss===undefined),'nenhum script de pedido foi executado');

// 3) preparar pedido 1 -> verificando -> sugestão de confirmar
await page.locator('#reqlist button').first().click();
await page.waitForFunction(()=>document.getElementById('nextbody').style.display==='block');
ok((await text('nstatus')).includes('Verificando'),'mostra "verificando" enquanto confere a letra');
await page.waitForFunction(()=>document.getElementById('confirmbox').style.display==='block',null,{timeout:6000});
ok((await text('ntitle')).includes('Grande é o Senhor'),'título da próxima');
ok((await text('nextslide')).includes('Na cidade do nosso Deus'),'prévia da letra aparece no mini slide');
ok((await text('conftxt')).includes('Grande é o Senhor — Adhemar'),'sugestão de confirmar cita música e autor');
ok((await page.inputValue('#nt'))==='Grande é o Senhor'&&(await page.inputValue('#na'))==='Adhemar','campos Música/Autor já vêm preenchidos');
ok((await page.getAttribute('#thumb','src')).includes('i.ytimg.com/vi/REQ00000001/'),'miniatura do vídeo');
ok(dialogs.length===0,'preparar sem próxima anterior não pede confirmação');
ok((await text('reqhead')).includes('(1)'),'pedido usado sai da lista');

// 4) confirmar letra
await page.click('#confirmbox button'); await page.waitForFunction(()=>!document.getElementById('confirmbox')||document.getElementById('confirmbox').style.display==='none');
ok((await text('st')).includes('Letra confirmada e salva'),'resultado da confirmação na tela');
await page.waitForFunction(()=>document.getElementById('nstatus').textContent.includes('já cadastrada'));
ok(true,'depois de confirmar a letra passa a "já cadastrada"');
ok((await text('st')).includes('Letra confirmada'),'a mensagem de confirmação não é apagada pela atualização automática');

// 5) tentar preparar outro pedido com próxima existente => pergunta
dialogs.length=0;
await page.locator('#reqlist button').first().click(); await page.waitForTimeout(500);
ok(dialogs.some(d=>d.includes('Trocar a próxima')),'pede confirmação antes de trocar a próxima');
ok(lastCall('reqpick')[1]==='r2|1','troca enviada com flag de substituição');
// próxima agora sem letra -> aviso ao tocar
await page.waitForFunction(()=>document.getElementById('playbtn').textContent.includes('sem letra'),null,{timeout:6000});
dialogs.length=0; await page.click('#playbtn'); await page.waitForTimeout(500);
ok(dialogs.some(d=>d.includes('Atenção')&&d.includes('sem letra cadastrada')),'tocar sem letra mostra o aviso');
await page.waitForFunction(()=>document.getElementById('nowinfo').textContent.includes('tocando'));
ok(true,'começou a tocar');
ok((await text('nowline')).includes('Grande é o Senhor')&&(await text('nownext')).includes('E digno de louvor'),'telão mostra a frase atual e a próxima');
ok(await page.isVisible('#nextempty'),'depois de tocar a próxima some do cartão');

// 6) frases manuais e voltar ao automático
ok(!(await page.isVisible('#autobtn')),'botão "voltar ao automático" escondido no modo automático');
await page.click('text=Frase ▶'); await page.waitForFunction(()=>document.getElementById('autobtn').style.display==='block');
ok((await text('nowinfo')).includes('modo manual'),'mostra modo manual');
await page.click('#autobtn'); await page.waitForFunction(()=>document.getElementById('autobtn').style.display==='none');
ok(lastCall('auto')!==undefined,'voltar ao automático');

// 7) volume e avançar
await page.click('text=⏪ −10 s'); await page.waitForTimeout(200); ok(lastCall('seek')[1]==='-10','voltar 10 s');
await page.click('text=+10 s ⏩'); await page.waitForTimeout(200); ok(lastCall('seek')[1]==='10','avançar 10 s');
await page.click('text=🔊 +'); await page.waitForTimeout(200); ok(lastCall('vol')[1]==='10','volume +');
await page.click('text=🔉 −'); await page.waitForTimeout(200); ok(lastCall('vol')[1]==='-10','volume −');
await page.click('text=🔇 Mudo'); await page.waitForTimeout(200); ok(lastCall('vol')[1]==='mute'&&(await text('st')).includes('mudo'),'mudo');

// 8) pausar / continuar
await page.click('text=⏯ Pausar / continuar'); await page.waitForFunction(()=>document.getElementById('nowinfo').textContent.includes('pausada'));
ok(true,'pausar atualiza o estado');

// 9) biblioteca: buscar e escolher
await page.fill('#lq','santo'); await page.click('text=🔍 Buscar'); await page.waitForSelector('#liblist .item');
ok((await page.locator('#liblist .item').count())===1&&(await text('liblist')).includes('letra sincronizada'),'busca na biblioteca mostra o resultado e a situação da letra');
dialogs.length=0; await page.click('#liblist button:has-text("Próxima")'); await page.waitForFunction(()=>document.getElementById('nextbody').style.display==='block');
ok(lastCall('libpick')[1]==='LIB00000001|next'&&dialogs.length===0,'biblioteca: preparar como próxima (sem próxima anterior, sem confirmação)');
dialogs.length=0; await page.click('#liblist button:has-text("Tocar já")'); await page.waitForTimeout(500);
ok(dialogs.some(d=>d.includes('Tocar esta música agora')&&d.includes('será trocada')),'tocar já pede confirmação e avisa que troca a atual');
ok(lastCall('libpick')[1]==='LIB00000001|play','biblioteca: tocar já');
await page.fill('#lq','zzzz'); await page.click('text=🔍 Buscar'); await page.waitForFunction(()=>document.getElementById('liblist').textContent.includes('Nada encontrado'));
ok(true,'busca sem resultado informa');

// 10) encerrar
dialogs.length=0; await page.click('text=⏹ Encerrar'); await page.waitForTimeout(500);
ok(dialogs.some(d=>d.includes('Encerrar')),'encerrar pede confirmação');
await page.waitForFunction(()=>document.getElementById('nowline').textContent.includes('Nenhuma música tocando'));
ok(true,'encerrar limpa o telão');

// 11) verificar com nomes digitados
await page.fill('#lq','reconstr'); await page.click('text=🔍 Buscar'); await page.waitForSelector('#liblist .item');
await page.click('#liblist button:has-text("Próxima")'); await page.waitForFunction(()=>document.getElementById('nextbody').style.display==='block');
await page.fill('#nt','Reconstrução'); await page.fill('#na','Eliã Oliveira'); await page.click('text=🔎 Verificar com estes nomes'); await page.waitForTimeout(500);
ok(lastCall('nextcheck')[1]==='Reconstrução\nEliã Oliveira','verificar envia música e autor digitados separados por linha');
ok((await text('st')).includes('Procurado: Reconstrução — Eliã Oliveira'),'resposta da verificação aparece');

// 12) bíblia continua funcionando
await page.click('#t-bible'); await page.fill('#ref','João 3 16'); await page.click('text=Projetar'); await page.waitForTimeout(300);
ok(lastCall('ref')[1]==='João 3 16','aba Bíblia continua enviando a referência');


// 12b) novidades: limpar telão, não usar / criar letra, nomes, marcação por toque, salvas
await page.click('#t-live');
S.now={title:'Grande é o Senhor — Adhemar',state:'tocando',line:'Grande é o Senhor',nextLine:'E digno de louvor'}; S.manual=false; S.hasCues=true; S.hasText=false;
S.next=mkNext('REQ00000077','Santo — Fernandinho','synced'); S.checkingUntil=0; S.next.track='Fernandinho'; S.next.artist='Santo';
await page.waitForFunction(()=>document.getElementById('confirmbox').style.display==='block',null,{timeout:6000});
// salvas recentes (e texto malicioso só como texto)
ok((await text('recentlist')).includes('Aleluia <b>x</b> — Coral'),'últimas salvas listadas (texto, não HTML)');
ok((await page.locator('#recentlist b').count())===0,'sem HTML injetado na lista de salvas');
// limpar telão
dialogs.length=0; await page.click('text=🧹 Limpar telão'); await page.waitForTimeout(400);
ok(dialogs.some(d=>d.includes('Limpar o telão')),'limpar telão pede confirmação');
ok(lastCall('clearlive')!==undefined,'comando clearlive enviado');
await page.waitForFunction(()=>document.getElementById('autobtn').style.display==='block'); ok(true,'depois de limpar aparece "Voltar ao automático"');
await page.click('#autobtn'); await page.waitForFunction(()=>document.getElementById('autobtn').style.display==='none');
// trocar campos (client) e corrigir nomes (servidor)
ok((await page.inputValue('#nt'))==='Fernandinho'&&(await page.inputValue('#na'))==='Santo','campos vêm (errados) do computador');
await page.click('text=⇄ Trocar campos'); ok((await page.inputValue('#nt'))==='Santo'&&(await page.inputValue('#na'))==='Fernandinho','⇄ troca música e intérprete');
await page.click('text=✨ Corrigir nomes'); await page.waitForTimeout(600);
ok(lastCall('fixnames')!==undefined,'✨ Corrigir nomes chama o computador');
await page.waitForFunction(()=>document.getElementById('nt').value==='Santo'&&document.getElementById('na').value==='Fernandinho',null,{timeout:6000}); ok(true,'campos são atualizados com os nomes corrigidos pelo computador');
// não usar a sugestão
await page.click('#confirmbox button:has-text("Não usar")'); await page.waitForFunction(()=>document.getElementById('rejectedtxt').style.display==='block');
ok(await page.evaluate(()=>document.getElementById('confirmbox').style.display==='none'),'"Não usar" esconde a sugestão e mostra a explicação');
// criar letra pelo celular (próxima)
await page.click('#createbtn'); ok(await page.isVisible('#pastepanel'),'✎ Criar letra abre a caixa de colar');
await page.fill('#pastebox','curta'); await page.click('text=💾 Salvar letra'); await page.waitForTimeout(300);
ok((await text('st')).includes('Cole a letra completa'),'letra curta demais é recusada no celular'); ok(await page.isVisible('#pastepanel'),'caixa continua aberta');
const longLyrics='Santo, santo, santo\nSenhor Deus todo-poderoso\n\nA terra e o céu\ncheios estão da tua glória\n';
await page.fill('#pastebox',longLyrics); await page.click('text=💾 Salvar letra'); await page.waitForTimeout(600);
ok(lastCall('lyricsnext')[1]===longLyrics,'letra com acentos e quebras de linha chega inteira ao computador');
ok(!(await page.isVisible('#pastepanel'))&&(await page.inputValue('#pastebox'))==='','caixa fecha e limpa depois de salvar');
// música tocando sem tempos: estimar / marcar por toque
S.hasCues=false; S.hasText=true; S.tap={active:false,marked:0,total:0,nextLine:''};
await page.waitForFunction(()=>document.getElementById('nolyrics').style.display==='block',null,{timeout:6000});
ok((await text('nolyricstxt')).includes('em texto, sem tempos')&&await page.isVisible('#b-estimate')&&!(await page.isVisible('#b-paste-now')),'sem tempos: oferece estimar e marcar (e não colar)');
await page.click('#b-estimate'); await page.waitForFunction(()=>document.getElementById('nolyrics').style.display==='none'); ok(lastCall('estimate')!==undefined,'⏱ Estimar tempos');
S.hasCues=false; S.hasText=false; await page.waitForFunction(()=>document.getElementById('b-paste-now').style.display==='block',null,{timeout:6000}); ok(true,'sem texto: oferece colar a letra desta música');
await page.click('#b-paste-now'); ok((await text('pastehint')).includes('está tocando'),'colar para a música que toca agora');
await page.fill('#pastebox',longLyrics); await page.click('text=💾 Salvar letra'); await page.waitForTimeout(600); ok(lastCall('lyricsnow')!==undefined,'letra da música atual enviada');
await page.waitForFunction(()=>document.getElementById('b-tapstart').style.display==='block',null,{timeout:6000});
await page.click('#b-tapstart'); await page.waitForFunction(()=>document.getElementById('tappanel').style.display==='block');
ok((await text('taptxt')).includes('Estrofe 1 de 3')&&(await text('taptxt')).includes('Santo, santo, santo'),'painel de toque mostra a estrofe e o início dela');
await page.click('#tapbig'); await page.waitForTimeout(500); ok((await text('taptxt')).includes('Estrofe 2 de 3'),'toque avança a estrofe');
await page.click('text=↩ Desfazer'); await page.waitForTimeout(500); ok((await text('taptxt')).includes('Estrofe 1 de 3'),'desfazer volta uma estrofe');
S.tap.marked=3; await page.waitForFunction(()=>document.getElementById('tapbig').textContent.includes('CONCLUIR'),null,{timeout:6000}); ok(true,'depois da última estrofe o botão vira CONCLUIR');
await page.click('#tapbig'); await page.waitForFunction(()=>document.getElementById('tappanel').style.display==='none',null,{timeout:6000});
await page.waitForFunction(()=>document.getElementById('recentlist').textContent.includes('Música nova'),null,{timeout:6000}); ok(true,'música cadastrada pelo celular aparece em "Últimas salvas"');
S.hasCues=true; S.tap={active:false,marked:0,total:0,nextLine:''};


// 12c) som do computador no celular
S.level='55'; await page.click('#t-live'); await page.waitForFunction(()=>document.getElementById('vu').style.width==='55%',null,{timeout:4000});
ok((await text('vutxt')).includes('saindo som'),'medidor mostra que está saindo som no PC');
S.level='0'; await page.waitForFunction(()=>document.getElementById('vu').style.width==='0%',null,{timeout:4000});
ok((await text('vutxt')).includes('Silêncio'),'medidor mostra silêncio');
S.level='ERR:formato de áudio não suportado'; await page.waitForFunction(()=>document.getElementById('vutxt').textContent.includes('Não consegui medir'),null,{timeout:4000});
ok((await text('vutxt')).includes('formato de áudio'),'erro de captura aparece com o motivo'); S.level='40';
ok((await text('audiobtn')).includes('DESLIGADO'),'botão de áudio começa desligado');
await page.click('#audiobtn'); await page.waitForFunction(()=>window.aud&&window.aud.chunks>=6,null,{timeout:6000});
ok((await text('audiobtn')).includes('LIGADO'),'botão mostra LIGADO ao ouvir');
const stats=await page.evaluate(()=>({chunks:aud.chunks,dropped:aud.dropped,state:aud.ctx.state,time:aud.ctx.currentTime,next:aud.next}));
ok(stats.chunks>=6&&stats.dropped===0,'blocos de áudio chegam e são agendados sem descartes: '+JSON.stringify(stats));
ok(stats.next>stats.time&&stats.next-stats.time<1.5,'atraso fica abaixo de 1,5 s (ao vivo): '+(stats.next-stats.time).toFixed(2));
await page.click('#audiobtn'); await page.waitForTimeout(500);
ok((await text('audiobtn')).includes('DESLIGADO'),'desligar volta ao estado desligado');
ok((S.audioClosed||0)>=1,'ao desligar a conexão de áudio é fechada (o PC deixa de enviar)');
S.pin='0000'; await page.click('#audiobtn'); await page.waitForFunction(()=>document.getElementById('st').textContent.includes('O áudio parou'),null,{timeout:4000});
ok((await text('st')).includes('PIN incorreto')&&(await text('audiobtn')).includes('DESLIGADO'),'PIN errado no áudio: avisa e fica desligado'); S.pin='1234';
await page.evaluate(()=>{localStorage.setItem('lyra-pin','1234');pin='1234';show();});


// 12d) link colado, fila, tocar sozinha, buscar texto
S.next=null; S.queue=[]; S.requests=[{id:'rq1',name:'Pedido um',group:'Jovens',videoId:'REQ00000011',lyrics:'🟡 Letra só em texto'}]; S.now={title:'Tocando — X',state:'tocando',line:'linha',nextLine:'outra'}; S.hasCues=false; S.hasText=false; S.tap={active:false,marked:0,total:0,nextLine:''};
await page.waitForFunction(()=>document.getElementById('nextempty').style.display!=='none',null,{timeout:6000});
await page.fill('#nl','isso não é link'); await page.click('text=▶ Como próxima'); await page.waitForTimeout(400);
ok((await text('st')).includes('Esse link não é')&&(await page.inputValue('#nl'))==='isso não é link','link inválido: avisa e mantém o texto digitado');
await page.fill('#nl','https://youtu.be/abcdefghijk'); dialogs.length=0; await page.click('text=▶ Como próxima'); await page.waitForTimeout(500);
ok(lastCall('setnext')[1]==='0|https://youtu.be/abcdefghijk'&&dialogs.length===0,'link vira a próxima (sem próxima antes: sem confirmação)');
ok((await page.inputValue('#nl'))==='','campo do link é limpo depois de usar');
await page.waitForFunction(()=>document.getElementById('nextbody').style.display==='block'); ok(true,'a próxima aparece no cartão');
await page.fill('#nl','https://youtu.be/zzzzzzzzzzz'); dialogs.length=0; await page.click('text=▶ Como próxima'); await page.waitForTimeout(500);
ok(dialogs.some(d=>d.includes('Trocar a próxima'))&&lastCall('setnext')[1]==='1|https://youtu.be/zzzzzzzzzzz','com próxima já preparada: pede confirmação e troca');
await page.fill('#nl','https://youtu.be/qqqqqqqqqqq'); await page.click('text=➕ Na fila'); await page.waitForTimeout(500);
ok(lastCall('qadd')[1]==='url|https://youtu.be/qqqqqqqqqqq','➕ Na fila envia o link');
await page.waitForFunction(()=>document.getElementById('queuecard').style.display==='block',null,{timeout:6000});
ok((await text('queuehead')).includes('(1)')&&(await text('queuelist')).includes('1. url:https://youtu.be/qqqqqqqqqqq'),'fila aparece com a música');
await page.locator('#reqlist button:has-text("Fila")').first().click(); await page.waitForTimeout(400);
ok(lastCall('qadd')[1]==='req|rq1','pedido ➕ Fila');
await page.waitForFunction(()=>document.getElementById('queuehead').textContent.includes('(2)'),null,{timeout:6000}); ok(true,'fila com 2 músicas');
await page.click('#liblist button:has-text("Fila")').catch(()=>{});
await page.fill('#lq','santo'); await page.click('text=🔍 Buscar'); await page.waitForSelector('#liblist .item');
await page.locator('#liblist button:has-text("Fila")').first().click(); await page.waitForTimeout(400); ok(lastCall('qadd')[1]==='LIB00000001'.replace(/^/,'lib|'),'biblioteca ➕ Fila');
await page.locator('#queuelist button:has-text("✖")').first().click(); await page.waitForTimeout(500); ok(lastCall('qremove')[1]==='0','✖ tira da fila');
dialogs.length=0; await page.click('text=🗑 Limpar fila'); await page.waitForTimeout(500); ok(dialogs.some(d=>d.includes('Limpar a fila'))&&lastCall('qclear')!==undefined,'limpar fila pede confirmação');
await page.waitForFunction(()=>document.getElementById('queuecard').style.display==='none',null,{timeout:6000}); ok(true,'fila vazia some');
// tocar a próxima sozinha
ok((await text('advbtn')).includes('DESLIGADO'),'tocar sozinha começa desligado'); await page.click('#advbtn'); await page.waitForFunction(()=>document.getElementById('advbtn').textContent.includes('LIGADO')&&!document.getElementById('advbtn').textContent.includes('DESLIGADO'),null,{timeout:6000}); ok(true,'ligar tocar sozinha');
// buscar texto automático (só com chave)
ok(!(await page.isVisible('#fetchbtn'))&&!(await page.isVisible('#b-fetch-now')),'sem chave do Vagalume: botões de buscar texto escondidos');
S.canFetchText=true; await page.waitForFunction(()=>document.getElementById('fetchbtn').style.display==='block'&&document.getElementById('b-fetch-now').style.display==='block',null,{timeout:6000}); ok(true,'com chave: botões de buscar texto aparecem (próxima e música atual)');
await page.fill('#nt','Santo'); await page.fill('#na','Fernandinho'); await page.click('#fetchbtn'); await page.waitForTimeout(500);
ok(lastCall('lyricsfetch')[1]==='next|Santo\nFernandinho'&&(await text('st')).includes('Vagalume'),'buscar texto da próxima envia música e intérprete');
await page.click('#b-fetch-now'); await page.waitForTimeout(500); ok(lastCall('lyricsfetch')[1]==='now|','buscar texto da música que toca');
S.canFetchText=false; S.hasCues=true; S.next=null; S.queue=[];

// 13) PIN trocado no computador: volta a pedir
S.pin='7777'; await page.click('#t-live'); await page.waitForFunction(()=>document.getElementById('pinbox').style.display==='block',null,{timeout:6000});
ok(true,'PIN trocado: celular volta a pedir o PIN');
await page.screenshot({path:'/tmp/shot_phone_pin.png'});
ok(errors.length===0,'sem erros de JavaScript: '+errors.join('|'));

// captura bonita da aba ao vivo (estado de exemplo)
S.pin='1234'; S.now={title:'Grande é o Senhor — Adhemar',state:'tocando',line:'Grande é o Senhor\ne digno de louvor',nextLine:'Na cidade do nosso Deus'};
S.next=mkNext('REQ00000009','Santo — Fernandinho','synced'); S.checkingUntil=0; S.requests=[{id:'r9',name:'Reconstrução',group:'Jovens',lyrics:'🟡 Letra só em texto (sem tempos)'}];
await page.evaluate(()=>{localStorage.setItem('lyra-pin','1234');}); await page.goto(base); await page.click('#t-live'); await page.waitForSelector('#confirmbox',{state:'visible'});
await page.waitForTimeout(500); await page.screenshot({path:'/tmp/shot_phone_live.png',fullPage:true});
await browser.close(); server.close();
console.log(`celular: ${pass} ok, ${fail} falhas`); process.exit(fail?1:0);
