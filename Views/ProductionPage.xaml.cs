using System.Windows.Controls;
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views
{
    public partial class ProductionPage : Page
    {
        public ProductionPage()
        {
            InitializeComponent();
            DataContext = new ProductionViewModel();
        }
    }
}
