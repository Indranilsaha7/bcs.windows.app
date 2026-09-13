using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using BarodaPOS.Services;

namespace BarodaPOS.Views
{
    public partial class SettingsSidebarPage : Page
    {
        public SettingsSidebarPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadSidebarSettings();
        }

        private async Task LoadSidebarSettings()
        {
            try
            {
                var firestore = new FirestoreService();
                var settings = await firestore.GetSettingsAsync();

                if (settings.ContainsKey("sidebarPosition") && settings["sidebarPosition"] != null)
                {
                    string pos = settings["sidebarPosition"].ToString();
                    if (pos == "Top") SidebarPositionCombo.SelectedIndex = 1;
                    else if (pos == "Bottom") SidebarPositionCombo.SelectedIndex = 2;
                    else SidebarPositionCombo.SelectedIndex = 0;
                }

                if (settings.ContainsKey("sidebarOpenOnHover") && settings["sidebarOpenOnHover"] != null)
                    OpenOnHoverToggle.IsChecked = Convert.ToBoolean(settings["sidebarOpenOnHover"]);

                if (settings.ContainsKey("sidebarAutoClose") && settings["sidebarAutoClose"] != null)
                    AutoCloseToggle.IsChecked = Convert.ToBoolean(settings["sidebarAutoClose"]);

                if (settings.ContainsKey("sidebarAutoOpen") && settings["sidebarAutoOpen"] != null)
                    AutoOpenToggle.IsChecked = Convert.ToBoolean(settings["sidebarAutoOpen"]);
            }
            catch { }
        }

        private async void SaveSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var firestore = new FirestoreService();
                var settings = await firestore.GetSettingsAsync();

                string position = "Left";
                if (SidebarPositionCombo.SelectedIndex == 1) position = "Top";
                else if (SidebarPositionCombo.SelectedIndex == 2) position = "Bottom";

                settings["sidebarPosition"] = position;
                settings["sidebarOpenOnHover"] = OpenOnHoverToggle.IsChecked ?? true;
                settings["sidebarAutoClose"] = AutoCloseToggle.IsChecked ?? false;
                settings["sidebarAutoOpen"] = AutoOpenToggle.IsChecked ?? true;

                await firestore.UpdateSettingsAsync(settings);

                // Try to apply immediately to the main window
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.ApplySidebarSettings(
                        position, 
                        OpenOnHoverToggle.IsChecked ?? true, 
                        AutoCloseToggle.IsChecked ?? false, 
                        AutoOpenToggle.IsChecked ?? true);
                }

                System.Windows.MessageBox.Show("Sidebar settings have been saved successfully.", "Settings Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error saving settings: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
