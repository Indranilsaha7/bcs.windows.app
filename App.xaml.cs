using System.Configuration;
using System.Data;
using System.Windows;

namespace BarodaPOS;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("crash.log", e.Exception.ToString());
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("crash2.log", e.ExceptionObject.ToString());
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Wpf.Ui.Appearance.ApplicationThemeManager.ApplySystemTheme();
            BarodaPOS.Services.NetworkStateMonitor.Instance.StartMonitoring();
            BarodaPOS.Services.NetworkSyncManager.Instance.Start();
        }
    }

