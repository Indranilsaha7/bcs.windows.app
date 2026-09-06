using System.Windows.Controls;
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage(System.Action onLoginSuccess)
        {
            InitializeComponent();
            
            this.Loaded += (s, e) =>
            {
                if (DataContext != null) return;
                var firestoreService = new Services.FirestoreService();
                var authService = new Services.AuthService(firestoreService);

                var vm = new LoginViewModel(authService);
                vm.OnLoginSuccessful = onLoginSuccess;
                vm.OnLoginResult = ShowNotification;
                DataContext = vm;

                // Check for Fast PIN
                try
                {
                    var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                    var fastLoginPath = System.IO.Path.Combine(appData, "BarodaPOS", "fast_login_settings.json");
                    if (System.IO.File.Exists(fastLoginPath))
                    {
                        var fastJson = System.IO.File.ReadAllText(fastLoginPath);
                        var fastSettings = System.Text.Json.JsonSerializer.Deserialize<Models.LoginSettingsModel>(fastJson);
                        if (fastSettings != null && fastSettings.Mode == Models.FastLoginMode.FastPIN)
                        {
                            _expectedFastPin = fastSettings.FastPIN;
                            NormalLoginContainer.Visibility = System.Windows.Visibility.Collapsed;
                            FastPinContainer.Visibility = System.Windows.Visibility.Visible;
                            QuickPinBox.Focus();
                        }
                    }
                }
                catch { }

                // Seed default admin user in a background thread to prevent UI stutter on low config PCs
                System.Threading.Tasks.Task.Run(() => authService.SeedAdminUserAsync());
            };
        }

        private string _expectedFastPin;


        private void ShowNotification(bool success, string message)
        {
            NotificationText.Text = message;
            if (success)
            {
                NotificationIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Checkmark24;
                NotificationIcon.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10b981"));
                NotificationPopup.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ecfdf5"));
                NotificationText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#065f46"));
            }
            else
            {
                NotificationIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Dismiss24;
                NotificationIcon.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ef4444"));
                NotificationPopup.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fef2f2"));
                NotificationText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#991b1b"));
            }

            NotificationPopup.Visibility = System.Windows.Visibility.Visible;
            NotificationPopup.Opacity = 0;

            var sb = new System.Windows.Media.Animation.Storyboard();
            var fade = new System.Windows.Media.Animation.DoubleAnimation(1, System.TimeSpan.FromMilliseconds(300));
            System.Windows.Media.Animation.Storyboard.SetTarget(fade, NotificationPopup);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(fade, new System.Windows.PropertyPath("Opacity"));

            var translate = new System.Windows.Media.Animation.DoubleAnimation(0, System.TimeSpan.FromMilliseconds(300));
            translate.EasingFunction = new System.Windows.Media.Animation.BackEase { Amplitude = 0.5, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
            System.Windows.Media.Animation.Storyboard.SetTarget(translate, NotificationPopup.RenderTransform);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(translate, new System.Windows.PropertyPath("Y"));

            sb.Children.Add(fade);
            sb.Children.Add(translate);
            sb.Begin();

            if (!success)
            {
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(t => 
                {
                    Dispatcher.Invoke(() => 
                    {
                        var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(0, System.TimeSpan.FromMilliseconds(300));
                        var sbOut = new System.Windows.Media.Animation.Storyboard();
                        System.Windows.Media.Animation.Storyboard.SetTarget(fadeOut, NotificationPopup);
                        System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeOut, new System.Windows.PropertyPath("Opacity"));
                        
                        sbOut.Completed += (s, e) => { NotificationPopup.Visibility = System.Windows.Visibility.Hidden; };
                        sbOut.Children.Add(fadeOut);
                        sbOut.Begin();
                    });
                });
            }
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        }

        private void CodeBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Code = CodeBox.Password;
            }
        }

        private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
                {
                    vm.LoginCommand.Execute(null);
                }
            }
        }
        private void QuickPinBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (QuickPinBox.Password == _expectedFastPin && !string.IsNullOrEmpty(_expectedFastPin))
            {
                if (DataContext is LoginViewModel vm)
                {
                    // PIN is correct, execute successful login action immediately
                    ShowNotification(true, "Fast PIN Verified. Redirecting...");
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(t => 
                    {
                        Dispatcher.Invoke(() => vm.OnLoginSuccessful?.Invoke());
                    });
                }
            }
        }

        private void SwitchUser_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FastPinContainer.Visibility = System.Windows.Visibility.Collapsed;
            NormalLoginContainer.Visibility = System.Windows.Visibility.Visible;
            QuickPinBox.Password = string.Empty;
        }
    }
}
