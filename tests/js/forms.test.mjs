import { createRequire } from 'module';
const require = createRequire((process.env.PW_MODULES||'/opt/npm-tools/node_modules/'));
const { chromium } = require('playwright');
import http from 'http';
import fs from 'fs';
import { buildForm } from './build.mjs';
let pass=0, fail=0; const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };

// "serviço" falso com CORS
const received=[];
const server=http.createServer((req,res)=>{
  const cors={'Access-Control-Allow-Origin':'*','Access-Control-Allow-Headers':'content-type','Access-Control-Allow-Methods':'POST,OPTIONS'};
  if(req.method==='OPTIONS'){res.writeHead(204,cors);return res.end();}
  let body=''; req.on('data',d=>body+=d); req.on('end',()=>{
    received.push({url:req.url,type:req.headers['content-type']||'',body});
    if((req.headers['content-type']||'').includes('json')){res.writeHead(200,{...cors,'content-type':'application/json'});res.end(JSON.stringify({ok:true,status:'Pedido enviado! O operador do som já pode ver.'}));}
    else{res.writeHead(200,{'content-type':'text/html; charset=utf-8'});res.end('<h2>Pedido enviado!</h2>');}
  });
}).listen(0);
const port=server.address().port;
const sendUrl='http://127.0.0.1:'+port+'/enviar';
const settings={number:'5582999999999',groupName:'Louvor Jovem',groupLink:'https://chat.whatsapp.com/AbCdEfGhIjKlMn'};
fs.writeFileSync('/tmp/form_cloud.html',buildForm(settings,sendUrl));
fs.writeFileSync('/tmp/form_wa.html',buildForm(settings,''));
const browser=await chromium.launch();

