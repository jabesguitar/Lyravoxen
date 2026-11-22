import fs from 'fs';
const P=JSON.parse(fs.readFileSync('./patterns.json','utf8'));
let pass=0,fail=0; const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };
const eq=(a,b,m)=>ok(JSON.stringify(a)===JSON.stringify(b),m+' -> '+JSON.stringify(a)+(JSON.stringify(a)===JSON.stringify(b)?'':' (esperado '+JSON.stringify(b)+')'));
const uniB=src=>{let n=0;return src.replace(/\\b/g,()=>(n++%2===0)?'(?<![\\p{L}\\p{N}_])':'(?![\\p{L}\\p{N}_])');};
const net=src=>uniB(src).replace('[\\W_]','[^\\p{L}\\p{N}]');
const R={brackets:new RegExp(P.cleanBrackets,'g'),emoji:new RegExp(P.emoji,'g'),feat:new RegExp(P.feat,'gi'),words:new RegExp(net(P.cleanWords),'giu'),
 split:new RegExp(P.partSplit),chSuffix:new RegExp(P.channelSuffix,'i'),notArtist:new RegExp(net(P.notArtist),'iu'),
 bracketLabel:new RegExp(P.bracketLabel,'iu'),plainLabel:new RegExp(P.plainLabel,'iu'),chord:new RegExp(P.chord),credit:new RegExp(P.credit,'iu'),
 repeat:new RegExp(P.repeat,'iu'),only:new RegExp(net(P.onlySymbols),'u')};
const normalize=v=>!v||!v.trim()?'':v.normalize('NFD').replace(/\p{Mn}/gu,'').toLowerCase().replace(/[^a-z0-9]+/g,' ').trim();
const containsPhrase=(t,p)=>t.length>0&&p.length>0&&(' '+t+' ').includes(' '+p+' ');
const cleanTitle=t=>(t||'').replace(R.brackets,' ').replace(R.emoji,' ').replace(R.feat,' ').replace(R.words,' ').replace(/\s+/g,' ').trim().replace(/^[ \-–—|:/"“”*]+|[ \-–—|:/"“”*]+$/g,'');
const splitParts=c=>(c||'').split(R.split).map(x=>x.trim()).filter(Boolean);
function cleanChannel(ch){let n=ch||'',prev;do{prev=n;n=n.replace(R.chSuffix,'').trim();}while(n!==prev);n=n.replace(/\s+/g,' ');return R.notArtist.test(n)?'':n;}
const same=(x,y)=>x.length>=3&&y.length>=3&&(x===y||containsPhrase(x,y)||containsPhrase(y,x));
function parse(title,channel,known=[]){
 const ch=cleanChannel(channel); const parts=splitParts(cleanTitle(title));
 if(parts.length>=2&&/^\d{1,3}$/.test(parts[0])) parts.shift();
 if(!parts.length) return {track:'',artist:ch,confident:ch.length>0};
 if(parts.length===1) return {track:parts[0],artist:ch,confident:true};
 const [a,b]=parts; const na=normalize(a),nb=normalize(b),nc=normalize(ch);
 const aC=same(na,nc),bC=same(nb,nc);
 if(aC&&!bC) return {track:b,artist:a,confident:true};
 if(bC&&!aC) return {track:a,artist:b,confident:true};
 const kn=new Set(known.map(normalize).filter(x=>x.length>=3)); const aK=kn.has(na),bK=kn.has(nb);
 if(aK&&!bK) return {track:b,artist:a,confident:true};
 if(bK&&!aK) return {track:a,artist:b,confident:true};
 return {track:b,artist:a,confident:false,partA:a,partB:b};
}
const looksUncorrected=(track,raw)=>{const t=(track||'').trim(); if(!t) return true; if(normalize(t)===normalize(raw)) return true; if(cleanTitle(t)!==t) return true; return splitParts(t).length>=2;};
function decideOrder(items,A,B){const a=normalize(A),b=normalize(B);let at=0,bt=0;const near=(x,y)=>x.length>=3&&y.length>=3&&(x===y||containsPhrase(x,y)||containsPhrase(y,x));
 for(const it of items){const t=normalize(it.trackName),ar=normalize(it.artistName); if(t===a&&near(ar,b)) at++; if(t===b&&near(ar,a)) bt++;}
 if(at>0&&bt===0) return true; if(bt>0&&at===0) return false; return null;}

