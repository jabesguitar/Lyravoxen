import fs from 'fs';
const P=JSON.parse(fs.readFileSync('./patterns.json','utf8'));
let pass=0,fail=0; const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };
const eq=(a,b,m)=>ok(JSON.stringify(a)===JSON.stringify(b),m+' -> '+JSON.stringify(a)+(JSON.stringify(a)===JSON.stringify(b)?'':' (esperado '+JSON.stringify(b)+')'));
const uniB=src=>{let n=0;return src.replace(/\\b/g,()=>(n++%2===0)?'(?<![\\p{L}\\p{N}_])':'(?![\\p{L}\\p{N}_])');};
const net=src=>uniB(src).replace('[\\W_]','[^\\p{L}\\p{N}]');
const R={brackets:new RegExp(P.cleanBrackets,'g'),emoji:new RegExp(P.emoji,'g'),words:new RegExp(net(P.cleanWords),'giu'),
 split:new RegExp(P.partSplit),chSuffix:new RegExp(P.channelSuffix,'i'),notArtist:new RegExp(net(P.notArtist),'iu'),
 bracketLabel:new RegExp(P.bracketLabel,'iu'),plainLabel:new RegExp(P.plainLabel,'iu'),chord:new RegExp(P.chord),credit:new RegExp(P.credit,'iu'),
 repeat:new RegExp(P.repeat,'iu'),only:new RegExp(net(P.onlySymbols),'u')};
const normalize=v=>!v||!v.trim()?'':v.normalize('NFD').replace(/\p{Mn}/gu,'').toLowerCase().replace(/[^a-z0-9]+/g,' ').trim();
const containsPhrase=(t,p)=>t.length>0&&p.length>0&&(' '+t+' ').includes(' '+p+' ');
const cleanTitle=t=>(t||'').replace(R.brackets,' ').replace(R.emoji,' ').replace(R.words,' ').replace(/\s+/g,' ').trim().replace(/^[ \-–—|:/"“”*]+|[ \-–—|:/"“”*]+$/g,'');
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
eq(parse('','Canal'),{track:'',artist:'Canal',confident:true},'título vazio');
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

console.log(`nomes e limpeza de letra: ${pass} ok, ${fail} falhas`); process.exit(fail?1:0);
