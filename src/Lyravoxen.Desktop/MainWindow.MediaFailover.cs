using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Threading;
using Lyravoxen.Models;
using Lyravoxen.Services;

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
    private DateTime _lastPlaybackPositionSaveUtc = DateTime.MinValue;
    private double _lastPersistedPlaybackPositionSeconds = double.NaN;
    private bool? _lastYoutubeProbeSucceeded;
    private DateTime _lastYoutubeProbeUtc = DateTime.MinValue;

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
        _pendingLocalPlaybackStartSeconds = OfflineLibraryMatcher.ResolveResumePosition(seconds, 0);
        // Sempre aguarda MediaOpened; inclusive quando o ponto solicitado é 00:00.
        _pendingLocalSeek = true;
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

            LocalAudioPlayer.Pause();
            LocalAudioPlayer.Position = TimeSpan.FromSeconds(target);
            _lastKnownPlaybackSeconds = target;
            _pendingLocalSeek = false;
            _isLocalAudioPlaying = true;
            _playbackEnded = false;
            LocalAudioPlayer.Play();
            _audioTimer.Start();
            _lastAutomaticCue = -1;
            UpdateOfflinePlaybackPosition();

            if (_cues.Count > 0 && !_manualProjectionMode && !_automaticLyricsActive)
            {
                var cueIndex = FindActiveCue(target);
                _lastAutomaticCue = cueIndex;
                if (cueIndex >= 0) PublishCueBlock(cueIndex);
            }

            PersistCurrentPlaybackPosition(target, force: true);
            AppLog.Info($"Áudio local aberto e posicionado em {TimeSpan.FromSeconds(target):mm\\:ss}; cues={_cues.Count}; posição confirmada após MediaOpened.");
            StatusText.Text = target > 0.05
                ? $"Cópia offline retomada em {TimeSpan.FromSeconds(target):mm\\:ss}. Música e letra alinhadas."
                : "Reprodução offline iniciada do começo.";
        }
        catch (Exception ex)
        {
            _pendingLocalSeek = false;
            _isLocalAudioPlaying = false;
            _audioTimer.Stop();
            AppLog.Error("Falha ao posicionar o áudio local no segundo solicitado", ex);
            StatusText.Text = "O áudio abriu, mas não foi possível posicioná-lo no ponto solicitado: " + ex.Message;
        }
    }

    private void PersistCurrentPlaybackPosition(double seconds, bool force = false)
    {
        if (_currentVideo is null || !double.IsFinite(seconds) || seconds < 0) return;
        _currentVideo.LastPlaybackPositionSeconds = seconds;

        var now = DateTime.UtcNow;
        if (!force &&
            now - _lastPlaybackPositionSaveUtc < TimeSpan.FromSeconds(4) &&
            double.IsFinite(_lastPersistedPlaybackPositionSeconds) &&
            Math.Abs(seconds - _lastPersistedPlaybackPositionSeconds) < 2.0)
            return;

        var changed = false;
        foreach (var item in _library.SelectMany(folder => folder.Videos))
        {
            var sameLocalFile = _currentVideo.IsOffline && item.IsOffline &&
                !string.IsNullOrWhiteSpace(_currentVideo.LocalPath) &&
                string.Equals(item.LocalPath, _currentVideo.LocalPath, StringComparison.OrdinalIgnoreCase);
            var sameYoutubeId = !string.IsNullOrWhiteSpace(_currentVideo.VideoId) &&
                string.Equals(item.VideoId, _currentVideo.VideoId, StringComparison.Ordinal);
            if (!sameLocalFile && !sameYoutubeId) continue;
            if (Math.Abs(item.LastPlaybackPositionSeconds - seconds) > 0.1)
            {
                item.LastPlaybackPositionSeconds = seconds;
                changed = true;
            }
        }

        var match = OfflineLibraryMatcher.FindMatchingBackup(
            _library.SelectMany(folder => folder.Videos), _currentVideo, DateTime.Now);
        if (match is not null && Math.Abs(match.Video.LastPlaybackPositionSeconds - seconds) > 0.1)
        {
            match.Video.LastPlaybackPositionSeconds = seconds;
            changed = true;
        }

        if (changed)
        {
            if (!SaveLibrary())
                AppLog.Error("Não foi possível persistir o ponto de reprodução da biblioteca.");
        }
        _lastPlaybackPositionSaveUtc = now;
        _lastPersistedPlaybackPositionSeconds = seconds;
    }

    private OfflineLibraryMatch? FindOfflineBackupMatchForCurrentVideo() =>
        OfflineLibraryMatcher.FindMatchingBackup(
            _library.SelectMany(folder => folder.Videos), _currentVideo, DateTime.Now);

    private LibraryVideo? FindOfflineBackupForCurrentVideo() =>
        FindOfflineBackupMatchForCurrentVideo()?.Video;

    private void BindOfflineBackupToCurrentRecording(LibraryVideo backup, LibraryVideo source)
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(backup.VideoId) && !string.IsNullOrWhiteSpace(source.VideoId))
        {
            backup.VideoId = source.VideoId;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(backup.Url) && !string.IsNullOrWhiteSpace(source.Url))
        {
            backup.Url = source.Url;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(backup.Title) && !string.IsNullOrWhiteSpace(source.Title))
        {
            backup.Title = source.Title;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(backup.TrackName) && !string.IsNullOrWhiteSpace(source.TrackName))
        {
            backup.TrackName = source.TrackName;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(backup.Artist) && !string.IsNullOrWhiteSpace(source.Artist))
        {
            backup.Artist = source.Artist;
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(backup.Composer) && !string.IsNullOrWhiteSpace(source.Composer))
        {
            backup.Composer = source.Composer;
            changed = true;
        }

        var sourceCues = source.Cues.Count > 0 ? source.Cues :
            (ReferenceEquals(source, _currentVideo) && _cues.Count > 0 ? _cues : new List<LyricCue>());
        if (backup.Cues.Count == 0 && sourceCues.Count > 0)
        {
            backup.Cues = sourceCues.Select(CloneLyricCue).ToList();
            changed = true;
        }
        if (string.IsNullOrWhiteSpace(backup.FullLyrics) && !string.IsNullOrWhiteSpace(source.FullLyrics))
        {
            backup.FullLyrics = source.FullLyrics;
            changed = true;
        }
        if (backup.Sections.Count == 0 && source.Sections.Count > 0)
        {
            backup.Sections = CloneLyricsSections(source.Sections);
            changed = true;
        }
        if (backup.ExecutionOrder.Count == 0 && source.ExecutionOrder.Count > 0)
        {
            backup.ExecutionOrder = CloneLyricsExecutionOrder(source.ExecutionOrder);
            changed = true;
        }
        if (backup.TimelineAdjustments.Count == 0 && source.TimelineAdjustments.Count > 0)
        {
            backup.TimelineAdjustments = CloneLyricsTimelineAdjustments(source.TimelineAdjustments);
            changed = true;
        }

        if (changed)
        {
            SaveLibrary();
            RefreshLibrarySongs();
            AppLog.Info($"Cópia offline vinculada à faixa {backup.TrackName} — {backup.Artist}; vídeo={backup.VideoId}; frases={backup.Cues.Count}.");
        }
    }

    private LibraryVideo CreateOfflinePlaybackCopy(LibraryVideo source, LibraryVideo backup)
    {
        var sourceCues = source.Cues.Count > 0
            ? source.Cues
            : (ReferenceEquals(source, _currentVideo) && _cues.Count > 0 ? _cues : backup.Cues);
        return new LibraryVideo
        {
            VideoId = string.IsNullOrWhiteSpace(source.VideoId) ? backup.VideoId : source.VideoId,
            Title = string.IsNullOrWhiteSpace(source.Title) ? backup.Title : source.Title,
            TrackName = string.IsNullOrWhiteSpace(source.TrackName) ? backup.TrackName : source.TrackName,
            Artist = string.IsNullOrWhiteSpace(source.Artist) ? backup.Artist : source.Artist,
            Composer = string.IsNullOrWhiteSpace(source.Composer) ? backup.Composer : source.Composer,
            Url = string.IsNullOrWhiteSpace(source.Url) ? backup.Url : source.Url,
            IsOffline = true, LocalPath = backup.LocalPath, Format = backup.Format,
            FullLyrics = string.IsNullOrWhiteSpace(source.FullLyrics) ? backup.FullLyrics : source.FullLyrics,
            Cues = sourceCues.Select(CloneLyricCue).ToList(),
            Sections = CloneLyricsSections(source.Sections.Count > 0 ? source.Sections : backup.Sections),
            ExecutionOrder = CloneLyricsExecutionOrder(source.ExecutionOrder.Count > 0 ? source.ExecutionOrder : backup.ExecutionOrder),
            TimelineAdjustments = CloneLyricsTimelineAdjustments(source.TimelineAdjustments.Count > 0 ? source.TimelineAdjustments : backup.TimelineAdjustments),
            SavedAt = source.SavedAt != default ? source.SavedAt : backup.SavedAt,
            OfflineExpiresAt = backup.OfflineExpiresAt,
            LastPlaybackPositionSeconds = source.LastPlaybackPositionSeconds > 0
                ? source.LastPlaybackPositionSeconds : backup.LastPlaybackPositionSeconds
        };
    }

    private static LibraryVideo CreateOnlinePlaybackCopy(LibraryVideo source) => new()
    {
        VideoId = source.VideoId, Title = source.Title, TrackName = source.TrackName, Artist = source.Artist,
        Composer = source.Composer, Url = source.Url, IsOffline = false, LocalPath = null, Format = null,
        FullLyrics = source.FullLyrics, Cues = source.Cues.Select(CloneLyricCue).ToList(),
        Sections = CloneLyricsSections(source.Sections), ExecutionOrder = CloneLyricsExecutionOrder(source.ExecutionOrder),
        TimelineAdjustments = CloneLyricsTimelineAdjustments(source.TimelineAdjustments), SavedAt = source.SavedAt,
        LastPlaybackPositionSeconds = source.LastPlaybackPositionSeconds
    };

    private async void MediaConnectivityTimer_Tick(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _connectivityProbeInFlight, 1) != 0) return;
        try
        {
            var checkOnline = _youtubePlaybackIntended && !_isLocalAudioPlaying && !_pendingOnlineResume;
            var checkReturn = _wasAutomaticallySwitchedToOffline && _isLocalAudioPlaying;
            if (!checkOnline && !checkReturn) { _consecutiveNetworkFailures = 0; return; }
            if (checkOnline && DateTime.UtcNow < _retryOnlineAfterUtc) return;

            // A sonda converte timeout/HTTP exceptions em false; perda de rede conta como falha,
            // em vez de cair silenciosamente no catch externo.
            var online = await IsYoutubeReachableAsync();
            if (checkOnline)
            {
                if (online)
                {
                    _consecutiveNetworkFailures = 0;
                    return;
                }

                _consecutiveNetworkFailures++;
                AppLog.Info($"Sonda do YouTube falhou ({_consecutiveNetworkFailures}/2) durante reprodução em {TimeSpan.FromSeconds(GetPlaybackSeconds()):mm\\:ss}.");
                if (_consecutiveNetworkFailures >= 2)
                {
                    _consecutiveNetworkFailures = 0;
                    await SwitchToOfflineAtCurrentPositionAsync();
                }
            }
            else if (checkReturn && online && DateTime.UtcNow >= _retryOnlineAfterUtc)
            {
                await BeginOnlineResumeAsync();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Falha interna no monitor de conexão da reprodução", ex);
        }
        finally { Volatile.Write(ref _connectivityProbeInFlight, 0); }
    }

    private async Task<bool> IsYoutubeReachableAsync()
    {
        try
        {
            var reachable = await CanReachYoutubeAsync();
            _lastYoutubeProbeSucceeded = reachable;
            _lastYoutubeProbeUtc = DateTime.UtcNow;
            return reachable;
        }
        catch (Exception ex)
        {
            _lastYoutubeProbeSucceeded = false;
            _lastYoutubeProbeUtc = DateTime.UtcNow;
            AppLog.Info("Sonda do YouTube terminou com erro de rede: " + ex.Message);
            return false;
        }
    }

    private async Task SwitchToOfflineAtCurrentPositionAsync()
    {
        if (!_youtubePlaybackIntended || _isLocalAudioPlaying || _currentVideo is null) return;

        var position = Math.Max(0, GetPlaybackSeconds());
        PersistCurrentPlaybackPosition(position, force: true);
        var match = FindOfflineBackupMatchForCurrentVideo();
        if (match is null)
        {
            _retryOnlineAfterUtc = DateTime.UtcNow.AddSeconds(8);
            var title = string.IsNullOrWhiteSpace(_currentVideo.TrackName) ? _currentVideo.Title : _currentVideo.TrackName;
            var validFiles = _library.SelectMany(folder => folder.Videos).Count(item =>
                item.IsOffline && !string.IsNullOrWhiteSpace(item.LocalPath) && File.Exists(item.LocalPath) &&
                (!item.OfflineExpiresAt.HasValue || item.OfflineExpiresAt.Value > DateTime.Now));
            AppLog.Error($"Failover não iniciado: sem cópia offline válida/única para '{title}' — {_currentVideo.Artist}; posição={TimeSpan.FromSeconds(position):mm\\:ss}; arquivos locais válidos={validFiles}.");
            StatusText.Text = $"A internet caiu, mas não encontrei uma cópia offline válida e identificável para '{title}'. Confira se o título e o intérprete do arquivo na Biblioteca Offline correspondem à faixa atual.";
            return;
        }

        var backup = match.Video;
        BindOfflineBackupToCurrentRecording(backup, _currentVideo);
        var localVersion = CreateOfflinePlaybackCopy(_currentVideo, backup);
        _wasAutomaticallySwitchedToOffline = true;
        try
        {
            if (YoutubePlayer.CoreWebView2 is not null)
                await YoutubePlayer.CoreWebView2.ExecuteScriptAsync("if(typeof player !== 'undefined' && player && typeof player.pauseVideo === 'function') player.pauseVideo();");
        }
        catch (Exception ex) { AppLog.Info("Player online já estava sem resposta ao pausar para failover: " + ex.Message); }

        AppLog.Info($"Failover YouTube -> offline: correspondência={(match.MatchedByVideoId ? "ID" : "título/intérprete único")}; faixa={localVersion.TrackName} — {localVersion.Artist}; ponto={TimeSpan.FromSeconds(position):mm\\:ss}; frases={localVersion.Cues.Count}; arquivo={localVersion.LocalPath}.");
        PlayLocalAudio(localVersion, position, preserveProjection: true);
        StatusText.Text = $"Conexão interrompida. Retomando música e letra em {TimeSpan.FromSeconds(position):mm\\:ss}.";
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
