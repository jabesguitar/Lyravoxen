using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using NAudio.Wave;

namespace Lyravoxen.Services;

public sealed class LiveAudioTranscriptionService : IAsyncDisposable
{
    private const int TargetRate = 24000;
    private const int SegmentSamples = TargetRate * 2;
    private readonly Func<double> _playbackSeconds;
    private readonly Channel<byte[]> _audio = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentQueue<double> _pendingSegmentStarts = new();
    private readonly ConcurrentDictionary<string, double> _segmentStarts = new();
    private readonly ConcurrentDictionary<string, StringBuilder> _partialTranscripts = new();
    private readonly CancellationTokenSource _stop = new();
    private ClientWebSocket? _socket;
    private WasapiLoopbackCapture? _capture;
    private Task? _sender;
    private Task? _receiver;
    private int _disposed;

    public event EventHandler<LiveTranscriptEventArgs>? TranscriptReceived;
    public event EventHandler<string>? StatusChanged;

    public LiveAudioTranscriptionService(Func<double> playbackSeconds) => _playbackSeconds = playbackSeconds;

    public async Task StartAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("Configure uma chave da API antes de iniciar.");
        if (_socket is not null) throw new InvalidOperationException("O reconhecimento já está ativo.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stop.Token);
        _socket = new ClientWebSocket();
        _socket.Options.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());
        await _socket.ConnectAsync(new Uri("wss://api.openai.com/v1/realtime?intent=transcription"), linked.Token);

        var session = new
        {
            type = "session.update",
            session = new
            {
                type = "transcription",
                audio = new
                {
                    input = new
                    {
                        format = new { type = "audio/pcm", rate = TargetRate },
                        transcription = new
                        {
                            model = "gpt-live-transcribe",
                            languages = new[] { "pt" },
                            delay = "low",
                            prompt = "Portuguese singing voice in a Christian hymn or worship song. Transcribe the sung lyrics faithfully."
                        },
                        turn_detection = (object?)null
                    }
                }
            }
        };
        await SendJsonAsync(session, linked.Token);

