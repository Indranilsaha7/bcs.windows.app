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
    public partial class SupplierDisplayModel : ObservableObject
    {
        public Supplier Supplier { get; set; }
        
        public string Id => Supplier.Id;
        public string Name => Supplier.Name;
        public string ShopName => Supplier.ShopName;
        public string Phone => Supplier.Phone;
        public string Email => Supplier.Email;
        
        [ObservableProperty]
        private double _totalPurchased;

        [ObservableProperty]
        private double _totalDue;

        [ObservableProperty]
        private double _totalReturned;
        
        [ObservableProperty]
        private string _lastPurchaseDate;
        
        public List<Purchase> Purchases { get; set; }
    }

    public partial class SupplierViewModel : ObservableObject
    {
        private readonly FirestoreDb _db;
        private List<SupplierDisplayModel> _allSuppliers = new();

        [ObservableProperty]
        private ObservableCollection<SupplierDisplayModel> _suppliers = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchQuery = "";

        [ObservableProperty]
        private bool _showDueOnly = false;

        partial void OnShowDueOnlyChanged(bool value)
        {
            FilterSuppliers();
        }

        [ObservableProperty]
        private double _totalPurchasedOverall;

        [ObservableProperty]
        private double _totalDueOverall;

        [ObservableProperty]
        private double _totalReturnedOverall;
        
        // Modal Properties
        [ObservableProperty]
        private bool _isModalOpen;

        [ObservableProperty]
        private string _modalTitle;

        [ObservableProperty]
        private Supplier _currentSupplier = new();

        public SupplierViewModel()
        {
            var firestoreService = new FirestoreService();
            _db = firestoreService.GetDb();
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (rawSuppliers, rawPurchases, rawReturns) = await Task.Run(async () => {
                    var sSnap = await _db.Collection("suppliers").GetSnapshotAsync();
                    var pSnap = await _db.Collection("purchases").GetSnapshotAsync();
                    var rSnap = await _db.Collection("purchaseReturns").GetSnapshotAsync();

                    var s = sSnap.Documents.Select(d => d.ConvertTo<Supplier>()).ToList();
                    var p = pSnap.Documents.Select(d => { var x = d.ConvertTo<Purchase>(); x.Id = d.Id; return x; }).ToList();
                    var r = rSnap.Documents.Select(d => d.ConvertTo<PurchaseReturn>()).ToList();
                    return (s, p, r);
                });

                var result = await Task.Run(() => {
                    double tPurchased = 0;
                    double tDue = 0;
                    double tReturned = rawReturns.Sum(r => r.Amount);

                    foreach (var p in rawPurchases)
                    {
                        tPurchased += p.TotalAmount;
                        tDue += p.DueAmount;
                    }
                    
                    var models = new List<SupplierDisplayModel>();
                    foreach (var sup in rawSuppliers)
                    {
                        var supplierPurchases = rawPurchases.Where(p => string.Equals(p.Supplier, sup.ShopName, StringComparison.OrdinalIgnoreCase)).ToList();
                        double supPurchased = supplierPurchases.Sum(p => p.TotalAmount);
                        double supDue = supplierPurchases.Sum(p => p.DueAmount);
                        
                        var supPurchaseIds = supplierPurchases.Select(p => p.Id).ToHashSet();
                        var supReturns = rawReturns.Where(r => supPurchaseIds.Contains(r.OriginalPurchaseId)).ToList();
                        double supReturned = supReturns.Sum(r => r.Amount);
                        
                        string lastDate = "N/A";
                        if (supplierPurchases.Any())
                        {
                            lastDate = supplierPurchases.Max(p => p.Date.ToDateTime().ToLocalTime()).ToString("dd-MMM-yyyy");
                        }

                        models.Add(new SupplierDisplayModel
                        {
                            Supplier = sup,
                            TotalPurchased = supPurchased,
                            TotalDue = supDue,
                            TotalReturned = supReturned,
                            LastPurchaseDate = lastDate,
                            Purchases = supplierPurchases
                        });
                    }
                    return (tPurchased, tDue, tReturned, models);
                });

                TotalPurchasedOverall = result.tPurchased;
                TotalDueOverall = result.tDue;
                TotalReturnedOverall = result.tReturned;

                _allSuppliers = result.models;

                FilterSuppliers();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to load data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            FilterSuppliers();
        }

        private void FilterSuppliers()
        {
            var filteredList = _allSuppliers.AsEnumerable();

            if (ShowDueOnly)
            {
                filteredList = filteredList.Where(s => s.TotalDue > 0);
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.ToLowerInvariant();
                filteredList = filteredList.Where(s => 
                    (s.Id?.ToLowerInvariant().Contains(q) == true) ||
                    (s.Name?.ToLowerInvariant().Contains(q) == true) ||
                    (s.ShopName?.ToLowerInvariant().Contains(q) == true) ||
                    (s.Phone?.ToLowerInvariant().Contains(q) == true) ||
                    (s.Email?.ToLowerInvariant().Contains(q) == true));
            }

            Suppliers = new ObservableCollection<SupplierDisplayModel>(filteredList);
        }

        [RelayCommand]
        public void AddSupplier()
        {
            CurrentSupplier = new Supplier();
            ModalTitle = "Add New Supplier";
            IsModalOpen = true;
        }

        [RelayCommand]
        public void EditSupplier(SupplierDisplayModel model)
        {
            if (model == null) return;
            CurrentSupplier = new Supplier
            {
                Id = model.Supplier.Id,
                Name = model.Supplier.Name,
                ShopName = model.Supplier.ShopName,
                Phone = model.Supplier.Phone,
                Email = model.Supplier.Email,
                Address = model.Supplier.Address
            };
            ModalTitle = "Edit Supplier";
            IsModalOpen = true;
        }

        [RelayCommand]
        public async Task SaveSupplierAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentSupplier.Name) || string.IsNullOrWhiteSpace(CurrentSupplier.ShopName))
            {
                await DialogHelper.ShowMessageAsync("Validation Error", "Proprietor Name and Shop Name are required.");
                return;
            }

            IsLoading = true;
            try
            {
                if (string.IsNullOrEmpty(CurrentSupplier.Id))
                {
                    var snapshot = await _db.Collection("suppliers").GetSnapshotAsync();
                    int maxId = 0;
                    foreach (var doc in snapshot.Documents)
                    {
                        if (doc.Id.StartsWith("S-") && int.TryParse(doc.Id.Substring(2), out int num))
                        {
                            if (num > maxId) maxId = num;
                        }
                    }
                    CurrentSupplier.Id = $"S-{maxId + 1}";
                    
                    await _db.Collection("suppliers").Document(CurrentSupplier.Id).SetAsync(CurrentSupplier);
                }
                else
                {
                    await _db.Collection("suppliers").Document(CurrentSupplier.Id).SetAsync(CurrentSupplier, SetOptions.MergeAll);
                }

                IsModalOpen = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to save supplier: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task DeleteSupplierAsync(SupplierDisplayModel model)
        {
            if (model == null) return;

            bool confirm = await DialogHelper.ShowConfirmAsync("Confirm Delete", $"Are you sure you want to delete supplier '{model.Name}'?");
            if (!confirm) return;

            IsLoading = true;
            try
            {
                await _db.Collection("suppliers").Document(model.Id).DeleteAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to delete supplier: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        public void ShowDetails(SupplierDisplayModel model)
        {
            if (model == null) return;
            
            var detailsWindow = new BarodaPOS.Views.SupplierDetailsWindow
            {
                DataContext = model
            };
            
            detailsWindow.ShowDialog();
        }

    }
}