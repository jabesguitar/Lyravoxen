// Porte fiel da conversão de AudioMonitorService (mono + redução para 24 kHz + nível) para conferir a matemática.
let pass=0,fail=0; const ok=(c,m)=>{ if(c){pass++;} else {fail++; console.log('FALHOU:',m);} };
const RATE=24000;
class Conv{constructor(src,ch){this.src=src;this.ch=ch;this.pending=[];this.pos=0;this.out=[];}
 feed(frames){ // posição inteira: 1 amostra de origem = RATE unidades; cada passo = src unidades
  for(const f of frames){this.pending.push(f.reduce((a,b)=>a+b,0)/this.ch);}
  while(this.pos+this.src<=this.pending.length*RATE){const s=Math.floor(this.pos/RATE),e=Math.min(this.pending.length,Math.floor((this.pos+this.src+RATE-1)/RATE));let sum=0,n=0;for(let i=s;i<e;i++){sum+=this.pending[i];n++;}
   const v=n>0?sum/n:0;this.out.push(Math.max(-32768,Math.min(32767,Math.round(v*32767))));this.pos+=this.src;}
  const c=Math.floor(this.pos/RATE);if(c>0){this.pending.splice(0,c);this.pos-=c*RATE;}}}
const sine=(rate,sec,hz,amp,ch)=>Array.from({length:Math.round(rate*sec)},(_,i)=>Array(ch).fill(amp*Math.sin(2*Math.PI*hz*i/rate)));
for(const [rate,ch] of [[48000,2],[44100,2],[48000,1],[96000,2]]){
 const conv=new Conv(rate,ch);const frames=sine(rate,2,440,0.5,ch);
 for(let i=0;i<frames.length;i+=480) conv.feed(frames.slice(i,i+480)); // em blocos de 10 ms como o WASAPI
 const expected=2*RATE; ok(Math.abs(conv.out.length-expected)<=2,`${rate} Hz/${ch}ch: 2 s viram ~${expected} amostras (veio ${conv.out.length})`);
 const peak=Math.max(...conv.out.map(Math.abs)); ok(peak>0.4*32767&&peak<=0.5*32767+50,`${rate} Hz: amplitude preservada (pico ${peak})`);
 // frequência: conta cruzamentos por zero
 let z=0;for(let i=1;i<conv.out.length;i++)if((conv.out[i-1]<0)!==(conv.out[i]<0))z++; const hz=z/2/2; ok(Math.abs(hz-440)<6,`${rate} Hz: tom de 440 Hz continua 440 Hz (${hz.toFixed(1)})`);
}
// blocos de tamanhos irregulares não mudam o resultado
const a=new Conv(44100,2),b=new Conv(44100,2),fr=sine(44100,1,300,0.4,2);
a.feed(fr); for(let i=0,k=[7,333,1024,50,4410];i<fr.length;i+=k[(i>>3)%5]) b.feed(fr.slice(i,i+k[(i>>3)%5]));
ok(a.out.length===b.out.length&&a.out.every((v,i)=>v===b.out[i]),'tamanho dos blocos recebidos não altera a conversão (resultado idêntico)');
// nível
function level(samples,prev){let ss=0;for(const s of samples)ss+=s*s;const rms=Math.sqrt(ss/samples.length)/32768;const db=20*Math.log10(rms+1e-9);const target=Math.max(0,Math.min(100,(db+60)/60*100));return target>prev?target:Math.max(target,prev-8);}
ok(Math.round(level(new Array(2400).fill(0),0))===0,'silêncio = nível 0');
const loud=Array.from({length:2400},(_,i)=>Math.round(32767*Math.sin(i/5)));ok(level(loud,0)>85,'senoide forte = nível alto ('+level(loud,0).toFixed(0)+')');
const quiet=Array.from({length:2400},(_,i)=>Math.round(300*Math.sin(i/5)));const q=level(quiet,0);ok(q>15&&q<45,'som baixo = nível médio-baixo ('+q.toFixed(0)+')');
ok(level(new Array(2400).fill(0),90)===82,'nível desce devagar (8 por 100 ms) quando o som para');
console.log(`áudio: ${pass} ok, ${fail} falhas`);process.exit(fail?1:0);
