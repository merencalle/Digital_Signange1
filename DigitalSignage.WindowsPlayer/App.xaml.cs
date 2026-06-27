using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DigitalSignage.WindowsPlayer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "windowsplayer-crash.log"),
                args.Exception.ToString());
            args.Handled = true;
            Shutdown(1);
        };

        base.OnStartup(e);
    }
}
