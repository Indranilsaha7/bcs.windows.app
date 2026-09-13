using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;

namespace BarodaPOS.ViewModels
{
    public partial class PurchaseViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;
        private List<Product> _allProducts = new();

        [ObservableProperty]
        private ObservableCollection<Product> _filteredProducts = new();

        [ObservableProperty]
        private Product _selectedProduct;

        partial void OnSelectedProductChanged(Product value)
        {
            if (value != null)
            {
                SearchText = value.Name;
                FilteredProducts.Clear();
                IsProductSearchResultsVisible = false;
                
                LoadUnitsForProduct(value);
                LoadPricesForProduct(value);
                UpdateLotNumberRequirement();
            }
        }

        [RelayCommand]
        private async Task SelectFirstProductAsync()
        {
            if (FilteredProducts != null && FilteredProducts.Count > 0)
            {
                var product = FilteredProducts.First();

                if (SelectedProduct == product)
                {
                    await AddToCartAsync();
                    return;
                }

                SelectedProduct = product;

                if (!string.IsNullOrWhiteSpace(product.Barcode) && 
                    !string.IsNullOrWhiteSpace(SearchText) && 
                    SearchText.Trim().Equals(product.Barcode, StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(100);
                    await AddToCartAsync();
                }
            }
        }

        [RelayCommand]
        private void SelectProduct(Product product)
        {
            if (product != null)
            {
                SelectedProduct = product;
            }
        }

        [ObservableProperty]
        private string _searchText;

        partial void OnSearchTextChanged(string value)
        {
            if (SelectedProduct != null && value == SelectedProduct.Name) return;

            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredProducts.Clear();
                IsProductSearchResultsVisible = false;
                return;
            }

            var lowerValue = value.ToLower();
            var matches = _allProducts.Where(p => 
                (p.Name?.ToLower().Contains(lowerValue) == true) ||
                (p.Barcode?.ToLower().Contains(lowerValue) == true) ||
                (p.Category?.ToLower().Contains(lowerValue) == true) ||
                (p.Brand?.ToLower().Contains(lowerValue) == true)
            ).Take(10).ToList();

            FilteredProducts = new ObservableCollection<Product>(matches);
            IsProductSearchResultsVisible = FilteredProducts.Count > 0;
        }

        [ObservableProperty]
        private bool _isProductSearchResultsVisible;

        [ObservableProperty]
        private ObservableCollection<string> _units = new();

        [ObservableProperty]
        private string _selectedUnit;

        [ObservableProperty]
        private int _quantity = 1;

        [ObservableProperty]
        private string _lotNumber;

        partial void OnLotNumberChanged(string value) => UpdateLotNumberRequirement();

        [ObservableProperty]
        private bool _isLotNumberRequired;

        [ObservableProperty]
        private string _lotNumberPlaceholder = "Any code (Optional)";

        // Visibility Toggles
        [ObservableProperty] private bool _isBoxPriceVisible;
        [ObservableProperty] private bool _isKgPriceVisible;
        [ObservableProperty] private bool _isLiterPriceVisible;
        [ObservableProperty] private bool _isPacketPriceVisible;
        [ObservableProperty] private bool _isStandardPriceVisible;

        // Box Fields
        [ObservableProperty] private double _buyPriceBox;
        [ObservableProperty] private double _wholesalePriceBoxNormal;
        [ObservableProperty] private double _wholesalePriceBoxShop;
        [ObservableProperty] private double _sellPriceBoxNormal;
        [ObservableProperty] private double _sellPriceBoxShop;
        
        [ObservableProperty] private double _buyPricePack;
        [ObservableProperty] private double _wholesalePricePackNormal;
        [ObservableProperty] private double _wholesalePricePackShop;
        
        [ObservableProperty] private double _buyPricePcs;
        [ObservableProperty] private double _sellPricePcsNormal;
        [ObservableProperty] private double _sellPricePcsShop;

