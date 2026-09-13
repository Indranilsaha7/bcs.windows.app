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
            private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = System.Windows.UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((System.Windows.Controls.Control)sender).Parent as System.Windows.UIElement;
                if (parent != null)
                {
                    parent.RaiseEvent(eventArg);
                }
            }
        }
    }
}