        _receiver = ReceiveLoopAsync(_stop.Token);
        _capture = new WasapiLoopbackCapture();
        _capture.DataAvailable += Capture_DataAvailable;
        _capture.RecordingStopped += Capture_RecordingStopped;
        _capture.StartRecording();
        _sender = SendLoopAsync(_stop.Token);
        StatusChanged?.Invoke(this, "Capturando o áudio de saída do Windows e reconhecendo a letra em tempo real.");
    }

    private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            if (_capture is null || e.BytesRecorded <= 0) return;
            var pcm = ConvertToPcm16Mono24k(e.Buffer, e.BytesRecorded, _capture.WaveFormat);
            if (pcm.Length > 0) _audio.Writer.TryWrite(pcm);
        }
        catch (Exception ex) { StatusChanged?.Invoke(this, "Falha ao processar áudio do sistema: " + ex.Message); }
    }

    private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            StatusChanged?.Invoke(this, "A captura de áudio foi interrompida: " + e.Exception.Message);
    }

    private async Task SendLoopAsync(CancellationToken cancellationToken)
    {
        var samplesInSegment = 0;
        var segmentStart = _playbackSeconds();
        while (await _audio.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_audio.Reader.TryRead(out var pcm))
            {
                await SendJsonAsync(new { type = "input_audio_buffer.append", audio = Convert.ToBase64String(pcm) }, cancellationToken);
                samplesInSegment += pcm.Length / 2;
                if (samplesInSegment >= SegmentSamples)
                {
                    _pendingSegmentStarts.Enqueue(segmentStart);
                    await SendJsonAsync(new { type = "input_audio_buffer.commit" }, cancellationToken);
                    samplesInSegment = 0;
                    segmentStart = _playbackSeconds();
                }
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var socket = _socket ?? throw new InvalidOperationException("WebSocket não conectado.");
        var buffer = new byte[16 * 1024];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) return;
                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            using var json = JsonDocument.Parse(message.ToArray());
            var root = json.RootElement;
            var type = root.TryGetProperty("type", out var typeValue) ? typeValue.GetString() : null;
            if (type == "input_audio_buffer.committed" && root.TryGetProperty("item_id", out var committedId))
            {
                if (_pendingSegmentStarts.TryDequeue(out var start))
                    _segmentStarts[committedId.GetString() ?? ""] = start;
            }
            else if (type == "conversation.item.input_audio_transcription.delta" &&
                     root.TryGetProperty("item_id", out var deltaId) &&
                     root.TryGetProperty("delta", out var delta))
            {
                var id = deltaId.GetString() ?? "";
                var builder = _partialTranscripts.GetOrAdd(id, _ => new StringBuilder());
                lock (builder) builder.Append(delta.GetString());
                if (_segmentStarts.TryGetValue(id, out var start))
                {
                    string text;
                    lock (builder) text = builder.ToString();
                    TranscriptReceived?.Invoke(this, new LiveTranscriptEventArgs(id, text, start, false));
                }
            }
            else if (type == "conversation.item.input_audio_transcription.completed" &&
                     root.TryGetProperty("item_id", out var completedId) &&
                     root.TryGetProperty("transcript", out var transcript))
            {
                var id = completedId.GetString() ?? "";
                var text = transcript.GetString() ?? "";
                if (_segmentStarts.TryGetValue(id, out var start))
                    TranscriptReceived?.Invoke(this, new LiveTranscriptEventArgs(id, text, start, true));
                _segmentStarts.TryRemove(id, out _);
                _partialTranscripts.TryRemove(id, out _);
            }
            else if (type == "error")
            {
                var detail = root.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var messageValue)
                    ? messageValue.GetString() : "Erro desconhecido na API de transcrição.";
                StatusChanged?.Invoke(this, detail ?? "Erro na API de transcrição.");
            }
        }
    }

    private async Task SendJsonAsync(object value, CancellationToken cancellationToken)
    {
        var socket = _socket ?? throw new InvalidOperationException("WebSocket não conectado.");
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static byte[] ConvertToPcm16Mono24k(byte[] input, int length, WaveFormat format)
    {
        var bytesPerSample = format.BitsPerSample / 8;
        if (bytesPerSample is not (2 or 4) || format.Channels < 1) return Array.Empty<byte>();
        var frameCount = length / (bytesPerSample * format.Channels);
        if (frameCount == 0 || format.SampleRate <= 0) return Array.Empty<byte>();

        var mono = new float[frameCount];
        for (var frame = 0; frame < frameCount; frame++)
        {
            var sum = 0f;
            for (var channel = 0; channel < format.Channels; channel++)
            {
                var offset = (frame * format.Channels + channel) * bytesPerSample;
                float sample = format.Encoding == WaveFormatEncoding.IeeeFloat && bytesPerSample == 4
                    ? BitConverter.ToSingle(input, offset)
                    : bytesPerSample == 4
                        ? BitConverter.ToInt32(input, offset) / 2147483648f
                        : BitConverter.ToInt16(input, offset) / 32768f;
                sum += sample;
            }
            mono[frame] = Math.Clamp(sum / format.Channels, -1f, 1f);
        }

        var outputFrames = Math.Max(1, (int)Math.Round(frameCount * (double)TargetRate / format.SampleRate));
        var output = new byte[outputFrames * 2];
        for (var i = 0; i < outputFrames; i++)
        {
            var sourcePosition = i * (double)format.SampleRate / TargetRate;
            var left = Math.Min((int)sourcePosition, frameCount - 1);
            var right = Math.Min(left + 1, frameCount - 1);
            var fraction = (float)(sourcePosition - left);
            var sample = mono[left] + (mono[right] - mono[left]) * fraction;
            var pcm = (short)Math.Clamp((int)Math.Round(sample * short.MaxValue), short.MinValue, short.MaxValue);
            BitConverter.GetBytes(pcm).CopyTo(output, i * 2);
        }
        return output;
    }

    public async Task StopAsync()
    {
        if (_capture is not null)
        {
            _capture.DataAvailable -= Capture_DataAvailable;
            _capture.RecordingStopped -= Capture_RecordingStopped;
            try { _capture.StopRecording(); } catch { }
            _capture.Dispose();
            _capture = null;
        }
        _audio.Writer.TryComplete();
        try { if (_sender is not null) await _sender.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
        _stop.Cancel();
        if (_socket is { State: WebSocketState.Open } socket)
        {
            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Lyravoxen stopped", CancellationToken.None); } catch { }
        }
        try { if (_receiver is not null) await _receiver.WaitAsync(TimeSpan.FromSeconds(2)); } catch { }
        _socket?.Dispose();
        _socket = null;
        StatusChanged?.Invoke(this, "Reconhecimento automático interrompido.");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await StopAsync();
        _stop.Dispose();
    }
}

public sealed class LiveTranscriptEventArgs(string itemId, string transcript, double audioStartSeconds, bool isFinal) : EventArgs
{
    public string ItemId { get; } = itemId;
    public string Transcript { get; } = transcript;
    public double AudioStartSeconds { get; } = audioStartSeconds;
    public bool IsFinal { get; } = isFinal;
}
