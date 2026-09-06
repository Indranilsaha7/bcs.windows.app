using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using BarodaPOS.Models;
using BarodaPOS.Services;

namespace BarodaPOS.ViewModels
{
    public partial class ReturnableItemModel : ObservableObject
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int PurchasedQty { get; set; }
        public int MaxReturnable { get; set; }
        public double Cost { get; set; }
        
        [ObservableProperty]
        private int _returnQty;

        partial void OnReturnQtyChanged(int value)
        {
            if (value < 0) ReturnQty = 0;
            if (value > MaxReturnable) ReturnQty = MaxReturnable;
            ReturnQtyChangedAction?.Invoke();
        }

        public Action ReturnQtyChangedAction { get; set; }
        
        public double TotalValue => ReturnQty * Cost;
    }

    public partial class PurchaseReturnViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;

        [ObservableProperty]
        private string _purchaseIdInput;

        [ObservableProperty]
        private bool _isPurchaseFound;

        [ObservableProperty]
        private string _foundPurchaseId;

        [ObservableProperty]
        private string _foundPurchaseSupplier;

        [ObservableProperty]
        private string _foundPurchaseDate;

        [ObservableProperty]
        private ObservableCollection<ReturnableItemModel> _returnableItems = new();

        [ObservableProperty]
        private double _totalReturnValue;

        [ObservableProperty]
        private DateTime _returnDate = DateTime.Now;

        [ObservableProperty]
        private string _returnNotes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotProcessing))]
        private bool _isProcessing;

        public bool IsNotProcessing => !IsProcessing;

        private Purchase _originalPurchase;

        public PurchaseReturnViewModel()
        {
            _firestoreService = new FirestoreService();
        }

        [RelayCommand]
        private async Task FindPurchaseAsync()
        {
            if (string.IsNullOrWhiteSpace(PurchaseIdInput))
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please enter a Purchase ID.");
                return;
            }

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var purchaseRef = db.Collection("purchases").Document(PurchaseIdInput.Trim());
                var purchaseSnap = await purchaseRef.GetSnapshotAsync();

                if (!purchaseSnap.Exists)
                {
                    await DialogHelper.ShowMessageAsync("Error", "Purchase ID not found.");
                    IsPurchaseFound = false;
                    ReturnableItems.Clear();
                    _originalPurchase = null;
                    return;
                }

                _originalPurchase = purchaseSnap.ConvertTo<Purchase>();
                _originalPurchase.Id = purchaseSnap.Id;

                // Fetch existing returns to calculate max returnable
                var returnsQuery = db.Collection("purchaseReturns")
                    .WhereEqualTo("originalPurchaseId", _originalPurchase.Id);
                var returnsSnap = await returnsQuery.GetSnapshotAsync();

                var previouslyReturnedMap = new Dictionary<string, int>();
                foreach (var doc in returnsSnap.Documents)
                {
                    var pr = doc.ConvertTo<PurchaseReturn>();
                    if (pr.Items != null)
                    {
                        foreach (var item in pr.Items)
                        {
                            if (previouslyReturnedMap.ContainsKey(item.ProductId))
                                previouslyReturnedMap[item.ProductId] += item.Quantity;
                            else
                                previouslyReturnedMap[item.ProductId] = item.Quantity;
                        }
                    }
                }

                ReturnableItems.Clear();
                foreach (var item in _originalPurchase.Items)
                {
                    int prevReturned = previouslyReturnedMap.TryGetValue(item.ProductId, out var val) ? val : 0;
                    int maxReturnable = Math.Max(0, item.Quantity - prevReturned);

                    var returnableItem = new ReturnableItemModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Name ?? "Unknown Product",
                        PurchasedQty = item.Quantity,
                        MaxReturnable = maxReturnable,
                        Cost = item.Cost,
                        ReturnQty = 0,
                        ReturnQtyChangedAction = CalculateTotal
                    };
                    ReturnableItems.Add(returnableItem);
                }

                FoundPurchaseId = _originalPurchase.Id;
                FoundPurchaseSupplier = _originalPurchase.Supplier ?? "N/A";
                FoundPurchaseDate = _originalPurchase.Date.ToDateTime().ToLocalTime().ToString("d");
                
                IsPurchaseFound = true;
                CalculateTotal();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to find purchase: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void CalculateTotal()
        {
            TotalReturnValue = ReturnableItems.Sum(x => x.TotalValue);
        }

        [RelayCommand]
        private async Task ProcessReturnAsync()
        {
            if (!IsPurchaseFound || _originalPurchase == null)
            {
                await DialogHelper.ShowMessageAsync("Validation", "No purchase selected for return.");
                return;
            }

            var itemsToReturn = ReturnableItems.Where(x => x.ReturnQty > 0).ToList();
            if (itemsToReturn.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please enter a quantity for at least one item to return.");
                return;
            }

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var batch = db.StartBatch();

                var returnItems = new List<PurchaseReturnItem>();
                var groupedReturns = new Dictionary<string, int>();

                foreach (var item in itemsToReturn)
                {
                    returnItems.Add(new PurchaseReturnItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.ReturnQty,
                        Cost = item.Cost
                    });

                    if (groupedReturns.ContainsKey(item.ProductId))
                        groupedReturns[item.ProductId] += item.ReturnQty;
                    else
                        groupedReturns[item.ProductId] = item.ReturnQty;
                }

                // Decrease stock
                foreach (var kvp in groupedReturns)
                {
                    var productRef = db.Collection("products").Document(kvp.Key);
                    
                    // Simple decrement
                    batch.Update(productRef, "quantity", FieldValue.Increment(-kvp.Value));
                    
                    // Note: If lot numbers are tracked and need decreasing, it requires reading the product first.
                    // For simplicity mirroring the HTML logic, we just decrement total quantity.
                }

                // Generate new PR ID
                var prPrefix = "PR-";
                var returnsSnapshot = await db.Collection("purchaseReturns").GetSnapshotAsync();
                int maxId = 0;
                foreach (var doc in returnsSnapshot.Documents)
                {
                    if (doc.Id.StartsWith(prPrefix) && int.TryParse(doc.Id.Substring(prPrefix.Length), out int num))
                    {
                        if (num > maxId) maxId = num;
                    }
                }
                var newReturnId = $"{prPrefix}{maxId + 1}";

                var newReturn = new PurchaseReturn
                {
                    Id = newReturnId,
                    OriginalPurchaseId = _originalPurchase.Id,
                    Items = returnItems,
                    Amount = TotalReturnValue,
                    Date = Timestamp.FromDateTime(ReturnDate.ToUniversalTime()),
                    Notes = ReturnNotes,
                    ProcessedBy = new Dictionary<string, object> { { "id", "admin" }, { "name", "Admin" } }
                };

                batch.Set(db.Collection("purchaseReturns").Document(newReturnId), newReturn);

                await batch.CommitAsync();
                await DialogHelper.ShowMessageAsync("Success", $"Return processed successfully! ID: {newReturnId}");

                // Print logic can be added here or in View if needed. We'll skip complex printing for WPF unless specified,
                // or open a simple PrintDialog if needed. The HTML version opens a new window and calls window.print().

                // Reset
                PurchaseIdInput = string.Empty;
                IsPurchaseFound = false;
                ReturnableItems.Clear();
                TotalReturnValue = 0;
                ReturnNotes = string.Empty;
                _originalPurchase = null;
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to process return: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
