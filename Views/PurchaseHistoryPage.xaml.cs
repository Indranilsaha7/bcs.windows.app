using System.Windows.Controls;
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views
{
    public partial class PurchaseHistoryPage : Page
    {
        public PurchaseHistoryPage()
        {
            InitializeComponent();
            DataContext = new PurchaseHistoryViewModel();
        }
    }
}
