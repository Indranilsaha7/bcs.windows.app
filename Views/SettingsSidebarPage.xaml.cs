using System.Windows.Controls;

namespace BarodaPOS.Views
{
    public partial class SettingsSidebarPage : Page
    {
        public SettingsSidebarPage()
        {
            InitializeComponent();
        }

        private void SaveSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Sidebar settings have been saved successfully.", "Settings Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
