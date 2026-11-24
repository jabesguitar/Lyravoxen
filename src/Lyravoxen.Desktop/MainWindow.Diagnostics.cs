using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using Lyravoxen.Services;

namespace Lyravoxen;

// Botões 📋 Relatório e 🩺 Diagnóstico.
public partial class MainWindow
{
    private void Report_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(AppLog.BuildReport());
            StatusText.Text = "Relatório copiado. Cole (Ctrl+V) na conversa.";
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
            text.AppendLine("Modelo de voz: " + (File.Exists(model) && new FileInfo(model).Length > 10_000_000
                ? "já baixado" : "ainda não baixado (baixa na 1ª voz, ~150 MB)"));
            text.AppendLine("Monitores: " + System.Windows.Forms.Screen.AllScreens.Length);

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                using var response = await http.GetAsync("https://lrclib.net/api/search?q=teste");
                text.AppendLine("Catálogo de letras (internet): " + (response.IsSuccessStatusCode ? "OK" : "HTTP " + (int)response.StatusCode));
            }
            catch (Exception ex) { text.AppendLine("Catálogo de letras (internet): falhou — " + ex.Message); }
        }
        catch (Exception ex) { text.AppendLine("Erro no diagnóstico: " + ex.Message); }

        var result = text.ToString();
        AppLog.Info(result);
        try { System.Windows.Clipboard.SetText(result); } catch { }
        StatusText.Text = "Diagnóstico concluído (copiado para a área de transferência).";
        System.Windows.MessageBox.Show(this, result + "\n(Copiado: cole na conversa.)", "Lyravoxen — Diagnóstico",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
