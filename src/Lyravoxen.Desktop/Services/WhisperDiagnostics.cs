using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Whisper.net;

namespace Lyravoxen.Services;

/// <summary>
/// Cria o modelo Whisper e, se a biblioteca nativa não carregar, explica o motivo provável
/// em português (em vez da mensagem técnica da biblioteca).
/// </summary>
internal static class WhisperDiagnostics
{
    public static WhisperFactory CreateFactory(string modelPath)
    {
        try
        {
            return WhisperFactory.FromPath(modelPath);
        }
        catch (Exception ex) when (IsNativeLoadFailure(ex))
        {
            throw new InvalidOperationException(Explain(), ex);
        }
    }

    private static bool IsNativeLoadFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            var text = current.Message ?? "";
            if (text.Contains("native whisper library", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("NativeLibrary", StringComparison.OrdinalIgnoreCase) ||
                current is DllNotFoundException or BadImageFormatException)
                return true;
            if (current.InnerException is null) break;
        }
        return false;
    }

    private static string Explain()
    {
        var text = new StringBuilder("Não foi possível carregar o motor de voz (whisper.dll).");

        var baseDir = AppContext.BaseDirectory;
        var found = new List<string>();
        try
        {
            var runtimes = Path.Combine(baseDir, "runtimes");
            if (Directory.Exists(runtimes))
                found.AddRange(Directory.EnumerateFiles(runtimes, "whisper.dll", SearchOption.AllDirectories));
            if (File.Exists(Path.Combine(baseDir, "whisper.dll"))) found.Add(Path.Combine(baseDir, "whisper.dll"));
        }
        catch { }

        var system = Environment.SystemDirectory;
        var missingCrt = new[] { "vcruntime140.dll", "msvcp140.dll" }
            .Where(name => !File.Exists(Path.Combine(baseDir, name)) && !File.Exists(Path.Combine(system, name)))
            .ToList();

        if (found.Count == 0)
            text.Append(" O arquivo whisper.dll não está na pasta do programa (reinstale a versão mais recente).");
        else if (missingCrt.Count > 0)
            text.Append(" Falta o componente do Windows \"Microsoft Visual C++ Redistributable 2015-2022 (x64)\" (")
                .Append(string.Join(", ", missingCrt)).Append("). Instale-o pelo site da Microsoft (aka.ms/vs/17/release/vc_redist.x64.exe) ou use a versão nova do instalador, que já o inclui.");
        else
            text.Append(" A biblioteca existe, mas o Windows recusou carregá-la.");

        if (!Environment.Is64BitProcess) text.Append(" O programa não está rodando em 64 bits.");
        text.Append(" [AVX: ").Append(Avx.IsSupported ? "sim" : "não")
            .Append("; whisper.dll: ").Append(found.Count == 0 ? "ausente" : "presente").Append(']');
        return text.ToString();
    }
}
