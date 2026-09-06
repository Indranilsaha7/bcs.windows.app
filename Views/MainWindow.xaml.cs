using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Wpf.Ui.Controls;

namespace BarodaPOS.Views
{
    public partial class MainWindow : FluentWindow
    {
        private readonly string _settingsFilePath;

        public MainWindow()
        {
            InitializeComponent();
            
            RootNavigation.SetServiceProvider(new BarodaPOS.Services.SimpleServiceProvider());
            
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "BarodaPOS");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _settingsFilePath = Path.Combine(appFolder, "window_state.json");

            LoadWindowState();
            
            this.Closing += MainWindow_Closing;
            
            if (CheckAutoLogin())
            {
                this.Loaded += (s, e) => 
                {
                    LoginFrame.Visibility = Visibility.Collapsed;
                    AppTitleBarHeader.Visibility = Visibility.Visible;
                    RootNavigation.Navigate(typeof(DashboardPage));
                };
            }
            else
            {
                // Navigate to Login Page
                LoginFrame.Navigate(new LoginPage(OnLoginSuccess));
                
                // Hide sidebar header buttons during login, but KEEP the TitleBar so the window is draggable
                AppTitleBarHeader.Visibility = Visibility.Collapsed;
            }
            
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        }

        [System.Runtime.InteropServices.DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Fallback Windows API for Titlebar dark mode if WPF-UI misses it
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int[] dark = new int[] { Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark ? 1 : 0 };
            DwmSetWindowAttribute(hwnd, 20, dark, 4);
        }

        private void OnLoginSuccess()
        {
            SaveLoginState(true);

            // Hide the login frame to reveal the main window
            LoginFrame.Visibility = Visibility.Collapsed;
            // Restore TitleBar header
            AppTitleBarHeader.Visibility = Visibility.Visible;
            
            // Auto-navigate to DashboardPage
            RootNavigation.Navigate(typeof(DashboardPage));
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveLoginState(false);

            // Log the user out and show the login screen again
            LoginFrame.Navigate(new LoginPage(OnLoginSuccess));
            LoginFrame.Visibility = Visibility.Visible;
            
            // Hide the topbar header
            AppTitleBarHeader.Visibility = Visibility.Collapsed;
        }

        private void TaskBtn_Click(object sender, RoutedEventArgs e)
        {
            RootNavigation.Navigate(typeof(TaskPage));
        }

        private void CalendarBtn_Click(object sender, RoutedEventArgs e)
        {
            RootNavigation.Navigate(typeof(CalendarPage));
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshCurrentPage();
        }

        private bool _isFullScreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private ResizeMode _previousResizeMode;
        private bool _previousExtendsContentIntoTitleBar;

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F5)
            {
                RefreshCurrentPage();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape && _isFullScreen)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _previousWindowState = this.WindowState;
                _previousWindowStyle = this.WindowStyle;
                _previousResizeMode = this.ResizeMode;
                _previousExtendsContentIntoTitleBar = this.ExtendsContentIntoTitleBar;

                this.ExtendsContentIntoTitleBar = false;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;

                // If currently maximized, switching briefly to Normal ensures DWM recalculates window boundaries to cover the taskbar
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.WindowState = WindowState.Maximized;

                TopBarGrid.Visibility = Visibility.Collapsed;
                _isFullScreen = true;
            }
            else
            {
                this.ExtendsContentIntoTitleBar = _previousExtendsContentIntoTitleBar;
                this.WindowStyle = _previousWindowStyle;
                this.ResizeMode = _previousResizeMode;
                this.WindowState = _previousWindowState;

                TopBarGrid.Visibility = Visibility.Visible;
                _isFullScreen = false;
            }
        }

        private void RefreshCurrentPage()
        {
            // Try to find the active page in the visual tree and call LoadDataAsync on its DataContext
            System.Windows.Controls.Page activePage = null;
            FindActivePage(RootNavigation, ref activePage);

            if (activePage != null && activePage.DataContext != null)
            {
                var method = activePage.DataContext.GetType().GetMethod("LoadDataAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    // Refresh icon animation could be added here if needed
                    method.Invoke(activePage.DataContext, null);
                }
            }
        }

        private void FindActivePage(DependencyObject obj, ref System.Windows.Controls.Page activePage)
        {
            if (obj == null || activePage != null) return;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child is System.Windows.Controls.Page page && page.IsVisible)
                {
                    activePage = page;
                    return;
                }
                FindActivePage(child, ref activePage);
            }
        }

        private void LoadWindowState()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var state = JsonSerializer.Deserialize<WindowStateModel>(json);
                    
                    if (state != null)
                    {
                        this.Width = state.Width;
                        this.Height = state.Height;
                        this.Left = state.Left;
                        this.Top = state.Top;
                        this.WindowState = state.IsMaximized ? WindowState.Maximized : WindowState.Normal;
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore load errors
                Console.WriteLine(ex.Message);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var state = new WindowStateModel
                {
                    Width = this.RestoreBounds.Width,
                    Height = this.RestoreBounds.Height,
                    Left = this.RestoreBounds.Left,
                    Top = this.RestoreBounds.Top,
                    IsMaximized = _isFullScreen ? (_previousWindowState == WindowState.Maximized) : (this.WindowState == WindowState.Maximized)
                };

                var json = JsonSerializer.Serialize(state);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Ignore save errors
                Console.WriteLine(ex.Message);
            }
        }

        private void SaveLoginState(bool isLoggedIn)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "BarodaPOS");
                var path = Path.Combine(appFolder, "login_state.json");
                
                var state = new LoginStateModel
                {
                    IsLoggedIn = isLoggedIn,
                    LoginTime = DateTime.Now
                };
                File.WriteAllText(path, JsonSerializer.Serialize(state));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving login state: " + ex.Message);
            }
        }

        private bool CheckAutoLogin()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "BarodaPOS");
                var path = Path.Combine(appFolder, "login_state.json");
                
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var state = JsonSerializer.Deserialize<LoginStateModel>(json);
                    
                    if (state != null && state.IsLoggedIn)
                    {
                        // Check Fast Login Mode
                        var fastLoginPath = Path.Combine(appFolder, "fast_login_settings.json");
                        if (File.Exists(fastLoginPath))
                        {
                            try
                            {
                                var fastJson = File.ReadAllText(fastLoginPath);
                                var fastSettings = JsonSerializer.Deserialize<Models.LoginSettingsModel>(fastJson);
                                if (fastSettings != null && fastSettings.Mode == Models.FastLoginMode.Forever)
                                {
                                    return true; // Bypass 1-hour timeout
                                }
                            }
                            catch { }
                        }

                        if ((DateTime.Now - state.LoginTime).TotalHours <= 1)
                        {
                            // Optionally refresh the login time to extend session by another hour
                            SaveLoginState(true);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking login state: " + ex.Message);
            }
            return false;
        }
    }

    public class WindowStateModel
    {
        public double Width { get; set; } = 1280;
        public double Height { get; set; } = 720;
        public double Left { get; set; }
        public double Top { get; set; }
        public bool IsMaximized { get; set; }
    }

    public class LoginStateModel
    {
        public bool IsLoggedIn { get; set; }
        public DateTime LoginTime { get; set; }
    }
}
