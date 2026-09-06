using System.Windows.Controls; 
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views 
{ 
    public partial class DashboardPage : Page 
    { 
        public DashboardPage() 
        { 
            InitializeComponent(); 
            DataContext = new DashboardViewModel();
        } 
    } 
}