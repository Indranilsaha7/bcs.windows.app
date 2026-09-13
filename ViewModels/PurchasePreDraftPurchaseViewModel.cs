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
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Select QR Code Image"
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    await Task.Run(async () => 
                    {
                        using (var bitmap = (Bitmap)Image.FromFile(dialog.FileName))
                        {
                            var reader = new BarcodeReader();
                            reader.Options.PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
                            reader.Options.TryHarder = true;
                            
                            var result = reader.Decode(bitmap);
                            if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    ScanText = result.Text;
                                });
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await LoadDraftAsync());
                            }
                            else
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    _ = DialogHelper.ShowMessageAsync("Error", "Could not detect a valid QR code in the image.");
                                });
                            }
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
                double due = TotalAmount - PaidAmount;
                
                var purchase = new Purchase
                {
                    Supplier = _loadedPrePurchase.SupplierId ?? "Mixed/Walk-in",
                    Date = Timestamp.FromDateTime(DateTime.UtcNow),
                    Items = DraftItems.ToList(),
                    Subtotal = TotalAmount,
                    TotalAmount = TotalAmount,
                    PaidAmount = PaidAmount,
                    DueAmount = due,
                    PurchasedBy = AuthService.CurrentUser != null ? new Dictionary<string, object> {
                        { "uid", AuthService.CurrentUser.Uid },
                        { "email", AuthService.CurrentUser.Email }
                    } : null
                };

                // Save Purchase
                await _db.Collection("purchases").AddAsync(purchase);

                // Update Draft status
                await _db.Collection("pre_purchases").Document(_loadedPrePurchase.Id).UpdateAsync("status", "Completed");

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