// ---------- títulos
const E='Eliã Oliveira';
eq(parse('Eliã Oliveira - Reconstrução (Clipe Oficial)',E),{track:'Reconstrução',artist:E,confident:true},'artista = canal (1ª parte)');
eq(parse('Reconstrução - Eliã Oliveira',E+' Oficial'),{track:'Reconstrução',artist:E,confident:true},'artista = canal (2ª parte, canal com "Oficial")');
eq(parse('Fernandinho - Santo (Ao Vivo)','Som Gospel Playback'),{track:'Santo',artist:'Fernandinho',confident:false,partA:'Fernandinho',partB:'Santo'},'canal de playback não conta; padrão "Intérprete - Música"');
eq(parse('Fernandinho - Santo (Ao Vivo)','Som Gospel Playback',['Fernandinho']),{track:'Santo',artist:'Fernandinho',confident:true},'intérprete conhecido na biblioteca');
eq(parse('Santo - Fernandinho','Canal X',['Fernandinho']),{track:'Santo',artist:'Fernandinho',confident:true},'ordem invertida resolvida pela biblioteca');
eq(parse('Grande é o Senhor (Legendado)','Adhemar de Campos - Topic'),{track:'Grande é o Senhor',artist:'Adhemar de Campos',confident:true},'título único + canal Topic');
eq(parse('Aleluia | Playback Legendado','Canal Playback Gospel'),{track:'Aleluia',artist:'',confident:true},'só título; canal de playback ignorado');
eq(parse('01 - Santo','Fernandinho'),{track:'Santo',artist:'Fernandinho',confident:true},'numeração de faixa');
eq(parse('🎵 Fernandinho - Santo 🎵','Fernandinho VEVO'),{track:'Santo',artist:'Fernandinho',confident:true},'emojis e VEVO');
eq(parse('Fernandinho - Santo - Ao Vivo em São Paulo','x',['Fernandinho']).track,'Santo','3 partes: usa as duas primeiras');
eq(parse('Diante do Trono - Quero Conhecer-te (Playback com Letra) [HD]','Diante do Trono').track,'Quero Conhecer-te','Playback com Letra + HD');
eq(parse('Hillsong - What A Beautiful Name (Official Music Video)','Hillsong Worship').artist,'Hillsong','Official Music Video; canal "Hillsong Worship" contém o artista');
eq(parse('Aline Barros - Ressuscita-me | Clipe Oficial','Aline Barros').track,'Ressuscita-me','hífen dentro do nome não separa');
eq(parse('Vivo por Ti','Canal').track,'Vivo por Ti','"Vivo" não é cortado');
eq(parse('',E),{track:'',artist:E,confident:true},'título vazio: usa o canal');
eq(cleanChannel('Legendado Gospel TV'),'','canal "Legendado" não é intérprete');
eq(cleanChannel('Maria Silva Music Oficial'),'Maria Silva','tira sufixos repetidos');

