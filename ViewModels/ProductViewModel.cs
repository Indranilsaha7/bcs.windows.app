using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;

namespace BarodaPOS.ViewModels
{
    public partial class ProductViewModel : ObservableObject
    {
        private readonly ProductService _productService;

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private ObservableCollection<Product> _filteredProducts = new();

        [ObservableProperty]
        private ObservableCollection<ProductRow> _filteredProductRows = new();

        [ObservableProperty]
        private bool _canLoadMore;

        private int _currentRenderedCount = 0;
        private const int PageSize = 48;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasNoProducts;

        [ObservableProperty]
        private Product _selectedProduct;

        [ObservableProperty]
        private bool _isDetailsOpen;

        [ObservableProperty]
        private int _totalProducts;

        [ObservableProperty]
        private int _inStock;

        [ObservableProperty]
        private int _outOfStock;

        [ObservableProperty]
        private int _lowStock;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string _sortBy = "name-asc";

        [ObservableProperty]
        private string _selectedCategory = "";

        [ObservableProperty]
        private string _selectedBrand = "";

        [ObservableProperty]
        private string _selectedStockStatus = "all";

        [ObservableProperty]
        private ObservableCollection<string> _availableCategories = new();

        [ObservableProperty]
        private ObservableCollection<string> _availableBrands = new();

        private System.Collections.Generic.IEnumerable<Product> _currentQuery;

        partial void OnSearchTextChanged(string value) => _ = FilterProductsAsync();
        partial void OnSortByChanged(string value) => _ = FilterProductsAsync();
        partial void OnSelectedCategoryChanged(string value) => _ = FilterProductsAsync();
        partial void OnSelectedBrandChanged(string value) => _ = FilterProductsAsync();
        partial void OnSelectedStockStatusChanged(string value) => _ = FilterProductsAsync();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = "";
            SortBy = "name-asc";
            SelectedCategory = "";
            SelectedBrand = "";
            SelectedStockStatus = "all";
            // FilterProducts is called automatically by the property changed events
        }

        private async Task FilterProductsAsync()
        {
            if (Products == null) return;
            
            var currentSearch = SearchText;
            await Task.Delay(250); // Debounce
            if (currentSearch != SearchText) return;
            
            var lowerSearch = SearchText?.ToLowerInvariant() ?? "";
            
            var query = Products.AsEnumerable();
            
            // Text Search
            if (!string.IsNullOrWhiteSpace(lowerSearch))
            {
                query = query.Where(p => 
                    (p.Name != null && p.Name.ToLowerInvariant().Contains(lowerSearch)) ||
                    (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(lowerSearch)) ||
                    (p.Category != null && p.Category.ToLowerInvariant().Contains(lowerSearch)) ||
                    (p.Brand != null && p.Brand.ToLowerInvariant().Contains(lowerSearch)));
            }

            // Category Filter
            if (!string.IsNullOrEmpty(SelectedCategory))
            {
                query = query.Where(p => p.Category == SelectedCategory);
            }

            // Brand Filter
            if (!string.IsNullOrEmpty(SelectedBrand))
            {
                query = query.Where(p => p.Brand == SelectedBrand);
            }

            // Stock Status Filter
            if (SelectedStockStatus == "in-stock")
            {
                query = query.Where(p => p.Quantity.GetValueOrDefault() > 0);
            }
            else if (SelectedStockStatus == "out-of-stock")
            {
                query = query.Where(p => p.Quantity.GetValueOrDefault() <= 0);
            }
            else if (SelectedStockStatus == "alert")
            {
                query = query.Where(p => p.Quantity.GetValueOrDefault() > 0 && p.Quantity.GetValueOrDefault() <= p.AlertQuantity.GetValueOrDefault());
            }

            // Sorting
            query = SortBy switch
            {
                "name-asc" => query.OrderBy(p => p.Name),
                "name-desc" => query.OrderByDescending(p => p.Name),
                "price-asc" => query.OrderBy(p => p.SellPrice),
                "price-desc" => query.OrderByDescending(p => p.SellPrice),
                "qty-asc" => query.OrderBy(p => p.Quantity.GetValueOrDefault()),
                "qty-desc" => query.OrderByDescending(p => p.Quantity.GetValueOrDefault()),
                _ => query.OrderBy(p => p.Name)
            };
            
            var list = await Task.Run(() => query.ToList());
            
            _currentQuery = list;
            _currentRenderedCount = 0;
            FilteredProducts = new ObservableCollection<Product>();
            FilteredProductRows = new ObservableCollection<ProductRow>();
            HasNoProducts = list.Count == 0 && !IsLoading;

            LoadMore();
        }

