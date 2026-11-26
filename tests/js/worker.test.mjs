import fs from 'fs';
import { buildWorker } from './build.mjs';
let pass=0, fail=0;
const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };
const code = buildWorker({number:'5582999999999', groupName:'Louvor Jovem', groupLink:'https://chat.whatsapp.com/AbCdEfGhIjKlMn'});
fs.writeFileSync('/tmp/worker_built.mjs', code);
const mod = await import('/tmp/worker_built.mjs');
const worker = mod.default;
// KV falso (com TTL e list por prefixo)
class KV { constructor(){this.m=new Map();this.ops={put:0,list:0,get:0,del:0};}
 async get(k){this.ops.get++;const e=this.m.get(k);return e?e.v:null;}
 async put(k,v,o){this.ops.put++;this.m.set(k,{v,ttl:o&&o.expirationTtl});}
 async delete(k){this.ops.del++;this.m.delete(k);}
 async list(o){this.ops.list++;const keys=[...this.m.keys()].filter(k=>k.startsWith(o.prefix||'')).sort().slice(0,o.limit||1000).map(name=>({name}));return {keys};}
}
const env={PEDIDOS:new KV(),ADMIN_TOKEN:'segredo-de-teste-123'};
const call=(method,path,{headers={},body}={})=>worker.fetch(new Request('https://x.workers.dev'+path,{method,headers,body}),env);
const auth={authorization:'Bearer segredo-de-teste-123'};
const jsonH={'content-type':'application/json'};

// página pública
let r=await call('GET','/pedido'); let t=await r.text();
ok(r.status===200&&t.includes('<form')&&t.includes('/enviar'),'página /pedido servida');
ok(!t.includes('segredo'),'senha não aparece na página');
r=await call('GET','/'); ok(r.status===200,'raiz serve a página');
// CORS
r=await call('OPTIONS','/enviar'); ok(r.headers.get('access-control-allow-origin')==='*','CORS preflight');

// envio JSON válido
r=await call('POST','/enviar',{headers:jsonH,body:JSON.stringify({text:'🎵 Pedido\nConjunto: Jovens\n1. Santo - https://youtu.be/abcdefghijk'})});
let j=await r.json(); ok(r.status===200&&j.ok,'envio JSON válido');
ok(r.headers.get('access-control-allow-origin')==='*','CORS na resposta do envio');
// sem link
r=await call('POST','/enviar',{headers:jsonH,body:JSON.stringify({text:'oi pessoal'})}); j=await r.json(); ok(r.status===400&&!j.ok,'sem link é recusado');
// vazio
r=await call('POST','/enviar',{headers:jsonH,body:JSON.stringify({text:'   '})}); ok(r.status===400,'vazio recusado');
// grande
r=await call('POST','/enviar',{headers:jsonH,body:JSON.stringify({text:'https://youtu.be/x '.repeat(500)})}); ok(r.status===413,'texto grande recusado');
// formulário simples (sem JS): urlencoded
r=await call('POST','/enviar',{headers:{'content-type':'application/x-www-form-urlencoded'},body:'text='+encodeURIComponent('Conjunto: Teste\n1. Aleluia - https://www.youtube.com/watch?v=12345678901')});
t=await r.text(); ok(r.status===200&&t.includes('Pedido enviado')&&(r.headers.get('content-type')||'').includes('text/html'),'formulário sem JavaScript devolve página de agradecimento');
// multipart (alguns navegadores)
const fd=new FormData(); fd.append('text','Conjunto: M\nhttps://youtu.be/zzzzzzzzzzz');
r=await worker.fetch(new Request('https://x.workers.dev/enviar',{method:'POST',body:fd}),env); ok(r.status===200,'multipart aceito');
// formulário sem link devolve HTML de erro
r=await call('POST','/enviar',{headers:{'content-type':'application/x-www-form-urlencoded'},body:'text=oi'}); t=await r.text(); ok(r.status===400&&t.includes('Voltar'),'erro em HTML no formulário simples');

// leitura exige senha
r=await call('GET','/pedidos'); ok(r.status===401,'/pedidos sem senha = 401');
r=await call('GET','/pedidos',{headers:{authorization:'Bearer errada'}}); ok(r.status===401,'/pedidos senha errada = 401');
r=await call('GET','/pedidos?token=segredo-de-teste-123'); ok(r.status===401,'senha na URL não vale');
r=await call('GET','/ping',{headers:auth}); j=await r.json(); ok(r.status===200&&j.ok,'/ping com senha');
r=await call('GET','/ping'); ok(r.status===401,'/ping sem senha');
r=await call('POST','/confirmar',{headers:jsonH,body:JSON.stringify({ids:['p:x']})}); ok(r.status===401,'/confirmar sem senha = 401');

