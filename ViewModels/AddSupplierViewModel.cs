using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;
using System;

namespace BarodaPOS.ViewModels
{
    public partial class AddSupplierViewModel : ObservableObject
    {
        private readonly FirestoreDb _db;

        [ObservableProperty]
        private string _pageTitle = "Add New Supplier";

        [ObservableProperty]
        private string _saveButtonText = "Save Supplier";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Supplier _currentSupplier = new();

        public AddSupplierViewModel()
        {
            var firestoreService = new FirestoreService();
            _db = firestoreService.GetDb();
        }

        public void LoadSupplier(Supplier supplier)
        {
            if (supplier != null)
            {
                CurrentSupplier = new Supplier
                {
                    Id = supplier.Id,
                    ShopName = supplier.ShopName,
                    Name = supplier.Name,
                    Phone = supplier.Phone,
                    Email = supplier.Email,
                    Address = supplier.Address
                };
                PageTitle = "Edit Supplier";
                SaveButtonText = "Update Supplier";
            }
            else
            {
                CurrentSupplier = new Supplier();
                PageTitle = "Add New Supplier";
                SaveButtonText = "Save Supplier";
            }
        }

        [RelayCommand]
        public async Task SaveSupplierAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentSupplier.Name))
            {
                await DialogHelper.ShowMessageAsync("Validation Error", "Proprietor Name is required.");
                return;
            }

            if (IsLoading) return;
            IsLoading = true;

            try
            {
                if (string.IsNullOrEmpty(CurrentSupplier.Id))
                {
                    // Add new
                    var docRef = await _db.Collection("suppliers").AddAsync(CurrentSupplier);
                    CurrentSupplier.Id = docRef.Id;
                    
                    await DialogHelper.ShowMessageAsync("Success", "Supplier added successfully.");
                    
                    // Reset form
                    CurrentSupplier = new Supplier();
                }
                else
                {
                    // Update existing
                    var docRef = _db.Collection("suppliers").Document(CurrentSupplier.Id);
                    await docRef.SetAsync(CurrentSupplier, SetOptions.MergeAll);
                    
                    await DialogHelper.ShowMessageAsync("Success", "Supplier updated successfully.");
                }
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
    }
}
