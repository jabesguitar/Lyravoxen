using System.Runtime.Versioning;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Lyravoxen.Audio;

/// <summary>
/// Experimental process-scoped WASAPI loopback capture.
/// Never falls back to system-wide loopback.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
internal sealed class ProcessAudioCapture : IAsyncDisposable
{
    private WasapiRecorder? _recorder;
    private readonly Func<double>? _mediaTimeProvider;
    private readonly Func<string>? _sessionProvider;

    public ProcessAudioCapture(Func<double>? mediaTimeProvider = null, Func<string>? sessionProvider = null)
    {
        _mediaTimeProvider = mediaTimeProvider;
        _sessionProvider = sessionProvider;
    }

    public event EventHandler<AudioChunkEventArgs>? AudioChunkAvailable;
    public event Action<Exception>? CaptureFailed;

    public bool IsCapturing => _recorder?.CaptureState == CaptureState.Capturing;
    public int SampleRate => _recorder?.WaveFormat.SampleRate ?? 0;
    public int Channels => _recorder?.WaveFormat.Channels ?? 0;
    public int BitsPerSample => _recorder?.WaveFormat.BitsPerSample ?? 0;

    public async Task StartAsync(uint processId)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            throw new PlatformNotSupportedException("A captura por processo exige Windows 10 versão 2004 (build 19041) ou superior.");
        if (processId == 0) throw new ArgumentOutOfRangeException(nameof(processId));

        await StopAsync().ConfigureAwait(false);

        var recorder = await new WasapiRecorderBuilder()
            .WithProcessLoopback(processId, ProcessLoopbackMode.IncludeTargetProcessTree)
            .WithBufferLength(50)
            .WithMmcssThreadPriority("Audio")
            .BuildAsync().ConfigureAwait(false);

        recorder.DataAvailable += OnDataAvailable;
        recorder.RecordingStopped += OnRecordingStopped;
        _recorder = recorder;
        try
        {
            recorder.StartRecording();
        }
        catch
        {
            _recorder = null;
            recorder.DataAvailable -= OnDataAvailable;
            recorder.RecordingStopped -= OnRecordingStopped;
            await recorder.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        var recorder = _recorder;
        if (recorder is null) return;

        // The WASAPI span is valid only during this callback. Copy immediately before
        // handing the packet to the bounded recognition pipeline.
        var copy = buffer.ToArray();
        var format = recorder.WaveFormat;
        var frames = format.BlockAlign > 0 ? copy.Length / format.BlockAlign : 0;
        AudioChunkAvailable?.Invoke(this, new AudioChunkEventArgs(
            copy, format.SampleRate, format.Channels, format.BitsPerSample, format.Encoding == WaveFormatEncoding.IeeeFloat, _mediaTimeProvider?.Invoke() ?? 0, _sessionProvider?.Invoke() ?? string.Empty, frames, devicePosition, qpcPosition));
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null) CaptureFailed?.Invoke(e.Exception);
    }

    public async Task StopAsync()
    {
        var recorder = _recorder;
        _recorder = null;
        if (recorder is null) return;

        recorder.DataAvailable -= OnDataAvailable;
        recorder.RecordingStopped -= OnRecordingStopped;
        await recorder.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}

internal sealed class AudioChunkEventArgs(
    byte[] data, int sampleRate, int channels, int bitsPerSample, bool isIeeeFloat,
    double mediaTimeSeconds, string sessionId, int frames, long devicePosition, long qpcPosition) : EventArgs
{
    public byte[] Data { get; } = data;
    public int SampleRate { get; } = sampleRate;
    public int Channels { get; } = channels;
    public int BitsPerSample { get; } = bitsPerSample;
    public bool IsIeeeFloat { get; } = isIeeeFloat;
    public double MediaTimeSeconds { get; } = mediaTimeSeconds;
    public string SessionId { get; } = sessionId;
    public int Frames { get; } = frames;
    public long DevicePosition { get; } = devicePosition;
    public long QpcPosition { get; } = qpcPosition;
}
