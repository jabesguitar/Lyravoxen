using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lyravoxen.Offline;

namespace Lyravoxen.Services;

/// <summary>
/// Obtém somente a faixa de áudio M4A pública selecionada pelo usuário, sem cookies, login,
/// DRM, contorno de paywall ou tentativa de remover anúncios.
/// O executável oficial é baixado sob demanda e validado pelo SHA-256 publicado pelo projeto.
/// </summary>
public sealed class YoutubeAudioDownloadService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private const string ExecutableUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    private const string ChecksumsUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS";
    private readonly string _toolsFolder;
    private readonly string _executable;

    public YoutubeAudioDownloadService(string appDataFolder)
    {
        _toolsFolder = Path.Combine(appDataFolder, "Tools");
        _executable = Path.Combine(_toolsFolder, "yt-dlp.exe");
    }

    public async Task<OfflineMediaResult> DownloadAuthorizedAudioAsync(string youtubeUrl, string destinationFolder, CancellationToken cancellationToken = default)
    {
        if (YouTubeLink.TryGetVideoId(youtubeUrl) is null)
            throw new InvalidOperationException("O endereço não é um vídeo válido do YouTube.");
        Directory.CreateDirectory(_toolsFolder);
        Directory.CreateDirectory(destinationFolder);
        await EnsureExecutableAsync(cancellationToken);

        var outputTemplate = Path.Combine(destinationFolder, "%(id)s.%(ext)s");
        var start = new ProcessStartInfo
        {
            FileName = _executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _toolsFolder
        };
        foreach (var arg in new[] { "--ignore-config", "--no-playlist", "--no-warnings", "--no-progress",
                     "--format", "bestaudio[ext=m4a]", "--output", outputTemplate,
                     "--print", "after_move:filepath", youtubeUrl })
            start.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new InvalidOperationException("Não foi possível iniciar o componente de download.");
        using var registration = cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } });
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException("O YouTube não forneceu uma faixa M4A compatível ou recusou a solicitação. " +
                (string.IsNullOrWhiteSpace(stderr) ? "" : stderr.Trim().Split('\n').LastOrDefault()));

        var path = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()).LastOrDefault(File.Exists);
        if (path is null) throw new InvalidDataException("O componente terminou sem informar um arquivo de áudio.");
        var info = new FileInfo(path);
        if (info.Length < 12 || info.Length > 50L * 1024 * 1024)
        {
            TryDelete(path);
            throw new InvalidDataException("O áudio M4A está vazio ou excede o limite de 50 MB.");
        }
        await using (var stream = File.OpenRead(path))
        {
            var header = new byte[12];
            if (await stream.ReadAsync(header, cancellationToken) < 12 ||
                System.Text.Encoding.ASCII.GetString(header, 4, 4) != "ftyp")
            {
                TryDelete(path);
                throw new InvalidDataException("O arquivo recebido não é um contêiner M4A/MP4 válido.");
            }
        }
        var hash = await SHA256.HashDataAsync(File.OpenRead(path), cancellationToken);
        var final = Path.Combine(destinationFolder, Convert.ToHexString(hash) + ".m4a");
        if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(final), StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(final)) TryDelete(path);
            else File.Move(path, final);
        }
        var finalInfo = new FileInfo(final);
        return new OfflineMediaResult(final, "m4a", finalInfo.Length, Convert.ToHexString(hash));
    }

    private async Task EnsureExecutableAsync(CancellationToken token)
    {
        if (File.Exists(_executable))
        {
            var version = await RunVersionAsync(token);
            if (version) return;
            TryDelete(_executable);
        }
        var binary = await Http.GetByteArrayAsync(ExecutableUrl, token);
        var sums = await Http.GetStringAsync(ChecksumsUrl, token);
        var match = Regex.Match(sums, @"(?m)^([0-9a-fA-F]{64})\s+\*?yt-dlp\.exe\s*$");
        if (!match.Success) throw new InvalidDataException("Não foi possível validar o hash oficial do yt-dlp.");
        var actual = Convert.ToHexString(SHA256.HashData(binary));
        if (!actual.Equals(match.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("A verificação SHA-256 do executável yt-dlp falhou.");
        var temp = _executable + ".download";
        await File.WriteAllBytesAsync(temp, binary, token);
        File.Move(temp, _executable, true);
    }

    private async Task<bool> RunVersionAsync(CancellationToken token)
    {
        try
        {
            var start = new ProcessStartInfo { FileName = _executable, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add("--version");
            using var p = Process.Start(start);
            if (p is null) return false;
            await p.WaitForExitAsync(token);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
}