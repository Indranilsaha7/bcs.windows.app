using System;
using System.Windows.Controls;

namespace BarodaPOS.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            Loaded += (s, e) => SettingsNav.Navigate(typeof(SettingsSystemPage));
        }
    }
}
