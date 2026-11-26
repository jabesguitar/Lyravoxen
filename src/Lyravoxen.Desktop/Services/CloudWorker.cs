using System.Text.Json;

namespace Lyravoxen.Services;

/// <summary>Gera o código do serviço gratuito (Cloudflare Worker + KV) que recebe os pedidos de qualquer rede.</summary>
internal static class CloudWorker
{
    /// <summary>O código inclui a página de pedidos (a mesma do formulário), servida em /pedido. A senha NÃO vai no código.</summary>
    public static string Build(WhatsAppSettings settings)
    {
        var page = OfflineRequestForm.Build(settings, "/enviar");
        return Template.Replace("__PAGE__", JsonSerializer.Serialize(page));
    }

    public static string Readme(string workerFileName) =>
$"""
COMO COLOCAR OS PEDIDOS NA INTERNET (grátis) — Lyravoxen + Cloudflare
=====================================================================
O que é: um pequeno serviço que guarda os pedidos enviados de QUALQUER rede (4G, outro Wi-Fi) até o Lyravoxen buscá-los.
Custa nada no plano gratuito do Cloudflare. Você faz isto UMA vez.

Os nomes dos menus do Cloudflare mudam de vez em quando. Se algo estiver diferente, procure o item de nome parecido
ou me mande o texto/foto da tela.

1) Crie a conta gratuita em https://dash.cloudflare.com/sign-up (só e-mail e senha).

2) Crie o armazenamento:
   Menu da esquerda → "Storage & Databases" (ou "Armazenamento") → "Workers KV" → "Create instance"/"Create namespace".
   Nome: pedidos   (pode ser outro).

3) Crie o serviço:
   Menu da esquerda → "Workers & Pages" → "Create application" → "Start with Hello World!" → "Get started".
   Nome: por exemplo lyravoxen-pedidos → "Deploy".
   Depois clique em "Edit code". Apague TODO o código que aparece e cole TODO o conteúdo do arquivo {workerFileName}. Clique em "Deploy".

4) Ligue o armazenamento ao serviço:
   Abra o serviço → aba "Bindings" (ou "Settings" → "Bindings") → "Add binding" → "KV namespace".
   Variable name: PEDIDOS   (exatamente assim, maiúsculas)
   KV namespace: o que você criou no passo 2. Salve/Deploy.

5) Coloque a senha:
   No serviço → "Settings" → "Variables and Secrets" → "Add".
   Type: Secret.  Name: ADMIN_TOKEN   (exatamente assim)
   Value: a senha que o Lyravoxen mostra na janela "Pedidos pela internet" (botão "Copiar senha"). Deploy.

6) Pegue o endereço do serviço (algo como https://lyravoxen-pedidos.SEUNOME.workers.dev), cole no Lyravoxen, clique em
   "Testar conexão". Se aparecer "Conectado", pronto.

ATUALIZAÇÃO PARA QUEM JÁ TEM O WORKER CRIADO
- No Lyravoxen, abra Pedidos → Pedidos pela internet → "Gerar código do serviço (worker.js)".
- No painel do Cloudflare, abra o Worker existente → "Edit code". Substitua o código antigo pelo novo worker.js e clique em "Deploy".
- NÃO apague nem recrie o namespace KV PEDIDOS e NÃO troque o ADMIN_TOKEN. Assim os pedidos existentes permanecem salvos.
- Esta atualização é necessária para o bloqueio de IPs pela internet; sem ela, o bloqueio continuará funcionando apenas para pedidos recebidos diretamente na rede local.

COMO USAR
- Divulgue o link  ENDEREÇO/pedido  (o Lyravoxen copia para você) ou mande o formulário .html gerado depois de salvar o endereço:
  a pessoa preenche e toca em "Enviar pedido ao operador"; o pedido entra na lista 📥 Pedidos em até ~1 minuto, de qualquer rede.
- O Lyravoxen precisa estar aberto e com internet para buscar. Pedidos ficam no serviço por 12 horas; todos os computadores com o mesmo endereço e senha recebem os mesmos pedidos.
- Segurança: quem tem o link pode enviar pedidos (só texto com link de música). Quem não tem a senha não consegue LER os pedidos.
  Não mande a senha para ninguém e não coloque o arquivo da senha em grupos.
- Limites do plano gratuito (por dia): cerca de 1.000 gravações no armazenamento — dá para algumas centenas de pedidos por dia.
""";

