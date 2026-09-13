using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using BarodaPOS.Models;
using BarodaPOS.Services;
using QRCoder;

namespace BarodaPOS.ViewModels
{
    public partial class PurchasePreDraftProductViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _db;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
        [ObservableProperty] private Supplier _selectedSupplier;
        [ObservableProperty] private ObservableCollection<PurchaseItem> _cart = new();
        [ObservableProperty] private double _totalEstimatedAmount;

        // Product search
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private ObservableCollection<Product> _filteredProducts = new();
        [ObservableProperty] private bool _isProductSearchResultsVisible;
        private List<Product> _allProducts = new();

        [ObservableProperty] private Product _selectedProduct;
        [ObservableProperty] private int _quantity = 1;
        [ObservableProperty] private double _cost;

        public PurchasePreDraftProductViewModel()
        {
            _firestoreService = new FirestoreService();
            _db = _firestoreService.GetDb();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var prodSnap = await Task.Run(async () => await _db.Collection("products").GetSnapshotAsync());
                _allProducts = await Task.Run(() => prodSnap.Documents.Select(d => { var p = d.ConvertTo<Product>(); p.Id = d.Id; return p; }).ToList());

                var supSnap = await Task.Run(async () => await _db.Collection("suppliers").GetSnapshotAsync());
                var sups = await Task.Run(() => supSnap.Documents.Select(d => { var s = d.ConvertTo<Supplier>(); s.Id = d.Id; return s; }).ToList());
                Suppliers = new ObservableCollection<Supplier>(sups.OrderBy(s => s.ShopName ?? s.Name));

                // Smart option: if only 1 supplier exists, auto-select it.
                if (Suppliers.Count == 1)
                {
                    SelectedSupplier = Suppliers.First();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredProducts.Clear();
                IsProductSearchResultsVisible = false;
                SelectedProduct = null;
                return;
            }

            var lowerQuery = value.ToLowerInvariant();
            var matches = _allProducts.Where(p => 
                (p.Name != null && p.Name.ToLowerInvariant().Contains(lowerQuery)) || 
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(lowerQuery))
            ).Take(10).ToList();

