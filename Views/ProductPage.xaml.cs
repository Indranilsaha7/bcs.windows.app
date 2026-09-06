using System.Windows.Controls;
using System.Windows.Input;
using BarodaPOS.ViewModels;

namespace BarodaPOS.Views
{
    public partial class ProductPage : Page
    {
        public ProductPage()
        {
            InitializeComponent();
            var viewModel = new ProductViewModel();
            DataContext = viewModel;
            this.Loaded += (s, e) => {
                _ = viewModel.LoadDataAsync();
            };
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scv && scv.ScrollableHeight > 0)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta * 0.8));
                e.Handled = true;
            }
        }

        private bool _isAutoLoading = false;

        private async void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scv)
            {
                // Trigger Load More when user scrolls within 200 pixels of the bottom
                if (!_isAutoLoading && scv.VerticalOffset >= scv.ScrollableHeight - 200 && scv.ScrollableHeight > 0)
                {
                    if (DataContext is ProductViewModel vm && vm.CanLoadMore)
                    {
                        _isAutoLoading = true;
                        vm.LoadMore();
                        await System.Threading.Tasks.Task.Delay(50); // Allow layout to update and scrollable height to adjust
                        _isAutoLoading = false;
                    }
                }
            }
        }
    }
}