ok(looksUncorrected('Fernandinho - Santo (Ao Vivo)','x'),'título inteiro = não corrigido');
ok(looksUncorrected('Santo (Playback)','x'),'com Playback = não corrigido');
ok(looksUncorrected('Santo','Santo'),'igual ao título do vídeo = não corrigido');
ok(looksUncorrected('','x'),'vazio = não corrigido');
ok(!looksUncorrected('Rei dos Reis','Aline - Rei dos Reis (Ao Vivo)'),'nome já limpo é preservado');
ok(looksUncorrected('Santo - Fernandinho','Outro'),'"Música - Intérprete" no campo da música = não corrigido');
// ---------- catálogo decide a ordem
const cat=[{trackName:'Santo',artistName:'Fernandinho'},{trackName:'Santo',artistName:'Outro'}];
eq(decideOrder(cat,'Fernandinho','Santo'),false,'catálogo: B é a música');
eq(decideOrder(cat,'Santo','Fernandinho'),true,'catálogo: A é a música');
eq(decideOrder([],'A b','C d'),null,'catálogo vazio: sem decisão');
eq(decideOrder([{trackName:'Santo',artistName:'Fernandinho'},{trackName:'Fernandinho',artistName:'Santo'}],'Fernandinho','Santo'),null,'ambíguo: sem decisão');

// ---------- limpeza de letra
const CH=s=>s.split(/\s+/).filter(Boolean);
const isChordLine=l=>{const t=l.split(/[ \t]+/).filter(Boolean);if(!t.length)return false;if(t.length===1&&t[0].length<2)return false;return t.every(x=>R.chord.test(x)||x==='|'||x==='-'||x==='/'||/^\(?x\d\)?$/i.test(x));};
function cleanLyrics(text){const out=[];let prevBlank=true;
 for(const raw of (text||'').replace(/\r/g,'').split('\n')){let line=raw.replace(/\u00a0/g,' ').trim().replace(/\s+/g,' ');
  if(line&&(R.bracketLabel.test(line)||R.plainLabel.test(line)||R.credit.test(line)||isChordLine(line)||R.only.test(line))) continue;
  line=line.replace(R.repeat,'').trim();
  if(!line){if(!prevBlank)out.push('');prevBlank=true;continue;} out.push(line);prevBlank=false;}
 while(out.length&&out[out.length-1]==='')out.pop(); return out.join('\n');}
const sample=`Letra de "Santo" - Fernandinho\nCompositor: Fernandinho\n\n[Intro]\nC   G   Am   F\n\n[Refrão]\nSanto, santo, santo\nSenhor Deus todo-poderoso (2x)\n\n\n   Verso 1:\nA  paz   do Senhor\nE\nFinal feliz\nBem-vindo, Espírito bis\nObis\n----\nhttps://www.letras.mus.br/fernandinho/santo/\n© 2020 Gravadora\n`;
const cleaned=cleanLyrics(sample);
eq(cleaned.split('\n'),['Santo, santo, santo','Senhor Deus todo-poderoso','','A paz do Senhor','E','Final feliz','Bem-vindo, Espírito','Obis'],'limpa cifras, rótulos, créditos, links e marcas de repetição');
ok(!/Compositor|Intro|Refrão|letras\.mus|©|----/.test(cleaned)||/Letra de/.test(cleaned),'sem créditos/rótulos');
eq(cleanLyrics('Am\nDm7\nG/B\nDeus é bom\nA').split('\n'),['Deus é bom','A'],'acordes saem; "A" sozinho (pode ser letra) fica');
eq(cleanLyrics('[Refrão 2x]\n(Intro 2x)\nRefrão:\nRefrão 2\nIntro\nVerso\nFinal'),'','rótulos soltos (inclusive "Final" sozinho) saem');
eq(cleanLyrics('Santo\n\n\n\nSanto').split('\n'),['Santo','','Santo'],'linhas em branco repetidas viram uma');
eq(cleanLyrics('  Deus\u00a0é   fiel  ').split('\n'),['Deus é fiel'],'espaços e espaço invisível');
const lyr=t=>{const c=cleanLyrics(t);return c.split('\n').filter(x=>x.trim()).length>=4&&c.length>=80;};
ok(lyr('Santo, santo, santo\nSenhor Deus todo-poderoso\nA terra e o céu cheios estão\nDa tua glória, Senhor'),'parece letra');
ok(!lyr('https://youtu.be/abcdefghijk')&&!lyr('oi')&&!lyr('Conjunto: X\nSanto https://youtu.be/abcdefghijk'),'link, texto curto e pedido NÃO parecem letra');


