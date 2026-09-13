using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;

namespace BarodaPOS.ViewModels
{
    public partial class SaleViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private FirestoreService _firestoreService;
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
                LoadLotsForProduct(value);
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
                // If user typed the barcode exactly, auto-add
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
                SelectedProduct = product;
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
                (p.Brand?.ToLower().Contains(lowerValue) == true) ||
                (p.Category?.ToLower().Contains(lowerValue) == true)
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

        partial void OnSelectedUnitChanged(string value)
        {
            // Refresh price display when unit changes (if needed in future)
        }

        [ObservableProperty]
        private ObservableCollection<string> _lots = new();

        [ObservableProperty]
        private string _selectedLot;

        [ObservableProperty]
        private bool _isLotSelectorVisible;

        [ObservableProperty]
        private int _quantity = 1;

        [ObservableProperty]
        private ObservableCollection<string> _saleModes = new(new[]
        {
            "normal_retail", "normal_wholesale", "shop_retail", "shop_wholesale"
        });

        [ObservableProperty]
        private string _selectedSaleMode = "normal_retail";

        partial void OnSelectedSaleModeChanged(string value)
        {
            UpdateCartPrices();
        }

        [ObservableProperty]
        private ObservableCollection<SaleItem> _cartItems = new();

        [ObservableProperty]
        private DateTime _saleDate = DateTime.Now;

        [ObservableProperty]
        private double _subtotal;

        [ObservableProperty]
        private double _discount;

        partial void OnDiscountChanged(double value) => CalculateTotals();

        [ObservableProperty]
        private double _grandTotal;

        [ObservableProperty]
        private double _paidAmount;

        partial void OnPaidAmountChanged(double value)
        {
            DueAmount = Math.Max(0, GrandTotal - value);
        }

        [ObservableProperty]
        private double _dueAmount;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private bool _printReceipt = true;

        [ObservableProperty]
        private bool _showSuccessMessage;

        [ObservableProperty]
        private string _successMessage;

        private string _editingSaleId = null;

        public SaleViewModel()
        {
            var firestoreService = new FirestoreService();
            _firestoreService = firestoreService;
            _saleService = new SaleService(firestoreService, NetworkSyncManager.Instance);
            CartItems.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (SaleItem item in e.NewItems) item.PropertyChanged += (_, _) => CalculateTotals();
                CalculateTotals();
            };
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var db = _firestoreService.GetDb();
                var snap = await Task.Run(async () => await db.Collection("products").GetSnapshotAsync());
                _allProducts = await Task.Run(() => snap.Documents.Select(d =>
                {
                    var p = d.ConvertTo<Product>();
                    p.Id = d.Id;
                    return p;
                }).ToList());

                // Check if we are editing an existing sale
                if (App.Current.Properties.Contains("EditSaleId"))
                {
                    string editId = App.Current.Properties["EditSaleId"] as string;
                    App.Current.Properties.Remove("EditSaleId"); // Consume it

                    if (!string.IsNullOrEmpty(editId))
                    {
                        var saleDoc = await db.Collection("sales").Document(editId).GetSnapshotAsync();
                        if (saleDoc.Exists)
                        {
                            var sale = saleDoc.ConvertTo<Sale>();
                            sale.Id = saleDoc.Id;
                            
                            // Populate the current view model
                            _editingSaleId = sale.Id;
                            Discount = sale.Discount;
                            PaidAmount = sale.PaidAmount;
                            CartItems.Clear();
                            if (sale.Items != null)
                            {
                                foreach (var item in sale.Items) CartItems.Add(item);
                            }
                        }
                    }
                }

