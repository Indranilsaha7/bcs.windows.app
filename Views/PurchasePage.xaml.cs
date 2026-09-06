using System.Windows.Controls;
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views
{
    public partial class PurchasePage : Page
    {
        public PurchasePage()
        {
            InitializeComponent();
            DataContext = new PurchaseViewModel();
        }
    }
}
