using System;
using System.Windows;

namespace Dashboard
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnStartup(e);

            StartLoginFlow();
        }

        private void StartLoginFlow()
        {
            while (true)
            {
                AuthSession.Clear();

                var loginWindow = new Views.LoginWindow();
                bool? loginResult = loginWindow.ShowDialog();

                if (loginResult != true)
                {
                    Shutdown();
                    return;
                }

                var mainWindow = new Views.MainWindow();
                MainWindow = mainWindow;

                if (mainWindow.DataContext is ViewModels.MainViewModel vm)
                {
                    vm.OnLogoutRequested = () =>
                    {
                        mainWindow.Close();
                    };
                }

                mainWindow.ShowDialog();

                if (AuthSession.Token == null)
                    continue;

                break;
            }
        }
    }
}