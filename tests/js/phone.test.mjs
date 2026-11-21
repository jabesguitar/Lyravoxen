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
 log:[],checkingUntil:0};
const live=()=>({now:{title:S.now?S.now.title:'',state:S.now?S.now.state:'parada',line:S.now?S.now.line:'',next:S.now?S.now.nextLine:'',manual:!!(S.now&&S.manual),cues:S.now?12:0},
 next:S.next?{videoId:S.next.videoId,title:S.next.title,status:Date.now()<S.checkingUntil?'checking':S.next.status,headline:Date.now()<S.checkingUntil?'🔎 Verificando se tem letra…':S.next.headline,preview:Date.now()<S.checkingUntil?[]:S.next.preview,track:S.next.track,artist:S.next.artist,canConfirm:Date.now()>=S.checkingUntil&&S.next.status==='synced',warning:S.next.warning}:null,
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
   default:return 'Comando desconhecido.';
  }
}
const server=http.createServer((req,res)=>{
  const u=new URL(req.url,'http://x');
  if(u.pathname==='/'){res.writeHead(200,{'content-type':'text/html; charset=utf-8'});return res.end(page_html);}
  if(u.pathname==='/api'){
    if(u.searchParams.get('pin')!==S.pin){res.writeHead(401,{'content-type':'application/json'});return res.end(JSON.stringify({ok:false,error:'PIN incorreto.'}));}
    res.writeHead(200,{'content-type':'application/json; charset=utf-8'});return res.end(JSON.stringify({ok:true,status:handle(u.searchParams.get('c'),u.searchParams.get('a')||'')}));}
  res.writeHead(404);res.end();
}).listen(0);
const base='http://127.0.0.1:'+server.address().port+'/';

const browser=await chromium.launch();
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