        [RelayCommand]
        public void LoadMore()
        {
            var list = _currentQuery as System.Collections.Generic.List<Product>;
            if (list == null) return;
            
            int nextCount = Math.Min(_currentRenderedCount + PageSize, list.Count);
            
            var newRows = new System.Collections.Generic.List<ProductRow>();
            for (int i = _currentRenderedCount; i < nextCount; i += 4)
            {
                FilteredProducts.Add(list[i]);
                if (i + 1 < nextCount) FilteredProducts.Add(list[i+1]);
                if (i + 2 < nextCount) FilteredProducts.Add(list[i+2]);
                if (i + 3 < nextCount) FilteredProducts.Add(list[i+3]);

                newRows.Add(new ProductRow
                {
                    Item1 = list[i],
                    Item2 = i + 1 < nextCount ? list[i + 1] : null,
                    Item3 = i + 2 < nextCount ? list[i + 2] : null,
                    Item4 = i + 3 < nextCount ? list[i + 3] : null
                });
            }
            
            foreach (var row in newRows)
            {
                FilteredProductRows.Add(row);
            }
            
            _currentRenderedCount = nextCount;
            CanLoadMore = _currentRenderedCount < list.Count;
        }

        public ProductViewModel()
        {
            var firestoreService = new FirestoreService();
            _productService = new ProductService(firestoreService, NetworkSyncManager.Instance);
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            HasNoProducts = false;

            try
            {
                var productsList = await _productService.GetAllProductsAsync();
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Products = new ObservableCollection<Product>(productsList);
                    
                    TotalProducts = Products.Count;
                    InStock = Products.Count(p => p.Quantity.GetValueOrDefault() > 0);
                    OutOfStock = Products.Count(p => p.Quantity.GetValueOrDefault() <= 0);
                    LowStock = Products.Count(p => p.Quantity.GetValueOrDefault() > 0 && p.Quantity.GetValueOrDefault() <= p.AlertQuantity.GetValueOrDefault());
                    
                    var categories = Products.Where(p => !string.IsNullOrWhiteSpace(p.Category)).Select(p => p.Category).Distinct().OrderBy(c => c);
                    AvailableCategories = new ObservableCollection<string>(categories);

                    var brands = Products.Where(p => !string.IsNullOrWhiteSpace(p.Brand)).Select(p => p.Brand).Distinct().OrderBy(b => b);
                    AvailableBrands = new ObservableCollection<string>(brands);

                    _ = FilterProductsAsync();
                });
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Data Load Error", $"Error loading products: {ex.Message}\n\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                HasNoProducts = FilteredProducts?.Count == 0;
            }
        }
        [RelayCommand]
        private void ShowDetails(Product product)
        {
            if (product != null)
            {
                SelectedProduct = product;
                IsDetailsOpen = true;
            }
        }

        [RelayCommand]
        private void CloseDetails()
        {
            IsDetailsOpen = false;
            SelectedProduct = null;
        }

        [RelayCommand]
        private async Task DeleteProductAsync(Product product)
        {
            if (product == null) return;
            
            bool isConfirmed = await DialogHelper.ShowConfirmAsync("Confirm Delete", $"Are you sure you want to delete {product.Name}?");
            
            if (isConfirmed)
            {
                try
                {
                    await _productService.DeleteProductAsync(product.Id);
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Products.Remove(product);
                        TotalProducts = Products.Count;
                        InStock = Products.Count(p => p.Quantity.GetValueOrDefault() > 0);
                        OutOfStock = Products.Count(p => p.Quantity.GetValueOrDefault() <= 0);
                        LowStock = Products.Count(p => p.Quantity.GetValueOrDefault() > 0 && p.Quantity.GetValueOrDefault() <= p.AlertQuantity.GetValueOrDefault());
                        _ = FilterProductsAsync();
                    });
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowMessageAsync("Error", $"Could not delete product: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task SendToAdminsAsync()
        {
            if (SelectedProduct == null) return;

            var p = SelectedProduct;
            
            string html = $@"
<div style=""background-color: #f8fafc; padding: 40px 20px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;"">
<div style=""max-width: 800px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);"">
<div style=""background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); padding: 30px 20px; text-align: center;"">
<h2 style=""color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;"">Product Details</h2>
</div>
<div style=""padding: 30px; color: #334155; line-height: 1.6; font-size: 14px;"">
    <div style=""font-family: Arial, sans-serif; color: #334155;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 20px;"">
    <tr>
    <td width=""120"" style=""vertical-align: top; padding-right: 20px;"">
    <img src=""{(string.IsNullOrEmpty(p.ImageUrl) ? "https://barodasell.web.app/images/no_image_--product.png" : p.ImageUrl)}"" alt=""{p.Name}"" style=""max-width: 120px; border-radius: 8px; border: 1px solid #e2e8f0;"">
    </td>
    <td style=""vertical-align: top;"">
    <h2 style=""margin: 0 0 10px 0; color: #0f172a; font-size: 22px;"">{p.Name}</h2>
    <p style=""margin: 0 0 15px 0; color: #64748b; font-size: 14px; line-height: 1.5;"">{p.Description ?? "No description available."}</p>
    <table width=""100%"" cellpadding=""8"" cellspacing=""0"" style=""border: 1px solid #e2e8f0; border-radius: 6px; font-size: 13px; background-color: #ffffff;"">
    <tr><td style=""color: #64748b; border-bottom: 1px solid #e2e8f0; width: 40%;"">Category:</td><td style=""text-align: right; font-weight: bold; color: #0f172a; border-bottom: 1px solid #e2e8f0;"">{p.Category ?? "N/A"}</td></tr>
    <tr><td style=""color: #64748b; border-bottom: 1px solid #e2e8f0;"">Brand:</td><td style=""text-align: right; font-weight: bold; color: #0f172a; border-bottom: 1px solid #e2e8f0;"">{p.Brand ?? "N/A"}</td></tr>
    <tr><td style=""color: #64748b; border-bottom: 1px solid #e2e8f0;"">Barcode:</td><td style=""text-align: right; font-weight: bold; color: #0f172a; border-bottom: 1px solid #e2e8f0;"">{p.Barcode ?? "N/A"}</td></tr>
    <tr><td style=""color: #64748b; border-bottom: 1px solid #e2e8f0;"">Unit:</td><td style=""text-align: right; font-weight: bold; color: #0f172a; border-bottom: 1px solid #e2e8f0;"">{p.Unit ?? "N/A"}</td></tr>
    <tr><td style=""color: #64748b; border-bottom: 1px solid #e2e8f0;"">Current Stock:</td><td style=""text-align: right; font-weight: bold; color: #10b981; border-bottom: 1px solid #e2e8f0;"">{p.Quantity.GetValueOrDefault()}</td></tr>
    <tr><td style=""color: #64748b;"">Alert Quantity:</td><td style=""text-align: right; font-weight: bold; color: #ef4444;"">{p.AlertQuantity.GetValueOrDefault()}</td></tr>
    </table>
    </td>
    </tr>
    </table>
    </div>
</div>
<div style=""background-color: #f1f5f9; padding: 15px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b;"">
<p style=""margin: 0;"">Sent from Baroda Computer And Sales System (Desktop App)</p>
</div>
</div>
</div>";

            try
            {
                bool success = await new FirestoreService().TriggerAdminEmailAsync($"Product Details Information - {p.Name}", html);
                if (success)
                {
                    await DialogHelper.ShowMessageAsync("Success", "Product details sent to admins successfully!");
                }
                else
                {
                    await DialogHelper.ShowMessageAsync("Error", "Failed to send: No admin emails configured in settings.");
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Error sending to admins: {ex.Message}");
            }
        }

        [RelayCommand]
        private void EditProduct(Product product)
        {
            if (product == null) return;
            
            AddProductViewModel.ProductToEdit = product;
            
            var mainWindow = Application.Current.MainWindow as Views.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.RootNavigation.Navigate(typeof(Views.AddProductPage));
            }
        }
    }

    public class ProductRow
    {
        public Product Item1 { get; set; }
        public Product Item2 { get; set; }
        public Product Item3 { get; set; }
        public Product Item4 { get; set; }

        public Visibility Item1Visibility => Item1 != null ? Visibility.Visible : Visibility.Hidden;
        public Visibility Item2Visibility => Item2 != null ? Visibility.Visible : Visibility.Hidden;
        public Visibility Item3Visibility => Item3 != null ? Visibility.Visible : Visibility.Hidden;
        public Visibility Item4Visibility => Item4 != null ? Visibility.Visible : Visibility.Hidden;
    }
}




