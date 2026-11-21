import fs from 'fs';
const P=JSON.parse(fs.readFileSync('./patterns.json','utf8'));
let pass=0,fail=0; const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };
const eq=(a,b,m)=>ok(JSON.stringify(a)===JSON.stringify(b),m+' -> '+JSON.stringify(a));
// .NET: \b é Unicode; em JS emulamos com lookarounds
const uniB=src=>{let n=0;return src.replace(/\\b/g,()=>(n++%2===0)?'(?<![\\p{L}\\p{N}_])':'(?![\\p{L}\\p{N}_])');};
const R={
 header:new RegExp(P.chatHeader,'u'), group:new RegExp(P.groupLine,'iu'), link:new RegExp(P.link,'i'),
 nameLead:new RegExp(P.nameClean,'g'), hasLink:new RegExp(P.hasLink,'i'), token:new RegExp(P.token),
 brackets:new RegExp(P.cleanBrackets,'g'), words:new RegExp(uniB(P.cleanWords),'giu'),
 lrcTag:new RegExp(P.lrcTag,'g'), lrcLead:new RegExp(P.lrcLead), split:new RegExp(P.split)
};
// ---- portes fiéis dos métodos C#
function splitChat(text){const msgs=[];for(const raw of (text||'').replace(/\r/g,'').split('\n')){const m=R.header.exec(raw);
  if(m){msgs.push({sender:m.groups.sender.trim(),body:m.groups.msg});} else if(msgs.length) msgs[msgs.length-1].body+='\n'+raw; else msgs.push({sender:'',body:raw});}return msgs;}
function looksLikeRequest(t){ if(!t||!t.trim()||t.length>20000) return false; if(t.includes('#LYRA:')) return true; if(!R.hasLink.test(t)) return false;
  if(t.toLowerCase().includes('pedido de músicas')) return true;
  for(const raw of t.replace(/\r/g,'').split('\n')){const line=raw.trim(); if(R.group.test(line)) return true; const h=R.header.exec(line); if(h&&R.group.test(h.groups.msg.trim())) return true;}
  return splitChat(t).filter(m=>m.sender.length>0).length>0;}
function messageToJson(text,def=''){ if(!text||!text.trim()) return null;
  const tk=R.token.exec(text);
  if(tk){try{const j=Buffer.from(tk[1],'base64').toString('utf8'); if(!def.trim()) return JSON.parse(j); const o=JSON.parse(j); if(!(o.grupo||'').trim()) o.grupo=def; return o;}catch(e){}}
  let group=def; const items=[];
  for(const raw of text.split('\n')){const line=raw.trim(); if(!line) continue; const g=R.group.exec(line); if(g){group=g[1];continue;}
   const l=R.link.exec(line); if(!l) continue; const name=line.slice(0,l.index).replace(R.nameLead,''); items.push({nome:name,link:l[0].replace(/[),.;]+$/,'')});}
  return items.length?{grupo:group,itens:items}:null;}
const messagesToJson=(t,last=40)=>splitChat(t).slice(-last).map(m=>messageToJson(m.body,m.sender)).filter(Boolean);
const normalize=v=>!v||!v.trim()?'':v.normalize('NFD').replace(/\p{Mn}/gu,'').toLowerCase().replace(/[^a-z0-9]+/g,' ').trim();
const cleanTitle=t=>{let x=(t||'').replace(R.brackets,' ').replace(R.words,' ');return x.replace(/\s+/g,' ').trim().replace(/^[ \-–—|:/]+|[ \-–—|:/]+$/g,'');};
const buildQueries=t=>{const c=cleanTitle(t);const q=[];if(c)q.push(c);const parts=c.split(new RegExp(P.split)).map(x=>x.trim()).filter(Boolean);if(parts.length>=2)q.push(...parts.slice(0,2));return [...new Set(q.map(x=>x))].slice(0,3);};
function parseLrc(lrc){const cues=[];for(const raw of (lrc||'').replace(/\r/g,'').split('\n')){const tags=[...raw.matchAll(R.lrcTag)];if(!tags.length)continue;const text=raw.replace(R.lrcLead,'').trim();if(!text)continue;
  for(const t of tags){cues.push({seconds:parseInt(t[1])*60+parseFloat(t[2].replace(',','.')),text});}}return cues.sort((a,b)=>a.seconds-b.seconds);}

