using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BarodaPOS.Services;

namespace BarodaPOS.Views
{
    public partial class SettingsHardwarePage : Page
    {
        private bool _isSettingsLoading = false;

        public SettingsHardwarePage()
        {
            InitializeComponent();
            Loaded += async (s,e) => await LoadPrinterSettings();
        }

        private async Task LoadPrinterSettings()
        {
            _isSettingsLoading = true;
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            PrinterComboBox.ItemsSource = printers;

            try
            {
                var firestore = new FirestoreService();
                var settings = await firestore.GetSettingsAsync();
                
                if (settings.ContainsKey("defaultPrinter") && settings["defaultPrinter"] != null)
                {
                    string savedPrinter = settings["defaultPrinter"].ToString();
                    if (printers.Contains(savedPrinter))
                        PrinterComboBox.SelectedItem = savedPrinter;
                }
                
                if (settings.ContainsKey("bottomMargin") && settings["bottomMargin"] != null)
                {
                    BottomMarginSlider.Value = Convert.ToDouble(settings["bottomMargin"]);
                }

                if (settings.ContainsKey("autoPrint") && settings["autoPrint"] != null)
                {
                    AutoPrintToggle.IsChecked = Convert.ToBoolean(settings["autoPrint"]);
                }
            }
            catch { }
            finally { _isSettingsLoading = false; }
        }

        private async void SavePrinter_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem == null)
            {
                ShowPrinterMessage("Please select a printer.", Colors.Red);
                return;
            }

            ShowPrinterMessage("Saving...", Colors.DarkOrange);
            try
            {
                var firestore = new FirestoreService();
                var settings = await firestore.GetSettingsAsync();
                settings["defaultPrinter"] = PrinterComboBox.SelectedItem.ToString();
                settings["bottomMargin"] = BottomMarginSlider.Value;
                settings["autoPrint"] = AutoPrintToggle.IsChecked ?? true;
                
                await firestore.UpdateSettingsAsync(settings);
                ShowPrinterMessage("Printer saved successfully!", Colors.Green);
            }
            catch (Exception ex)
            {
                ShowPrinterMessage("Error: " + ex.Message, Colors.Red);
            }
        }

        private async void AutoPrintToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isSettingsLoading) return;
            try
            {
                var firestore = new FirestoreService();
                var settings = await firestore.GetSettingsAsync();
                settings["autoPrint"] = AutoPrintToggle.IsChecked ?? true;
                await firestore.UpdateSettingsAsync(settings);
            }
            catch { }
        }

        private void TestPrint_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem == null)
            {
                ShowPrinterMessage("Please select a printer to test.", Colors.Red);
                return;
            }

            ShowPrinterMessage("Sending test print...", Colors.DarkOrange);
            try
            {
                var pd = new PrintDocument();
                pd.PrintController = new StandardPrintController();
                pd.PrinterSettings.PrinterName = PrinterComboBox.SelectedItem.ToString();
                pd.PrintPage += (s, ev) =>
                {
                    var font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
                    ev.Graphics.DrawString("BarodaPOS Test Print", font, System.Drawing.Brushes.Black, 10, 10);
                    ev.Graphics.DrawString("Printer is working correctly!", new System.Drawing.Font("Arial", 10), System.Drawing.Brushes.Black, 10, 40);
                };
                pd.Print();
                ShowPrinterMessage("Test print sent to queue.", Colors.Green);
            }
            catch (Exception ex)
            {
                ShowPrinterMessage("Test print failed: " + ex.Message, Colors.Red);
            }
        }

        private async void RemovePrinter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var firestore = new FirestoreService();
                var settings = await firestore.GetSettingsAsync();
                settings["defaultPrinter"] = null;
                await firestore.UpdateSettingsAsync(settings);
                PrinterComboBox.SelectedItem = null;
                ShowPrinterMessage("Printer removed.", Colors.Green);
            }
            catch (Exception ex)
            {
                ShowPrinterMessage("Error removing printer: " + ex.Message, Colors.Red);
            }
        }

        private void ShowPrinterMessage(string message, Color color)
        {
            PrinterStatusText.Text = message;
            PrinterStatusText.Foreground = new SolidColorBrush(color);
            PrinterStatusText.Visibility = Visibility.Visible;
            Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() => PrinterStatusText.Visibility = Visibility.Collapsed));
        }
    }
}