        // Kg Fields
        [ObservableProperty] private double _buyPriceKg;
        [ObservableProperty] private double _wholesalePriceKgNormal;
        [ObservableProperty] private double _wholesalePriceKgShop;
        [ObservableProperty] private double _sellPriceKgNormal;
        [ObservableProperty] private double _sellPriceKgShop;

        [ObservableProperty] private double _buyPriceGram;
        [ObservableProperty] private double _wholesalePriceGramNormal;
        [ObservableProperty] private double _wholesalePriceGramShop;
        [ObservableProperty] private double _sellPriceGramNormal;
        [ObservableProperty] private double _sellPriceGramShop;

        // Liter Fields
        [ObservableProperty] private double _buyPriceLiter;
        [ObservableProperty] private double _wholesalePriceLiterNormal;
        [ObservableProperty] private double _wholesalePriceLiterShop;
        [ObservableProperty] private double _sellPriceLiterNormal;
        [ObservableProperty] private double _sellPriceLiterShop;

        [ObservableProperty] private double _buyPriceMl;
        [ObservableProperty] private double _wholesalePriceMlNormal;
        [ObservableProperty] private double _wholesalePriceMlShop;
        [ObservableProperty] private double _sellPriceMlNormal;
        [ObservableProperty] private double _sellPriceMlShop;

        // Packet Fields
        [ObservableProperty] private double _buyPricePacket;
        [ObservableProperty] private double _wholesalePricePacketNormal;
        [ObservableProperty] private double _wholesalePricePacketShop;
        [ObservableProperty] private double _sellPricePacketNormal;
        [ObservableProperty] private double _sellPricePacketShop;

        [ObservableProperty] private double _buyPricePacketPcs;
        [ObservableProperty] private double _wholesalePricePacketPcsNormal;
        [ObservableProperty] private double _wholesalePricePacketPcsShop;
        [ObservableProperty] private double _sellPricePacketPcsNormal;
        [ObservableProperty] private double _sellPricePacketPcsShop;

        // Standard/Pcs Fields (Fallback)
        [ObservableProperty] private double _buyPrice;
        [ObservableProperty] private double _wholesalePriceNormal;
        [ObservableProperty] private double _wholesalePriceShop;
        [ObservableProperty] private double _sellPriceNormal;
        [ObservableProperty] private double _sellPriceShop;
        
        // Listeners to trigger lot number requirement check on price change
        partial void OnBuyPriceBoxChanged(double value) => UpdateLotNumberRequirement();
        partial void OnBuyPriceKgChanged(double value) => UpdateLotNumberRequirement();
        partial void OnBuyPriceLiterChanged(double value) => UpdateLotNumberRequirement();
        partial void OnBuyPricePacketChanged(double value) => UpdateLotNumberRequirement();
        partial void OnBuyPriceChanged(double value) => UpdateLotNumberRequirement();

        [ObservableProperty]
        private ObservableCollection<PurchaseItem> _cartItems = new();

        [ObservableProperty]
        private ObservableCollection<Supplier> _suppliers = new();

        [ObservableProperty]
        private Supplier _selectedSupplier;

        [ObservableProperty]
        private DateTime _purchaseDate = DateTime.Now;

        [ObservableProperty] private double _subtotal;
        [ObservableProperty] private double _discount;
        [ObservableProperty] private double _tax;
        [ObservableProperty] private double _shipping;
        [ObservableProperty] private double _carryCharge;
        [ObservableProperty] private double _maintenance;
        [ObservableProperty] private double _totalAmount;
        [ObservableProperty] private double _paidAmount;
        [ObservableProperty] private double _dueAmount;

        partial void OnDiscountChanged(double value) => CalculateTotals();
        partial void OnTaxChanged(double value) => CalculateTotals();
        partial void OnShippingChanged(double value) => CalculateTotals();
        partial void OnCarryChargeChanged(double value) => CalculateTotals();
        partial void OnMaintenanceChanged(double value) => CalculateTotals();
        partial void OnPaidAmountChanged(double value) => CalculateTotals();

