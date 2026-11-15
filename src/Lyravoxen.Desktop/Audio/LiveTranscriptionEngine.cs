using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading.Channels;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace Lyravoxen.Audio;

/// <summary>
/// Offline Portuguese transcription worker for process-loopback audio.
/// Uses overlapping 8-second windows and a bounded queue so inference never blocks capture.
/// </summary>
internal sealed class LiveTranscriptionEngine : IAsyncDisposable
{
    private const int TargetSampleRate = 16000;
    private const int WindowSamples = TargetSampleRate * 8;
    private const int HopSamples = TargetSampleRate * 3;
    private readonly Channel<AudioChunkEventArgs> _queue = Channel.CreateBounded<AudioChunkEventArgs>(
        new BoundedChannelOptions(24) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false });
    private readonly CancellationTokenSource _cts = new();
    private readonly string _modelPath;
    private readonly int _threads;
    private Task? _worker;
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private readonly object _sampleLock = new();
    private readonly List<float> _samples = new(WindowSamples * 2);
    private long _sampleBaseIndex;
    private long _nextWindowEnd = WindowSamples;
    private double _lastEmittedEndSeconds;
    private int _started;

    public event Action<string>? StatusChanged;
    public event Action<TranscriptionSegment>? SegmentRecognized;
    public event Action<Exception>? Failed;

    public LiveTranscriptionEngine(string modelPath, int threads)
    {
        _modelPath = modelPath;
        _threads = Math.Clamp(threads, 1, 8);
    }

    public bool IsRunning => Volatile.Read(ref _started) == 1;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) return;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
            if (!File.Exists(_modelPath))
            {
                StatusChanged?.Invoke("Baixando o modelo de transcrição em português (primeira execução)...");
                using var model = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Tiny, linked.Token);
                await using var output = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await model.CopyToAsync(output, linked.Token);
            }

            StatusChanged?.Invoke("Carregando o modelo local de transcrição...");
            _factory = WhisperFactory.FromPath(_modelPath);
            _processor = _factory.CreateBuilder()
                .WithLanguage("pt")
                .WithThreads(_threads)
                .WithNoContext()
                .Build();
            _worker = Task.Run(() => ProcessQueueAsync(_cts.Token), CancellationToken.None);
            StatusChanged?.Invoke("Reconhecimento local pronto. Aguardando áudio do YouTube...");
        }
        catch
        {
            Interlocked.Exchange(ref _started, 0);
            _processor?.Dispose(); _processor = null;
            _factory?.Dispose(); _factory = null;
            throw;
        }
    }

    public bool TryWrite(AudioChunkEventArgs chunk) => IsRunning && _queue.Writer.TryWrite(chunk);

    private async Task ProcessQueueAsync(CancellationToken token)
    {
        try
        {
            await foreach (var chunk in _queue.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                var mono = ConvertToMono16k(chunk);
                if (mono.Length == 0) continue;
                List<float[]> windows;
                lock (_sampleLock)
                {
                    _samples.AddRange(mono);
                    windows = ExtractWindows();
                }
                foreach (var window in windows)
                {
                    token.ThrowIfCancellationRequested();
                    await TranscribeWindowAsync(window, token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Failed?.Invoke(ex); }
    }

    private List<float[]> ExtractWindows()
    {
        var windows = new List<float[]>();
        var currentEnd = _sampleBaseIndex + _samples.Count;
        while (currentEnd >= _nextWindowEnd)
        {
            var start = _nextWindowEnd - WindowSamples;
            var offset = checked((int)(start - _sampleBaseIndex));
            if (offset >= 0 && offset + WindowSamples <= _samples.Count)
                windows.Add(_samples.GetRange(offset, WindowSamples).ToArray());
            _nextWindowEnd += HopSamples;
        }

        var keepFrom = Math.Max(_sampleBaseIndex, _nextWindowEnd - WindowSamples - TargetSampleRate);
        var remove = checked((int)Math.Clamp(keepFrom - _sampleBaseIndex, 0, _samples.Count));
        if (remove > 0)
        {
            _samples.RemoveRange(0, remove);
            _sampleBaseIndex += remove;
        }
        return windows;
    }

    private async Task TranscribeWindowAsync(float[] samples, CancellationToken token)
    {
        var processor = _processor;
        if (processor is null) return;
        await foreach (var segment in processor.ProcessAsync(samples, token).ConfigureAwait(false))
        {
            var start = segment.Start.TotalSeconds;
            var end = segment.End.TotalSeconds;
            var text = segment.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || end <= _lastEmittedEndSeconds + 0.35) continue;
            _lastEmittedEndSeconds = end;
            SegmentRecognized?.Invoke(new TranscriptionSegment(start, end, text));
        }
    }

    private static float[] ConvertToMono16k(AudioChunkEventArgs chunk)
    {
        if (chunk.Channels < 1 || chunk.SampleRate < 1 || chunk.BitsPerSample is not (16 or 24 or 32))
            return Array.Empty<float>();
        var bytesPerSample = chunk.BitsPerSample / 8;
        var frameBytes = bytesPerSample * chunk.Channels;
        var frames = chunk.Data.Length / frameBytes;
        if (frames == 0) return Array.Empty<float>();
        var mono = new float[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            double sum = 0;
            for (var channel = 0; channel < chunk.Channels; channel++)
            {
                var offset = (frame * chunk.Channels + channel) * bytesPerSample;
                float value;
                if (chunk.BitsPerSample == 32 && chunk.IsIeeeFloat)
                    value = BitConverter.ToSingle(chunk.Data, offset);
                else if (chunk.BitsPerSample == 16)
                    value = BinaryPrimitives.ReadInt16LittleEndian(chunk.Data.AsSpan(offset, 2)) / 32768f;
                else if (chunk.BitsPerSample == 24)
                {
                    var raw = chunk.Data[offset] | (chunk.Data[offset + 1] << 8) | (chunk.Data[offset + 2] << 16);
                    if ((raw & 0x800000) != 0) raw |= unchecked((int)0xFF000000);
                    value = raw / 8388608f;
                }
                else
                    value = BinaryPrimitives.ReadInt32LittleEndian(chunk.Data.AsSpan(offset, 4)) / 2147483648f;
                sum += float.IsFinite(value) ? Math.Clamp(value, -1f, 1f) : 0f;
            }
            mono[frame] = (float)(sum / chunk.Channels);
        }

        if (chunk.SampleRate == TargetSampleRate) return mono;
        var outputLength = Math.Max(1, (int)Math.Round(frames * (double)TargetSampleRate / chunk.SampleRate));
        var resampled = new float[outputLength];
        var ratio = (double)chunk.SampleRate / TargetSampleRate;
        for (var i = 0; i < outputLength; i++)
        {
            var position = i * ratio;
            var left = Math.Min((int)position, frames - 1);
            var right = Math.Min(left + 1, frames - 1);
            var fraction = position - left;
            resampled[i] = (float)(mono[left] * (1 - fraction) + mono[right] * fraction);
        }
        return resampled;
    }

    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        _cts.Cancel();
        if (_worker is not null)
        {
            try { await _worker.ConfigureAwait(false); } catch (OperationCanceledException) { }
        }
        _processor?.Dispose();
        _factory?.Dispose();
        _cts.Dispose();
        Interlocked.Exchange(ref _started, 0);
    }
}

internal sealed record TranscriptionSegment(double StartSeconds, double EndSeconds, string Text);