    private const string Template = """"
// Lyravoxen — serviço de pedidos de músicas (Cloudflare Worker + KV).
// Variáveis: PEDIDOS (KV namespace) e ADMIN_TOKEN (secret).
const PAGE = __PAGE__;
const MAX = 4000;
const CORS = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
  "Access-Control-Allow-Headers": "content-type, authorization"
};

function json(obj, status) {
  return new Response(JSON.stringify(obj), {
    status: status || 200,
    headers: Object.assign({ "content-type": "application/json; charset=utf-8", "cache-control": "no-store" }, CORS)
  });
}

function html(body, status) {
  return new Response(body, {
    status: status || 200,
    headers: { "content-type": "text/html; charset=utf-8", "cache-control": "no-store" }
  });
}

function sameText(a, b) {
  if (a.length !== b.length) return false;
  var diff = 0;
  for (var i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

function authorized(request, env) {
  var header = request.headers.get("authorization") || "";
  var token = header.replace(/^Bearer\s+/i, "");
  return !!env.ADMIN_TOKEN && token.length > 0 && sameText(token, String(env.ADMIN_TOKEN));
}

function esc(text) {
  return String(text).replace(/[&<>]/g, function (c) { return c === "&" ? "&amp;" : c === "<" ? "&lt;" : "&gt;"; });
}

function resultPage(ok, message) {
  return html("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
    "<title>Pedido de músicas</title></head><body style=\"margin:0;padding:20px;font-family:system-ui,sans-serif;background:#0b1117;color:#e8eef4\">" +
    "<h2 style=\"color:" + (ok ? "#6fd6c8" : "#e08a8a") + "\">" + (ok ? "🎶 " : "") + esc(message) + "</h2>" +
    "<p><a style=\"color:#6fd6c8\" href=\"/pedido\">" + (ok ? "Enviar outro pedido" : "Voltar") + "</a></p></body></html>", ok ? 200 : 400);
}

async function readText(request) {
  var type = request.headers.get("content-type") || "";
  if (type.indexOf("application/json") >= 0) {
    var data = await request.json();
    return String((data && data.text) || "");
  }
  var form = await request.formData();
  return String(form.get("text") || "");
}

async function enviar(request, env) {
  var wantsJson = (request.headers.get("content-type") || "").indexOf("application/json") >= 0;
  function reply(ok, message, status) {
    return wantsJson ? json({ ok: ok, status: message }, status) : resultPage(ok, message);
  }
  var senderIp = request.headers.get("CF-Connecting-IP") || "";
  if (senderIp && await env.PEDIDOS.get("b:" + senderIp)) return reply(false, "Este remetente está bloqueado para enviar pedidos.", 403);
  // Limite básico por IP: até 5 envios a cada 10 minutos. A regra é complementar
  // ao limite de fila e às regras Rate Limiting/WAF do Cloudflare.
  if (senderIp) {
    var rateKey = "r:" + senderIp;
    var nowMs = Date.now();
    var rate = null;
    try { rate = JSON.parse((await env.PEDIDOS.get(rateKey)) || "null"); } catch (e) { rate = null; }
    if (!rate || !Number.isFinite(rate.started) || nowMs - rate.started >= 600000) rate = { started: nowMs, count: 0 };
    if (rate.count >= 5) return reply(false, "Limite de 5 pedidos por IP a cada 10 minutos. Tente novamente mais tarde.", 429);
    rate.count++;
    await env.PEDIDOS.put(rateKey, JSON.stringify(rate), { expirationTtl: 600 });
  }
  var text = (await readText(request)).replace(/\u0000/g, "").trim();
  if (!text) return reply(false, "Escreva ou cole pelo menos um link de música.", 400);
  if (text.length > MAX) return reply(false, "Mensagem grande demais.", 413);
  if (!/https?:\/\/|youtu\.be\/|youtube\.com\/|www\./i.test(text)) return reply(false, "Não encontrei nenhum link de música na mensagem.", 400);

  var pending = await env.PEDIDOS.list({ prefix: "p:", limit: 150 });
  if (pending.keys.length >= 150) return reply(false, "A fila do operador está cheia. Tente mais tarde.", 429);

  var id = String(Date.now()).padStart(13, "0") + "-" + Math.random().toString(36).slice(2, 8);
  await env.PEDIDOS.put("p:" + id, JSON.stringify({ text: text, at: new Date().toISOString(), ip: senderIp }), { expirationTtl: 43200 });
  await env.PEDIDOS.put("ultimo", id);
  return reply(true, "Pedido enviado! O operador do som já pode ver.", 200);
}

async function listar(request, env, url) {
  var seen = url.searchParams.get("v") || "";
  var last = (await env.PEDIDOS.get("ultimo")) || "";
  if (seen && seen === last) return json({ ok: true, v: last, pedidos: [] });
  var listed = await env.PEDIDOS.list({ prefix: "p:", limit: 100 });
  var pedidos = [];
  for (var i = 0; i < listed.keys.length; i++) {
    var raw = await env.PEDIDOS.get(listed.keys[i].name);
    if (!raw) continue;
    try {
      var item = JSON.parse(raw);
      pedidos.push({ id: listed.keys[i].name, text: item.text, at: item.at, ip: item.ip || "" });
    } catch (e) { }
  }
  return json({ ok: true, v: last, pedidos: pedidos });
}

async function bloqueios(request, env) {
  if (request.method === "GET") {
    var listed = await env.PEDIDOS.list({ prefix: "b:", limit: 200 });
    var blocked = [];
    for (var i = 0; i < listed.keys.length; i++) {
      var raw = await env.PEDIDOS.get(listed.keys[i].name);
      if (!raw) continue;
      try { var item = JSON.parse(raw); blocked.push({ ip: listed.keys[i].name.slice(2), name: item.name || "" }); } catch (e) { }
    }
    return json({ ok: true, blocked: blocked });
  }
  if (request.method === "POST") {
    var data = await request.json();
    var ip = String(data.ip || "").trim();
    var name = String(data.name || "").trim().slice(0, 60);
    var blocked = data.blocked !== false;
    if (!/^[0-9a-fA-F:.]{3,45}$/.test(ip)) return json({ ok: false, status: "IP inválido." }, 400);
    if (blocked) await env.PEDIDOS.put("b:" + ip, JSON.stringify({ name: name, at: new Date().toISOString() }));
    else await env.PEDIDOS.delete("b:" + ip);
    return json({ ok: true, status: blocked ? "Remetente bloqueado." : "Bloqueio removido." });
  }
  return json({ ok: false, status: "Método não permitido." }, 405);
}

async function confirmar(request, env) {
  var data = await request.json();
  var ids = (data && data.ids ? data.ids : []).filter(function (id) { return typeof id === "string" && id.indexOf("p:") === 0; }).slice(0, 100);
  await Promise.all(ids.map(function (id) { return env.PEDIDOS.delete(id); }));
  return json({ ok: true, n: ids.length });
}

export default {
  async fetch(request, env) {
    try {
      var url = new URL(request.url);
      var path = url.pathname;
      if (request.method === "OPTIONS") return new Response(null, { headers: CORS });
      if (request.method === "GET" && (path === "/" || path === "/pedido")) return html(PAGE);
      if (request.method === "POST" && path === "/enviar") return await enviar(request, env);
      if (path === "/ping" && request.method === "GET") {
        return authorized(request, env) ? json({ ok: true, status: "Conectado." }) : json({ ok: false, status: "Senha incorreta." }, 401);
      }
      if (path === "/pedidos" && request.method === "GET") {
        return authorized(request, env) ? await listar(request, env, url) : json({ ok: false, status: "Senha incorreta." }, 401);
      }
      if (path === "/bloqueios" && (request.method === "GET" || request.method === "POST")) {
        return authorized(request, env) ? await bloqueios(request, env) : json({ ok: false, status: "Senha incorreta." }, 401);
      }
      if (path === "/confirmar" && request.method === "POST") {
        return authorized(request, env) ? await confirmar(request, env) : json({ ok: false, status: "Senha incorreta." }, 401);
      }
      return json({ ok: false, status: "Não encontrado." }, 404);
    } catch (e) {
      return json({ ok: false, status: "Serviço sem armazenamento configurado (confira o passo 4 do LEIA-ME) ou erro interno." }, 500);
    }
  }
};
"""";
}
