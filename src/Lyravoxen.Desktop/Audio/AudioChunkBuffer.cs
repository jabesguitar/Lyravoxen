namespace Lyravoxen.Audio;

/// <summary>
/// Small bounded queue between the real-time WASAPI callback and recognition.
/// Enqueue only copies a reference and performs a short lock; inference never runs here.
/// When full, the oldest packet is removed so consumers always receive recent audio.
/// </summary>
internal sealed class AudioChunkBuffer : IDisposable
{
    private readonly object _sync = new();
    private readonly Queue<AudioChunkEventArgs> _queue = new();
    private readonly SemaphoreSlim _available = new(0);
    private readonly int _capacity;
    private bool _completed;
    private long _droppedChunks;

    public AudioChunkBuffer(int capacity = 40)
    {
        if (capacity < 2 || capacity > 400)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be between 2 and 400 chunks.");
        _capacity = capacity;
    }

    public long DroppedChunks => Interlocked.Read(ref _droppedChunks);
    public int QueuedChunks { get { lock (_sync) return _queue.Count; } }

    public bool TryWrite(AudioChunkEventArgs chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        lock (_sync)
        {
            if (_completed) return false;
            if (_queue.Count >= _capacity)
            {
                _queue.Dequeue();
                Interlocked.Increment(ref _droppedChunks);
                // The semaphore already represents the removed packet.
            }
            else
            {
                _available.Release();
            }
            _queue.Enqueue(chunk);
            return true;
        }
    }

    public async ValueTask<AudioChunkEventArgs?> ReadAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            await _available.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_sync)
            {
                if (_queue.Count > 0) return _queue.Dequeue();
                if (_completed) return null;
            }
        }
    }

    public void Complete()
    {
        lock (_sync)
        {
            if (_completed) return;
            _completed = true;
            _available.Release();
        }
    }

    public void Dispose()
    {
        Complete();
        _available.Dispose();
    }
}

/// <summary>Conservative CPU policy; recognition remains separate from playback.</summary>
internal static class RecognitionResourcePolicy
{
    public static int GetDefaultInferenceThreads()
    {
        var logicalProcessors = Math.Max(1, Environment.ProcessorCount);
        return Math.Clamp(logicalProcessors / 8, 1, 2);
    }

    public static bool IsAvx2Available => System.Runtime.Intrinsics.X86.Avx2.IsSupported;
    public static bool IsAvxAvailable => System.Runtime.Intrinsics.X86.Avx.IsSupported;
    public static int DefaultAudioBufferCapacity => 40;
    public static string CompatibilitySummary =>
        $"CPU threads: {Environment.ProcessorCount}; AVX: {IsAvxAvailable}; AVX2: {IsAvx2Available}; " +
        $"default inference workers: {GetDefaultInferenceThreads()}";
}
