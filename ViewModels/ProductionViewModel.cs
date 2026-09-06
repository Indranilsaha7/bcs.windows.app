using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace BarodaPOS.ViewModels
{
    public partial class ProductionRawMaterialItem : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private double _cost;

        [ObservableProperty]
        private double _qtyPerUnit = 1.0;

        public double Subtotal => Cost * QtyPerUnit;

        partial void OnQtyPerUnitChanged(double value)
        {
            OnPropertyChanged(nameof(Subtotal));
            OnQuantityChanged?.Invoke();
        }

        public Action OnQuantityChanged { get; set; }
    }

    public partial class ProductionViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;
        private readonly ProductService _productService;
        private List<Product> _allProducts = new();

        public ProductionViewModel()
        {
            _firestoreService = new FirestoreService();
            _productService = new ProductService(_firestoreService, NetworkSyncManager.Instance);
            ProductionDate = DateTime.Now;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                _allProducts = products ?? new List<Product>();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Could not load products: " + ex.Message);
            }
        }

        [ObservableProperty]
        private string _finishedProductSearchText;

        [ObservableProperty]
        private ObservableCollection<Product> _filteredFinishedProducts = new();

        [ObservableProperty]
        private bool _isFinishedProductSearchResultsVisible;

        [ObservableProperty]
        private Product _selectedFinishedProduct;

        partial void OnFinishedProductSearchTextChanged(string value) => _ = SearchFinishedProductsAsync(value);

        private async Task SearchFinishedProductsAsync(string value)
        {
            if (SelectedFinishedProduct != null && value == SelectedFinishedProduct.Name) return;

            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredFinishedProducts = new ObservableCollection<Product>();
                IsFinishedProductSearchResultsVisible = false;
                return;
            }

            var currentSearch = FinishedProductSearchText;
            await Task.Delay(250);
            if (currentSearch != FinishedProductSearchText) return;

            var lowerValue = value.ToLowerInvariant();
            var matches = await Task.Run(() => _allProducts.Where(p =>
                (p.Name != null && p.Name.ToLowerInvariant().Contains(lowerValue)) ||
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(lowerValue))
            ).Take(10).ToList());

            FilteredFinishedProducts = new ObservableCollection<Product>(matches);
            IsFinishedProductSearchResultsVisible = FilteredFinishedProducts.Count > 0;
        }

        [RelayCommand]
        private void SelectFinishedProduct(Product product)
        {
            if (product != null)
            {
                SelectedFinishedProduct = product;
                FinishedProductSearchText = product.Name;
                IsFinishedProductSearchResultsVisible = false;
                CalculateCosts();
            }
        }

        [ObservableProperty]
        private string _rawMaterialSearchText;

        [ObservableProperty]
        private ObservableCollection<Product> _filteredRawMaterials = new();

        [ObservableProperty]
        private bool _isRawMaterialSearchResultsVisible;

        partial void OnRawMaterialSearchTextChanged(string value) => _ = SearchRawMaterialsAsync(value);

        private async Task SearchRawMaterialsAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredRawMaterials = new ObservableCollection<Product>();
                IsRawMaterialSearchResultsVisible = false;
                return;
            }

            var currentSearch = RawMaterialSearchText;
            await Task.Delay(250);
            if (currentSearch != RawMaterialSearchText) return;

            var lowerValue = value.ToLowerInvariant();
            var matches = await Task.Run(() => _allProducts.Where(p =>
                (p.Name != null && p.Name.ToLowerInvariant().Contains(lowerValue)) ||
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(lowerValue))
            ).Take(10).ToList());

            FilteredRawMaterials = new ObservableCollection<Product>(matches);
            IsRawMaterialSearchResultsVisible = FilteredRawMaterials.Count > 0;
        }

        [RelayCommand]
        private void AddRawMaterial(Product product)
        {
            if (product != null)
            {
                if (!RawMaterials.Any(m => m.Id == product.Id))
                {
                    double buyPrice = product.BuyPrice ?? 0;
                    if (product.PriceTiers != null && product.PriceTiers.ContainsKey("pcs"))
                    {
                        var t = product.PriceTiers["pcs"];
                        if (t != null && t.Buy.HasValue) buyPrice = t.Buy.Value;
                    }

                    var item = new ProductionRawMaterialItem
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Cost = buyPrice,
                        QtyPerUnit = 1.0,
                        OnQuantityChanged = CalculateCosts
                    };
                    RawMaterials.Add(item);
                    CalculateCosts();
                }
                RawMaterialSearchText = string.Empty;
                IsRawMaterialSearchResultsVisible = false;
            }
        }

        [RelayCommand]
        private void RemoveRawMaterial(ProductionRawMaterialItem item)
        {
            if (item != null)
            {
                RawMaterials.Remove(item);
                CalculateCosts();
            }
        }

        [ObservableProperty]
        private ObservableCollection<ProductionRawMaterialItem> _rawMaterials = new();

        [ObservableProperty]
        private double _produceQuantity = 1;

        partial void OnProduceQuantityChanged(double value) => CalculateCosts();

        [ObservableProperty]
        private string _batchNumber;

        [ObservableProperty]
        private DateTime _productionDate;

        [ObservableProperty]
        private string _note;

        [ObservableProperty]
        private double _unitCost;

        [ObservableProperty]
        private double _totalProductionCost;

        [ObservableProperty]
        private bool _isProcessing;

        private void CalculateCosts()
        {
            UnitCost = RawMaterials.Sum(m => m.Subtotal);
            TotalProductionCost = UnitCost * ProduceQuantity;
        }

        [RelayCommand]
        private async Task ConfirmProductionAsync()
        {
            if (SelectedFinishedProduct == null)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please select a finished product.");
                return;
            }
            if (RawMaterials.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please add at least one raw material.");
                return;
            }
            if (ProduceQuantity <= 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Production quantity must be greater than 0.");
                return;
            }

            bool confirm = await DialogHelper.ShowConfirmAsync("Confirm Production", 
                $"Are you sure you want to produce {ProduceQuantity} units of {SelectedFinishedProduct.Name}?\nTotal Cost: {TotalProductionCost:C}");
            if (!confirm) return;

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var batch = db.StartBatch();

                var materialsUsed = RawMaterials.Select(m => new ProductionRawMaterial
                {
                    Id = m.Id,
                    Name = m.Name,
                    Cost = m.Cost,
                    QtyPerUnit = m.QtyPerUnit,
                    Quantity = m.QtyPerUnit * ProduceQuantity
                }).ToList();

                // 1. Create Production Record
                var user = SessionManager.CurrentUser;
                var prodRef = db.Collection("productions").Document();
                var prodData = new ProductionRecord
                {
                    Id = prodRef.Id,
                    Date = FieldValue.ServerTimestamp,
                    FinishedProductId = SelectedFinishedProduct.Id,
                    FinishedProductName = SelectedFinishedProduct.Name,
                    QuantityProduced = ProduceQuantity,
                    BatchNumber = BatchNumber,
                    Note = Note,
                    UnitCost = UnitCost,
                    TotalCost = TotalProductionCost,
                    ProducedBy = new Dictionary<string, object> { { "id", user?.Id ?? "" }, { "name", user?.Name ?? "" } },
                    RawMaterials = materialsUsed
                };
                batch.Set(prodRef, prodData);

                // 2. Increase Finished Product Stock & Update Buy Price
                var finishedRef = db.Collection("products").Document(SelectedFinishedProduct.Id);
                var updates = new Dictionary<string, object>
                {
                    { "quantity", FieldValue.Increment(ProduceQuantity) },
                    { "buyPrice", UnitCost }
                };
                if (SelectedFinishedProduct.PriceTiers != null && SelectedFinishedProduct.PriceTiers.ContainsKey("pcs"))
                {
                    updates["priceTiers.pcs.buy"] = UnitCost;
                }
                batch.Update(finishedRef, updates);

                // 3. Decrease Raw Material Stock
                foreach (var m in materialsUsed)
                {
                    var matRef = db.Collection("products").Document(m.Id);
                    batch.Update(matRef, new Dictionary<string, object>
                    {
                        { "quantity", FieldValue.Increment(-m.Quantity) }
                    });
                }

                await batch.CommitAsync();

                // Clear Form
                SelectedFinishedProduct = null;
                FinishedProductSearchText = "";
                ProduceQuantity = 1;
                BatchNumber = "";
                Note = "";
                RawMaterials.Clear();
                CalculateCosts();

                await DialogHelper.ShowMessageAsync("Success", "Production recorded successfully!");
                await LoadDataAsync(); // Reload products to get latest stock
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to record production: " + ex.Message);
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