// busca com versão
r=await call('GET','/pedidos?v=',{headers:auth}); j=await r.json();
ok(j.ok&&j.pedidos.length===3&&j.v,'busca traz os 3 pedidos válidos (JSON+form+multipart) -> '+j.pedidos.length);
ok(j.pedidos.every(p=>p.id.startsWith('p:')&&p.text&&p.at),'campos dos pedidos');
const v=j.v; const ids=j.pedidos.map(p=>p.id);
// mesma versão => nada novo, sem listar
const listsBefore=env.PEDIDOS.ops.list;
r=await call('GET','/pedidos?v='+encodeURIComponent(v),{headers:auth}); j=await r.json();
ok(j.pedidos.length===0&&env.PEDIDOS.ops.list===listsBefore,'mesma versão: nada novo e sem gastar listagem');
// novo envio muda a versão
await call('POST','/enviar',{headers:jsonH,body:JSON.stringify({text:'https://youtu.be/novonovonov'})});
r=await call('GET','/pedidos?v='+encodeURIComponent(v),{headers:auth}); j=await r.json();
ok(j.pedidos.length===4&&j.v!==v,'novo pedido muda a versão e traz a lista');
// vários computadores veem os mesmos pedidos (sem apagar)
r=await call('GET','/pedidos?v=',{headers:auth}); j=await r.json(); ok(j.pedidos.length===4,'segundo computador vê os mesmos pedidos');
// TTL de 12 h
ok([...env.PEDIDOS.m.entries()].filter(([k])=>k.startsWith('p:')).every(([,e])=>e.ttl===43200),'pedidos expiram em 12 h');
// confirmar apaga (compatibilidade)
r=await call('POST','/confirmar',{headers:{...jsonH,...auth},body:JSON.stringify({ids:[ids[0],'ultimo','../x']})}); j=await r.json();
ok(j.ok&&j.n===1&&env.PEDIDOS.m.has('ultimo'),'confirmar só apaga chaves p:');
// IP de origem: identifica o remetente no Worker e permite bloqueio/desbloqueio autenticado.
const senderHeaders={...jsonH,'CF-Connecting-IP':'198.51.100.42'};
r=await call('POST','/enviar',{headers:senderHeaders,body:JSON.stringify({text:'https://youtu.be/iptest00001'})});
ok(r.status===200,'pedido com IP é aceito antes do bloqueio');
let stored=[...env.PEDIDOS.m.entries()].find(([k])=>k.startsWith('p:')&&JSON.parse(env.PEDIDOS.m.get(k).v).ip==='198.51.100.42');
ok(!!stored,'IP público foi gravado junto ao pedido');
r=await call('POST','/bloqueios',{headers:{...jsonH,...auth},body:JSON.stringify({ip:'198.51.100.42',name:'Celular teste',blocked:true})});
j=await r.json();ok(r.status===200&&j.ok,'bloqueio autenticado é criado');
r=await call('POST','/enviar',{headers:senderHeaders,body:JSON.stringify({text:'https://youtu.be/iptest00002'})});
j=await r.json();ok(r.status===403&&!j.ok,'IP bloqueado não consegue enviar');
r=await call('GET','/bloqueios',{headers:auth});j=await r.json();ok(j.blocked.some(x=>x.ip==='198.51.100.42'&&x.name==='Celular teste'),'lista de bloqueios devolve IP e nome');
r=await call('POST','/bloqueios',{headers:{...jsonH,...auth},body:JSON.stringify({ip:'198.51.100.42',blocked:false})});
j=await r.json();ok(r.status===200&&j.ok,'desbloqueio autenticado funciona');
r=await call('POST','/enviar',{headers:senderHeaders,body:JSON.stringify({text:'https://youtu.be/iptest00003'})});
ok(r.status===200,'remetente pode enviar novamente após desbloqueio');

// fila cheia
for(let i=0;i<150;i++) env.PEDIDOS.m.set('p:cheio'+String(i).padStart(4,'0'),{v:'{}'});
r=await call('POST','/enviar',{headers:jsonH,body:JSON.stringify({text:'https://youtu.be/xxxxxxxxxxx'})}); ok(r.status===429,'fila cheia = 429');
// rota inexistente e método errado
r=await call('GET','/nada'); ok(r.status===404,'404');
r=await call('GET','/enviar'); ok(r.status===404,'GET /enviar não existe');
// sem KV configurado -> mensagem útil
const r2=await worker.fetch(new Request('https://x.workers.dev/enviar',{method:'POST',headers:jsonH,body:JSON.stringify({text:'https://youtu.be/xxxxxxxxxxx'})}),{});
j=await r2.json(); ok(r2.status===500&&/armazenamento/.test(j.status),'sem binding PEDIDOS: erro explicado');
// sem ADMIN_TOKEN configurado ninguém lê
const r3=await worker.fetch(new Request('https://x.workers.dev/pedidos',{headers:{authorization:'Bearer '}}),{PEDIDOS:new KV()}); ok(r3.status===401,'sem ADMIN_TOKEN: leitura bloqueada');
// Limite por IP: até cinco pedidos em dez minutos, independente da fila global.
const rateEnv={PEDIDOS:new KV(),ADMIN_TOKEN:'segredo-de-teste-123'};
for(let i=0;i<5;i++){
 const rr=await worker.fetch(new Request('https://x.workers.dev/enviar',{method:'POST',headers:{...jsonH,'CF-Connecting-IP':'203.0.113.77'},body:JSON.stringify({text:'https://youtu.be/rate'+String(i).padStart(8,'0')})}),rateEnv);
 ok(rr.status===200,'limite por IP permite envio '+(i+1));
}
r=await worker.fetch(new Request('https://x.workers.dev/enviar',{method:'POST',headers:{...jsonH,'CF-Connecting-IP':'203.0.113.77'},body:JSON.stringify({text:'https://youtu.be/rate0000006'})}),rateEnv);
j=await r.json();ok(r.status===429&&/5 pedidos por IP/.test(j.status),'sexto pedido em dez minutos é limitado por IP');
r=await worker.fetch(new Request('https://x.workers.dev/enviar',{method:'POST',headers:{...jsonH,'CF-Connecting-IP':'203.0.113.78'},body:JSON.stringify({text:'https://youtu.be/rateother001'})}),rateEnv);
ok(r.status===200,'limite de um IP não afeta outro IP');

console.log(`worker: ${pass} ok, ${fail} falhas`); process.exit(fail?1:0);
