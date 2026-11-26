using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Lyravoxen.Offline;

public sealed record OfflineMediaResult(string FilePath, string Format, long Size, string Sha256);

public sealed class OfflineMediaService
{
    public const long DefaultMaxBytes = 50L * 1024 * 1024;
    private readonly HttpClient _http;
    private readonly long _maxBytes;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolveHost;

    public OfflineMediaService(HttpMessageHandler? handler = null, long maxBytes = DefaultMaxBytes, Func<string, CancellationToken, Task<IPAddress[]>>? resolveHost = null)
    {
        if (maxBytes < 1024) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        _maxBytes = maxBytes;
        _resolveHost = resolveHost ?? ((host, token) => Dns.GetHostAddressesAsync(host, token));
        _http = handler is null
            ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }, true) { Timeout = TimeSpan.FromSeconds(45) }
            : new HttpClient(handler, false) { Timeout = TimeSpan.FromSeconds(45) };
    }

    public async Task<OfflineMediaResult> DownloadAsync(string address, string mediaFolder, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Informe um endereço HTTP ou HTTPS direto para o arquivo de áudio.");
        if (IsYoutubeHost(uri.Host)) throw new InvalidOperationException("Links do YouTube não são arquivos de áudio diretos. O Lyravoxen não extrai áudio nem contorna restrições da plataforma.");
        Directory.CreateDirectory(mediaFolder);
        string? temp = null;
        try
        {
            using var response = await SendPublicRequestAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
            if (response.Content.Headers.ContentLength is long declared && (declared <= 0 || declared > _maxBytes))
                throw new InvalidDataException("O arquivo está vazio ou excede o limite de 50 MB.");
            if (contentType.Length > 0 && contentType is not ("audio/mpeg" or "audio/mp3" or "audio/wav" or "audio/x-wav" or "audio/wave" or "application/octet-stream"))
                throw new InvalidDataException("O endereço não respondeu com um formato de áudio MP3 ou WAV.");
            var extension = Path.GetExtension(response.RequestMessage?.RequestUri?.AbsolutePath ?? uri.AbsolutePath).ToLowerInvariant();
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            temp = Path.Combine(mediaFolder, Guid.NewGuid().ToString("N") + ".download");
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > _maxBytes) throw new InvalidDataException("O arquivo excede o limite de 50 MB.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (total < 12) throw new InvalidDataException("O arquivo recebido é pequeno demais para ser um áudio válido.");
            }
            var format = DetectFormat(temp);
            if (format == "mp3" && extension is not (".mp3" or "")) throw new InvalidDataException("A extensão do endereço não corresponde ao áudio MP3 recebido.");
            if (format == "wav" && extension is not (".wav" or "")) throw new InvalidDataException("A extensão do endereço não corresponde ao áudio WAV recebido.");
            var hash = await HashFileAsync(temp, cancellationToken);
            var existing = Directory.EnumerateFiles(mediaFolder, "*." + format)
                .FirstOrDefault(path => string.Equals(HashFileAsync(path, CancellationToken.None).GetAwaiter().GetResult(), hash, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                File.Delete(temp); temp = null;
                return new OfflineMediaResult(existing, format, new FileInfo(existing).Length, hash);
            }
            var finalPath = Path.Combine(mediaFolder, hash + "." + format);
            if (File.Exists(finalPath)) File.Delete(temp);
            else File.Move(temp, finalPath);
            temp = null;
            return new OfflineMediaResult(finalPath, format, new FileInfo(finalPath).Length, hash);
        }
        finally { if (temp is not null) try { File.Delete(temp); } catch { } }
    }

    public async Task<OfflineMediaResult> ImportLocalAsync(string sourcePath, string mediaFolder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension is not (".mp3" or ".wav")) throw new InvalidDataException("Selecione um arquivo MP3 ou WAV.");
        Directory.CreateDirectory(mediaFolder);
        var info = new FileInfo(sourcePath);
        if (!info.Exists || info.Length < 12 || info.Length > _maxBytes) throw new InvalidDataException("O arquivo está ausente, vazio ou excede o limite de 50 MB.");
        var format = DetectFormat(sourcePath);
        if (extension != "." + format) throw new InvalidDataException("O conteúdo do arquivo não corresponde à extensão informada.");
        var hash = await HashFileAsync(sourcePath, cancellationToken);
        var target = Path.Combine(mediaFolder, hash + "." + format);
        if (!File.Exists(target))
        {
            var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.Copy(sourcePath, temp, false); File.Move(temp, target); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        return new OfflineMediaResult(target, format, new FileInfo(target).Length, hash);
    }

    private async Task<HttpResponseMessage> SendPublicRequestAsync(Uri start, CancellationToken token)
    {
        var current = start;
        for (var hop = 0; hop <= 3; hop++)
        {
            await EnsurePublicHostAsync(current, token);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.UserAgent.ParseAdd("Lyravoxen/1.0");
            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
            {
                var next = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(current, response.Headers.Location);
                response.Dispose();
                if (hop == 3) throw new HttpRequestException("O endereço excedeu o limite de redirecionamentos.");
                if (next.Scheme is not ("http" or "https")) throw new InvalidOperationException("Redirecionamento para protocolo não permitido.");
                if (IsYoutubeHost(next.Host)) throw new InvalidOperationException("O redirecionamento aponta para o YouTube, que não é aceito como arquivo de áudio direto.");
                current = next;
                continue;
            }
            return response;
        }
        throw new HttpRequestException("Não foi possível obter o arquivo.");
    }

    private async Task EnsurePublicHostAsync(Uri uri, CancellationToken token)
    {
        if (IPAddress.TryParse(uri.Host, out var literal))
        {
            if (!IsPublicAddress(literal)) throw new InvalidOperationException("Endereços locais ou privados não são permitidos.");
            return;
        }
        var addresses = await _resolveHost(uri.Host, token);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
            throw new InvalidOperationException("O domínio resolve para um endereço local, privado ou não permitido.");
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 10 || b[0] == 127 || b[0] == 0 || b[0] == 169 && b[1] == 254 ||
                     b[0] == 172 && b[1] >= 16 && b[1] <= 31 || b[0] == 192 && b[1] == 168 ||
                     b[0] >= 224 || b[0] == 100 && b[1] >= 64 && b[1] <= 127);
        }
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            var bytes = address.GetAddressBytes();
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !address.IsIPv6Multicast && !address.IsIPv6Teredo && (bytes[0] & 0xFE) != 0xFC;
        return false;
    }

    private static bool IsYoutubeHost(string host) => host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase) || host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".youtu.be", StringComparison.OrdinalIgnoreCase);

    private static string DetectFormat(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[12];
        if (stream.Read(header) < 12) throw new InvalidDataException("Arquivo de áudio inválido.");
        if (header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WAVE"u8)) return "wav";
        if (header[..3].SequenceEqual("ID3"u8) || header[0] == 0xFF && (header[1] & 0xE0) == 0xE0) return "mp3";
        throw new InvalidDataException("O conteúdo não é um MP3 ou WAV válido.");
    }

    private static async Task<string> HashFileAsync(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
    }
}
