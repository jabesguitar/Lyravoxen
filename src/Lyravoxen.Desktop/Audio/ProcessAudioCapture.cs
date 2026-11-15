using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace Lyravoxen.Audio;

/// <summary>
/// Experimental, process-scoped WASAPI loopback capture.
/// This class never falls back to system-wide loopback.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
internal sealed class ProcessAudioCapture : IAsyncDisposable
{
    private AudioClient? _client;
    private AudioCaptureClient? _capture;
    private CancellationTokenSource? _stop;
    private Task? _worker;

    public event EventHandler<AudioChunkEventArgs>? AudioChunkAvailable;
    public event EventHandler<Exception>? CaptureFailed;

    public bool IsCapturing => _worker is { IsCompleted: false };
    public int SampleRate { get; private set; }
    public int Channels { get; private set; }
    public int BitsPerSample { get; private set; }

    public async Task StartAsync(uint processId, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            throw new PlatformNotSupportedException("A captura por processo exige Windows 10 versão 2004 (build 19041) ou superior.");
        if (processId == 0)
            throw new ArgumentOutOfRangeException(nameof(processId));

        await StopAsync().ConfigureAwait(false);

        var client = await AudioClient.ActivateProcessLoopbackAsync(
            processId, ProcessLoopbackMode.IncludeTargetProcessTree).ConfigureAwait(false);

        try
        {
            var format = client.MixFormat;
            client.Initialize(
                AudioClientShareMode.Shared,
                AudioClientStreamFlags.Loopback,
                10_000_000, // buffer de referência de 1 segundo; captura é drenada em pacotes menores
                0,
                format,
                Guid.Empty);

            var capture = client.AudioCaptureClient;
            var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _client = client;
            _capture = capture;
            _stop = stop;
            SampleRate = format.SampleRate;
            Channels = format.Channels;
            BitsPerSample = format.BitsPerSample;

            client.Start();
            _worker = Task.Run(() => CaptureLoopAsync(client, capture, format.BlockAlign, stop.Token), CancellationToken.None);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private async Task CaptureLoopAsync(AudioClient client, AudioCaptureClient capture, int blockAlign, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var packetFrames = capture.GetNextPacketSize();
                if (packetFrames == 0)
                {
                    await Task.Delay(4, token).ConfigureAwait(false);
                    continue;
                }

                while (packetFrames > 0 && !token.IsCancellationRequested)
                {
                    var pointer = capture.GetBuffer(out var frames, out var flags, out _, out _);
                    try
                    {
                        var byteCount = checked(frames * blockAlign);
                        var bytes = new byte[byteCount];
                        if ((flags & AudioClientBufferFlags.Silent) == 0 && byteCount > 0)
                            Marshal.Copy(pointer, bytes, 0, byteCount);

                        AudioChunkAvailable?.Invoke(this, new AudioChunkEventArgs(bytes, SampleRate, Channels, BitsPerSample, frames));
                    }
                    finally
                    {
                        capture.ReleaseBuffer(frames);
                    }

                    packetFrames = capture.GetNextPacketSize();
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Expected during track change or shutdown.
        }
        catch (Exception ex)
        {
            CaptureFailed?.Invoke(this, ex);
        }
    }

    public async Task StopAsync()
    {
        var stop = _stop;
        var worker = _worker;
        var client = _client;
        var capture = _capture;

        _stop = null;
        _worker = null;
        _client = null;
        _capture = null;

        if (stop is not null)
        {
            await stop.CancelAsync().ConfigureAwait(false);
            if (worker is not null)
            {
                try { await worker.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            stop.Dispose();
        }

        if (client is not null)
        {
            try { client.Stop(); } catch { /* already stopped by device/OS */ }
        }
        capture?.Dispose();
        client?.Dispose();
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}

internal sealed class AudioChunkEventArgs(
    byte[] data, int sampleRate, int channels, int bitsPerSample, int frames) : EventArgs
{
    public byte[] Data { get; } = data;
    public int SampleRate { get; } = sampleRate;
    public int Channels { get; } = channels;
    public int BitsPerSample { get; } = bitsPerSample;
    public int Frames { get; } = frames;
}
