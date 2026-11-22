using System.Threading.Channels;
using NAudio.Wave;

namespace Lyravoxen.Services;

/// <summary>
/// Escuta o som que o PC está tocando (captura "loopback" do Windows) para o celular poder ouvir e conferir que a música rolou.
/// Entrega áudio mono de 16 bits a 24 kHz em blocos de 100 ms e um nível de volume (0 a 100). Só captura enquanto há celular
/// usando (para sozinho depois de 20 s sem uso). Não precisa de internet: o som vai pela rede local.
/// </summary>
internal sealed class AudioMonitorService : IDisposable
{
    public const int SampleRate = 24000;

    private readonly object _gate = new();
    private readonly List<Channel<byte[]>> _clients = new();
    private readonly List<float> _pending = new();   // mono, na taxa da placa de som
    private readonly List<short> _output = new();    // 24 kHz, mono, 16 bits, aguardando o próximo bloco
    private WasapiLoopbackCapture? _capture;
    private CancellationTokenSource? _pumpCts;
    private int _sourceRate;
    private int _channels;
    private int _bits;
    private long _position; // em fração de amostra: unidades de 1/SampleRate da amostra de origem (aritmética exata, sem erro acumulado)
    private DateTime _lastUse = DateTime.UtcNow;
    private volatile int _level;
    private double _smoothedLevel;

    /// <summary>Volume atual do som do PC, de 0 (silêncio) a 100.</summary>
    public int Level => _level;
    public bool Running => _capture is not null;
    public string LastError { get; private set; } = "";

    /// <summary>Liga a captura se estiver desligada e marca que há alguém usando.</summary>
    public void EnsureStarted()
    {
        lock (_gate)
        {
            _lastUse = DateTime.UtcNow;
            if (_capture is not null) return;
            try
            {
                var capture = new WasapiLoopbackCapture();
                var format = capture.WaveFormat;
                if (format.BitsPerSample != 32 && format.BitsPerSample != 16)
                {
                    LastError = "formato de áudio do Windows não suportado (" + format.BitsPerSample + " bits)";
                    capture.Dispose();
                    return;
                }
                _sourceRate = format.SampleRate;
                _channels = Math.Max(1, format.Channels);
                _bits = format.BitsPerSample;
                _pending.Clear();
                _output.Clear();
                _position = 0;
                capture.DataAvailable += OnData;
                capture.RecordingStopped += (_, e) => OnStopped(capture, e.Exception);
                capture.StartRecording();
                _capture = capture;
                _pumpCts = new CancellationTokenSource();
                var token = _pumpCts.Token;
                _ = Task.Run(() => PumpAsync(token));
                LastError = "";
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppLog.Error("Monitor de áudio: não foi possível capturar o som do PC", ex);
            }
        }
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        var bytesPerSample = _bits / 8;
        var frameBytes = bytesPerSample * _channels;
        if (frameBytes <= 0) return;
        var frames = e.BytesRecorded / frameBytes;
        lock (_gate)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                var offset = frame * frameBytes;
                var sum = 0f;
                for (var channel = 0; channel < _channels; channel++)
                {
                    sum += _bits == 32
                        ? BitConverter.ToSingle(e.Buffer, offset + channel * 4)
                        : BitConverter.ToInt16(e.Buffer, offset + channel * 2) / 32768f;
                }
                _pending.Add(sum / _channels);
            }
            ResampleLocked();
        }
    }

    // Reduz para 24 kHz fazendo a média das amostras de cada passo (suficiente para ouvir e conferir a música).
    // A posição é inteira (1 amostra de origem = SampleRate unidades; cada passo = _sourceRate unidades).
    private void ResampleLocked()
    {
        while (_position + _sourceRate <= (long)_pending.Count * SampleRate)
        {
            var start = (int)(_position / SampleRate);
            var end = (int)Math.Min(_pending.Count, (_position + _sourceRate + SampleRate - 1) / SampleRate);
            var sum = 0f;
            var count = 0;
            for (var i = start; i < end; i++) { sum += _pending[i]; count++; }
            var value = count > 0 ? sum / count : 0f;
            _output.Add((short)Math.Clamp(value * 32767f, -32768f, 32767f));
            _position += _sourceRate;
        }
        var consumed = (int)(_position / SampleRate);
        if (consumed > 0)
        {
            _pending.RemoveRange(0, consumed);
            _position -= (long)consumed * SampleRate;
        }
        // Se ninguém estiver lendo, não acumula.
        if (_output.Count > SampleRate * 5) _output.RemoveRange(0, _output.Count - SampleRate * 2);
    }

    private async Task PumpAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(100, token);
                short[] samples;
                lock (_gate)
                {
                    samples = _output.ToArray();
                    _output.Clear();
                }
                // Nada tocando: a placa de som não envia dados; mandamos silêncio contínuo para o relógio do celular não parar.
                if (samples.Length == 0) samples = new short[SampleRate / 10];
                UpdateLevel(samples);
                var bytes = new byte[samples.Length * 2];
                Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
                bool idle;
                lock (_gate)
                {
                    foreach (var client in _clients) client.Writer.TryWrite(bytes);
                    idle = _clients.Count == 0 && DateTime.UtcNow - _lastUse > TimeSpan.FromSeconds(20);
                }
                if (idle)
                {
                    Stop();
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void UpdateLevel(short[] samples)
    {
        double sumSquares = 0;
        foreach (var sample in samples) sumSquares += (double)sample * sample;
        var rms = Math.Sqrt(sumSquares / samples.Length) / 32768.0;
        var decibels = 20 * Math.Log10(rms + 1e-9);
        var target = Math.Clamp((decibels + 60) / 60 * 100, 0, 100);
        _smoothedLevel = target > _smoothedLevel ? target : Math.Max(target, _smoothedLevel - 8);
        _level = (int)Math.Round(_smoothedLevel);
    }

    private void OnStopped(WasapiLoopbackCapture capture, Exception? error)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_capture, capture)) return;
            if (error is not null) LastError = error.Message; // ex.: trocou a saída de áudio; liga de novo no próximo uso
            _capture = null;
            _pumpCts?.Cancel();
            _level = 0;
            _smoothedLevel = 0;
        }
        try { capture.Dispose(); } catch { }
    }

    public void Stop()
    {
        WasapiLoopbackCapture? capture;
        lock (_gate)
        {
            capture = _capture;
            _capture = null;
            _pumpCts?.Cancel();
            _level = 0;
            _smoothedLevel = 0;
            _pending.Clear();
            _output.Clear();
        }
        try { capture?.StopRecording(); } catch { }
        try { capture?.Dispose(); } catch { }
    }

    /// <summary>Envia o som ao celular (PCM 16 bits, mono, 24 kHz) até ele desconectar.</summary>
    public async Task StreamAsync(Stream output, CancellationToken token)
    {
        var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(60)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        lock (_gate) _clients.Add(channel);
        try
        {
            EnsureStarted();
            if (!Running) return; // sem captura (ver LastError): a conexão fecha e o celular avisa
            await foreach (var chunk in channel.Reader.ReadAllAsync(token))
            {
                await output.WriteAsync(chunk, token);
                lock (_gate) _lastUse = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { } // o celular fechou a conexão
        finally
        {
            lock (_gate) _clients.Remove(channel);
        }
    }

    public void Dispose() => Stop();
}
