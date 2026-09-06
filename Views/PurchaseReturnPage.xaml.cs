using System.Windows.Controls;
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views
{
    public partial class PurchaseReturnPage : Page
    {
        public PurchaseReturnPage()
        {
            InitializeComponent();
            DataContext = new PurchaseReturnViewModel();
        }
    }
}
