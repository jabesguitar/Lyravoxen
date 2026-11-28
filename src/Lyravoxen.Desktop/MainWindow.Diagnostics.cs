using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Runtime.Intrinsics.X86;
using Lyravoxen.Models;
using Lyravoxen.Services;

namespace Lyravoxen;

// Botões 📋 Relatório e 🩺 Diagnóstico.
public partial class MainWindow
{
    private void Report_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var report = AppLog.BuildReport() + Environment.NewLine + BuildCurrentPlaybackReport();
            System.Windows.Clipboard.SetText(report);
            AppLog.Info("Relatório técnico copiado com estado da reprodução e disponibilidade da reserva offline.");
            StatusText.Text = "Relatório técnico copiado. Cole (Ctrl+V) na conversa.";
        }
        catch (Exception ex) { StatusText.Text = "Não foi possível copiar o relatório: " + ex.Message; }
    }

    private async void Diagnostics_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Executando diagnóstico…";
        var text = new StringBuilder("=== Diagnóstico Lyravoxen ===\n");
        try
        {
            text.AppendLine("Holyrics:");
            text.AppendLine(await _holyrics.DiagnoseAsync());

            try
            {
                var mics = MicrophoneReferenceService.ListDevices();
                text.AppendLine("Microfones: " + Math.Max(0, mics.Count - 1) + " dedicado(s) (+ padrão do Windows)");
            }
            catch (Exception ex) { text.AppendLine("Microfones: erro — " + ex.Message); }

            var model = LiveAudioTranscriptionService.ModelPath;
            var modelReady = File.Exists(model) && new FileInfo(model).Length > 10_000_000;
            text.AppendLine("Modelo de voz: " + (modelReady ? "já baixado" : "ainda não baixado (baixa na 1ª voz, ~150 MB)"));
            if (modelReady)
            {
                try
                {
                    using var whisper = WhisperDiagnostics.CreateFactory(model);
                    text.AppendLine("Motor Whisper nativo: OK (biblioteca carregada)");
                }
                catch (Exception ex)
                {
                    text.AppendLine("Motor Whisper nativo: FALHOU — " + ex.Message);
                }
            }
            else text.AppendLine("Motor Whisper nativo: não testado (modelo ainda não existe)");
            text.AppendLine("Monitores: " + System.Windows.Forms.Screen.AllScreens.Length);
            text.AppendLine("AVX2 disponível: " + (Avx2.IsSupported ? "sim" : "não — a compatibilidade do reconhecimento Whisper deve ser verificada separadamente"));

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                using var response = await http.GetAsync("https://lrclib.net/api/search?q=teste");
                text.AppendLine("Catálogo de letras LRCLIB: " + (response.IsSuccessStatusCode ? "OK" : "HTTP " + (int)response.StatusCode));
            }
            catch (Exception ex) { text.AppendLine("Catálogo de letras LRCLIB: falhou — " + ex.Message); }

            var youtubeOnline = await IsYoutubeReachableAsync();
            text.AppendLine("YouTube (sonda usada pelo failover): " + (youtubeOnline ? "OK" : "INDISPONÍVEL"));
            text.AppendLine(BuildCurrentPlaybackReport());
        }
        catch (Exception ex) { text.AppendLine("Erro no diagnóstico: " + ex.Message); }

        var result = text.ToString();
        AppLog.Info(result);
        try { System.Windows.Clipboard.SetText(result); } catch { }
        StatusText.Text = "Diagnóstico concluído (copiado para a área de transferência).";
        System.Windows.MessageBox.Show(this, result + "\n(Copiado: cole na conversa.)", "Lyravoxen — Diagnóstico",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string BuildCurrentPlaybackReport()
    {
        var text = new StringBuilder();
        text.AppendLine("=== Reprodução e failover ===");
        if (_currentVideo is null)
        {
            text.AppendLine("Faixa atual: nenhuma");
            text.AppendLine("Posição: —");
            text.AppendLine("Cópia offline correspondente: não avaliada (nenhuma faixa carregada)");
            return text.ToString();
        }

        var title = string.IsNullOrWhiteSpace(_currentVideo.TrackName) ? _currentVideo.Title : _currentVideo.TrackName;
        text.AppendLine($"Faixa atual: {title} — {_currentVideo.Artist}");
        text.AppendLine($"Fonte atual: {(_isLocalAudioPlaying ? "arquivo local" : "YouTube")} | player state={_lastPlayerState} | reprodução pretendida={_youtubePlaybackIntended}");
        text.AppendLine($"Posição atual: {TimeSpan.FromSeconds(Math.Max(0, GetPlaybackSeconds())):mm\\:ss}");
        text.AppendLine($"Frases temporizadas carregadas: {_cues.Count}");
        text.AppendLine($"Failover automático em uso: {(_wasAutomaticallySwitchedToOffline ? "sim" : "não")}");
        text.AppendLine($"Última sonda do YouTube: {(_lastYoutubeProbeSucceeded.HasValue ? (_lastYoutubeProbeSucceeded.Value ? "OK" : "falhou") : "não executada")}" +
            (_lastYoutubeProbeUtc == DateTime.MinValue ? "" : " às " + _lastYoutubeProbeUtc.ToLocalTime().ToString("HH:mm:ss")));

        var match = FindOfflineBackupMatchForCurrentVideo();
        if (match is null)
        {
            var validFiles = _library.SelectMany(folder => folder.Videos).Count(item =>
                item.IsOffline && !string.IsNullOrWhiteSpace(item.LocalPath) && File.Exists(item.LocalPath) &&
                (!item.OfflineExpiresAt.HasValue || item.OfflineExpiresAt.Value > DateTime.Now));
            text.AppendLine($"Cópia offline correspondente: NÃO encontrada (arquivos locais válidos: {validFiles})");
        }
        else
        {
            text.AppendLine($"Cópia offline correspondente: SIM ({(match.MatchedByVideoId ? "ID do YouTube" : "título/intérprete único")})");
            text.AppendLine($"Arquivo local: {match.Video.LocalPath}");
            text.AppendLine($"Validade: {(match.Video.OfflineExpiresAt.HasValue ? match.Video.OfflineExpiresAt.Value.ToString("dd/MM/yyyy HH:mm") : "sem expiração cadastrada")}");
            text.AppendLine($"Frases na cópia offline: {match.Video.Cues.Count} | último ponto salvo: {TimeSpan.FromSeconds(Math.Max(0, match.Video.LastPlaybackPositionSeconds)):mm\\:ss}");
        }

        return text.ToString();
    }
}
