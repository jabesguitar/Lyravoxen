using System.Net;
using System.Net.Http.Headers;
using Lyravoxen.Offline;

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    Console.WriteLine("PASS: " + message);
}

static byte[] MakeWav()
{
    var bytes = new byte[48];
    "RIFF"u8.CopyTo(bytes.AsSpan(0, 4));
    BitConverter.GetBytes(40).CopyTo(bytes, 4);
    "WAVE"u8.CopyTo(bytes.AsSpan(8, 4));
    "fmt "u8.CopyTo(bytes.AsSpan(12, 4));
    BitConverter.GetBytes(16).CopyTo(bytes, 16);
    BitConverter.GetBytes((short)1).CopyTo(bytes, 20);
    BitConverter.GetBytes((short)1).CopyTo(bytes, 22);
    BitConverter.GetBytes(8000).CopyTo(bytes, 24);
    BitConverter.GetBytes(16000).CopyTo(bytes, 28);
    BitConverter.GetBytes((short)2).CopyTo(bytes, 32);
    BitConverter.GetBytes((short)16).CopyTo(bytes, 34);
    "data"u8.CopyTo(bytes.AsSpan(36, 4));
    BitConverter.GetBytes(4).CopyTo(bytes, 40);
    bytes[44] = 1; bytes[45] = 2; bytes[46] = 3; bytes[47] = 4;
    return bytes;
}

var root = Path.Combine(Path.GetTempPath(), "lyravoxen-offline-tests-" + Guid.NewGuid().ToString("N"));
var media = Path.Combine(root, "Media");
Directory.CreateDirectory(root);
try
{
    var wav = MakeWav();
    var source = Path.Combine(root, "fixture.wav");
    await File.WriteAllBytesAsync(source, wav);
    var service = new OfflineMediaService();
    var first = await service.ImportLocalAsync(source, media);
    Assert(first.Format == "wav" && File.Exists(first.FilePath), "importa WAV local e cria cópia na biblioteca");
    var second = await service.ImportLocalAsync(source, media);
    Assert(first.FilePath == second.FilePath && first.Sha256 == second.Sha256, "deduplica arquivo por SHA-256");
    Assert(File.ReadAllBytes(first.FilePath).SequenceEqual(wav), "preserva bytes do áudio importado");

    var rejectedYoutube = false;
    try { await service.DownloadAsync("https://www.youtube.com/watch?v=abc123", media); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("não extrai áudio")) { rejectedYoutube = true; }
    Assert(rejectedYoutube, "recusa explicitamente extração de áudio de URL do YouTube");

    var privateDnsService = new OfflineMediaService(
        new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)),
        resolveHost: (_, _) => Task.FromResult(new[] { IPAddress.Parse("192.168.1.20") }));
    var rejectedPrivate = false;
    try { await privateDnsService.DownloadAsync("https://media.example/test.wav", media); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("local, privado")) { rejectedPrivate = true; }
    Assert(rejectedPrivate, "bloqueia domínio que resolve para IP privado antes de solicitar o arquivo");

    var directService = new OfflineMediaService(
        new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(wav) { Headers = { ContentType = new MediaTypeHeaderValue("audio/wav") } }
        }),
        resolveHost: (_, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }));
    var downloaded = await directService.DownloadAsync("https://media.example/test.wav", media);
    Assert(downloaded.Format == "wav" && File.Exists(downloaded.FilePath), "baixa arquivo WAV direto por HTTP com DNS público simulado");
    Assert(downloaded.Sha256 == first.Sha256 && downloaded.FilePath == first.FilePath, "download direto converge para a mesma cópia deduplicada");

    var redirectCalls = 0;
    var redirectService = new OfflineMediaService(
        new StubHandler(_ =>
        {
            redirectCalls++;
            if (redirectCalls == 1)
                return new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://cdn.example/final.wav") } };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(wav) { Headers = { ContentType = new MediaTypeHeaderValue("audio/wav") } }
            };
        }),
        resolveHost: (_, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.4.4") }));
    var redirected = await redirectService.DownloadAsync("https://media.example/redirect", media);
    Assert(redirectCalls == 2 && redirected.FilePath == first.FilePath, "segue redirecionamento HTTPS limitado e valida o destino");

    var redirectToPrivateCalls = 0;
    var redirectToPrivateService = new OfflineMediaService(
        new StubHandler(_ =>
        {
            redirectToPrivateCalls++;
            return new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://private.example/hidden.wav") } };
        }),
        resolveHost: (host, _) => Task.FromResult(new[] { IPAddress.Parse(host == "private.example" ? "10.0.0.8" : "8.8.8.8") }));
    var rejectedPrivateRedirect = false;
    try { await redirectToPrivateService.DownloadAsync("https://media.example/redirect", media); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("local, privado")) { rejectedPrivateRedirect = true; }
    Assert(rejectedPrivateRedirect && redirectToPrivateCalls == 1, "bloqueia redirecionamento para endereço privado antes do segundo pedido");

    var oversized = new byte[2048];
    wav.CopyTo(oversized, 0);
    var sizeLimitedService = new OfflineMediaService(
        new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(oversized) { Headers = { ContentType = new MediaTypeHeaderValue("audio/wav") } }
        }),
        maxBytes: 1024,
        resolveHost: (_, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }));
    var rejectedOversized = false;
    try { await sizeLimitedService.DownloadAsync("https://media.example/large.wav", media); }
    catch (InvalidDataException) { rejectedOversized = true; }
    Assert(rejectedOversized, "recusa áudio acima do limite configurado");


    var htmlService = new OfflineMediaService(
        new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>not audio</html>") { Headers = { ContentType = new MediaTypeHeaderValue("text/html") } }
        }),
        resolveHost: (_, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }));
    var rejectedHtml = false;
    try { await htmlService.DownloadAsync("https://media.example/fake.wav", media); }
    catch (InvalidDataException) { rejectedHtml = true; }
    Assert(rejectedHtml, "recusa página HTML apresentada como arquivo de áudio");

    Assert(!BrowserRequestPolicy.ShouldBlock("www.youtube.com", new[] { "youtube.com", "googlevideo.com" }, new[] { "doubleclick.net" }, Array.Empty<string>()),
        "preserva domínio protegido do YouTube");
    Assert(!BrowserRequestPolicy.ShouldBlock("r1.googlevideo.com", new[] { "youtube.com", "googlevideo.com" }, new[] { "googlevideo.com" }, Array.Empty<string>()),
        "preserva subdomínio de entrega de vídeo do YouTube");
    Assert(BrowserRequestPolicy.ShouldBlock("ads.doubleclick.net", new[] { "youtube.com", "googlevideo.com" }, new[] { "doubleclick.net" }, Array.Empty<string>()),
        "bloqueia subdomínio de rastreador/anúncio");
    Assert(!BrowserRequestPolicy.ShouldBlock("ads.doubleclick.net", new[] { "youtube.com" }, new[] { "doubleclick.net" }, new[] { "doubleclick.net" }),
        "respeita exceção de domínio permitido pelo usuário");
    Assert(!BrowserRequestPolicy.ShouldBlock("notdoubleclick.net", new[] { "youtube.com" }, new[] { "doubleclick.net" }, Array.Empty<string>()),
        "não bloqueia domínio por correspondência parcial insegura");
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}

sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request));
}