            FilteredProducts = new ObservableCollection<Product>(matches);
            IsProductSearchResultsVisible = matches.Count > 0;
        }

        [RelayCommand]
        public void SelectProduct(Product product)
        {
            if (product != null)
            {
                SelectedProduct = product;
                SearchText = product.Name;
                IsProductSearchResultsVisible = false;
                Cost = product.DisplayBuyPrice;
                Quantity = 1;
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
                    AddToCart();
                    return;
                }

                SelectProduct(product);

                if (!string.IsNullOrWhiteSpace(product.Barcode) && 
                    !string.IsNullOrWhiteSpace(SearchText) && 
                    SearchText.Trim().Equals(product.Barcode, StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(100);
                    AddToCart();
                }
            }
        }

        [RelayCommand]
        public void AddToCart()
        {
            if (SelectedProduct == null || Quantity <= 0 || Cost < 0)
            {
                _ = DialogHelper.ShowMessageAsync("Validation", "Please select a product, and ensure quantity and cost are valid.");
                return;
            }

            var item = new PurchaseItem
            {
                ProductId = SelectedProduct.Id,
                Name = SelectedProduct.Name,
                Cost = Cost,
                Quantity = Quantity,
                TargetSupplierId = SelectedSupplier?.Id,
                TargetSupplierName = SelectedSupplier?.ShopName
            };
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(PurchaseItem.Quantity) || e.PropertyName == nameof(PurchaseItem.Total)) UpdateTotal(); };


            var existing = Cart.FirstOrDefault(c => c.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
            }
            else
            {
                Cart.Add(item);
            }

            SelectedProduct = null;
            SearchText = "";
            Quantity = 1;
            Cost = 0;
            UpdateTotal();
        }

        [RelayCommand]
        public void RemoveFromCart(PurchaseItem item)
        {
            if (item != null)
            {
                Cart.Remove(item);
                UpdateTotal();
            }
        }

        private void UpdateTotal()
        {
            TotalEstimatedAmount = Cart.Sum(c => c.Total);
        }

        [RelayCommand]
        private async Task SaveAndPrintReceiptAsync()
        {
            await ProcessPrePurchaseAsync(false);
        }

        [RelayCommand]
        private async Task SaveAndExportPdfAsync()
        {
            await ProcessPrePurchaseAsync(true);
        }

        private async Task ProcessPrePurchaseAsync(bool isPdf)
        {
            if (Cart.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Cart is empty.");
                return;
            }

            IsLoading = true;
            try
            {
                string newId = "PRE-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                
                // Ensure supplier names are correct based on current IDs (in case user changed them in combobox)
                foreach(var item in Cart)
                {
                    if (!string.IsNullOrEmpty(item.TargetSupplierId))
                    {
                        var sup = Suppliers.FirstOrDefault(s => s.Id == item.TargetSupplierId);
                        if (sup != null)
                        {
                            item.TargetSupplierName = string.IsNullOrEmpty(sup.ShopName) ? sup.Name : sup.ShopName;
                        }
                    }
                }

                var prePurchase = new PrePurchase
                {
                    SupplierId = SelectedSupplier?.Id,
                    SupplierName = SelectedSupplier?.ShopName ?? SelectedSupplier?.Name ?? "Mixed/Default",
                    Date = Timestamp.FromDateTime(DateTime.UtcNow),
                    Items = Cart.ToList(),
                    TotalEstimatedAmount = TotalEstimatedAmount,
                    Status = "Draft"
                };

                await _db.Collection("pre_purchases").Document(newId).SetAsync(prePurchase);

                // Print
                PrintDraft(prePurchase, newId, isPdf);

                // Clear
                Cart.Clear();
                SelectedSupplier = null;
                UpdateTotal();

                await DialogHelper.ShowMessageAsync("Success", "Pre-Purchase list created successfully.");
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to save Pre-Purchase: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrintDraft(PrePurchase model, string id, bool isPdf)
        {
            try
            {
                var pd = new PrintDocument();
                
                if (isPdf)
                {
                    pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    pd.PrinterSettings.PrintToFile = true;
                    pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                    
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "PDF Files (*.pdf)|*.pdf",
                        FileName = $"PrePurchase_{id}.pdf"
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        pd.PrinterSettings.PrintFileName = dialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    int width = 280;
                    int height = 600 + (model.Items.Count * 30);
                    pd.DefaultPageSettings.PaperSize = new PaperSize("Custom", width, height);
                }

                pd.PrintPage += (s, e) => PrintDraftPageEvent(e, model, id, isPdf);
                pd.Print();
            }
            catch (Exception ex)
            {
                _ = DialogHelper.ShowMessageAsync("Print Error", "Failed to print: " + ex.Message);
            }
        }

        private void PrintDraftPageEvent(PrintPageEventArgs e, PrePurchase model, string id, bool isPdf)
        {
            var g = e.Graphics;
            var fontTitle = new Font("Arial", 16, FontStyle.Bold);
            var fontSubTitle = new Font("Arial", 12, FontStyle.Bold);
            var fontBold = new Font("Arial", 10, FontStyle.Bold);
            var fontRegular = new Font("Arial", 10, FontStyle.Regular);
            var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far };

            float yPos = 10;
            float leftMargin = 10;
            float rightMargin = isPdf ? 800 : 270;
            float center = (leftMargin + rightMargin) / 2;

            if (isPdf)
            {
                leftMargin = 50;
                rightMargin = 770;
                center = 413;
                yPos = 50;
            }

            // Generate QR Code
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(id, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrBitmap = qrCode.GetGraphic(5);

            int qrSize = 100;
            g.DrawImage(qrBitmap, center - (qrSize / 2), yPos, qrSize, qrSize);
            yPos += qrSize + 10;

            g.DrawString("PRE-PURCHASE DRAFT", fontTitle, Brushes.Black, new RectangleF(leftMargin, yPos, rightMargin - leftMargin, 25), centerFormat);
            yPos += 30;
            
            g.DrawString($"Draft ID: {id}", fontRegular, Brushes.Black, leftMargin, yPos);
            g.DrawString($"Date: {model.Date.ToDateTime().ToLocalTime():g}", fontRegular, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            g.DrawString($"Target Supplier: {model.SupplierName}", fontBold, Brushes.Black, leftMargin, yPos);
            yPos += 25;

            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            float col1 = leftMargin;
            float col2 = isPdf ? rightMargin - 200 : rightMargin - 90;

            g.DrawString("Item", fontBold, Brushes.Black, col1, yPos);
            g.DrawString("Qty", fontBold, Brushes.Black, col2, yPos, rightFormat);
            g.DrawString("Est. Cost", fontBold, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            foreach (var item in model.Items)
            {
                var nameRect = new RectangleF(col1, yPos, col2 - col1 - 10, 40);
                g.DrawString(item.Name, fontRegular, Brushes.Black, nameRect);
                g.DrawString(item.Quantity.ToString(), fontRegular, Brushes.Black, col2, yPos, rightFormat);
                g.DrawString(item.Cost.ToString("N2"), fontRegular, Brushes.Black, rightMargin, yPos, rightFormat);
                
                var size = g.MeasureString(item.Name, fontRegular, (int)nameRect.Width);
                yPos += Math.Max(20, size.Height + 5);

                if (!string.IsNullOrEmpty(item.TargetSupplierName))
                {
                    g.DrawString($"  Sup: {item.TargetSupplierName}", new Font("Arial", 9, FontStyle.Italic), Brushes.DimGray, col1, yPos);
                    yPos += 15;
                }
            }

            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 10;

            g.DrawString("Total Est:", fontBold, Brushes.Black, col2, yPos, rightFormat);
            g.DrawString(model.TotalEstimatedAmount.ToString("N2"), fontBold, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 30;

            g.DrawString("Scan QR Code in Fast Purchase to verify items.", fontRegular, Brushes.Black, new RectangleF(leftMargin, yPos, rightMargin - leftMargin, 40), centerFormat);
        }
    }
}
