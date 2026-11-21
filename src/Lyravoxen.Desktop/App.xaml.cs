using System.Windows;
using Lyravoxen.Services;

namespace Lyravoxen;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppLog.Purge();
        AppLog.Info("Lyravoxen iniciado");
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
