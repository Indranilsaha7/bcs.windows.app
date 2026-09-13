using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;

namespace BarodaPOS.ViewModels
{
    public partial class AddProductViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;

        public static Product ProductToEdit { get; set; }

        [ObservableProperty]
        private bool _isEditing;
        private string _editingProductId;
        private string _originalBarcode;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _barcode;

        [ObservableProperty]
        private string _barcodeType = "Code 128";

        [ObservableProperty]
        private string _brand;

        [ObservableProperty]
        private string _category;

        [ObservableProperty]
        private string _selectedUnit = "Pcs";

        public List<string> Units { get; } = new() { "Pcs", "Box", "Kg", "Liter", "Packet" };

        [ObservableProperty]
        private int _alertQuantity = 1;
        
        [ObservableProperty]
        private string _imageUrl;

        [ObservableProperty]
        private string _description;

        // Packaging Relations
        [ObservableProperty]
        private int? _packetPerBox;
        
        [ObservableProperty]
        private int? _pcsPerBox;
        
        [ObservableProperty]
        private int? _pcsPerPacket;

        // UI Visibility Toggles
        [ObservableProperty]
        private bool _isBoxPriceVisible;
        
        [ObservableProperty]
        private bool _isKgPriceVisible;
        
        [ObservableProperty]
        private bool _isLiterPriceVisible;
        
        [ObservableProperty]
        private bool _isPacketPriceVisible;
        
        [ObservableProperty]
        private bool _isStandardPriceVisible = true;

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

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _saveButtonText = "Save Product";

        [ObservableProperty]
        private string _saveButtonIcon = "Save24";

        [ObservableProperty]
        private string _pageTitle = "Add New Product";

        partial void OnSelectedUnitChanged(string value)
        {
            IsBoxPriceVisible = value == "Box";
            IsKgPriceVisible = value == "Kg";
            IsLiterPriceVisible = value == "Liter";
            IsPacketPriceVisible = value == "Packet";
            IsStandardPriceVisible = value == "Pcs" || string.IsNullOrEmpty(value);
        }

