using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BarodaPOS.Models;
using BarodaPOS.Services;

namespace BarodaPOS.Views
{
    public partial class SettingsLoginPage : Page
    {
        private readonly string _settingsFilePath;
        private LoginSettingsModel _currentSettings;

        public SettingsLoginPage()
        {
            InitializeComponent();
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "BarodaPOS");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "fast_login_settings.json");

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    _currentSettings = JsonSerializer.Deserialize<LoginSettingsModel>(json);
                }
            }
            catch { }

            if (_currentSettings == null)
            {
                _currentSettings = new LoginSettingsModel();
            }

            // Bind to UI without triggering events
            FastLoginToggle.Checked -= FastLoginToggle_Checked;
            FastLoginToggle.Unchecked -= FastLoginToggle_Unchecked;

            if (_currentSettings.Mode != FastLoginMode.None)
            {
                FastLoginToggle.IsChecked = true;
                OptionsContainer.Visibility = Visibility.Visible;
                AuthContainer.Visibility = Visibility.Collapsed;
                ConfigContainer.Visibility = Visibility.Visible;
                
                if (_currentSettings.Mode == FastLoginMode.Forever)
                {
                    RadioForever.IsChecked = true;
                }
                else if (_currentSettings.Mode == FastLoginMode.FastPIN)
                {
                    RadioFastPIN.IsChecked = true;
                    PinConfigContainer.Visibility = Visibility.Visible;
                    FastPinBox.Password = _currentSettings.FastPIN;
                }
            }
            else
            {
                FastLoginToggle.IsChecked = false;
                OptionsContainer.Visibility = Visibility.Collapsed;
            }

            FastLoginToggle.Checked += FastLoginToggle_Checked;
            FastLoginToggle.Unchecked += FastLoginToggle_Unchecked;
        }

        private void FastLoginToggle_Checked(object sender, RoutedEventArgs e)
        {
            OptionsContainer.Visibility = Visibility.Visible;
            AuthContainer.Visibility = Visibility.Visible;
            ConfigContainer.Visibility = Visibility.Collapsed;
        }

        private void FastLoginToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            OptionsContainer.Visibility = Visibility.Collapsed;
            _currentSettings.Mode = FastLoginMode.None;
            SaveSettings();
        }

        private void FastLoginCard_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the switch manually
            FastLoginToggle.IsChecked = !FastLoginToggle.IsChecked;
        }

        private async void VerifyBtn_Click(object sender, RoutedEventArgs e)
        {
            AuthErrorText.Visibility = Visibility.Collapsed;
            string username = AuthUsername.Text;
            string password = AuthPassword.Password;
            string code = AuthCode.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(code))
            {
                AuthErrorText.Text = "Please fill all fields.";
                AuthErrorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var firestoreService = new FirestoreService();
                var authService = new AuthService(firestoreService);
                bool success = await authService.LoginAsync(username, password, code);

                if (success)
                {
                    _currentSettings.SavedUsername = username;
                    AuthContainer.Visibility = Visibility.Collapsed;
                    ConfigContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    AuthErrorText.Text = "Invalid credentials.";
                    AuthErrorText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                AuthErrorText.Text = "Error verifying: " + ex.Message;
                AuthErrorText.Visibility = Visibility.Visible;
            }
        }

        private void RadioMode_Checked(object sender, RoutedEventArgs e)
        {
            if (PinConfigContainer == null) return;

            if (RadioFastPIN.IsChecked == true)
            {
                PinConfigContainer.Visibility = Visibility.Visible;
            }
            else
            {
                PinConfigContainer.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RadioForever.IsChecked == true)
            {
                _currentSettings.Mode = FastLoginMode.Forever;
            }
            else if (RadioFastPIN.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(FastPinBox.Password))
                {
                    Wpf.Ui.Controls.MessageBox msg = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Error",
                        Content = "Please enter a Fast PIN to save."
                    };
                    await msg.ShowDialogAsync();
                    return;
                }
                _currentSettings.Mode = FastLoginMode.FastPIN;
                _currentSettings.FastPIN = FastPinBox.Password; // Usually we would hash this!
            }

            SaveSettings();
            
            Wpf.Ui.Controls.MessageBox successMsg = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Success",
                Content = "Fast Login configuration saved locally."
            };
            await successMsg.ShowDialogAsync();
        }

        private void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_currentSettings);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not save settings: " + ex.Message);
            }
        }
    }
}
