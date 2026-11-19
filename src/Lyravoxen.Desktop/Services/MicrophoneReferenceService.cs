using System.IO;
using System.Text;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace Lyravoxen.Services;

public sealed record MicrophoneDevice(int Number, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Grava um comando curto de voz (apertar para falar) por um microfone ou interface de áudio
/// escolhida e o transcreve localmente com o mesmo modelo Whisper da sincronização de letras.
/// Nada é enviado pela internet.
/// </summary>
public sealed class MicrophoneReferenceService : IDisposable
{
    private const int TargetRate = 16000;
    private const int MaxSeconds = 12;
    private const string Prompt = "Referência bíblica: João 3:16, Salmos 23, Gênesis 1:1, 1 Coríntios 13:4-7, Romanos 8:28, Mateus 5, Apocalipse 21:4.";

    private readonly object _sync = new();
    private readonly List<short> _samples = new();
    private readonly SemaphoreSlim _modelGate = new(1, 1);
    private WaveInEvent? _capture;
    private int _captureRate = TargetRate;
    private bool _maxRaised;
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private int _disposed;

    public event EventHandler<double>? LevelChanged;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler? MaxDurationReached;

    public static IReadOnlyList<MicrophoneDevice> ListDevices()
    {
        var devices = new List<MicrophoneDevice> { new(-1, "Padrão do Windows") };
        int count;
        try { count = WaveInEvent.DeviceCount; }
        catch { return devices; }

        for (var i = 0; i < count; i++)
        {
            string name;
            try { name = WaveInEvent.GetCapabilities(i).ProductName; }
            catch { name = "Dispositivo " + (i + 1); }
            devices.Add(new MicrophoneDevice(i, name));
        }
        return devices;
    }

    public void StartRecording(int deviceNumber)
    {
        StopCapture();
        lock (_sync) _samples.Clear();
        _maxRaised = false;

        Exception? lastError = null;
        foreach (var rate in new[] { 16000, 48000, 44100 })
        {
            WaveInEvent? capture = null;
            try
            {
                capture = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(rate, 16, 1),
                    BufferMilliseconds = 60
                };
                capture.DataAvailable += Capture_DataAvailable;
                _captureRate = rate;
                _capture = capture;
                capture.StartRecording();
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _capture = null;
                if (capture is not null)
                {
                    capture.DataAvailable -= Capture_DataAvailable;
                    try { capture.Dispose(); } catch { }
                }
            }
        }

        throw new InvalidOperationException(
            "Não foi possível abrir o microfone escolhido. Confira se ele está ligado e não está em uso por outro programa. (" +
            lastError?.Message + ")", lastError);
    }

    private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            var count = e.BytesRecorded / 2;
            if (count <= 0) return;

            var peak = 0;
            var chunk = new short[count];
            for (var i = 0; i < count; i++)
            {
                var sample = BitConverter.ToInt16(e.Buffer, i * 2);
                chunk[i] = sample;
                var magnitude = Math.Abs((int)sample);
                if (magnitude > peak) peak = magnitude;
            }

            bool reachedMax;
            lock (_sync)
            {
                _samples.AddRange(chunk);
                reachedMax = !_maxRaised && _samples.Count >= _captureRate * MaxSeconds;
                if (reachedMax) _maxRaised = true;
            }

            LevelChanged?.Invoke(this, Math.Min(1d, peak / 32768d * 1.6d));
            if (reachedMax) MaxDurationReached?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Falha na captura do microfone: " + ex.Message);
        }
    }

    private void StopCapture()
    {
        var capture = _capture;
        _capture = null;
        if (capture is null) return;
        capture.DataAvailable -= Capture_DataAvailable;
        try { capture.StopRecording(); } catch { }
        try { capture.Dispose(); } catch { }
    }

    /// <summary>Encerra a gravação e devolve o áudio em 16 kHz, mono, já com o volume ajustado.</summary>
    public short[] StopRecording()
    {
        StopCapture();
        short[] raw;
        lock (_sync)
        {
            raw = _samples.ToArray();
            _samples.Clear();
        }
        LevelChanged?.Invoke(this, 0);
        return NormalizeGain(Resample(raw, _captureRate, TargetRate));
    }

    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        await _modelGate.WaitAsync(cancellationToken);
        try
        {
            if (_processor is not null) return;
            await EnsureModelAsync(cancellationToken);
            _factory = WhisperDiagnostics.CreateFactory(LiveAudioTranscriptionService.ModelPath);
            _processor = _factory.CreateBuilder()
                .WithLanguage("pt")
                .WithPrompt(Prompt)
                .Build();
        }
        finally
        {
            _modelGate.Release();
        }
    }

    public async Task<string> TranscribeAsync(short[] samples16k, CancellationToken cancellationToken = default)
    {
        if (samples16k.Length < TargetRate * 4 / 10) return "";
        await PrepareAsync(cancellationToken);

        await _modelGate.WaitAsync(cancellationToken);
        try
        {
            var processor = _processor ?? throw new InvalidOperationException("Modelo de voz não inicializado.");
            await using var stream = new MemoryStream(CreateWav(samples16k), writable: false);
            var text = new StringBuilder();
            await foreach (var result in processor.ProcessAsync(stream))
            {
                if (!string.IsNullOrWhiteSpace(result.Text))
                    text.Append(' ').Append(result.Text.Trim());
            }
            return text.ToString().Trim();
        }
        finally
        {
            _modelGate.Release();
        }
    }

    private async Task EnsureModelAsync(CancellationToken cancellationToken)
    {
        var modelPath = LiveAudioTranscriptionService.ModelPath;
        if (File.Exists(modelPath) && new FileInfo(modelPath).Length > 10_000_000) return;

        StatusChanged?.Invoke(this, "Baixando o modelo gratuito de voz (somente na primeira vez)...");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        var temporaryPath = modelPath + ".download";
        try
        {
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
            await using (var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await modelStream.CopyToAsync(output, cancellationToken);
            File.Move(temporaryPath, modelPath, true);
        }
        catch
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
            throw;
        }
    }

    private static short[] Resample(short[] source, int fromRate, int toRate)
    {
        if (source.Length == 0 || fromRate == toRate) return source;
        var count = Math.Max(1, (int)Math.Round(source.Length * (double)toRate / fromRate));
        var result = new short[count];
        for (var i = 0; i < count; i++)
        {
            var position = i * (double)fromRate / toRate;
            var left = Math.Min((int)position, source.Length - 1);
            var right = Math.Min(left + 1, source.Length - 1);
            var fraction = position - left;
            result[i] = (short)Math.Clamp(
                (int)Math.Round(source[left] + (source[right] - source[left]) * fraction),
                short.MinValue, short.MaxValue);
        }
        return result;
    }

    // Interfaces de áudio costumam entregar sinal baixo: eleva o pico até cerca de 60% (ganho máximo 10x).
    private static short[] NormalizeGain(short[] samples)
    {
        if (samples.Length == 0) return samples;
        var peak = 0;
        foreach (var sample in samples)
        {
            var magnitude = Math.Abs((int)sample);
            if (magnitude > peak) peak = magnitude;
        }
        if (peak < 200 || peak >= 19000) return samples;

        var gain = Math.Min(10d, 19000d / peak);
        var result = new short[samples.Length];
        for (var i = 0; i < samples.Length; i++)
            result[i] = (short)Math.Clamp((int)Math.Round(samples[i] * gain), short.MinValue, short.MaxValue);
        return result;
    }

    private static byte[] CreateWav(short[] samples)
    {
        using var stream = new MemoryStream(44 + samples.Length * 2);
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + samples.Length * 2);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(TargetRate);
        writer.Write(TargetRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(samples.Length * 2);
        foreach (var sample in samples) writer.Write(sample);
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        StopCapture();
        _processor?.Dispose();
        _processor = null;
        _factory?.Dispose();
        _factory = null;
        _modelGate.Dispose();
    }
}