                // Load print settings
                var settings = await new FirestoreService().GetSettingsAsync();
                if (settings.ContainsKey("autoPrint") && settings["autoPrint"] != null)
                {
                    PrintReceipt = Convert.ToBoolean(settings["autoPrint"]);
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to load products/sale: " + ex.Message);
            }
        }

        private void LoadUnitsForProduct(Product p)
        {
            Units.Clear();
            if (p == null) return;
            var tiers = p.PriceTiers ?? new Dictionary<string, PriceTier>();
            var mainUnit = (p.Unit ?? "Pcs").Trim().ToLower();

            if (mainUnit == "box")
            {
                if (tiers.ContainsKey("box")) Units.Add("box");
                if (tiers.ContainsKey("pack")) Units.Add("pack");
                if (tiers.ContainsKey("pcs")) Units.Add("pcs");
            }
            else if (mainUnit == "packet")
            {
                if (tiers.ContainsKey("pack")) Units.Add("pack");
                if (tiers.ContainsKey("pcs")) Units.Add("pcs");
            }
            else if (mainUnit == "kg")
            {
                if (tiers.ContainsKey("kg")) Units.Add("kg");
                if (tiers.ContainsKey("gram")) Units.Add("gram");
            }
            else if (mainUnit == "liter")
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

        private void LoadLotsForProduct(Product p)
        {
            Lots.Clear();
            Lots.Add("Default");
            if (p?.Lots != null)
            {
                foreach (var lot in p.Lots.Where(l => l.Quantity > 0))
                    Lots.Add(lot.LotNumber);
            }
            SelectedLot = Lots.First();
            IsLotSelectorVisible = Lots.Count > 1;
        }

        private double DeterminePrice(Product product, string unit, string saleMode, string lotNumber)
        {
            if (lotNumber != null && lotNumber != "Default")
            {
                var lot = product.Lots?.FirstOrDefault(l => l.LotNumber == lotNumber);
                if (lot != null)
                {
                    double markup = (product.SellPrice ?? 0) - (product.BuyPrice ?? 0);
                    return (lot.BuyPrice ?? 0) + markup;
                }
            }

            if (product.PriceTiers != null && product.PriceTiers.TryGetValue(unit, out var tier))
            {
                return saleMode switch
                {
                    "normal_retail" => tier.Sell ?? 0,
                    "normal_wholesale" => tier.Wholesale ?? tier.Sell ?? 0,
                    "shop_retail" => tier.ShopSell ?? tier.Sell ?? 0,
                    "shop_wholesale" => tier.ShopWholesale ?? tier.Wholesale ?? 0,
                    _ => tier.Sell ?? 0
                };
            }

            return saleMode switch
            {
                "normal_wholesale" or "shop_wholesale" => product.WholesalePrice ?? product.SellPrice ?? 0,
                _ => product.SellPrice ?? 0
            };
        }

        private void UpdateCartPrices()
        {
            foreach (var item in CartItems)
            {
                var product = _allProducts.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                    item.Price = DeterminePrice(product, item.Unit, SelectedSaleMode, item.LotNumber);
            }
            CalculateTotals();
        }

        [RelayCommand]
        private async Task AddToCartAsync()
        {
            if (SelectedProduct == null)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please select a product first.");
                return;
            }

            string lotNumber = (SelectedLot == "Default" || string.IsNullOrWhiteSpace(SelectedLot)) ? null : SelectedLot;
            string unitKey = SelectedUnit ?? "pcs";

            // Stock check
            var existingItem = CartItems.FirstOrDefault(i =>
                i.ProductId == SelectedProduct.Id &&
                i.LotNumber == lotNumber &&
                i.Unit == unitKey);
            int currentQty = existingItem?.Quantity ?? 0;
            int requested = currentQty + Quantity;

            int available;
            if (lotNumber != null)
                available = SelectedProduct.Lots?.FirstOrDefault(l => l.LotNumber == lotNumber)?.Quantity ?? 0;
            else
                available = SelectedProduct.Quantity ?? 0;

            if (requested > available)
            {
                await DialogHelper.ShowMessageAsync("Stock Error",
                    $"Cannot add {Quantity} of \"{SelectedProduct.Name}\". Only {available} available.");
                return;
            }

            double price = DeterminePrice(SelectedProduct, unitKey, SelectedSaleMode, lotNumber);
            double buyPrice = lotNumber != null
                ? SelectedProduct.Lots?.FirstOrDefault(l => l.LotNumber == lotNumber)?.BuyPrice ?? 0
                : SelectedProduct.BuyPrice ?? 0;

            if (existingItem != null)
            {
                existingItem.Quantity += Quantity;
            }
            else
            {
                CartItems.Add(new SaleItem
                {
                    ProductId = SelectedProduct.Id,
                    Name = SelectedProduct.Name,
                    Quantity = Quantity,
                    Unit = unitKey,
                    LotNumber = lotNumber,
                    Price = price,
                    BuyPrice = buyPrice,
                    SaleType = SelectedSaleMode
                });
            }

            CalculateTotals();
            // Reset form
            Quantity = 1;
            SelectedProduct = null;
            SearchText = string.Empty;
            Units.Clear();
            Lots.Clear();
        }

        private void CalculateTotals()
        {
            Subtotal = Math.Round(CartItems.Sum(i => i.Total), 2);
            GrandTotal = Math.Round(Subtotal - Discount, 2);
            PaidAmount = GrandTotal; // Auto-fill paid
            DueAmount = 0;
        }

        [RelayCommand]
        private void IncreaseQuantity(SaleItem item)
        {
            if (item == null) return;
            var product = _allProducts.FirstOrDefault(p => p.Id == item.ProductId);
            int available = 0;
            if (product != null)
            {
                if (item.LotNumber != null)
                    available = product.Lots?.FirstOrDefault(l => l.LotNumber == item.LotNumber)?.Quantity ?? 0;
                else
                    available = product.Quantity ?? 0;
            }
            if (item.Quantity >= available)
            {
                _ = DialogHelper.ShowMessageAsync("Stock Limit", $"Cannot add more. Only {available} available.");
                return;
            }
            item.Quantity++;
            CalculateTotals();
        }

        [RelayCommand]
        private void DecreaseQuantity(SaleItem item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
                item.Quantity--;
            else
                CartItems.Remove(item);
            CalculateTotals();
        }

        [RelayCommand]
        private void RemoveFromCart(SaleItem item)
        {
            if (item != null)
            {
                CartItems.Remove(item);
                CalculateTotals();
            }
        }

        [RelayCommand]
        private void ClearCart()
        {
            CartItems.Clear();
            CalculateTotals();
        }

        [RelayCommand]
        private async Task CompleteSaleAsync()
        {
            if (CartItems.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Cart is empty. Please add products first.");
                return;
            }

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var batch = db.StartBatch();

                // Decrement stock
                foreach (var item in CartItems)
                {
                    var pRef = db.Collection("products").Document(item.ProductId);
                    if (item.LotNumber != null)
                    {
                        var pSnap = await pRef.GetSnapshotAsync();
                        if (pSnap.Exists)
                        {
                            var pData = pSnap.ConvertTo<Product>();
                            var lots = pData.Lots ?? new List<Lot>();
                            var lot = lots.FirstOrDefault(l => l.LotNumber == item.LotNumber);
                            if (lot != null) lot.Quantity -= item.Quantity;
                            var updatedLots = lots.Where(l => l.Quantity > 0)
                                .Select(l => new Dictionary<string, object> {
                                    { "lotNumber", l.LotNumber },
                                    { "quantity", l.Quantity ?? 0 },
                                    { "buyPrice", l.BuyPrice ?? 0 }
                                }).ToList();
                            batch.Update(pRef, new Dictionary<string, object> {
                                { "lots", updatedLots },
                                { "quantity", FieldValue.Increment(-item.Quantity) }
                            });
                        }
                    }
                    else
                    {
                        batch.Update(pRef, new Dictionary<string, object> {
                            { "quantity", FieldValue.Increment(-item.Quantity) }
                        });
                    }
                }

                string newSaleId = _editingSaleId;
                if (string.IsNullOrEmpty(newSaleId))
                {
                    // Fetch the latest add_sale to increment the ID
                    var lastSaleSnapshot = await db.Collection("sales")
                        .WhereEqualTo("source", "add_sale")
                        .OrderByDescending("date")
                        .Limit(1)
                        .GetSnapshotAsync();

                    int nextIdNumber = 1;
                    if (lastSaleSnapshot.Documents.Count > 0)
                    {
                        string lastId = lastSaleSnapshot.Documents[0].Id;
                        if (lastId.StartsWith("as-", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(lastId.Substring(3), out int lastNum))
                            {
                                nextIdNumber = lastNum + 1;
                            }
                        }
                    }
                    
                    newSaleId = $"AS-{nextIdNumber}";
                    
                    // Safety check to ensure we don't overwrite an existing document if multiple clients sell at once
                    while (true)
                    {
                        var docCheck = await db.Collection("sales").Document(newSaleId).GetSnapshotAsync();
                        if (!docCheck.Exists) break;
                        nextIdNumber++;
                        newSaleId = $"AS-{nextIdNumber}";
                    }
                }

                var saleData = new Dictionary<string, object>
                {
                    { "firestoreId", newSaleId },
                    { "source", "add_sale" },
                    { "customer", null },
                    { "date", Timestamp.FromDateTime(SaleDate.ToUniversalTime()) },
                    { "soldBy", new Dictionary<string, object> { { "id", "admin" }, { "name", "Admin" } } },
                    { "saleMode", SelectedSaleMode },
                    { "items", CartItems.Select(i => new Dictionary<string, object> {
                        { "productId", i.ProductId },
                        { "name", i.Name },
                        { "quantity", i.Quantity },
                        { "unit", i.Unit },
                        { "lotNumber", i.LotNumber ?? (object)null },
                        { "price", i.Price },
                        { "buyPrice", i.BuyPrice },
                        { "saleType", i.SaleType ?? (object)null }
                    }).ToList() },
                    { "subtotal", Subtotal },
                    { "discount", Discount },
                    { "tax", 0.0 },
                    { "shipping", 0.0 },
                    { "totalAmount", GrandTotal },
                    { "paidAmount", PaidAmount },
                    { "dueAmount", DueAmount },
                    { "paymentStatus", DueAmount > 0 ? "Partial" : "Paid" },
                    { "createdAt", FieldValue.ServerTimestamp }
                };

                batch.Set(db.Collection("sales").Document(newSaleId), saleData);
                await batch.CommitAsync();

                if (PrintReceipt)
                {
                    try
                    {
                        var settings = await new FirestoreService().GetSettingsAsync();
                        string printerName = settings.ContainsKey("defaultPrinter") ? settings["defaultPrinter"]?.ToString() : null;
                        double bottomMargin = 50;
                        if (settings.ContainsKey("bottomMargin") && settings["bottomMargin"] != null)
                        {
                            bottomMargin = Convert.ToDouble(settings["bottomMargin"]);
                        }
                        PrintReceiptDocument(saleData, newSaleId, printerName, (int)bottomMargin);
                    }
                    catch (Exception ex)
                    {
                        await DialogHelper.ShowMessageAsync("Print Error", $"Sale completed, but receipt failed to print: {ex.Message}");
                    }
                }

                SuccessMessage = $"Sale {newSaleId} completed!";
                ShowSuccessMessage = true;
                _ = Task.Delay(3000).ContinueWith(_ => ShowSuccessMessage = false);

                CartItems.Clear();
                Discount = 0;
                PaidAmount = 0;
                _editingSaleId = null;
                CalculateTotals();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to process sale: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        private void PrintReceiptDocument(Dictionary<string, object> saleData, string saleId, string printerName, int bottomMargin = 50)
        {
            var pd = new System.Drawing.Printing.PrintDocument();
            
            // Suppress the "Printing..." status dialog box to make it 100% silent and direct
            pd.PrintController = new System.Drawing.Printing.StandardPrintController();
            
            if (!string.IsNullOrEmpty(printerName))
            {
                pd.PrinterSettings.PrinterName = printerName;
            }

            pd.PrintPage += (sender, e) =>
            {
                var g = e.Graphics;
                var fontLarge = new System.Drawing.Font("Courier New", 14, System.Drawing.FontStyle.Bold);
                var fontRegular = new System.Drawing.Font("Courier New", 10, System.Drawing.FontStyle.Regular);
                var fontBold = new System.Drawing.Font("Courier New", 10, System.Drawing.FontStyle.Bold);
                var fontSmall = new System.Drawing.Font("Courier New", 8, System.Drawing.FontStyle.Regular);
                
                int y = 10;
                int left = 10;
                // standard thermal width is roughly 280-300 units depending on DPI
                int right = 280; 
                int center = right / 2 + left;

                // Header
                var sfCenter = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center };
                g.DrawString("BARODA COMPUTERS & SALES", fontLarge, System.Drawing.Brushes.Black, center, y, sfCenter);
                y += 25;
                g.DrawString("Receipt: " + saleId, fontRegular, System.Drawing.Brushes.Black, center, y, sfCenter);
                y += 20;
                g.DrawString("Date: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm"), fontRegular, System.Drawing.Brushes.Black, center, y, sfCenter);
                y += 30;

                // Separator
                g.DrawString(new string('-', 32), fontRegular, System.Drawing.Brushes.Black, left, y);
                y += 20;

                // Headers
                var sfRight = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Far };
                g.DrawString("Item", fontBold, System.Drawing.Brushes.Black, left, y);
                g.DrawString("Total", fontBold, System.Drawing.Brushes.Black, right, y, sfRight);
                y += 20;
                g.DrawString(new string('-', 32), fontRegular, System.Drawing.Brushes.Black, left, y);
                y += 20;

                // Items
                var items = saleData["items"] as List<Dictionary<string, object>>;
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        string name = item["name"]?.ToString() ?? "";
                        if (name.Length > 22) name = name.Substring(0, 22) + "..";
                        
                        string qty = item["quantity"]?.ToString() ?? "0";
                        string price = item["price"]?.ToString() ?? "0";
                        double t = double.Parse(qty) * double.Parse(price);

                        g.DrawString($"{name}", fontRegular, System.Drawing.Brushes.Black, left, y);
                        y += 20;
                        g.DrawString($"  {qty} x {price}", fontSmall, System.Drawing.Brushes.Black, left, y);
                        g.DrawString(t.ToString("N2"), fontRegular, System.Drawing.Brushes.Black, right, y, sfRight);
                        y += 25;
                    }
                }

                g.DrawString(new string('-', 32), fontRegular, System.Drawing.Brushes.Black, left, y);
                y += 20;

                // Totals
                g.DrawString("Subtotal:", fontRegular, System.Drawing.Brushes.Black, left, y);
                g.DrawString(Convert.ToDouble(saleData["subtotal"]).ToString("N2"), fontRegular, System.Drawing.Brushes.Black, right, y, sfRight);
                y += 20;

                double discount = Convert.ToDouble(saleData["discount"]);
                if (discount > 0)
                {
                    g.DrawString("Discount:", fontRegular, System.Drawing.Brushes.Black, left, y);
                    g.DrawString("-" + discount.ToString("N2"), fontRegular, System.Drawing.Brushes.Black, right, y, sfRight);
                    y += 20;
                }

                g.DrawString("GRAND TOTAL:", fontBold, System.Drawing.Brushes.Black, left, y);
                g.DrawString(Convert.ToDouble(saleData["totalAmount"]).ToString("N2"), fontBold, System.Drawing.Brushes.Black, right, y, sfRight);
                y += 30;

                g.DrawString("Paid:", fontRegular, System.Drawing.Brushes.Black, left, y);
                g.DrawString(Convert.ToDouble(saleData["paidAmount"]).ToString("N2"), fontRegular, System.Drawing.Brushes.Black, right, y, sfRight);
                y += 20;

                double due = Convert.ToDouble(saleData["dueAmount"]);
                if (due > 0)
                {
                    g.DrawString("Due:", fontBold, System.Drawing.Brushes.Black, left, y);
                    g.DrawString(due.ToString("N2"), fontBold, System.Drawing.Brushes.Black, right, y, sfRight);
                    y += 20;
                }

                y += 10;
                g.DrawString(new string('-', 32), fontRegular, System.Drawing.Brushes.Black, left, y);
                y += 20;
                g.DrawString("Thank you for your business!", fontBold, System.Drawing.Brushes.Black, center, y, sfCenter);
                
                // Bottom Padding for auto-cutter / bottleneck
                int extraFeedUnits = (int)(bottomMargin * 3.937);
                y += extraFeedUnits; 
                using (var cutPen = new System.Drawing.Pen(System.Drawing.Color.LightGray, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    g.DrawLine(cutPen, left, y, right, y);
                }
            };

            pd.Print();
        }
    }
}