// ---- 1) COM JavaScript, com nuvem
{
 const ctx=await browser.newContext({viewport:{width:390,height:800}}); const page=await ctx.newPage();
 const nav=[]; await page.route(/wa\.me|whatsapp\.com/,r=>{nav.push(r.request().url());r.abort();});
 const errors=[]; page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('file:///tmp/form_cloud.html');
 ok(await page.isVisible('#fancy')&&!(await page.isVisible('#plain')),'JS ligado: formulário completo visível e o simples escondido');
 ok((await page.locator('body').innerText()).includes('IP público')&&(await page.locator('body').innerText()).includes('MAC não é coletado'),'aviso transparente sobre IP e MAC no formulário público');
 ok((await page.locator('.card').count())===1,'começa com 1 cartão');
 await page.click('#b-add'); ok((await page.locator('.card').count())===2,'adicionar música');
 await page.fill('#g','Jovens'); await page.locator('.nm').nth(0).fill('Santo'); await page.locator('.lk').nth(0).fill('https://youtu.be/abcdefghijk');
 await page.click('#b-build'); ok(await page.isVisible('#out'),'mensagem gerada');
 const msg=await page.inputValue('#msg');
 ok(msg.includes('Conjunto: Jovens')&&msg.includes('1. Santo - https://youtu.be/abcdefghijk')&&msg.includes('#LYRA:'),'mensagem tem conjunto, música e marca #LYRA');
 const token=/#LYRA:([A-Za-z0-9+/=]+)/.exec(msg)[1]; const decoded=JSON.parse(decodeURIComponent(escape(Buffer.from(token,'base64').toString('binary'))));
 ok(decoded.grupo==='Jovens'&&decoded.itens[0].nome==='Santo','marca #LYRA decodifica certo (UTF-8)');
 ok(await page.isVisible('#b-send')&&await page.isVisible('#b-grp')&&await page.isVisible('#b-open'),'botões de nuvem e grupo visíveis');
 await page.click('#b-send'); await page.waitForFunction(()=>document.getElementById('st').textContent.includes('✅'));
 ok(received.length===1&&received[0].type.includes('json')&&JSON.parse(received[0].body).text.includes('#LYRA:'),'XHR chegou ao serviço com o texto');
 ok(!(await page.isVisible('#b-send')),'botão de enviar some depois de enviado');
 await page.click('#b-wa'); await page.waitForTimeout(300);
 ok(nav.some(u=>u.startsWith('https://wa.me/5582999999999?text=')&&decodeURIComponent(u).includes('Pedido de músicas')),'botão WhatsApp abre wa.me/<número>?text=');
 await page.goto('file:///tmp/form_cloud.html'); await page.fill('.lk','https://youtu.be/abcdefghijk'); await page.click('#b-build'); nav.length=0;
 await page.click('#b-grp'); await page.waitForTimeout(300);
 ok(nav.some(u=>u.startsWith('https://wa.me/?text=')),'botão do grupo abre a lista de conversas com o texto');
 await page.goto('file:///tmp/form_cloud.html'); await page.click('#b-build'); ok((await page.textContent('#st')).includes('pelo menos um link'),'sem link: avisa');
 await page.screenshot({path:'/tmp/shot_form_js.png'});
 ok(errors.length===0,'sem erros de JavaScript: '+errors.join('|'));
 await ctx.close();
}
// ---- 2) SEM JavaScript, com nuvem
{
 const ctx=await browser.newContext({javaScriptEnabled:false,viewport:{width:390,height:800}}); const page=await ctx.newPage();
 const nav=[]; await page.route(/wa\.me/,r=>{nav.push(r.request().url());r.abort();});
 received.length=0;
 await page.goto('file:///tmp/form_cloud.html');
 ok(await page.isVisible('#plain')&&!(await page.isVisible('#fancy')),'sem JS: formulário simples visível');
 ok((await page.locator('body').innerText()).includes('IP público'),'aviso de IP também aparece sem JavaScript');
 await page.fill('textarea[name=text]','Conjunto: Teste\n1. Aleluia - https://youtu.be/abcdefghijk');
 await page.click('button.main');
 await page.waitForLoadState('load');
 ok(received.length===1&&received[0].type.includes('x-www-form-urlencoded')&&decodeURIComponent(received[0].body.replace(/\+/g,' ')).includes('Aleluia'),'POST sem JS chega ao serviço (urlencoded, UTF-8)');
 ok((await page.content()).includes('Pedido enviado'),'mostra a resposta do serviço');
 await page.goto('file:///tmp/form_cloud.html'); await page.fill('textarea[name=text]','Conjunto: X\nhttps://youtu.be/abcdefghijk');
 await page.click('button[formaction="https://wa.me/5582999999999"]'); await page.waitForTimeout(300);
 ok(nav.some(u=>u.startsWith('https://wa.me/5582999999999?text=')),'sem JS: botão WhatsApp envia por GET com ?text=');
 await page.goto('file:///tmp/form_cloud.html');
 ok((await page.locator('a[href="https://chat.whatsapp.com/AbCdEfGhIjKlMn"]').count())===1,'link do grupo presente');
 nav.length=0; await page.click('button.grp'); await page.waitForTimeout(300);
 ok(nav.some(u=>u.startsWith('https://wa.me/?text=')),'sem JS: botão do grupo abre a lista de conversas');
 await page.screenshot({path:'/tmp/shot_form_nojs.png'});
 await ctx.close();
}
// ---- 3) Só WhatsApp (sem nuvem), sem JS
{
 const ctx=await browser.newContext({javaScriptEnabled:false}); const page=await ctx.newPage();
 const nav=[]; await page.route(/wa\.me/,r=>{nav.push(r.request().url());r.abort();});
 await page.goto('file:///tmp/form_wa.html');
 ok((await page.locator('#plain button:has-text("Enviar pedido ao operador")').count())===0,'sem nuvem: o formulário simples não mostra botão da nuvem');
 await page.fill('textarea[name=text]','Conjunto: X\nhttps://youtu.be/abcdefghijk'); await page.click('button.main'); await page.waitForTimeout(300);
 ok(nav.some(u=>u.startsWith('https://wa.me/5582999999999?text=')),'sem nuvem e sem JS: envia ao WhatsApp');
 await ctx.close();
}
// ---- 4) JS com erro de rede no envio: mensagem clara
{
 const ctx=await browser.newContext(); const page=await ctx.newPage();
 await page.goto('file:///tmp/form_cloud.html'); await page.fill('.lk','https://youtu.be/abcdefghijk'); await page.click('#b-build');
 await page.evaluate(()=>{window.XMLHttpRequest.prototype.send=function(){this.readyState;Object.defineProperty(this,'status',{value:0});Object.defineProperty(this,'readyState',{value:4});this.responseText='';this.onreadystatechange();};});
 await page.click('#b-send'); ok((await page.textContent('#st')).includes('Não foi possível enviar'),'falha de rede: mensagem orienta a usar o WhatsApp');
 ok(await page.isVisible('#b-send'),'botão de enviar continua disponível para tentar de novo');
 await ctx.close();
}
await browser.close(); server.close();
console.log(`formulários: ${pass} ok, ${fail} falhas`); process.exit(fail?1:0);