        [ObservableProperty]
        private bool _isProcessing;

        private Dictionary<string, PriceTier> _originalPriceTiers;

        public PurchaseViewModel()
        {
            _firestoreService = new FirestoreService();
            CartItems.CollectionChanged += CartItems_CollectionChanged;
            _ = LoadDataAsync();
        }

        private void CartItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PurchaseItem item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (PurchaseItem item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PurchaseItem.Quantity))
            {
                CalculateTotals();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var db = _firestoreService.GetDb();
                
                var productsSnap = await Task.Run(async () => await db.Collection("products").GetSnapshotAsync());
                _allProducts = await Task.Run(() => productsSnap.Documents.Select(d => 
                {
                    var p = d.ConvertTo<Product>();
                    p.Id = d.Id;
                    return p;
                }).ToList());

                var suppliersSnap = await Task.Run(async () => await db.Collection("suppliers").GetSnapshotAsync());
                var suppliers = await Task.Run(() => suppliersSnap.Documents.Select(d => 
                {
                    var s = d.ConvertTo<Supplier>();
                    s.Id = d.Id;
                    return s;
                }).ToList());

                Suppliers = new ObservableCollection<Supplier>(suppliers);
                
                // Smart option: if only 1 supplier exists, auto-select it.
                if (Suppliers.Count == 1)
                {
                    SelectedSupplier = Suppliers.First();
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to load products/suppliers: " + ex.Message);
            }
        }

        private void LoadUnitsForProduct(Product p)
        {
            Units.Clear();
            if (p == null) return;
            
            var tiers = p.PriceTiers ?? new Dictionary<string, PriceTier>();
            var mainUnit = p.Unit ?? "Pcs";

            if (mainUnit == "Box")
            {
                if (tiers.ContainsKey("box")) Units.Add("box");
                if (tiers.ContainsKey("pack")) Units.Add("pack");
                if (tiers.ContainsKey("pcs")) Units.Add("pcs");
            }
            else if (mainUnit == "Packet")
            {
                if (tiers.ContainsKey("pack")) Units.Add("pack");
                if (tiers.ContainsKey("pcs")) Units.Add("pcs");
            }
            else if (mainUnit == "Kg")
            {
                if (tiers.ContainsKey("kg")) Units.Add("kg");
                if (tiers.ContainsKey("gram")) Units.Add("gram");
            }
            else if (mainUnit == "Liter")
            {
                if (tiers.ContainsKey("liter")) Units.Add("liter");
                if (tiers.ContainsKey("ml")) Units.Add("ml");
            }
            else
            {
                Units.Add("pcs");
            }

            if (Units.Count == 0) Units.Add("pcs");
            SelectedUnit = Units.First();
        }

        private void LoadPricesForProduct(Product p)
        {
            if (p == null) return;

            IsBoxPriceVisible = p.Unit == "Box";
            IsKgPriceVisible = p.Unit == "Kg";
            IsLiterPriceVisible = p.Unit == "Liter";
            IsPacketPriceVisible = p.Unit == "Packet";
            IsStandardPriceVisible = p.Unit == "Pcs" || string.IsNullOrEmpty(p.Unit);

            var tiers = p.PriceTiers ?? new Dictionary<string, PriceTier>();
            
            // Create a deep copy for checking if edited
            _originalPriceTiers = new Dictionary<string, PriceTier>();
            foreach(var kv in tiers) {
                _originalPriceTiers[kv.Key] = new PriceTier { 
                    Buy = kv.Value.Buy, Wholesale = kv.Value.Wholesale, Sell = kv.Value.Sell, 
                    ShopWholesale = kv.Value.ShopWholesale, ShopSell = kv.Value.ShopSell 
                };
            }

            if (p.Unit == "Box")
            {
                if (tiers.TryGetValue("box", out var box)) { BuyPriceBox = box.Buy ?? 0; WholesalePriceBoxNormal = box.Wholesale ?? 0; WholesalePriceBoxShop = box.ShopWholesale ?? 0; SellPriceBoxNormal = box.Sell ?? 0; SellPriceBoxShop = box.ShopSell ?? 0; }
                if (tiers.TryGetValue("pack", out var pack)) { BuyPricePack = pack.Buy ?? 0; WholesalePricePackNormal = pack.Wholesale ?? 0; WholesalePricePackShop = pack.ShopWholesale ?? 0; }
                if (tiers.TryGetValue("pcs", out var pcs)) { BuyPricePcs = pcs.Buy ?? 0; SellPricePcsNormal = pcs.Sell ?? 0; SellPricePcsShop = pcs.ShopSell ?? 0; }
            }
            else if (p.Unit == "Kg")
            {
                if (tiers.TryGetValue("kg", out var kg)) { BuyPriceKg = kg.Buy ?? 0; WholesalePriceKgNormal = kg.Wholesale ?? 0; WholesalePriceKgShop = kg.ShopWholesale ?? 0; SellPriceKgNormal = kg.Sell ?? 0; SellPriceKgShop = kg.ShopSell ?? 0; }
                if (tiers.TryGetValue("gram", out var gram)) { BuyPriceGram = gram.Buy ?? 0; WholesalePriceGramNormal = gram.Wholesale ?? 0; WholesalePriceGramShop = gram.ShopWholesale ?? 0; SellPriceGramNormal = gram.Sell ?? 0; SellPriceGramShop = gram.ShopSell ?? 0; }
            }
            else if (p.Unit == "Liter")
            {
                if (tiers.TryGetValue("liter", out var liter)) { BuyPriceLiter = liter.Buy ?? 0; WholesalePriceLiterNormal = liter.Wholesale ?? 0; WholesalePriceLiterShop = liter.ShopWholesale ?? 0; SellPriceLiterNormal = liter.Sell ?? 0; SellPriceLiterShop = liter.ShopSell ?? 0; }
                if (tiers.TryGetValue("ml", out var ml)) { BuyPriceMl = ml.Buy ?? 0; WholesalePriceMlNormal = ml.Wholesale ?? 0; WholesalePriceMlShop = ml.ShopWholesale ?? 0; SellPriceMlNormal = ml.Sell ?? 0; SellPriceMlShop = ml.ShopSell ?? 0; }
            }
            else if (p.Unit == "Packet")
            {
                if (tiers.TryGetValue("pack", out var pack)) { BuyPricePacket = pack.Buy ?? 0; WholesalePricePacketNormal = pack.Wholesale ?? 0; WholesalePricePacketShop = pack.ShopWholesale ?? 0; SellPricePacketNormal = pack.Sell ?? 0; SellPricePacketShop = pack.ShopSell ?? 0; }
                if (tiers.TryGetValue("pcs", out var pcs)) { BuyPricePacketPcs = pcs.Buy ?? 0; WholesalePricePacketPcsNormal = pcs.Wholesale ?? 0; WholesalePricePacketPcsShop = pcs.ShopWholesale ?? 0; SellPricePacketPcsNormal = pcs.Sell ?? 0; SellPricePacketPcsShop = pcs.ShopSell ?? 0; }
            }
            else
            {
                if (tiers.TryGetValue("pcs", out var pcs)) { BuyPrice = pcs.Buy ?? 0; WholesalePriceNormal = pcs.Wholesale ?? 0; WholesalePriceShop = pcs.ShopWholesale ?? 0; SellPriceNormal = pcs.Sell ?? 0; SellPriceShop = pcs.ShopSell ?? 0; }
                else { BuyPrice = p.DisplayBuyPrice; WholesalePriceNormal = p.DisplayWholesalePrice; SellPriceNormal = p.DisplaySellPrice; }
            }
        }

        private bool CheckIfPricesEdited()
        {
            if (SelectedProduct == null || _originalPriceTiers == null) return false;
            
            bool isEdited = false;
            if (SelectedProduct.Unit == "Box")
            {
                var orig = _originalPriceTiers.ContainsKey("box") ? _originalPriceTiers["box"] : new PriceTier();
                if (orig.Buy != BuyPriceBox) isEdited = true;
            }
            else if (SelectedProduct.Unit == "Kg")
            {
                var orig = _originalPriceTiers.ContainsKey("kg") ? _originalPriceTiers["kg"] : new PriceTier();
                if (orig.Buy != BuyPriceKg) isEdited = true;
            }
            else if (SelectedProduct.Unit == "Liter")
            {
                var orig = _originalPriceTiers.ContainsKey("liter") ? _originalPriceTiers["liter"] : new PriceTier();
                if (orig.Buy != BuyPriceLiter) isEdited = true;
            }
            else if (SelectedProduct.Unit == "Packet")
            {
                var orig = _originalPriceTiers.ContainsKey("pack") ? _originalPriceTiers["pack"] : new PriceTier();
                if (orig.Buy != BuyPricePacket) isEdited = true;
            }
            else
            {
                var orig = _originalPriceTiers.ContainsKey("pcs") ? _originalPriceTiers["pcs"] : new PriceTier();
                if (orig.Buy != BuyPrice) isEdited = true;
            }

            return isEdited;
        }

        private void UpdateLotNumberRequirement()
        {
            if (SelectedProduct != null && (SelectedProduct.Quantity ?? 0) > 0)
            {
                IsLotNumberRequired = true;
                LotNumberPlaceholder = "Lot # (Required because in stock)";
            }
            else
            {
                IsLotNumberRequired = false;
                LotNumberPlaceholder = "Any code (Optional)";
            }
        }

        private Dictionary<string, PriceTier> CaptureNewPriceTiers()
        {
            var priceTiers = new Dictionary<string, PriceTier>();

            if (SelectedProduct.Unit == "Box")
            {
                priceTiers["box"] = new PriceTier { Buy = BuyPriceBox, Wholesale = WholesalePriceBoxNormal, ShopWholesale = WholesalePriceBoxShop, Sell = SellPriceBoxNormal, ShopSell = SellPriceBoxShop };
                priceTiers["pack"] = new PriceTier { Buy = BuyPricePack, Wholesale = WholesalePricePackNormal, ShopWholesale = WholesalePricePackShop };
                priceTiers["pcs"] = new PriceTier { Buy = BuyPricePcs, Sell = SellPricePcsNormal, ShopSell = SellPricePcsShop };
            }
            else if (SelectedProduct.Unit == "Kg")
            {
                priceTiers["kg"] = new PriceTier { Buy = BuyPriceKg, Wholesale = WholesalePriceKgNormal, ShopWholesale = WholesalePriceKgShop, Sell = SellPriceKgNormal, ShopSell = SellPriceKgShop };
                priceTiers["gram"] = new PriceTier { Buy = BuyPriceGram, Wholesale = WholesalePriceGramNormal, ShopWholesale = WholesalePriceGramShop, Sell = SellPriceGramNormal, ShopSell = SellPriceGramShop };
            }
            else if (SelectedProduct.Unit == "Liter")
            {
                priceTiers["liter"] = new PriceTier { Buy = BuyPriceLiter, Wholesale = WholesalePriceLiterNormal, ShopWholesale = WholesalePriceLiterShop, Sell = SellPriceLiterNormal, ShopSell = SellPriceLiterShop };
                priceTiers["ml"] = new PriceTier { Buy = BuyPriceMl, Wholesale = WholesalePriceMlNormal, ShopWholesale = WholesalePriceMlShop, Sell = SellPriceMlNormal, ShopSell = SellPriceMlShop };
            }
            else if (SelectedProduct.Unit == "Packet")
            {
                priceTiers["pack"] = new PriceTier { Buy = BuyPricePacket, Wholesale = WholesalePricePacketNormal, ShopWholesale = WholesalePricePacketShop, Sell = SellPricePacketNormal, ShopSell = SellPricePacketShop };
                priceTiers["pcs"] = new PriceTier { Buy = BuyPricePacketPcs, Wholesale = WholesalePricePacketPcsNormal, ShopWholesale = WholesalePricePacketPcsShop, Sell = SellPricePacketPcsNormal, ShopSell = SellPricePacketPcsShop };
            }
            else
            {
                priceTiers["pcs"] = new PriceTier { Buy = BuyPrice, Wholesale = WholesalePriceNormal, ShopWholesale = WholesalePriceShop, Sell = SellPriceNormal, ShopSell = SellPriceShop };
            }
            
            return priceTiers;
        }

        [RelayCommand]
        private async Task AddToCartAsync()
        {
            if (SelectedProduct == null)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please select a product.");
                return;
            }

            if (string.IsNullOrEmpty(SelectedUnit))
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please select a unit.");
                return;
            }

            if (IsLotNumberRequired && string.IsNullOrWhiteSpace(LotNumber))
            {
                await DialogHelper.ShowMessageAsync("Validation", "Lot # is required because the product is in stock.");
                return;
            }

            double cost = 0;
            if (SelectedProduct.Unit == "Box" && SelectedUnit == "box") cost = BuyPriceBox;
            else if (SelectedProduct.Unit == "Kg" && SelectedUnit == "kg") cost = BuyPriceKg;
            else if (SelectedProduct.Unit == "Liter" && SelectedUnit == "liter") cost = BuyPriceLiter;
            else if (SelectedProduct.Unit == "Packet" && SelectedUnit == "pack") cost = BuyPricePacket;
            else cost = BuyPrice;

            var existingItem = CartItems.FirstOrDefault(i => i.ProductId == SelectedProduct.Id && i.PurchaseUnit == SelectedUnit && i.Cost == cost && i.LotNumber == LotNumber);
            if (existingItem != null)
            {
                existingItem.Quantity += Quantity;
                // Force UI update
                var index = CartItems.IndexOf(existingItem);
                CartItems.RemoveAt(index);
                CartItems.Insert(index, existingItem);
            }
            else
            {
                var newTiers = CheckIfPricesEdited() ? CaptureNewPriceTiers() : null;
                CartItems.Add(new PurchaseItem
                {
                    ProductId = SelectedProduct.Id,
                    Name = SelectedProduct.Name,
                    Quantity = Quantity,
                    PurchaseUnit = SelectedUnit,
                    Cost = cost,
                    LotNumber = LotNumber?.Trim(),
                    NewPriceTiers = newTiers
                });
            }

            CalculateTotals();

            // Reset inputs
            Quantity = 1;
            LotNumber = string.Empty;
            SelectedProduct = null;
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void IncreaseQuantity(PurchaseItem item)
        {
            if (item != null)
            {
                item.Quantity++;
                CalculateTotals();
            }
        }

        [RelayCommand]
        private void DecreaseQuantity(PurchaseItem item)
        {
            if (item != null && item.Quantity > 1)
            {
                item.Quantity--;
                CalculateTotals();
            }
        }

        [RelayCommand]
        private void RemoveFromCart(PurchaseItem item)
        {
            if (item != null)
            {
                CartItems.Remove(item);
                CalculateTotals();
            }
        }

        [RelayCommand]
        private async Task ClearCartAsync()
        {
            if (CartItems.Count > 0)
            {
                bool confirm = await DialogHelper.ShowConfirmAsync("Confirm", "Are you sure you want to clear all items in the cart?");
                if (confirm)
                {
                    CartItems.Clear();
                    CalculateTotals();
                }
            }
        }

        private void CalculateTotals()
        {
            Subtotal = CartItems.Sum(i => i.Total);
            TotalAmount = Math.Round(Subtotal - Discount + Tax + Shipping + CarryCharge + Maintenance);
            DueAmount = Math.Max(0, TotalAmount - PaidAmount);
        }

        [RelayCommand]
        private async Task CompletePurchaseAsync()
        {
            if (CartItems.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Cart is empty.");
                return;
            }

            if (SelectedSupplier == null)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please select a supplier.");
                return;
            }

            if (PaidAmount < TotalAmount)
            {
                // In a real app we might show a dialog, but here we just proceed with a due amount.
            }

            IsProcessing = true;

            try
            {
                var db = _firestoreService.GetDb();
                var batch = db.StartBatch();

                var pPrefix = "P-"; // Could be loaded from settings
                var purchasesQuery = await db.Collection("purchases").GetSnapshotAsync();
                var count = purchasesQuery.Count;
                var newPurchaseId = $"{pPrefix}{count + 1}";

                var newPurchase = new Purchase
                {
                    Id = newPurchaseId,
                    Supplier = SelectedSupplier.ShopName,
                    Date = Timestamp.FromDateTime(PurchaseDate.ToUniversalTime()),
                    PurchasedBy = new Dictionary<string, object> { { "id", "admin" }, { "name", "Admin" } }, // Mocked
                    Items = CartItems.ToList(),
                    Subtotal = Subtotal,
                    Discount = Discount,
                    Tax = Tax,
                    Shipping = Shipping,
                    CarryCharge = CarryCharge,
                    Maintenance = Maintenance,
                    TotalAmount = TotalAmount,
                    PaidAmount = PaidAmount,
                    DueAmount = DueAmount
                };

                batch.Set(db.Collection("purchases").Document(newPurchaseId), newPurchase);

                var productIds = CartItems.Select(i => i.ProductId).Distinct().ToList();

                foreach (var pId in productIds)
                {
                    var pRef = db.Collection("products").Document(pId);
                    var pDoc = await pRef.GetSnapshotAsync();
                    if (!pDoc.Exists) continue;

                    var pData = pDoc.ConvertTo<Product>();
                    var lots = pData.Lots ?? new List<Lot>();
                    var itemsForProduct = CartItems.Where(i => i.ProductId == pId).ToList();
                    
                    int totalQty = pData.Quantity ?? 0;
                    var updatedTiers = pData.PriceTiers ?? new Dictionary<string, PriceTier>();

                    foreach (var item in itemsForProduct)
                    {
                        totalQty += item.Quantity;
                        
                        if (item.NewPriceTiers != null && string.IsNullOrEmpty(item.LotNumber))
                        {
                            updatedTiers = item.NewPriceTiers;
                        }

                        string targetLot = string.IsNullOrWhiteSpace(item.LotNumber) ? "Default" : item.LotNumber.Trim();
                        var lot = lots.FirstOrDefault(l => l.LotNumber == targetLot);
                        if (lot != null)
                        {
                            lot.Quantity = (lot.Quantity ?? 0) + item.Quantity;
                            lot.BuyPrice = item.Cost;
                        }
                        else
                        {
                            lots.Add(new Lot { LotNumber = targetLot, Quantity = item.Quantity, BuyPrice = item.Cost });
                        }
                    }

                    var updates = new Dictionary<string, object>
                    {
                        { "quantity", totalQty },
                        { "lots", lots.Select(l => new { lotNumber = l.LotNumber, quantity = l.Quantity, buyPrice = l.BuyPrice }).ToList() },
                        { "priceTiers", updatedTiers }
                    };

                    batch.Update(pRef, updates);
                }

                await batch.CommitAsync();

                await DialogHelper.ShowMessageAsync("Success", "Purchase completed successfully.");
                
                // Clear cart and reset
                CartItems.Clear();
                CalculateTotals();
                SelectedProduct = null;
                SearchText = string.Empty;
                PaidAmount = 0;
                Discount = 0;
                Tax = 0;
                Shipping = 0;
                CarryCharge = 0;
                Maintenance = 0;
                LotNumber = string.Empty;
                Quantity = 1;
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to process purchase: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
