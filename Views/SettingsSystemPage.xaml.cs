using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace BarodaPOS.Views
{
    public partial class SettingsSystemPage : Page
    {
        public SettingsSystemPage()
        {
            InitializeComponent();
            Loaded += (s,e) => UpdateSystemStats();
        }

        private void UpdateSystemStats()
        {
            OsVersionText.Text = RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")";
            DotNetVersionText.Text = RuntimeInformation.FrameworkDescription;

            using (var process = Process.GetCurrentProcess())
            {
                long memoryBytes = process.PrivateMemorySize64;
                double memoryMb = memoryBytes / 1024.0 / 1024.0;
                MemoryUsageText.Text = memoryMb.ToString("0.00") + " MB";
            }
        }

        private void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            UpdateSystemStats();
        }

        private void ActivateWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tempScriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WinActivate.bat");
                string scriptContent = @"
sc config LicenseManager start= auto
net start LicenseManager
sc config wuauserv start= auto
net start wuauserv
changepk.exe /productkey VK7JG-NPHTM-C97JM-9MPGT-3V66T
slmgr /ipk W269N-WFGWX-YVC9B-4J6C9-T83GX
slmgr /skms kms8.msguides.com
slmgr /ato
exit
";
                System.IO.File.WriteAllText(tempScriptPath, scriptContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempScriptPath,
                    UseShellExecute = true,
                    Verb = "runas" // Prompt for Admin
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching Windows Activation: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateOffice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tempScriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OfficeActivate.bat");
                string scriptContent = @"@echo off
echo Starting Automatic Office Activation (Ohook)...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ""$script = irm https://get.activated.win; $block = [ScriptBlock]::Create($script); & $block /ohook""
";
                System.IO.File.WriteAllText(tempScriptPath, scriptContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempScriptPath,
                    UseShellExecute = true,
                    Verb = "runas" // Prompt for Admin
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching Office Activation: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
