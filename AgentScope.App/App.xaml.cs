using System.Windows;

namespace AgentScope.App;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance enforcement
        _mutex = new Mutex(true, "AgentScope.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("AgentScope is already running.", "AgentScope",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
