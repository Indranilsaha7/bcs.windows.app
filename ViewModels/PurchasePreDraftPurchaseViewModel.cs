using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Microsoft.Win32;
using ZXing;
using ZXing.Windows.Compatibility;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.IO;

namespace BarodaPOS.ViewModels
{
    public partial class PurchasePreDraftPurchaseViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _db;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _scanText = "";
        
        [ObservableProperty] private string _loadedDraftId = "None";
        [ObservableProperty] private string _defaultSupplierName = "N/A";
        
        [ObservableProperty] private ObservableCollection<PurchaseItem> _draftItems = new();
        [ObservableProperty] private double _totalAmount;
        [ObservableProperty] private double _paidAmount;
        
        private PrePurchase _loadedPrePurchase;

        public PurchasePreDraftPurchaseViewModel()
        {
            _firestoreService = new FirestoreService();
            _db = _firestoreService.GetDb();
        }

        [RelayCommand]
        private async Task LoadDraftAsync()
        {
            if (string.IsNullOrWhiteSpace(ScanText)) return;

            string id = ScanText.Trim();
            IsLoading = true;
            try
            {
                var doc = await _db.Collection("pre_purchases").Document(id).GetSnapshotAsync();
                if (!doc.Exists)
                {
                    await DialogHelper.ShowMessageAsync("Not Found", $"Could not find any Pre-Purchase draft with ID: {id}");
                    return;
                }

                var draft = doc.ConvertTo<PrePurchase>();
                draft.Id = doc.Id;
                
                if (draft.Status == "Completed")
                {
                    await DialogHelper.ShowMessageAsync("Already Completed", $"This draft ({id}) has already been processed as a full purchase.");
                    return;
                }

                _loadedPrePurchase = draft;
                LoadedDraftId = draft.Id;
                DefaultSupplierName = draft.SupplierName ?? "None";
                
                DraftItems.Clear();
                foreach(var item in draft.Items)
                {
                    var newItem = new PurchaseItem
                    {
                        ProductId = item.ProductId,
                        Name = item.Name,
                        Cost = item.Cost,
                        Quantity = item.Quantity,
                        TargetSupplierId = item.TargetSupplierId,
                        TargetSupplierName = item.TargetSupplierName
                    };
                    newItem.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(PurchaseItem.Quantity) || e.PropertyName == nameof(PurchaseItem.Cost) || e.PropertyName == nameof(PurchaseItem.Total)) UpdateTotal(); };
                    DraftItems.Add(newItem);
                }