// ---------- nomes: tons, instrumental, feat., canais genéricos
eq(parse('Fernandinho - Santo - Um Tom Abaixo','Playback Gospel',['Fernandinho']),{track:'Santo',artist:'Fernandinho',confident:true},'"Um Tom Abaixo" some do título');
eq(parse('Santo (Playback) Dois Tons Abaixo','Canal Playback Gospel').track,'Santo','"Dois Tons Abaixo" depois do parêntese');
eq(parse('Santo - Meio Tom Acima','Fernandinho').track,'Santo','"Meio Tom Acima"');
eq(parse('Santo - Playback Legendado Tom de G','Fernandinho').track,'Santo','"Tom de G"');
eq(parse('Quero Conhecer-te - Tom: Em - Playback','Diante do Trono').track,'Quero Conhecer-te','"Tom: Em"');
eq(parse('Fernandinho - Santo feat. Aline Barros (Ao Vivo)','Fernandinho').track,'Santo','feat. sai da música');
eq(parse('Hillsong - Oceans ft. Taya','Hillsong').track,'Oceans','ft. sai da música');
eq(parse('Santo - Instrumental','Fernandinho').track,'Santo','"Instrumental"');
eq(parse('Santo - 3 tons abaixo','Fernandinho').track,'Santo','"3 tons abaixo"');
eq(parse('Santo - 1 tom abaixo','Fernandinho').track,'Santo','"1 tom abaixo"');
eq(parse('Santo - Um Tom Abaixo','Fernandinho').track,'Santo','"Um Tom Abaixo" (2 partes)');
eq(parse('Santo - Dois Tons Acima','Fernandinho').track,'Santo','"Dois Tons Acima" (2 partes)');
eq(cleanTitle('Soft Rain - Tons de Esperança'),'Soft Rain - Tons de Esperança','"Soft" e "Tons de" não são cortados');
eq(parse('Rei dos Reis','Hinos Cantados TV'),{track:'Rei dos Reis',artist:'',confident:true},'canal de "Hinos" não é intérprete');
eq(cleanChannel('Louvores de Adoração'),'','canal de "Louvores" não é intérprete');

// ---------- busca por nomes (PickByNames)
const mkItem=(t,a,synced,plain)=>({trackName:t,artistName:a,syncedLyrics:synced||'',plainLyrics:plain||''});
const LRC='[00:10.00] Primeira\n[00:20.00] Segunda\n[00:30.00] Terceira';
function nameScore(typed,found){if(!typed||!found)return 0;if(typed===found)return 4;if(containsPhrase(found,typed)||containsPhrase(typed,found))return 3;
 const wa=new Set(typed.split(' ').filter(Boolean)),wb=new Set(found.split(' ').filter(Boolean));if(!wa.size||!wb.size)return 0;let c=0;for(const w of wa)if(wb.has(w))c++;return c/Math.min(wa.size,wb.size)>=0.6?1:0;}
