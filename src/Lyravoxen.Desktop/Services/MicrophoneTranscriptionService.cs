using System.Collections.Concurrent;
using System.IO;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace Lyravoxen.Services;

/// <summary>
/// Captures a selected microphone input and transcribes it locally with Whisper.net.
/// No audio is uploaded and no API key or paid service is used.
/// </summary>
public sealed class MicrophoneTranscriptionService : IAsyncDisposable
{
    private const int CaptureRate = 24000;
    private const int ModelRate = 16000;
    private const int SegmentSeconds = 5;
    private const int SegmentSamples = CaptureRate * SegmentSeconds;
    private static readonly string ModelFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Lyravoxen", "Models");
    private static readonly string ModelPath = Path.Combine(ModelFolder, "ggml-base.bin");

    private readonly Func<double> _playbackSeconds;
    private readonly ConcurrentQueue<(short[] Samples, double Start)> _segments = new();
    private readonly SemaphoreSlim _segmentReady = new(0);
    private readonly CancellationTokenSource _stop = new();
    private readonly List<short> _pendingSamples = new();
    private readonly object _captureSync = new();
    private WaveInEvent? _capture;
    private readonly int _deviceNumber;
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private Task? _worker;
    private double _segmentStart;
    private int _disposed;

    public event EventHandler<LiveTranscriptEventArgs>? TranscriptReceived;
    public event EventHandler<string>? StatusChanged;

    public MicrophoneTranscriptionService(int deviceNumber) { _deviceNumber = deviceNumber; _playbackSeconds = () => 0; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_capture is not null) throw new InvalidOperationException("O reconhecimento já está ativo.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stop.Token);

        StatusChanged?.Invoke(this, "Verificando o modelo gratuito de reconhecimento local...");
        await EnsureModelAsync(linked.Token);

        _factory = WhisperFactory.FromPath(ModelPath);
        _processor = _factory.CreateBuilder()
            .WithLanguage("pt")
            .Build();

        _capture = new WaveInEvent { DeviceNumber = _deviceNumber, WaveFormat = new WaveFormat(CaptureRate, 16, 1), BufferMilliseconds = 250 };
        _capture.DataAvailable += Capture_DataAvailable;
        _capture.RecordingStopped += Capture_RecordingStopped;
        _segmentStart = 0;
        _worker = ProcessSegmentsAsync(_stop.Token);
        _capture.StartRecording();
        StatusChanged?.Invoke(this, "Reconhecimento local ativo. O áudio permanece neste computador; não há API paga.");
    }

