using System.Windows;
using System.Windows.Threading;
using Lyravoxen.Models;

namespace Lyravoxen;

public partial class MainWindow
{
    private readonly DispatcherTimer _mediaConnectivityTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(2) };
    private bool _mediaConnectivityMonitorInitialized;
    private int _connectivityProbeInFlight;
    private int _consecutiveNetworkFailures;
    private bool _youtubePlaybackIntended;
    private bool _wasAutomaticallySwitchedToOffline;
    private bool _pendingOnlineResume;
    private bool _onlineResumeCompleting;
    private LibraryVideo? _pendingOnlineResumeVideo;
    private DateTime _retryOnlineAfterUtc;
    private CancellationTokenSource? _onlineResumeTimeout;
    private bool _pendingLocalSeek;
    private double _pendingLocalPlaybackStartSeconds;

    private void InitializeMediaConnectivityMonitor()
    {
        if (_mediaConnectivityMonitorInitialized) return;
        _mediaConnectivityMonitorInitialized = true;
        _mediaConnectivityTimer.Tick += MediaConnectivityTimer_Tick;
        _mediaConnectivityTimer.Start();
    }

    private void StopMediaConnectivityMonitor()
    {
        _mediaConnectivityTimer.Stop();
        CancelPendingOnlineResume();
    }

    private void ResetMediaFailoverState()
    {
        _youtubePlaybackIntended = false;
        _wasAutomaticallySwitchedToOffline = false;
        _consecutiveNetworkFailures = 0;
        _retryOnlineAfterUtc = DateTime.MinValue;
        CancelPendingOnlineResume();
    }

    private void SetPendingLocalPlaybackStart(double seconds)
    {
        _pendingLocalPlaybackStartSeconds = Math.Max(0, double.IsFinite(seconds) ? seconds : 0);
        _pendingLocalSeek = _pendingLocalPlaybackStartSeconds > 0.05;
    }

    private void LocalAudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (!_pendingLocalSeek) return;
        try
        {
            var target = _pendingLocalPlaybackStartSeconds;
            if (LocalAudioPlayer.NaturalDuration.HasTimeSpan)
            {
                var duration = LocalAudioPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                if (duration > 0) target = Math.Clamp(target, 0, Math.Max(0, duration - 0.1));
            }
            LocalAudioPlayer.Position = TimeSpan.FromSeconds(target);
            _lastKnownPlaybackSeconds = target;
            _pendingLocalSeek = false;
            _lastAutomaticCue = -1;
            StatusText.Text = $"Cópia offline retomada em {TimeSpan.FromSeconds(target).ToString(@"mm\:ss")}.";
        }
        catch { _pendingLocalSeek = false; }
    }

    private LibraryVideo? FindOfflineBackupForCurrentVideo()
    {
        var videoId = _currentVideo?.VideoId;
        if (string.IsNullOrWhiteSpace(videoId)) return null;
        return _library.SelectMany(folder => folder.Videos)
            .FirstOrDefault(item => item.IsOffline && item.VideoId == videoId &&
                !string.IsNullOrWhiteSpace(item.LocalPath) && File.Exists(item.LocalPath) &&
                (!item.OfflineExpiresAt.HasValue || item.OfflineExpiresAt.Value > DateTime.Now));
    }

    private static LibraryVideo CreateOfflinePlaybackCopy(LibraryVideo source, LibraryVideo backup) => new()
    {
        VideoId = string.IsNullOrWhiteSpace(source.VideoId) ? backup.VideoId : source.VideoId,
        Title = string.IsNullOrWhiteSpace(source.Title) ? backup.Title : source.Title,
        TrackName = string.IsNullOrWhiteSpace(source.TrackName) ? backup.TrackName : source.TrackName,
        Artist = string.IsNullOrWhiteSpace(source.Artist) ? backup.Artist : source.Artist,
        Composer = string.IsNullOrWhiteSpace(source.Composer) ? backup.Composer : source.Composer,
        Url = string.IsNullOrWhiteSpace(source.Url) ? backup.Url : source.Url,
        IsOffline = true, LocalPath = backup.LocalPath, Format = backup.Format,
        FullLyrics = string.IsNullOrWhiteSpace(source.FullLyrics) ? backup.FullLyrics : source.FullLyrics,
        Cues = (source.Cues.Count > 0 ? source.Cues : backup.Cues).Select(CloneLyricCue).ToList(),
        Sections = CloneLyricsSections(source.Sections.Count > 0 ? source.Sections : backup.Sections),
        ExecutionOrder = CloneLyricsExecutionOrder(source.ExecutionOrder.Count > 0 ? source.ExecutionOrder : backup.ExecutionOrder),
        TimelineAdjustments = CloneLyricsTimelineAdjustments(source.TimelineAdjustments.Count > 0 ? source.TimelineAdjustments : backup.TimelineAdjustments),
        SavedAt = source.SavedAt != default ? source.SavedAt : backup.SavedAt,
        OfflineExpiresAt = backup.OfflineExpiresAt
    };

    private static LibraryVideo CreateOnlinePlaybackCopy(LibraryVideo source) => new()
    {
        VideoId = source.VideoId, Title = source.Title, TrackName = source.TrackName, Artist = source.Artist,
        Composer = source.Composer, Url = source.Url, IsOffline = false, LocalPath = null, Format = null,
        FullLyrics = source.FullLyrics, Cues = source.Cues.Select(CloneLyricCue).ToList(),
        Sections = CloneLyricsSections(source.Sections), ExecutionOrder = CloneLyricsExecutionOrder(source.ExecutionOrder),
        TimelineAdjustments = CloneLyricsTimelineAdjustments(source.TimelineAdjustments), SavedAt = source.SavedAt
    };

    private async void MediaConnectivityTimer_Tick(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _connectivityProbeInFlight, 1) != 0) return;
        try
        {
            var checkOnline = _youtubePlaybackIntended && !_isLocalAudioPlaying && _lastPlayerState == 1;
            var checkReturn = _wasAutomaticallySwitchedToOffline && _isLocalAudioPlaying;
            if (!checkOnline && !checkReturn) { _consecutiveNetworkFailures = 0; return; }
            var online = await IsYoutubeReachableAsync();
            if (checkOnline)
            {
                if (online) { _consecutiveNetworkFailures = 0; return; }
                _consecutiveNetworkFailures++;
                if (_consecutiveNetworkFailures >= 2)
                {
                    _consecutiveNetworkFailures = 0;
                    await SwitchToOfflineAtCurrentPositionAsync();
                }
            }
            else if (checkReturn && online && DateTime.UtcNow >= _retryOnlineAfterUtc)
                await BeginOnlineResumeAsync();
        }
        catch { /* Falhas transitórias de rede são reavaliadas no próximo ciclo. */ }
        finally { Volatile.Write(ref _connectivityProbeInFlight, 0); }
    }

    private static async Task<bool> IsYoutubeReachableAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(1800));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.youtube.com/generate_204");
        using var response = await MetadataHttp.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        return response.IsSuccessStatusCode;
    }

    private async Task SwitchToOfflineAtCurrentPositionAsync()
    {
        if (!_youtubePlaybackIntended || _isLocalAudioPlaying || _currentVideo is null) return;
        var backup = FindOfflineBackupForCurrentVideo();
        if (backup is null)
        {
            _retryOnlineAfterUtc = DateTime.UtcNow.AddSeconds(8);
            StatusText.Text = "A internet caiu, mas esta música não tem uma cópia offline válida para continuar.";
            return;
        }
        var position = Math.Max(0, _lastKnownPlaybackSeconds);
        var localVersion = CreateOfflinePlaybackCopy(_currentVideo, backup);
        _wasAutomaticallySwitchedToOffline = true;
        try
        {
            if (YoutubePlayer.CoreWebView2 is not null)
                await YoutubePlayer.CoreWebView2.ExecuteScriptAsync("if(typeof player !== 'undefined' && player && typeof player.pauseVideo === 'function') player.pauseVideo();");
        }
        catch { }
        PlayLocalAudio(localVersion, position);
        StatusText.Text = $"Conexão interrompida. Música e letra continuaram offline em {TimeSpan.FromSeconds(position).ToString(@"mm\:ss")}.";
    }

    private async Task BeginOnlineResumeAsync()
    {
        if (!_wasAutomaticallySwitchedToOffline || !_isLocalAudioPlaying || _currentVideo is null ||
            _pendingOnlineResume || DateTime.UtcNow < _retryOnlineAfterUtc) return;
        var videoId = YouTubeLink.TryGetVideoId(_currentVideo.Url) ?? _currentVideo.VideoId;
        if (string.IsNullOrWhiteSpace(videoId)) return;
        try
        {
            _pendingOnlineResume = true;
            _onlineResumeCompleting = false;
            _pendingOnlineResumeVideo = CreateOnlinePlaybackCopy(_currentVideo);
            var position = Math.Max(0, LocalAudioPlayer.Position.TotalSeconds);
            _playerSessionId = Guid.NewGuid().ToString("N");
            _playerReady = false;
            _lastPlayerState = -1;
            _playerDurationSeconds = 0;
            _lastKnownPlaybackSeconds = position;
            await YoutubePlayer.EnsureCoreWebView2Async();
            Directory.CreateDirectory(_playerFolder);
            if (!_virtualHostMapped)
            {
                YoutubePlayer.CoreWebView2.SetVirtualHostNameToFolderMapping("lyravoxen.local", _playerFolder, CoreWebView2HostResourceAccessKind.Allow);
                _virtualHostMapped = true;
            }
            var html = YouTubePlayerPage.Build(videoId, _playerSessionId, position, autoplay: true);
            await File.WriteAllTextAsync(Path.Combine(_playerFolder, "index.html"), html);
            YoutubePlayer.Visibility = Visibility.Visible;
            LocalAudioPlayer.Visibility = Visibility.Visible;
            LocalAudioPlayer.Opacity = 0;
            LocalAudioPlayer.IsHitTestVisible = false;
            YoutubePlayer.CoreWebView2.Navigate("https://lyravoxen.local/index.html");
            _onlineResumeTimeout?.Cancel();
            _onlineResumeTimeout?.Dispose();
            _onlineResumeTimeout = new CancellationTokenSource();
            _ = ObserveOnlineResumeTimeoutAsync(_onlineResumeTimeout.Token);
            StatusText.Text = $"Internet disponível. Preparando vídeo online em {TimeSpan.FromSeconds(position).ToString(@"mm\:ss")}; o áudio local continua até a confirmação.";
        }
        catch (Exception ex) { HandleOnlineResumeFailure("Não foi possível preparar o player online: " + ex.Message, permanent: false); }
    }

    private async Task ObserveOnlineResumeTimeoutAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(12), token);
            await Dispatcher.InvokeAsync(() =>
            {
                if (_pendingOnlineResume)
                    HandleOnlineResumeFailure("O YouTube não confirmou a retomada em 12 segundos.", permanent: false);
            });
        }
        catch (OperationCanceledException) { }
    }

    private async Task CompleteOnlineResumeAsync()
    {
        if (!_pendingOnlineResume || _onlineResumeCompleting || _pendingOnlineResumeVideo is null) return;
        _onlineResumeCompleting = true;
        var position = Math.Max(0, LocalAudioPlayer.Position.TotalSeconds);
        try
        {
            var seconds = position.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            await YoutubePlayer.CoreWebView2.ExecuteScriptAsync("if(typeof player !== 'undefined' && player){player.seekTo(" + seconds + ", true);player.playVideo();}");
        }
        catch (Exception ex)
        {
            HandleOnlineResumeFailure("Não foi possível alinhar o player online: " + ex.Message, permanent: false);
            return;
        }
        if (!_pendingOnlineResume || _pendingOnlineResumeVideo is null) return;
        var onlineVersion = CreateOnlinePlaybackCopy(_pendingOnlineResumeVideo);
        LocalAudioPlayer.Pause();
        _isLocalAudioPlaying = false;
        _audioTimer.Stop();
        _currentVideo = onlineVersion;
        MusicTrackBox.Text = onlineVersion.TrackName;
        MusicArtistBox.Text = onlineVersion.Artist;
        ManualLyricsBox.Text = onlineVersion.FullLyrics ?? "";
        _cues.Clear();
        _cues.AddRange(CueTimeline.Normalize(onlineVersion.Cues));
        CueList.ItemsSource = null;
        CueList.ItemsSource = _cues;
        ReloadLyricsCardsFromCurrentVideo();
        _pendingOnlineResume = false;
        _onlineResumeCompleting = false;
        _pendingOnlineResumeVideo = null;
        _onlineResumeTimeout?.Cancel();
        _onlineResumeTimeout?.Dispose();
        _onlineResumeTimeout = null;
        _wasAutomaticallySwitchedToOffline = false;
        _youtubePlaybackIntended = true;
        _lastPlayerState = 1;
        _lastKnownPlaybackSeconds = position;
        _lastAutomaticCue = FindActiveCue(position);
        LocalAudioPlayer.Opacity = 1;
        LocalAudioPlayer.IsHitTestVisible = true;
        LocalAudioPlayer.Visibility = Visibility.Collapsed;
        YoutubePlayer.Visibility = Visibility.Visible;
        OfflinePlaybackBar.Visibility = Visibility.Collapsed;
        if (_lastAutomaticCue >= 0) PublishCueBlock(_lastAutomaticCue);
        StatusText.Text = $"Internet restabelecida. Vídeo online retomado em {TimeSpan.FromSeconds(position).ToString(@"mm\:ss")}; sincronização preservada.";
    }

    private void HandleOnlineResumeFailure(string explanation, bool permanent)
    {
        _pendingOnlineResume = false;
        _onlineResumeCompleting = false;
        _pendingOnlineResumeVideo = null;
        _onlineResumeTimeout?.Cancel();
        _onlineResumeTimeout?.Dispose();
        _onlineResumeTimeout = null;
        YoutubePlayer.Visibility = Visibility.Collapsed;
        LocalAudioPlayer.Visibility = Visibility.Visible;
        LocalAudioPlayer.Opacity = 1;
        LocalAudioPlayer.IsHitTestVisible = true;
        _isLocalAudioPlaying = true;
        _audioTimer.Start();
        if (permanent)
        {
            _wasAutomaticallySwitchedToOffline = false;
            _youtubePlaybackIntended = false;
        }
        else
        {
            _wasAutomaticallySwitchedToOffline = true;
            _retryOnlineAfterUtc = DateTime.UtcNow.AddSeconds(10);
        }
        StatusText.Text = "A tentativa de voltar ao YouTube não concluiu (" + explanation + "). A cópia offline continua tocando.";
    }

    private void CancelPendingOnlineResume()
    {
        _pendingOnlineResume = false;
        _onlineResumeCompleting = false;
        _pendingOnlineResumeVideo = null;
        _onlineResumeTimeout?.Cancel();
        _onlineResumeTimeout?.Dispose();
        _onlineResumeTimeout = null;
        if (LocalAudioPlayer is not null)
        {
            LocalAudioPlayer.Opacity = 1;
            LocalAudioPlayer.IsHitTestVisible = true;
        }
    }
}