                UpdateTotal();
                ScanText = "";
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", "Failed to load draft: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UploadImageAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Document Files (*.jpg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.bmp;*.pdf",
                Title = "Select QR Code Image or PDF"
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    await Task.Run(async () => 
                    {
                        string extractedText = null;

                        if (dialog.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            using (PdfDocument document = PdfDocument.Open(dialog.FileName))
                            {
                                foreach (var page in document.GetPages())
                                {
                                    foreach (var image in page.GetImages())
                                    {
                                        byte[] bytesToDecode = null;
                                        if (image.TryGetPng(out var pngBytes))
                                        {
                                            bytesToDecode = pngBytes.ToArray();
                                        }
                                        else if (image.TryGetBytes(out var rawBytes))
                                        {
                                            bytesToDecode = rawBytes.ToArray();
                                        }

                                        if (bytesToDecode != null)
                                        {
                                            try 
                                            {
                                                using (var ms = new MemoryStream(bytesToDecode))
                                                using (var bitmap = new Bitmap(ms))
                                                {
                                                    var reader = new BarcodeReader();
                                                    reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
                                                    reader.Options.TryHarder = true;
                                                    
                                                    var result = reader.Decode(bitmap);
                                                    if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                                                    {
                                                        extractedText = result.Text;
                                                        break;
                                                    }
                                                }
                                            } catch { } // Ignore format errors on individual images
                                        }
                                    }
                                    if (extractedText != null) break;
                                }
                            }
                        }
                        else
                        {
                            // Standard Image
                            using (var bitmap = (Bitmap)Image.FromFile(dialog.FileName))
                            {
                                var reader = new BarcodeReader();
                                reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
                                reader.Options.TryHarder = true;
                                
                                var result = reader.Decode(bitmap);
                                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                                {
                                    extractedText = result.Text;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                ScanText = extractedText;
                            });
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await LoadDraftAsync());
                        }
                        else
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                _ = DialogHelper.ShowMessageAsync("Error", "Could not detect a valid QR code in the selected file.");
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowMessageAsync("Error", "Failed to process image: " + ex.Message);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        private void RemoveItem(PurchaseItem item)
        {
            if (item != null)
            {
                DraftItems.Remove(item);
                UpdateTotal();
            }
        }

        private void UpdateTotal()
        {
            TotalAmount = DraftItems.Sum(i => i.Total);
        }

        [RelayCommand]
        private async Task CompletePurchaseAsync()
        {
            if (_loadedPrePurchase == null || DraftItems.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "No draft loaded or cart is empty.");
                return;
            }

            IsLoading = true;
            try
            {
                // Preload products for validation
                var productIds = DraftItems.Select(i => i.ProductId).Distinct().ToList();
                var pDocs = new Dictionary<string, Product>();

                foreach (var pId in productIds)
                {
                    var pRef = _db.Collection("products").Document(pId);
                    var pDoc = await pRef.GetSnapshotAsync();
                    if (pDoc.Exists)
                    {
                        pDocs[pId] = pDoc.ConvertTo<Product>();
                    }
                }

                // Validate Lot Requirement (If stock > 0, LotNumber is required)
                foreach (var item in DraftItems)
                {
                    if (pDocs.TryGetValue(item.ProductId, out var productData))
                    {
                        int currentStock = productData.Quantity ?? 0;
                        if (currentStock > 0 && string.IsNullOrWhiteSpace(item.LotNumber))
                        {
                            await DialogHelper.ShowMessageAsync("Validation Error", $"Product '{item.Name}' is currently in stock ({currentStock}). You MUST provide a Lot # for this purchase.");
                            IsLoading = false;
                            return;
                        }
                    }
                }

                var batch = _db.StartBatch();
                
                var pPrefix = "P-";
                var purchasesQuery = await _db.Collection("purchases").GetSnapshotAsync();
                int count = purchasesQuery.Count;
                
                double remainingPaid = PaidAmount;
                var groups = DraftItems.GroupBy(i => string.IsNullOrWhiteSpace(i.TargetSupplierName) ? "Mixed/Walk-in" : i.TargetSupplierName).ToList();

                foreach (var group in groups)
                {
                    count++;
                    var newPurchaseId = $"{pPrefix}{count}";
                    
                    double groupTotal = group.Sum(i => i.Total);
                    double groupPaid = Math.Min(groupTotal, remainingPaid);
                    remainingPaid -= groupPaid;
                    double groupDue = groupTotal - groupPaid;

                    var purchase = new Purchase
                    {
                        Id = newPurchaseId,
                        Supplier = group.Key,
                        Date = Timestamp.FromDateTime(DateTime.UtcNow),
                        Items = group.ToList(),
                        Subtotal = groupTotal,
                        TotalAmount = groupTotal,
                        PaidAmount = groupPaid,
                        DueAmount = groupDue,
                        PurchasedBy = new Dictionary<string, object> { { "id", "admin" }, { "name", "Admin" } }
                    };

                    // Save Purchase
                    batch.Set(_db.Collection("purchases").Document(newPurchaseId), purchase);
                }

                // Update Draft status
                batch.Update(_db.Collection("pre_purchases").Document(_loadedPrePurchase.Id), "status", "Completed");

                // Update Product Inventory
                foreach (var pId in productIds)
                {
                    if (!pDocs.TryGetValue(pId, out var pData)) continue;
                    var pRef = _db.Collection("products").Document(pId);
                    
                    var lots = pData.Lots ?? new List<Lot>();
                    var itemsForProduct = DraftItems.Where(i => i.ProductId == pId).ToList();
                    
                    int totalQty = pData.Quantity ?? 0;
                    var updatedTiers = pData.PriceTiers ?? new Dictionary<string, PriceTier>();
                    
                    foreach (var item in itemsForProduct)
                    {
                        totalQty += item.Quantity;
                        
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

                // Clear
                DraftItems.Clear();
                LoadedDraftId = "None";
                DefaultSupplierName = "N/A";
                TotalAmount = 0;
                PaidAmount = 0;
                _loadedPrePurchase = null;

                await DialogHelper.ShowMessageAsync("Success", "Purchase completed successfully from Draft!");
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
