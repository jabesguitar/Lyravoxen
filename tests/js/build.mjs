import fs from 'fs';
const tpl = fs.readFileSync(new URL('./form_template.html', import.meta.url), 'utf8');
const digits = v => (v||'').replace(/\D/g,'');
export function buildForm(s, sendUrl) {
  const cloud = !!(sendUrl && sendUrl.trim());
  sendUrl = cloud ? sendUrl.trim().replace(/[^A-Za-z0-9\-._~:/]/g,'') : '';
  const number = digits(s.number);
  const m = /chat\.whatsapp\.com\/([A-Za-z0-9_\-]{10,40})/i.exec(s.groupLink||'');
  const groupLink = m ? 'https://chat.whatsapp.com/'+m[1] : '';
  let gn = (s.groupName||'').replace(/[^\p{L}\p{N} \-.,]/gu,'').split(' ').filter(Boolean).join(' ').slice(0,60);
  const hasGroup = groupLink.length>0 || gn.length>0;
  const title = hasGroup && gn ? 'Pedido de músicas — '+gn : 'Pedido de músicas';
  const directAction = number ? 'https://wa.me/'+number : 'https://wa.me/';
  const directLabel = number ? 'Enviar para o operador no WhatsApp' : 'Enviar pelo WhatsApp (escolher contato)';
  const plainMain = cloud
    ? '<button class="main" type="submit">Enviar pedido ao operador</button>\n<button type="submit" formmethod="get" formaction="'+directAction+'">'+directLabel+'</button>'
    : '<button class="main" type="submit">'+directLabel+'</button>';
  const plainGroup = hasGroup ? '<button class="grp" type="submit" formmethod="get" formaction="https://wa.me/">Enviar para o grupo'+(gn?' '+gn:'')+' (escolha o grupo na lista)</button>' : '';
  const plainOpen = groupLink ? '<p class="hint">Ou abra o grupo e cole a mensagem: <a href="'+groupLink+'">abrir o grupo no WhatsApp</a></p>' : '';
  return tpl.split('__TITLE__').join(title).split('__WA__').join(number).split('__GL__').join(groupLink).split('__GN__').join(gn)
    .split('__DIRECT_ACTION__').join(directAction).split('__DIRECT_LABEL__').join(directLabel)
    .split('__PLAIN_METHOD__').join(cloud?'post':'get').split('__PLAIN_ACTION__').join(cloud?sendUrl:directAction)
    .split('__PLAIN_MAIN__').join(plainMain).split('__SEND__').join(sendUrl)
    .split('__PLAIN_GROUP__').join(plainGroup).split('__PLAIN_GROUP_OPEN__').join(plainOpen);
}
export function buildWorker(s) {
  const wt = fs.readFileSync(new URL('./worker_template.js', import.meta.url), 'utf8');
  return wt.split('__PAGE__').join(JSON.stringify(buildForm(s,'/enviar')));
}