// ---- cabeçalhos de conversa
const U='https://youtu.be/abcdefghijk';
let m=splitChat(`19/11/2026 20:15 - Maria: Conjunto: Jovens\n1. Santo - ${U}\n2. Aleluia - https://youtu.be/bbbbbbbbbbb\n19/11/2026 20:16 - João: oi gente`);
eq(m.map(x=>x.sender),['Maria','João'],'Android: remetentes'); ok(m[0].body.includes('2. Aleluia'),'Android: linhas seguintes pertencem à mensagem');
eq(splitChat(`\u200e[19/11/2026 20:15:10] Pedro: Santo ${U}`).map(x=>x.sender),['Pedro'],'iPhone com marca invisível (LRM)');
eq(splitChat(`[20:15, 19/11/2026] Lia: Santo ${U}`).map(x=>x.sender),['Lia'],'WhatsApp Web');
eq(splitChat(`11/19/26, 7:15 PM - Ana: Santo ${U}`).map(x=>x.sender),['Ana'],'Android em inglês (AM/PM)');
eq(splitChat(`19/11/2026, 7:15\u202fPM - Ana: Santo ${U}`).map(x=>x.sender),['Ana'],'espaço fino antes de PM');
eq(splitChat(`19/11/2026 20:15 - +55 82 99999-9999: Santo ${U}`).map(x=>x.sender),['+55 82 99999-9999'],'remetente é só o número');
eq(splitChat(`19/11/2026 20:15 - ~ Carlos: Santo ${U}`).map(x=>x.sender),['~ Carlos'],'remetente com ~');
eq(splitChat(`19/11/2026 20:15 - As mensagens são protegidas`).length,1,'mensagem de sistema sem ":" não quebra');
eq(splitChat(`Conjunto: X\n1. A - ${U}`).map(x=>x.sender),[''],'texto sem cabeçalho = 1 mensagem sem remetente');
eq(splitChat(`19/11/2026 20:15 - Rui: Hora 20:30: ensaio ${U}`)[0].sender,'Rui','"20:30:" no meio do texto não vira remetente');

// ---- pedidos
eq(messagesToJson(`19/11/2026 20:15 - Maria: Conjunto: Jovens\n1. Santo - ${U}\n19/11/2026 20:16 - Joao: Aleluia ${U.replace('abcdefghijk','zzzzzzzzzzz')}\n19/11/2026 20:17 - Ana: oi`).map(x=>x.grupo),['Jovens','Joao'],'grupo: "Conjunto:" vale; senão o remetente');
eq(messagesToJson(`Conjunto: Jovens\n1. Santo - ${U}\n2) Reconstrução https://www.youtube.com/watch?v=12345678901)\n• Aleluia: www.youtube.com/watch?v=aaaaaaaaaaa,`)[0].itens.map(i=>i.nome),['Santo','Reconstrução','Aleluia'],'nomes limpos de numeração/pontuação');
eq(messagesToJson(`Conjunto: Jovens\n1. Santo - ${U}\n2) Reconstrução https://www.youtube.com/watch?v=12345678901)`)[0].itens.map(i=>i.link),[U,'https://www.youtube.com/watch?v=12345678901'],'links sem ")" no fim');
eq(messagesToJson(`Conjunto: Jovens\n1. 10.000 Razões - ${U}\n2. 40 dias - ${U}1\n- Grande é o Senhor ${U}2\n3.Aleluia ${U}3\n1 - Santo ${U}4`)[0].itens.map(i=>i.nome),['10.000 Razões','40 dias','Grande é o Senhor','Aleluia','Santo'],'nomes que começam com número ficam inteiros');
const tokenJson=Buffer.from(JSON.stringify({grupo:'',itens:[{nome:'A',link:U}]}),'utf8').toString('base64');
eq(messagesToJson(`[19/11/2026 20:15:10] Rui: 🎵 Pedido de músicas\n\n1. A - ${U}\n\n#LYRA:${tokenJson}`)[0].grupo,'Rui','formulário: remetente preenche grupo vazio');
eq(messagesToJson(`🎵 Pedido de músicas\nConjunto: Louvor\n\n1. A - ${U}\n\n#LYRA:${Buffer.from(JSON.stringify({grupo:'Louvor',itens:[{nome:'A',link:U}]})).toString('base64')}`)[0].grupo,'Louvor','formulário sem cabeçalho: grupo do formulário');
eq(messagesToJson('#LYRA:@@@@\nConjunto: X\nSanto '+U).length,1,'token inválido cai para o texto livre');
eq(messagesToJson('só conversa sem link').length,0,'sem link não gera pedido');
eq(messagesToJson(`Santo ${U}`)[0].grupo,'','link sem conjunto e sem remetente: grupo vazio');
// 60 mensagens: lê só as últimas 40
const many=Array.from({length:60},(_,i)=>`19/11/2026 20:${String(i%60).padStart(2,'0')} - P${i}: Música ${i} https://youtu.be/${String(i).padStart(11,'x')}`).join('\n');
eq(messagesToJson(many).length,40,'conversa longa: só as últimas 40 mensagens');

