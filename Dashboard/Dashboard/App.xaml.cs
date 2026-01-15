using System.Configuration;
using System.Data;
using System.Windows;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            base.OnStartup(e);
            StartLoginFlow();
        }

        public void StartLoginFlow()
        {
            var login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true)
            {
                try
                {
                    var main = new MainWindow();
                    this.MainWindow = main;
                    main.Show();
                }
                catch (Exception ex)
                {
                    Shutdown();
                }
            }
            else
            {
                Shutdown();
            }
        }
    }
}