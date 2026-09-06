using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace BarodaPOS.ViewModels
{
    public partial class FastPurchaseItem : ObservableObject
    {
        public string ProductId { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string LastSupplier { get; set; }
        
        [ObservableProperty]
        private int _quantity = 1;
        
        [ObservableProperty]
        private double _buyPrice;

        public double Total => Quantity * BuyPrice;
        
        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(Total));
        partial void OnBuyPriceChanged(double value) => OnPropertyChanged(nameof(Total));
    }

    public partial class FastPurchaseViewModel : ObservableObject
    {
        private readonly FirestoreDb _db;
        private List<Product> _allProducts = new();
        private List<Supplier> _allSuppliersList = new();

        [ObservableProperty]
        private ObservableCollection<FastPurchaseItem> _cart = new();

        [ObservableProperty]
        private ObservableCollection<Supplier> _suppliers = new();

        [ObservableProperty]
        private Supplier _selectedSupplier;

        [ObservableProperty]
        private string _scannedBarcode = "";

        // Manual search properties
        [ObservableProperty]
        private string _searchText = "";
        
        [ObservableProperty]
        private ObservableCollection<Product> _filteredProducts = new();
        
        [ObservableProperty]
        private bool _isProductSearchResultsVisible;

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredProducts.Clear();
                IsProductSearchResultsVisible = false;
                return;
            }

            var lowerQuery = value.ToLowerInvariant();
            var matches = _allProducts.Where(p => 
                (p.Name != null && p.Name.ToLowerInvariant().Contains(lowerQuery)) || 
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(lowerQuery)) ||
                (p.Brand != null && p.Brand.ToLowerInvariant().Contains(lowerQuery))
            ).Take(10).ToList();

            FilteredProducts = new ObservableCollection<Product>(matches);
            IsProductSearchResultsVisible = matches.Count > 0;
        }

        [RelayCommand]
        public void SelectManualProduct(Product product)
        {
            if (product != null)
            {
                SearchText = "";
                IsProductSearchResultsVisible = false;
                AddProductToCart(product);
            }
        }

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private double _grandTotal;

        // Inline Add Supplier
        [ObservableProperty]
        private bool _isAddingNewSupplier;

        [ObservableProperty]
        private string _newSupplierName;

        [ObservableProperty]
        private string _newSupplierPhone;
        
        [ObservableProperty]
        private string _newSupplierShop;

        public FastPurchaseViewModel()
        {
            var firestoreService = new FirestoreService();
            _db = firestoreService.GetDb();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var productsSnap = await Task.Run(async () => await _db.Collection("products").GetSnapshotAsync());
                _allProducts = await Task.Run(() => {
                    var list = productsSnap.Documents.Select(d => d.ConvertTo<Product>()).ToList();
                    for (int i = 0; i < list.Count; i++) list[i].Id = productsSnap.Documents[i].Id;
                    return list;
                });

                var suppliersSnap = await Task.Run(async () => await _db.Collection("suppliers").GetSnapshotAsync());
                _allSuppliersList = await Task.Run(() => {
                    var list = suppliersSnap.Documents.Select(d => d.ConvertTo<Supplier>()).ToList();
                    for (int i = 0; i < list.Count; i++) list[i].Id = suppliersSnap.Documents[i].Id;
                    return list;
                });
                
                Suppliers = new ObservableCollection<Supplier>(_allSuppliersList.OrderBy(s => s.ShopName ?? s.Name));
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnScannedBarcodeChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value.EndsWith("\n"))
            {
                ProcessBarcode(value.Trim());
                ScannedBarcode = "";
            }
        }

        [RelayCommand]
        public void ProcessBarcodeCommand()
        {
            if (!string.IsNullOrWhiteSpace(ScannedBarcode))
            {
                ProcessBarcode(ScannedBarcode.Trim());
                ScannedBarcode = "";
            }
        }

        private async void ProcessBarcode(string barcode)
        {
            if (barcode.StartsWith("PRE-"))
            {
                // This is a Pre-Purchase QR Code!
                await LoadPrePurchaseAsync(barcode);
                return;
            }

            var product = _allProducts.FirstOrDefault(p => string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
            if (product != null)
            {
                AddProductToCart(product);
            }
            else
            {
                _ = DialogHelper.ShowMessageAsync("Not Found", $"Product with barcode '{barcode}' not found.");
            }
        }

        private async Task LoadPrePurchaseAsync(string id)
        {
            IsLoading = true;
            try
            {
                var doc = await _db.Collection("pre_purchases").Document(id).GetSnapshotAsync();
                if (doc.Exists)
                {
                    var prePurchase = doc.ConvertTo<PrePurchase>();
                    
                    // Set the supplier automatically if it exists
                    if (!string.IsNullOrEmpty(prePurchase.SupplierId))
                    {
                        var sup = Suppliers.FirstOrDefault(s => s.Id == prePurchase.SupplierId);
                        if (sup != null)
                        {
                            SelectedSupplier = sup;
                        }
                    }

                    // Load all items into the cart
                    foreach (var item in prePurchase.Items)
                    {
                        var existing = Cart.FirstOrDefault(c => c.ProductId == item.ProductId);
                        if (existing != null)
                        {
                            existing.Quantity += item.Quantity;
                        }
                        else
                        {
                            var cartItem = new FastPurchaseItem
                            {
                                ProductId = item.ProductId,
                                Name = item.Name,
                                Barcode = _allProducts.FirstOrDefault(p => p.Id == item.ProductId)?.Barcode ?? "",
                                BuyPrice = item.Cost,
                                LastSupplier = prePurchase.SupplierName ?? "Unknown",
                                Quantity = item.Quantity
                            };
                            cartItem.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(FastPurchaseItem.Total)) UpdateTotals(); };
                            Cart.Add(cartItem);
                        }
                    }
                    UpdateTotals();
                    
                    await DialogHelper.ShowMessageAsync("Draft Loaded", $"Successfully loaded {prePurchase.Items.Count} items from Pre-Purchase Draft {id}.");
                }
                else
                {
                    await DialogHelper.ShowMessageAsync("Not Found", $"Pre-Purchase draft with ID '{id}' was not found.");
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to load pre-purchase: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void AddProductToCart(Product product)
        {
            var existing = Cart.FirstOrDefault(c => c.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                // Find last supplier asynchronously
                string lastSupplier = "Unknown";
                try
                {
                    var purchaseSnap = await _db.Collection("purchases")
                        .WhereArrayContains("items.productId", product.Id)
                        .OrderByDescending("date")
                        .Limit(1)
                        .GetSnapshotAsync();
                        
                    if (purchaseSnap.Documents.Count > 0)
                    {
                        var pur = purchaseSnap.Documents[0].ConvertTo<Purchase>();
                        lastSupplier = pur.Supplier ?? "Unknown";
                    }
                }
                catch { }

                var item = new FastPurchaseItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Barcode = product.Barcode,
                    BuyPrice = product.BuyPrice ?? 0,
                    LastSupplier = lastSupplier,
                    Quantity = 1
                };
                item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(FastPurchaseItem.Total)) UpdateTotals(); };
                Cart.Add(item);
            }
            UpdateTotals();
        }


        [RelayCommand]
        public void RemoveItem(FastPurchaseItem item)
        {
            if (item != null && Cart.Contains(item))
            {
                Cart.Remove(item);
                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            GrandTotal = Cart.Sum(c => c.Total);
        }

        [RelayCommand]
        public void ToggleAddNewSupplier()
        {
            IsAddingNewSupplier = !IsAddingNewSupplier;
            if (IsAddingNewSupplier)
            {
                NewSupplierName = "";
                NewSupplierPhone = "";
                NewSupplierShop = "";
            }
        }

        [RelayCommand]
        public async Task SaveNewSupplierAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName))
            {
                await DialogHelper.ShowMessageAsync("Validation", "Supplier Proprietor Name is required.");
                return;
            }

            IsLoading = true;
            try
            {
                var newSup = new Supplier
                {
                    Name = NewSupplierName,
                    Phone = NewSupplierPhone,
                    ShopName = NewSupplierShop
                };
                
                var docRef = await _db.Collection("suppliers").AddAsync(newSup);
                newSup.Id = docRef.Id;
                
                _allSuppliersList.Add(newSup);
                Suppliers = new ObservableCollection<Supplier>(_allSuppliersList.OrderBy(s => s.ShopName));
                SelectedSupplier = newSup;
                
                IsAddingNewSupplier = false;
                await DialogHelper.ShowMessageAsync("Success", "New supplier added & selected.");
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to add supplier: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CompleteFastPurchaseAsync()
        {
            if (SelectedSupplier == null)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please select a Supplier.");
                return;
            }
            if (Cart.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Cart is empty.");
                return;
            }

            IsLoading = true;
            try
            {
                var itemsList = new List<PurchaseItem>();
                foreach (var c in Cart)
                {
                    itemsList.Add(new PurchaseItem
                    {
                        ProductId = c.ProductId,
                        Name = c.Name,
                        Quantity = c.Quantity,
                        Cost = c.BuyPrice
                    });
                }

                var purchase = new Purchase
                {
                    Supplier = SelectedSupplier.ShopName ?? SelectedSupplier.Name,
                    Date = Timestamp.FromDateTime(DateTime.UtcNow),
                    Subtotal = GrandTotal,
                    TotalAmount = GrandTotal,
                    DueAmount = GrandTotal,
                    Items = itemsList
                };

                // Save Purchase
                await _db.Collection("purchases").AddAsync(purchase);

                // Update Stock
                foreach (var item in Cart)
                {
                    var prodRef = _db.Collection("products").Document(item.ProductId);
                    await _db.RunTransactionAsync(async tx =>
                    {
                        var snap = await tx.GetSnapshotAsync(prodRef);
                        if (snap.Exists)
                        {
                            var prod = snap.ConvertTo<Product>();
                            int newQty = (prod.Quantity ?? 0) + item.Quantity;
                            tx.Update(prodRef, "quantity", newQty);
                        }
                    });
                }

                await DialogHelper.ShowMessageAsync("Success", "Fast Purchase completed successfully! Stock updated.");
                Cart.Clear();
                UpdateTotals();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to complete purchase: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
