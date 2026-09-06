using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarodaPOS.Views
{
    public partial class SettingsMaintenancePage : Page
    {
        public SettingsMaintenancePage()
        {
            InitializeComponent();
        }

        private void CleanRam_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage("Cleaning RAM...", Colors.DarkOrange);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            try
            {
                var process = Process.GetCurrentProcess();
                process.MinWorkingSet = (IntPtr)(300000);
            }
            catch { }
            
            ShowStatusMessage("RAM Optimized Successfully!", Colors.Green);
        }

        private async void CheckDb_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage("Pinging Google Cloud...", Colors.DarkOrange);
            
            try
            {
                Ping myPing = new Ping();
                String host = "firestore.googleapis.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = await myPing.SendPingAsync(host, timeout, buffer, pingOptions);
                if (reply.Status == IPStatus.Success)
                {
                    ShowStatusMessage($"Connected! Latency: {reply.RoundtripTime} ms", Colors.Green);
                }
                else
                {
                    ShowStatusMessage("Ping Failed: " + reply.Status.ToString(), Colors.Red);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Connection Error: " + ex.Message, Colors.Red);
            }
        }

        private async void ExportDb_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage("Exporting database...", Colors.DarkOrange);
            
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"BarodaPOS_Backup_{DateTime.Now:yyyyMMdd}.json",
                    DefaultExt = ".json",
                    Filter = "JSON files (*.json)|*.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var backupService = new BarodaPOS.Services.DatabaseBackupService(new BarodaPOS.Services.FirestoreService());
                    bool success = await backupService.ExportDatabaseToJsonAsync(dialog.FileName);
                    
                    if (success)
                    {
                        ShowStatusMessage("Export Successful!", Colors.Green);
                    }
                    else
                    {
                        ShowStatusMessage("Export Failed (Are you offline?)", Colors.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Export Error: " + ex.Message, Colors.Red);
            }
        }

        private void ShowStatusMessage(string message, Color color)
        {
            StatusMessageText.Text = message;
            StatusMessageText.Foreground = new SolidColorBrush(color);
            StatusMessageText.Visibility = Visibility.Visible;
            
            Task.Delay(3000).ContinueWith(_ => 
            {
                Dispatcher.Invoke(() => StatusMessageText.Visibility = Visibility.Collapsed);
            });
        }
    }
}