function pickByNames(items,track,artist){
 const tt=normalize(track),ta=normalize(artist);const cands=[];
 for(const it of items){const ft=normalize(it.trackName),fa=normalize(it.artistName);
  let ts=nameScore(tt,ft),as=ta.length===0?1:nameScore(ta,fa);const sts=ta.length===0?0:nameScore(ta,ft),sas=nameScore(tt,fa);let used=tt;
  if(sts>ts&&sts>=3&&sas>=1){ts=sts;as=sas;used=ta;}
  if(ts<3||(used.length<6&&ts<4&&as<3))continue;
  const synced=(it.syncedLyrics.match(/\[\d+:\d+/g)||[]).length>=2; const text=synced?it.syncedLyrics:it.plainLyrics; if(!text.trim())continue;
  cands.push({score:ts*2+as*2,synced,track:it.trackName,artist:it.artistName});}
 if(!cands.length)return {found:false,count:items.length};
 cands.sort((x,y)=>(y.synced-x.synced)||(y.score-x.score)); return {found:true,...cands[0],count:items.length};}
let r=pickByNames([mkItem('Reconstrução - Ao Vivo','Eliã Oliveira',LRC)],'Reconstrução','Eliã Oliveira');
ok(r.found&&r.synced&&r.track==='Reconstrução - Ao Vivo','busca: aceita a música com sufixo "- Ao Vivo" do catálogo');
r=pickByNames([mkItem('Reconstrução','Eliã Oliveira',LRC)],'Eliã Oliveira','Reconstrução'); ok(r.found&&r.synced,'busca: campos trocados pelo usuário mesmo assim acham');
r=pickByNames([mkItem('Reconstrução','Eliã Oliveira','','Linha 1\nLinha 2')],'Reconstrução','Elia Oliveira'); ok(r.found&&!r.synced,'busca: sem acento e só texto');
r=pickByNames([mkItem('Deus é fiel','Banda X','','a\nb')],'Deus',''); ok(!r.found,'busca: "Deus" sozinho não casa com "Deus é fiel"');
r=pickByNames([mkItem('Santo (Ao Vivo)','Fernandinho',LRC)],'Santo','Fernandinho'); ok(r.found,'busca: "Santo" + intérprete certo aceita "Santo (Ao Vivo)"');
r=pickByNames([mkItem('Santo (Ao Vivo)','Outro Cantor',LRC)],'Santo','Fernandinho'); ok(!r.found,'busca: "Santo" com intérprete diferente não casa');
r=pickByNames([mkItem('Outra Música','Fulano',LRC),mkItem('Mais uma','Beltrano',LRC)],'Reconstrução','Eliã'); ok(!r.found&&r.count===2,'busca: nada compatível informa quantos resultados vieram');
r=pickByNames([mkItem('Reconstrução','Eliã Oliveira','','texto\nsem tempos'),mkItem('Reconstrução - Ao Vivo','Eliã Oliveira',LRC)],'Reconstrução','Eliã Oliveira'); ok(r.synced,'busca: prefere a letra sincronizada');

// ---------- estimador de tempos (EstimateStarts)
function estimateStarts(stanzas,D,firstStart=-1,tail=-1){const out=[];if(!stanzas.length||D<=0)return out;
 const first=firstStart>=0?Math.min(firstStart,D*0.5):Math.min(20,D*0.06);const t=tail>=0?tail:Math.min(15,D*0.05);const span=Math.max(1,D-first-t);
 const w=stanzas.map(s=>s.reduce((a,l)=>a+Math.max(1,l.length),0)+25);const total=w.reduce((a,b)=>a+b,0);let cum=0;
 for(const x of w){out.push(first+span*cum/total);cum+=x;}return out;}
const st=[['a'.repeat(40),'b'.repeat(40)],['c'.repeat(40),'d'.repeat(40)],['e'.repeat(80),'f'.repeat(80)]];
let es=estimateStarts(st,240);
ok(es.length===3&&Math.abs(es[0]-14.4)<1e-9,'estimativa: 1ª estrofe após introdução de 6% (14,4 s em 240 s)');
ok(es[0]<es[1]&&es[1]<es[2]&&es[2]<240-12,'estimativa: estrofes crescentes e dentro da música');
const es2=estimateStarts([['a'.repeat(200)],['b'.repeat(20)],['c'.repeat(20)]],240); ok((es2[1]-es2[0])>(es2[2]-es2[1]),'estimativa: estrofe maior ocupa mais tempo');
es=estimateStarts(st,240,30); ok(Math.abs(es[0]-30)<1e-9,'estimativa: 1ª estrofe no instante informado (toque do usuário)');
es=estimateStarts(st,100,500); ok(es[0]<=50,'estimativa: instante absurdo é limitado a metade da música');
ok(estimateStarts([],200).length===0&&estimateStarts(st,0).length===0,'estimativa: sem estrofes ou sem duração = vazio');

console.log(`nomes e limpeza de letra: ${pass} ok, ${fail} falhas`); process.exit(fail?1:0);
