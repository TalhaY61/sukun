using Sukun.Core;
using System.Threading;
using System.Windows;

namespace Sukun.Windows;

public partial class App : System.Windows.Application
{
    private SingleInstance? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        L.Apply(null);
        base.OnStartup(e);
        if (e.Args.Contains("--smoke"))
        {
            DispatcherUnhandledException += (_, error) =>
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Sukun-smoke-error.txt"), error.Exception.ToString());
                error.Handled = true;
                Shutdown(1);
            };
        }
        instance = new SingleInstance();
        if (!instance.IsPrimary)
        {
            instance.RequestActivation();
            Shutdown();
            return;
        }
        MainWindow window;
        try { window = new MainWindow(e.Args.Contains("--smoke")); }
        catch (Exception error)
        {
            if (e.Args.Contains("--smoke")) System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Sukun-smoke-error.txt"), error.ToString());
            else System.Windows.MessageBox.Show(L.Get("StartupFailed"), "Sükûn");
            Shutdown(1);
            return;
        }
        MainWindow = window;
        instance.Listen(() =>
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => window.ShowApp());
        });
        if (!e.Args.Contains("--background")) window.Show();
        window.Start();
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}