    private async Task EnsureModelAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(ModelPath) && new FileInfo(ModelPath).Length > 10_000_000)
        {
            return;
        }

        Directory.CreateDirectory(ModelFolder);
        var temporaryPath = ModelPath + ".download";
        try
        {
            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
            await using (var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await modelStream.CopyToAsync(output, cancellationToken);
            File.Move(temporaryPath, ModelPath, true);
            StatusChanged?.Invoke(this, "Modelo gratuito instalado. Próximas utilizações não precisarão baixá-lo novamente.");
        }
        catch
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
            throw;
        }
    }

    private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            if (_capture is null || e.BytesRecorded <= 0) return;
            var format = _capture.WaveFormat;
            var pcm = ConvertToPcm16Mono24k(e.Buffer, e.BytesRecorded, format);
            if (pcm.Length == 0) return;

            lock (_captureSync)
            {
                _pendingSamples.AddRange(pcm);
                while (_pendingSamples.Count >= SegmentSamples)
                {
                    var segment = _pendingSamples.Take(SegmentSamples).ToArray();
                    _pendingSamples.RemoveRange(0, SegmentSamples);
                    _segments.Enqueue((segment, _segmentStart));
                    _segmentReady.Release();
                    _segmentStart += SegmentSeconds;
                }
            }
        }
        catch (Exception ex) { StatusChanged?.Invoke(this, "Falha na captura de áudio: " + ex.Message); }
    }

    private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            StatusChanged?.Invoke(this, "A captura de áudio foi interrompida: " + e.Exception.Message);
    }

    private async Task ProcessSegmentsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _segmentReady.WaitAsync(cancellationToken);
            if (!_segments.TryDequeue(out var segment)) continue;
            try
            {
                var wav = CreateWav16Khz(segment.Samples);
                await using var stream = new MemoryStream(wav, writable: false);
                var processor = _processor ?? throw new InvalidOperationException("Modelo local não inicializado.");
                await foreach (var result in processor.ProcessAsync(stream))
                {
                    var transcript = result.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(transcript)) continue;
                    var start = segment.Start;
                    TranscriptReceived?.Invoke(this, new LiveTranscriptEventArgs(
                        Guid.NewGuid().ToString("N"), transcript, start + result.Start.TotalSeconds, true));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
            catch (Exception ex) { StatusChanged?.Invoke(this, "Falha na transcrição local: " + ex.Message); }
        }
    }

    private static byte[] CreateWav16Khz(short[] source)
    {
        var outputSamples = new short[(int)Math.Round(source.Length * (double)ModelRate / CaptureRate)];
        for (var i = 0; i < outputSamples.Length; i++)
        {
            var position = i * (double)CaptureRate / ModelRate;
            var left = Math.Min((int)position, source.Length - 1);
            var right = Math.Min(left + 1, source.Length - 1);
            var fraction = position - left;
            outputSamples[i] = (short)Math.Clamp(
                (int)Math.Round(source[left] + (source[right] - source[left]) * fraction),
                short.MinValue, short.MaxValue);
        }

        using var stream = new MemoryStream(44 + outputSamples.Length * 2);
        using var writer = new BinaryWriter(stream);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + outputSamples.Length * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(ModelRate);
        writer.Write(ModelRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(outputSamples.Length * 2);
        foreach (var sample in outputSamples) writer.Write(sample);
        return stream.ToArray();
    }

    private static short[] ConvertToPcm16Mono24k(byte[] input, int length, WaveFormat format)
    {
        var bytesPerSample = format.BitsPerSample / 8;
        if (bytesPerSample is not (2 or 4) || format.Channels < 1) return Array.Empty<short>();
        var isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat ||
            (format is WaveFormatExtensible ext && ext.SubFormat == new Guid("00000003-0000-0010-8000-00AA00389B71"));
        var frames = length / (bytesPerSample * format.Channels);
        if (frames == 0) return Array.Empty<short>();
        var mono = new float[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            var sum = 0f;
            for (var channel = 0; channel < format.Channels; channel++)
            {
                var offset = (frame * format.Channels + channel) * bytesPerSample;
                var sample = isFloat && bytesPerSample == 4
                    ? BitConverter.ToSingle(input, offset)
                    : bytesPerSample == 4
                        ? BitConverter.ToInt32(input, offset) / 2147483648f
                        : BitConverter.ToInt16(input, offset) / 32768f;
                sum += sample;
            }
            mono[frame] = Math.Clamp(sum / format.Channels, -1f, 1f);
        }

        var count = Math.Max(1, (int)Math.Round(frames * (double)CaptureRate / format.SampleRate));
        var result = new short[count];
        for (var i = 0; i < count; i++)
        {
            var position = i * (double)format.SampleRate / CaptureRate;
            var left = Math.Min((int)position, frames - 1);
            var right = Math.Min(left + 1, frames - 1);
            var fraction = position - left;
            result[i] = (short)Math.Clamp((int)Math.Round(
                (mono[left] + (mono[right] - mono[left]) * fraction) * short.MaxValue),
                short.MinValue, short.MaxValue);
        }
        return result;
    }

    public async Task StopAsync()
    {
        if (_capture is not null)
        {
            _capture.DataAvailable -= Capture_DataAvailable;
            _capture.RecordingStopped -= Capture_RecordingStopped;
            try { _capture.StopRecording(); } catch { }
            lock (_captureSync)
            {
                if (_pendingSamples.Count > 0)
                {
                    _segments.Enqueue((_pendingSamples.ToArray(), _segmentStart));
                    _segmentReady.Release();
                    _pendingSamples.Clear();
                }
            }
            _capture.Dispose();
            _capture = null;
        }

        _segmentReady.Release();
        _stop.Cancel();
        try { if (_worker is not null) await _worker.WaitAsync(TimeSpan.FromSeconds(3)); } catch { }
        _processor?.Dispose();
        _processor = null;
        _factory?.Dispose();
        _factory = null;
        while (_segments.TryDequeue(out _)) { }
        StatusChanged?.Invoke(this, "Reconhecimento local interrompido.");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await StopAsync();
        _stop.Dispose();
        _segmentReady.Dispose();
    }
}
