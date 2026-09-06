using System.Windows.Controls;

namespace BarodaPOS.Views
{
    public partial class SupplierPage : Page
    {
        public SupplierPage()
        {
            InitializeComponent();
        }
        
        private void CloseModal_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is BarodaPOS.ViewModels.SupplierViewModel vm)
            {
                vm.IsModalOpen = false;
            }
        }
    }
}