        public AddProductViewModel()
        {
            _firestoreService = new FirestoreService();

            if (ProductToEdit != null)
            {
                IsEditing = true;
                PageTitle = "Edit Product";
                SaveButtonText = "Update Product";
                SaveButtonIcon = "Edit24";
                _editingProductId = ProductToEdit.Id;
                _originalBarcode = ProductToEdit.Barcode;

                Name = ProductToEdit.Name;
                Barcode = ProductToEdit.Barcode;
                BarcodeType = string.IsNullOrEmpty(ProductToEdit.BarcodeType) ? "Code 128" : ProductToEdit.BarcodeType;
                Brand = ProductToEdit.Brand;
                Category = ProductToEdit.Category;
                SelectedUnit = string.IsNullOrEmpty(ProductToEdit.Unit) ? "Pcs" : ProductToEdit.Unit;
                AlertQuantity = ProductToEdit.AlertQuantity.GetValueOrDefault(1);
                Description = ProductToEdit.Description;
                ImageUrl = ProductToEdit.ImageUrl;
                
                PacketPerBox = ProductToEdit.PacketPerBox;
                PcsPerBox = ProductToEdit.PcsPerBox;
                PcsPerPacket = ProductToEdit.PcsPerPacket;

                OnSelectedUnitChanged(SelectedUnit);

                if (ProductToEdit.PriceTiers != null)
                {
                    if (SelectedUnit == "Box")
                    {
                        if (ProductToEdit.PriceTiers.TryGetValue("box", out var box))
                        {
                            BuyPriceBox = box.Buy ?? 0;
                            WholesalePriceBoxNormal = box.Wholesale ?? 0;
                            WholesalePriceBoxShop = box.ShopWholesale ?? 0;
                            SellPriceBoxNormal = box.Sell ?? 0;
                            SellPriceBoxShop = box.ShopSell ?? 0;
                        }
                        if (ProductToEdit.PriceTiers.TryGetValue("pack", out var pack))
                        {
                            BuyPricePack = pack.Buy ?? 0;
                            WholesalePricePackNormal = pack.Wholesale ?? 0;
                            WholesalePricePackShop = pack.ShopWholesale ?? 0;
                        }
                        if (ProductToEdit.PriceTiers.TryGetValue("pcs", out var pcs))
                        {
                            BuyPricePcs = pcs.Buy ?? 0;
                            SellPricePcsNormal = pcs.Sell ?? 0;
                            SellPricePcsShop = pcs.ShopSell ?? 0;
                        }
                    }
                    else if (SelectedUnit == "Kg")
                    {
                        if (ProductToEdit.PriceTiers.TryGetValue("kg", out var kg))
                        {
                            BuyPriceKg = kg.Buy ?? 0;
                            WholesalePriceKgNormal = kg.Wholesale ?? 0;
                            WholesalePriceKgShop = kg.ShopWholesale ?? 0;
                            SellPriceKgNormal = kg.Sell ?? 0;
                            SellPriceKgShop = kg.ShopSell ?? 0;
                        }
                        if (ProductToEdit.PriceTiers.TryGetValue("gram", out var gram))
                        {
                            BuyPriceGram = gram.Buy ?? 0;
                            WholesalePriceGramNormal = gram.Wholesale ?? 0;
                            WholesalePriceGramShop = gram.ShopWholesale ?? 0;
                            SellPriceGramNormal = gram.Sell ?? 0;
                            SellPriceGramShop = gram.ShopSell ?? 0;
                        }
                    }
                    else if (SelectedUnit == "Liter")
                    {
                        if (ProductToEdit.PriceTiers.TryGetValue("liter", out var liter))
                        {
                            BuyPriceLiter = liter.Buy ?? 0;
                            WholesalePriceLiterNormal = liter.Wholesale ?? 0;
                            WholesalePriceLiterShop = liter.ShopWholesale ?? 0;
                            SellPriceLiterNormal = liter.Sell ?? 0;
                            SellPriceLiterShop = liter.ShopSell ?? 0;
                        }
                        if (ProductToEdit.PriceTiers.TryGetValue("ml", out var ml))
                        {
                            BuyPriceMl = ml.Buy ?? 0;
                            WholesalePriceMlNormal = ml.Wholesale ?? 0;
                            WholesalePriceMlShop = ml.ShopWholesale ?? 0;
                            SellPriceMlNormal = ml.Sell ?? 0;
                            SellPriceMlShop = ml.ShopSell ?? 0;
                        }
                    }
                    else if (SelectedUnit == "Packet")
                    {
                        if (ProductToEdit.PriceTiers.TryGetValue("pack", out var pack))
                        {
                            BuyPricePacket = pack.Buy ?? 0;
                            WholesalePricePacketNormal = pack.Wholesale ?? 0;
                            WholesalePricePacketShop = pack.ShopWholesale ?? 0;
                            SellPricePacketNormal = pack.Sell ?? 0;
                            SellPricePacketShop = pack.ShopSell ?? 0;
                        }
                        if (ProductToEdit.PriceTiers.TryGetValue("pcs", out var pcs))
                        {
                            BuyPricePacketPcs = pcs.Buy ?? 0;
                            WholesalePricePacketPcsNormal = pcs.Wholesale ?? 0;
                            WholesalePricePacketPcsShop = pcs.ShopWholesale ?? 0;
                            SellPricePacketPcsNormal = pcs.Sell ?? 0;
                            SellPricePacketPcsShop = pcs.ShopSell ?? 0;
                        }
                    }
                    else // Pcs
                    {
                        if (ProductToEdit.PriceTiers.TryGetValue("pcs", out var pcs))
                        {
                            BuyPrice = pcs.Buy ?? 0;
                            WholesalePriceNormal = pcs.Wholesale ?? 0;
                            WholesalePriceShop = pcs.ShopWholesale ?? 0;
                            SellPriceNormal = pcs.Sell ?? 0;
                            SellPriceShop = pcs.ShopSell ?? 0;
                        }
                    }
                }
                else
                {
                    // Fallback to top-level properties if old data
                    BuyPrice = ProductToEdit.BuyPrice ?? 0;
                    WholesalePriceNormal = ProductToEdit.WholesalePrice ?? 0;
                    SellPriceNormal = ProductToEdit.SellPrice ?? 0;
                }
                
                // Clear the static reference
                ProductToEdit = null;
            }
            else
            {
                OnSelectedUnitChanged("Pcs");
            }
        }

