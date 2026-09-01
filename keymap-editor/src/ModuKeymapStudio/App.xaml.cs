using System.Windows;

namespace ModuKeymapStudio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Contains("--smoke-test", StringComparer.Ordinal))
        {
            Shutdown(0);
            return;
        }
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "MODU Keymap Studio", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        base.OnStartup(e);
        new MainWindow().Show();
    }
}