// ---- reconhecer pedido na área de transferência
ok(looksLikeRequest(`[20:15, 19/11/2026] Lia: Santo ${U}`),'copiado do WhatsApp Web');
ok(looksLikeRequest(`Conjunto: X\n${U}`),'"Conjunto:" + link');
ok(looksLikeRequest(`🎵 Pedido de músicas\n${U}`),'marca do formulário');
ok(looksLikeRequest(`x\n#LYRA:${tokenJson}`),'marca #LYRA');
ok(!looksLikeRequest(U),'link solto do YouTube NÃO é capturado');
ok(!looksLikeRequest('Reunião às 20:30 hoje'),'texto comum');
ok(!looksLikeRequest('http://192.168.0.10:5280/pedido'),'link do próprio programa');
ok(!looksLikeRequest('🎵 *Pedido de músicas*\nPara pedir uma música, responda aqui no grupo neste formato:\n\nConjunto: nome do seu grupo\n1. Nome da música - link do YouTube'),'texto de convite que o app copia NÃO se auto-importa');
ok(!looksLikeRequest('x'.repeat(30000)),'texto enorme ignorado');

// ---- título de vídeo
eq(cleanTitle('Eliã Oliveira - Reconstrução (Clipe Oficial) [HD]'),'Eliã Oliveira - Reconstrução','título: colchetes e "Clipe Oficial"');
eq(cleanTitle('Fernandinho - Santo | Ao Vivo'),'Fernandinho - Santo','título: "Ao Vivo" depois de barra');
eq(cleanTitle('Santo (Áudio Oficial)'),'Santo','título: "Áudio Oficial" com acento');
eq(cleanTitle('Aleluia - Playback com letra'),'Aleluia','título: "Playback com letra"');
eq(cleanTitle('Deus não está morto'),'Deus não está morto','título: não corta palavras normais');
eq(cleanTitle('Vivo para Ti - Live Worship'),'Vivo para Ti - Worship','título: "Vivo" não é cortado, "Live" sim');
eq(buildQueries('Reconstrução - Eliã Oliveira (Ao Vivo)'),['Reconstrução - Eliã Oliveira','Reconstrução','Eliã Oliveira'],'consultas ao catálogo');
eq(buildQueries('Santo'),['Santo'],'título simples: uma consulta');
eq(normalize('  Reconstrução — Eliã!! '),'reconstrucao elia','normalização');

// ---- letras .lrc
const cues=parseLrc('[ti:Santo]\n[00:12.30] Santo\n[01:05.5][02:00.0] Refrão\n[03:00.00]\n[mm:ss] x');
eq(cues.map(c=>[c.seconds,c.text]),[[12.3,'Santo'],[65.5,'Refrão'],[120,'Refrão']],'.lrc: tempos, várias marcas e linhas vazias');
eq(parseLrc('[00:05,50] vírgula decimal')[0].seconds,5.5,'.lrc: vírgula decimal');
console.log(`parsers: ${pass} ok, ${fail} falhas`); process.exit(fail?1:0);
