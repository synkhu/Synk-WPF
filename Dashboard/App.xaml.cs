using System.Windows;

namespace Dashboard.Views;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Keep the app alive while we transition between the login window and the main window.
        // Without this, closing the login dialog would shut down the process before Main opens.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        base.OnStartup(e);
        StartLoginFlow();
    }

    internal void StartLoginFlow()
    {
        var login = new LoginWindow();

        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        try
        {
            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open the dashboard: {ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }
}
