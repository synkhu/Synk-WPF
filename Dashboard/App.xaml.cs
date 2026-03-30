using System;
using System.Windows;
using Dashboard.Services;
using Dashboard.ViewModels;
using Dashboard.Views;

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

                var loginVm = new LoginViewModel(new AuthService());
                var loginWindow = new LoginWindow(loginVm);

                bool? loginResult = loginWindow.ShowDialog();

                if (loginResult != true)
                {
                    Shutdown();
                    return;
                }

                var mainVm = new MainViewModel(new UserService());

                var mainWindow = new MainWindow(mainVm);

                MainWindow = mainWindow;
                mainVm.OnLogoutRequested = () =>
                {
                    mainWindow.Close();
                };

                mainWindow.ShowDialog();

                if (AuthSession.Token == null)
                    continue;

                break;
            }
        }
    }
}