        [RelayCommand]
        public async Task SaveProductAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await DialogHelper.ShowMessageAsync("Validation Error", "Product Name is required.");
                return;
            }

            if (IsSaving) return;
            IsSaving = true;

            try
            {
                var db = _firestoreService.GetDb();
                
                // Construct PriceTiers based on logic
                var priceTiers = new Dictionary<string, PriceTier>();

                if (SelectedUnit == "Box")
                {
                    priceTiers["box"] = new PriceTier { Buy = BuyPriceBox, Wholesale = WholesalePriceBoxNormal, ShopWholesale = WholesalePriceBoxShop, Sell = SellPriceBoxNormal, ShopSell = SellPriceBoxShop };
                    priceTiers["pack"] = new PriceTier { Buy = BuyPricePack, Wholesale = WholesalePricePackNormal, ShopWholesale = WholesalePricePackShop };
                    priceTiers["pcs"] = new PriceTier { Buy = BuyPricePcs, Sell = SellPricePcsNormal, ShopSell = SellPricePcsShop };
                }
                else if (SelectedUnit == "Kg")
                {
                    priceTiers["kg"] = new PriceTier { Buy = BuyPriceKg, Wholesale = WholesalePriceKgNormal, ShopWholesale = WholesalePriceKgShop, Sell = SellPriceKgNormal, ShopSell = SellPriceKgShop };
                    priceTiers["gram"] = new PriceTier { Buy = BuyPriceGram, Wholesale = WholesalePriceGramNormal, ShopWholesale = WholesalePriceGramShop, Sell = SellPriceGramNormal, ShopSell = SellPriceGramShop };
                }
                else if (SelectedUnit == "Liter")
                {
                    priceTiers["liter"] = new PriceTier { Buy = BuyPriceLiter, Wholesale = WholesalePriceLiterNormal, ShopWholesale = WholesalePriceLiterShop, Sell = SellPriceLiterNormal, ShopSell = SellPriceLiterShop };
                    priceTiers["ml"] = new PriceTier { Buy = BuyPriceMl, Wholesale = WholesalePriceMlNormal, ShopWholesale = WholesalePriceMlShop, Sell = SellPriceMlNormal, ShopSell = SellPriceMlShop };
                }
                else if (SelectedUnit == "Packet")
                {
                    priceTiers["pack"] = new PriceTier { Buy = BuyPricePacket, Wholesale = WholesalePricePacketNormal, ShopWholesale = WholesalePricePacketShop, Sell = SellPricePacketNormal, ShopSell = SellPricePacketShop };
                    priceTiers["pcs"] = new PriceTier { Buy = BuyPricePacketPcs, Wholesale = WholesalePricePacketPcsNormal, ShopWholesale = WholesalePricePacketPcsShop, Sell = SellPricePacketPcsNormal, ShopSell = SellPricePacketPcsShop };
                }
                else
                {
                    priceTiers["pcs"] = new PriceTier { Buy = BuyPrice, Wholesale = WholesalePriceNormal, ShopWholesale = WholesalePriceShop, Sell = SellPriceNormal, ShopSell = SellPriceShop };
                }

                double rootBuy = 0, rootWholesale = 0, rootSell = 0;
                if (SelectedUnit == "Box") { rootBuy = BuyPriceBox; rootWholesale = WholesalePriceBoxNormal; rootSell = SellPriceBoxNormal; }
                else if (SelectedUnit == "Kg") { rootBuy = BuyPriceKg; rootWholesale = WholesalePriceKgNormal; rootSell = SellPriceKgNormal; }
                else if (SelectedUnit == "Liter") { rootBuy = BuyPriceLiter; rootWholesale = WholesalePriceLiterNormal; rootSell = SellPriceLiterNormal; }
                else if (SelectedUnit == "Packet") { rootBuy = BuyPricePacket; rootWholesale = WholesalePricePacketNormal; rootSell = SellPricePacketNormal; }
                else { rootBuy = BuyPrice; rootWholesale = WholesalePriceNormal; rootSell = SellPriceNormal; }

                var productData = new Dictionary<string, object>
                {
                    { "name", Name },
                    { "barcode", Barcode ?? string.Empty },
                    { "barcodeType", BarcodeType },
                    { "brand", Brand ?? string.Empty },
                    { "category", Category ?? string.Empty },
                    { "unit", SelectedUnit },
                    { "alertQuantity", AlertQuantity },
                    { "description", Description ?? string.Empty },
                    { "imageUrl", ImageUrl ?? string.Empty },
                    { "priceTiers", priceTiers },
                    { "packetPerBox", PacketPerBox },
                    { "pcsPerBox", PcsPerBox },
                    { "pcsPerPacket", PcsPerPacket },
                    { "buyPrice", rootBuy },
                    { "wholesalePrice", rootWholesale },
                    { "sellPrice", rootSell }
                };

                if (IsEditing)
                {
                    if (!string.IsNullOrWhiteSpace(Barcode) && Barcode != _originalBarcode)
                    {
                        var existing = await db.Collection("products").WhereEqualTo("barcode", Barcode).GetSnapshotAsync();
                        if (existing.Count > 0)
                        {
                            await DialogHelper.ShowMessageAsync("Validation Error", "This barcode is already in use by another product.");
                            IsSaving = false;
                            return;
                        }
                    }
                    productData["updatedAt"] = FieldValue.ServerTimestamp;
                    await db.Collection("products").Document(_editingProductId).UpdateAsync(productData);
                    await DialogHelper.ShowMessageAsync("Success", "Product updated successfully!");
                    
                    var mainWindow = Application.Current.MainWindow as Views.MainWindow;
                    mainWindow?.RootNavigation.Navigate(typeof(Views.ProductPage));
                }
                else
                {
                    var product = new Product
                    {
                        Name = Name,
                        Barcode = Barcode,
                        BarcodeType = BarcodeType,
                        Brand = Brand,
                        Category = Category,
                        Unit = SelectedUnit,
                        AlertQuantity = AlertQuantity,
                        Description = Description,
                        ImageUrl = ImageUrl,
                        PriceTiers = priceTiers,
                        Quantity = 0, // Initial stock is 0
                        PacketPerBox = PacketPerBox,
                        PcsPerBox = PcsPerBox,
                        PcsPerPacket = PcsPerPacket
                    };

                    if (!string.IsNullOrWhiteSpace(Barcode))
                    {
                        // Check uniqueness
                        var existing = await db.Collection("products").WhereEqualTo("barcode", Barcode).GetSnapshotAsync();
                        if (existing.Count > 0)
                        {
                            await DialogHelper.ShowMessageAsync("Validation Error", "This barcode is already in use.");
                            IsSaving = false;
                            return;
                        }
                        await db.Collection("products").Document(Barcode).SetAsync(product);
                    }
                    else
                    {
                        await db.Collection("products").AddAsync(product);
                    }

                    await DialogHelper.ShowMessageAsync("Success", "Product added successfully!");
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Error saving product: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void GenerateBarcode()
        {
            var random = new Random();
            Barcode = random.Next(10000000, 99999999).ToString();
        }

        private void ResetForm()
        {
            Name = string.Empty;
            Barcode = string.Empty;
            BarcodeType = "Code 128";
            Brand = string.Empty;
            Category = string.Empty;
            SelectedUnit = "Pcs";
            AlertQuantity = 1;
            Description = string.Empty;
            ImageUrl = string.Empty;
            PacketPerBox = null;
            PcsPerBox = null;
            PcsPerPacket = null;

            BuyPriceBox = 0; WholesalePriceBoxNormal = 0; WholesalePriceBoxShop = 0; SellPriceBoxNormal = 0; SellPriceBoxShop = 0;
            BuyPricePack = 0; WholesalePricePackNormal = 0; WholesalePricePackShop = 0;
            BuyPricePcs = 0; SellPricePcsNormal = 0; SellPricePcsShop = 0;

            BuyPriceKg = 0; WholesalePriceKgNormal = 0; WholesalePriceKgShop = 0; SellPriceKgNormal = 0; SellPriceKgShop = 0;
            BuyPriceGram = 0; WholesalePriceGramNormal = 0; WholesalePriceGramShop = 0; SellPriceGramNormal = 0; SellPriceGramShop = 0;

            BuyPriceLiter = 0; WholesalePriceLiterNormal = 0; WholesalePriceLiterShop = 0; SellPriceLiterNormal = 0; SellPriceLiterShop = 0;
            BuyPriceMl = 0; WholesalePriceMlNormal = 0; WholesalePriceMlShop = 0; SellPriceMlNormal = 0; SellPriceMlShop = 0;

            BuyPricePacket = 0; WholesalePricePacketNormal = 0; WholesalePricePacketShop = 0; SellPricePacketNormal = 0; SellPricePacketShop = 0;
            BuyPricePacketPcs = 0; WholesalePricePacketPcsNormal = 0; WholesalePricePacketPcsShop = 0; SellPricePacketPcsNormal = 0; SellPricePacketPcsShop = 0;

            BuyPrice = 0; WholesalePriceNormal = 0; WholesalePriceShop = 0; SellPriceNormal = 0; SellPriceShop = 0;
        }
    }
}
