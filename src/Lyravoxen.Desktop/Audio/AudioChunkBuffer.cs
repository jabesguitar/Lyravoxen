using System.Threading.Channels;

namespace Lyravoxen.Audio;

/// <summary>
/// Bounded, non-blocking handoff from the WASAPI callback to background recognition.
/// Oldest queued windows are discarded under pressure so audio capture never waits
/// for a slow inference worker and memory use remains bounded.
/// </summary>
internal sealed class AudioChunkBuffer : IDisposable
{
    private readonly Channel<AudioChunkEventArgs> _channel;
    private long _droppedChunks;
    private int _completed;

    public AudioChunkBuffer(int capacity = 40)
    {
        if (capacity < 2 || capacity > 400)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be between 2 and 400 chunks.");

        _channel = Channel.CreateBounded<AudioChunkEventArgs>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public long DroppedChunks => Interlocked.Read(ref _droppedChunks);
    public ChannelReader<AudioChunkEventArgs> Reader => _channel.Reader;

    /// <summary>
    /// Safe for the audio callback: does not await inference or perform conversion.
    /// </summary>
    public bool TryWrite(AudioChunkEventArgs chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (Volatile.Read(ref _completed) != 0) return false;

        // DropOldest is intentionally used for bounded memory and freshness.
        // Count pressure by checking capacity before enqueue; this is telemetry,
        // not a synchronization guarantee.
        if (_channel.Reader.Count >= 39)
            Interlocked.Increment(ref _droppedChunks);

        return _channel.Writer.TryWrite(chunk);
    }

    public IAsyncEnumerable<AudioChunkEventArgs> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    public void Complete()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 0)
            _channel.Writer.TryComplete();
    }

    public void Dispose() => Complete();
}

/// <summary>
/// Conservative CPU policy. No AVX2, AVX-512 or GPU is required.
/// Inference runs separately from playback/capture; callers should schedule it
/// below normal UI/audio priority and expose the thread count as a setting.
/// </summary>
internal static class RecognitionResourcePolicy
{
    public static int GetDefaultInferenceThreads()
    {
        var logicalProcessors = Math.Max(1, Environment.ProcessorCount);
        return Math.Clamp(logicalProcessors / 8, 1, 2);
    }

    public static bool IsAvx2Available =>
        System.Runtime.Intrinsics.X86.Avx2.IsSupported;

    public static bool IsAvxAvailable =>
        System.Runtime.Intrinsics.X86.Avx.IsSupported;

    public static int DefaultAudioBufferCapacity => 40;

    public static string CompatibilitySummary =>
        $"CPU threads: {Environment.ProcessorCount}; AVX: {IsAvxAvailable}; AVX2: {IsAvx2Available}; " +
        $"default inference workers: {GetDefaultInferenceThreads()}";
}
