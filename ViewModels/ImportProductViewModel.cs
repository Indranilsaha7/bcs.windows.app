using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Services;
using ClosedXML.Excel;
using System.IO;
using System.Text.Json;
using CsvHelper;
using System.Globalization;
using BarodaPOS.Models;
using System.Linq;

namespace BarodaPOS.ViewModels
{
    public class ProductImportDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public int? AlertQuantity { get; set; }
        public double? BuyPrice { get; set; }
        public double? WholesalePriceNormal { get; set; }
        public double? WholesalePriceShop { get; set; }
        public double? SellPriceNormal { get; set; }
        public double? SellPriceShop { get; set; }
        public string Description { get; set; }
    }

    public partial class ImportProductViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;

        [ObservableProperty]
        private string _selectedFilePath;

        [ObservableProperty]
        private bool _isImporting;

        [ObservableProperty]
        private string _importStatus;

        public ImportProductViewModel()
        {
            _firestoreService = new FirestoreService();
        }

        [RelayCommand]
        private void BrowseFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Data Files (*.csv;*.json;*.xlsx)|*.csv;*.json;*.xlsx|CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "Select Product Data File"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                ImportStatus = "File selected. Ready to import.";
            }
        }

        [RelayCommand]
        private async Task DownloadCsvTemplate()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV (Excel Compatible) (*.csv)|*.csv",
                Title = "Save CSV / Excel Template",
                FileName = "product_template.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (dialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Products");

                            // Headers
                            string[] headers = { "Name", "Barcode", "Brand", "Category", "Unit", "AlertQuantity", "BuyPrice", "WholesalePriceNormal", "WholesalePriceShop", "SellPriceNormal", "SellPriceShop", "Description" };
                            for (int i = 0; i < headers.Length; i++)
                            {
                                worksheet.Cell(1, i + 1).Value = headers[i];
                            }

                            // Sample Data
                            var row = worksheet.Row(2);
                            row.Cell(1).Value = "Sample Product";
                            row.Cell(2).Value = "12345678"; // Format as text to keep leading zeros if any
                            row.Cell(2).Style.NumberFormat.Format = "@";
                            row.Cell(3).Value = "Sample Brand";
                            row.Cell(4).Value = "Electronics";
                            row.Cell(5).Value = "Pcs";
                            row.Cell(6).Value = 5;
                            row.Cell(7).Value = 100;
                            row.Cell(8).Value = 120;
                            row.Cell(9).Value = 110;
                            row.Cell(10).Value = 150;
                            row.Cell(11).Value = 140;
                            row.Cell(12).Value = "This is a sample product.";

                            // Make it beautiful: Create an Excel Table with a nice theme
                            var range = worksheet.Range(1, 1, 2, headers.Length);
                            var table = range.CreateTable();
                            table.Theme = XLTableTheme.TableStyleMedium9; // Professional blue theme with banded rows

                            // Freeze the top row so headers stay visible while scrolling
                            worksheet.SheetView.FreezeRows(1);

                            // Format pricing columns cleanly to 2 decimal places
                            for (int col = 7; col <= 11; col++)
                            {
                                worksheet.Column(col).Style.NumberFormat.Format = "0.00";
                            }

                            // Auto-fit all columns for perfect width
                            worksheet.Columns().AdjustToContents();
                            workbook.SaveAs(dialog.FileName);
                        }
                    }
                    else
                    {
                        string csvTemplate = "Name,Barcode,Brand,Category,Unit,AlertQuantity,BuyPrice,WholesalePriceNormal,WholesalePriceShop,SellPriceNormal,SellPriceShop,Description\n" +
                                             "Sample Product,12345678,Sample Brand,Electronics,Pcs,5,100.00,120.00,110.00,150.00,140.00,This is a sample product.";
                        System.IO.File.WriteAllText(dialog.FileName, csvTemplate);
                    }

                    await DialogHelper.ShowMessageAsync("Success", "Template downloaded successfully!");
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowMessageAsync("Error", $"Error saving template: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DownloadJsonTemplate()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Save JSON Template",
                FileName = "product_template.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string jsonTemplate = "[\n  {\n    \"Name\": \"Sample Product\",\n    \"Barcode\": \"12345678\",\n    \"Brand\": \"Sample Brand\",\n    \"Category\": \"Electronics\",\n    \"Unit\": \"Pcs\",\n    \"AlertQuantity\": 5,\n    \"BuyPrice\": 100,\n    \"WholesalePriceNormal\": 120,\n    \"WholesalePriceShop\": 110,\n    \"SellPriceNormal\": 150,\n    \"SellPriceShop\": 140,\n    \"Description\": \"This is a sample product.\"\n  }\n]";
                    System.IO.File.WriteAllText(dialog.FileName, jsonTemplate);
                    await DialogHelper.ShowMessageAsync("Success", "JSON Template downloaded successfully!");
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowMessageAsync("Error", $"Error saving template: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        public async Task ImportProductsAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
            {
                await DialogHelper.ShowMessageAsync("Validation Error", "Please select a valid file first.");
                return;
            }

            if (IsImporting) return;
            IsImporting = true;
            ImportStatus = "Parsing file...";

            try
            {
                var importedProducts = await Task.Run(() => ParseFile(SelectedFilePath));

                if (importedProducts == null || importedProducts.Count == 0)
                {
                    await DialogHelper.ShowMessageAsync("Warning", "No valid products found in the file.");
                    return;
                }

                ImportStatus = $"Saving {importedProducts.Count} products to database...";
                await SaveToFirestoreAsync(importedProducts);

                await DialogHelper.ShowMessageAsync("Success", $"{importedProducts.Count} products imported successfully!");
                ImportStatus = "Import completed.";
                SelectedFilePath = string.Empty;
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Error importing products: {ex.Message}");
                ImportStatus = "Import failed.";
            }
            finally
            {
                IsImporting = false;
            }
        }

        private List<ProductImportDto> ParseFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".json")
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<ProductImportDto>>(json) ?? new List<ProductImportDto>();
            }
            else if (ext == ".csv")
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                return csv.GetRecords<ProductImportDto>().ToList();
            }
            else if (ext == ".xlsx")
            {
                var list = new List<ProductImportDto>();
                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheet(1);
                var rows = ws.RangeUsed().RowsUsed().Skip(1); // skip header
                
                foreach (var row in rows)
                {
                    if (row.Cell(1).IsEmpty()) continue; // skip empty rows

                    list.Add(new ProductImportDto
                    {
                        Name = row.Cell(1).GetString(),
                        Barcode = row.Cell(2).GetString(),
                        Brand = row.Cell(3).GetString(),
                        Category = row.Cell(4).GetString(),
                        Unit = row.Cell(5).GetString(),
                        AlertQuantity = row.Cell(6).TryGetValue(out int aq) ? aq : null,
                        BuyPrice = row.Cell(7).TryGetValue(out double bp) ? bp : null,
                        WholesalePriceNormal = row.Cell(8).TryGetValue(out double wpn) ? wpn : null,
                        WholesalePriceShop = row.Cell(9).TryGetValue(out double wps) ? wps : null,
                        SellPriceNormal = row.Cell(10).TryGetValue(out double spn) ? spn : null,
                        SellPriceShop = row.Cell(11).TryGetValue(out double sps) ? sps : null,
                        Description = row.Cell(12).GetString()
                    });
                }
                return list;
            }
            else
            {
                throw new NotSupportedException("Unsupported file format. Please use .json, .csv, or .xlsx");
            }
        }

        private async Task SaveToFirestoreAsync(List<ProductImportDto> dtos)
        {
            var db = _firestoreService.GetDb();
            var collection = db.Collection("products");
            
            // Firestore batches support up to 500 operations
            int batchLimit = 500;
            for (int i = 0; i < dtos.Count; i += batchLimit)
            {
                var batch = db.StartBatch();
                var currentBatchDtos = dtos.Skip(i).Take(batchLimit);

                foreach (var dto in currentBatchDtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                    string barcode = string.IsNullOrWhiteSpace(dto.Barcode) 
                        ? new Random().Next(10000000, 99999999).ToString() 
                        : dto.Barcode;

                    string unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Pcs" : dto.Unit;

                    var priceTiers = new Dictionary<string, PriceTier>();
                    priceTiers[unit.ToLowerInvariant()] = new PriceTier
                    {
                        Buy = dto.BuyPrice,
                        Wholesale = dto.WholesalePriceNormal,
                        ShopWholesale = dto.WholesalePriceShop,
                        Sell = dto.SellPriceNormal,
                        ShopSell = dto.SellPriceShop
                    };

                    var product = new Product
                    {
                        Name = dto.Name,
                        Barcode = barcode,
                        Brand = dto.Brand ?? "",
                        Category = dto.Category ?? "",
                        Unit = dto.Unit ?? "Pcs",
                        AlertQuantity = dto.AlertQuantity ?? 1,
                        Description = dto.Description ?? "",
                        Quantity = 0,
                        PriceTiers = priceTiers,
                        BuyPrice = dto.BuyPrice,
                        WholesalePrice = dto.WholesalePriceNormal,
                        SellPrice = dto.SellPriceNormal
                    };

                    var docRef = collection.Document(barcode);
                    batch.Set(docRef, product); // Using Set to overwrite if exists, or Add new
                }

                await batch.CommitAsync();
            }
        }
    }
}
