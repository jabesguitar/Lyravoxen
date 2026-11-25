using System.IO;
using System.Windows;
using System.Windows.Media;
using Lyravoxen.Services;

namespace Lyravoxen;

public partial class App : System.Windows.Application
{
    private static readonly string ThemeFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Lyravoxen", "theme-mode.txt");

    private static readonly IReadOnlyDictionary<string, string> LightColors = new Dictionary<string, string>
    {
        ["F2F6F8"] = "#17324D",
        ["174A78"] = "#DCEAF5",
        ["0D2A50"] = "#C7DDED",
        ["267DA8"] = "#75A6C5",
        ["00E5FF"] = "#008DA8",
        ["00D9F5"] = "#008DA8",
        ["087F9E"] = "#087F9E",
        ["6CF2FF"] = "#007E99",
        ["071A3B"] = "#FFFFFF",
        ["245B88"] = "#A8C3D5",
        ["0B2450"] = "#EAF2F8",
        ["123A66"] = "#DCEAF5",
        ["FFFFFF"] = "#17324D",
        ["6B9DBA"] = "#7896AB",
        ["DCEBFA"] = "#24445C",
        ["102D52"] = "#D7E6F0",
        ["C7DDF2"] = "#35536B",
        ["0B5478"] = "#D7EAF4",
        ["38CDEB"] = "#007E99",
        ["EAF3FF"] = "#24445C",
        ["F5F7FF"] = "#17324D",
        ["315E9D"] = "#DCEAF5",
        ["28346F"] = "#D0E1EF",
        ["6489D5"] = "#79A6C4",
        ["F5C75E"] = "#A66B08",
        ["477FC2"] = "#C8DEEF",
        ["39458D"] = "#D4E3F0",
        ["A8C7FF"] = "#5F8BAA",
        ["4C8DFF"] = "#2F77B5",
        ["13C8E4"] = "#008DA8",
        ["1764C7"] = "#2F77B5",
        ["493EAA"] = "#7566B0",
        ["8C3B3B"] = "#B54A4A",
        ["B8CAD5"] = "#607C90",
        ["07172C"] = "#F4F8FC",
        ["020A18"] = "#FFFFFF",
        ["00AFCB"] = "#008DA8",
        ["0B2D56"] = "#E3EFF7",
        ["00BFD8"] = "#008DA8"
    };

    public static bool IsLightTheme { get; private set; }

    public static void SetTheme(bool light, bool persist = true)
    {
        IsLightTheme = light;
        var resources = Current.Resources;
        foreach (var pair in LightColors)
        {
            var selected = light ? pair.Value : "#" + pair.Key;
            var color = (Color)ColorConverter.ConvertFromString(selected);
            resources["Color_" + pair.Key] = color;
            resources["Brush_" + pair.Key] = new SolidColorBrush(color);
        }
        resources["Bg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#F4F8FC" : "#06152F"));
        resources["Panel"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#E5EFF7" : "#0B2450"));
        resources["Accent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#007F9B" : "#00D9F5"));
        resources["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#536D83" : "#B5C8E7"));
        if (persist)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemeFile)!);
                File.WriteAllText(ThemeFile, light ? "light" : "dark");
            }
            catch (Exception ex) { AppLog.Error("Não foi possível salvar o tema visual", ex); }
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppLog.Purge();
        AppLog.Info("Lyravoxen iniciado");
        var light = false;
        try { light = File.Exists(ThemeFile) && File.ReadAllText(ThemeFile).Trim().Equals("light", StringComparison.OrdinalIgnoreCase); } catch { }
        SetTheme(light, persist: false);

        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Error("Erro inesperado", args.Exception);
            args.Handled = true;
            System.Windows.MessageBox.Show("Ocorreu um erro inesperado, mas o programa continua aberto.\n\n" +
                args.Exception.Message + "\n\nUse o botão 📋 Relatório e envie o texto para análise.",
                "Lyravoxen", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => AppLog.Error("Erro fatal", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Error("Erro em tarefa em segundo plano", args.Exception);
            args.SetObserved();
        };
        base.OnStartup(e);
    }
}
