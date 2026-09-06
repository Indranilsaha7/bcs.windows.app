using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BarodaPOS.Models;
using BarodaPOS.Services;
using System.Windows.Data;
using System.ComponentModel;

namespace BarodaPOS.ViewModels
{
    public partial class AddSaleHistoryViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;
        private ICollectionView _salesView;

        public ObservableCollection<Sale> AllSales { get; } = new();

        [ObservableProperty]
        private double _totalSaleAmount;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Now.AddDays(-7);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Now;

        [ObservableProperty]
        private bool _isProcessing;

        public ICollectionView SalesView => _salesView;

        public AddSaleHistoryViewModel()
        {
            _firestoreService = new FirestoreService();
            _salesView = CollectionViewSource.GetDefaultView(AllSales);
            _salesView.Filter = FilterSales;
            
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                
                var startTs = Google.Cloud.Firestore.Timestamp.FromDateTime(StartDate.Date.ToUniversalTime());
                var endTs = Google.Cloud.Firestore.Timestamp.FromDateTime(EndDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime());

                var query = db.Collection("sales")
                    .WhereGreaterThanOrEqualTo("date", startTs)
                    .WhereLessThanOrEqualTo("date", endTs)
                    .OrderByDescending("date")
                    .Limit(300);

                var snapshot = await query.GetSnapshotAsync();
                
                AllSales.Clear();
                double total = 0;

                foreach (var doc in snapshot.Documents)
                {
                    var sale = doc.ConvertTo<Sale>();
                    sale.Id = doc.Id;
                    
                    if (sale.Source == "add_sale" || sale.Source == "manual")
                    {
                        AllSales.Add(sale);
                        total += sale.TotalAmount;
                    }
                }

                TotalSaleAmount = total;
                _salesView.Refresh();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to load sales: " + ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            _salesView.Refresh();
        }

        private bool FilterSales(object obj)
        {
            if (obj is Sale sale)
            {
                if (string.IsNullOrWhiteSpace(SearchText)) return true;
                return sale.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       (sale.Customer != null && sale.Customer.ContainsKey("name") && sale.Customer["name"].ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task DeleteSale(Sale sale)
        {
            if (sale == null) return;
            
            try
            {
                IsProcessing = true;
                var db = _firestoreService.GetDb();
                await db.Collection("sales").Document(sale.Id).DeleteAsync();
                AllSales.Remove(sale);
                TotalSaleAmount -= sale.TotalAmount;
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to delete sale: " + ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void ViewDetails(Sale sale)
        {
            if (sale == null) return;
            var detailsWindow = new Views.SaleDetailsWindow(sale);
            detailsWindow.Owner = App.Current.MainWindow;
            detailsWindow.ShowDialog();
        }

        [RelayCommand]
        private void EditSale(Sale sale)
        {
            if (sale == null) return;
            
            // Set the EditSaleId in global properties
            App.Current.Properties["EditSaleId"] = sale.Id;
            
            // Navigate to SalePage
            if (App.Current.MainWindow is Views.MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(Views.SalePage));
            }
        }
    }
}
