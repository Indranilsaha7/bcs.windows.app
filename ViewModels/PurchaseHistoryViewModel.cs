using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using BarodaPOS.Models;
using BarodaPOS.Services;

namespace BarodaPOS.ViewModels
{
    public partial class PurchaseDisplayModel : ObservableObject
    {
        public Purchase Purchase { get; set; }
        public string Id => Purchase.Id;
        public string Supplier => Purchase.Supplier;
        public int TotalQuantity => Purchase.Items?.Sum(i => i.Quantity) ?? 0;
        public double TotalCost => Purchase.TotalAmount;
        public double DueAmount => Purchase.DueAmount;
        public DateTime Date => Purchase.Date.ToDateTime().ToLocalTime();
        
        [ObservableProperty]
        private string _status;
        
        [ObservableProperty]
        private bool _isReturned;
    }

    public partial class PurchaseHistoryViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;

        [ObservableProperty] private double _totalPurchaseValue;
        [ObservableProperty] private int _totalPurchases;
        [ObservableProperty] private double _avgPurchaseValue;
        [ObservableProperty] private double _totalPurchaseDue;

        [ObservableProperty] private DateTime? _startDate;
        [ObservableProperty] private DateTime? _endDate;
        [ObservableProperty] private string _supplierSearch;

        [ObservableProperty] private ObservableCollection<PurchaseDisplayModel> _purchasesList = new();
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotProcessing))]
        private bool _isProcessing;

        public bool IsNotProcessing => !IsProcessing;

        // Payment dialog properties
        [ObservableProperty] private bool _isPayDueDialogOpen;
        [ObservableProperty] private PurchaseDisplayModel _selectedPurchaseForPayment;
        [ObservableProperty] private double _paymentAmount;

        // Details dialog properties
        [ObservableProperty] private bool _isDetailsDialogOpen;
        [ObservableProperty] private PurchaseDisplayModel _selectedPurchaseForDetails;

        public PurchaseHistoryViewModel()
        {
            _firestoreService = new FirestoreService();
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var purchasesRef = db.Collection("purchases");
                Query query = purchasesRef.OrderByDescending("date");

                if (StartDate.HasValue)
                {
                    query = query.WhereGreaterThanOrEqualTo("date", Timestamp.FromDateTime(StartDate.Value.Date.ToUniversalTime()));
                }
                if (EndDate.HasValue)
                {
                    query = query.WhereLessThanOrEqualTo("date", Timestamp.FromDateTime(EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime()));
                }
                if (!string.IsNullOrWhiteSpace(SupplierSearch))
                {
                    query = query.WhereEqualTo("supplier", SupplierSearch.Trim());
                }

                var result = await Task.Run(async () =>
                {
                    var purchaseSnapshot = await query.GetSnapshotAsync();
                    var returnsSnapshot = await db.Collection("purchaseReturns").GetSnapshotAsync();

                    var returnedIds = new HashSet<string>();
                    foreach (var doc in returnsSnapshot.Documents)
                    {
                        var pr = doc.ConvertTo<PurchaseReturn>();
                        if (!string.IsNullOrEmpty(pr.OriginalPurchaseId))
                        {
                            returnedIds.Add(pr.OriginalPurchaseId);
                        }
                    }

                    var allPurchasesLocal = new List<PurchaseDisplayModel>();
                    double sumValueLocal = 0;
                    double sumDueLocal = 0;

                    foreach (var doc in purchaseSnapshot.Documents)
                    {
                        var p = doc.ConvertTo<Purchase>();
                        p.Id = doc.Id;
                        
                        bool isReturned = returnedIds.Contains(p.Id);
                        string status = "Due";
                        if (isReturned) status = "Returned";
                        else if (p.DueAmount <= 0.5) status = "Paid";
                        else if (p.PaidAmount > 0) status = "Partial";

                        allPurchasesLocal.Add(new PurchaseDisplayModel
                        {
                            Purchase = p,
                            Status = status,
                            IsReturned = isReturned
                        });

                        sumValueLocal += p.TotalAmount;
                        if (!isReturned) sumDueLocal += p.DueAmount;
                    }
                    
                    return (allPurchasesLocal, sumValueLocal, sumDueLocal);
                });

                PurchasesList = new ObservableCollection<PurchaseDisplayModel>(result.allPurchasesLocal);

                TotalPurchases = result.allPurchasesLocal.Count;
                TotalPurchaseValue = result.sumValueLocal;
                AvgPurchaseValue = TotalPurchases > 0 ? TotalPurchaseValue / TotalPurchases : 0;
                TotalPurchaseDue = result.sumDueLocal;
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to load data: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void ResetFilter()
        {
            StartDate = null;
            EndDate = null;
            SupplierSearch = string.Empty;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task ClearHistoryAsync()
        {
            bool confirm = await DialogHelper.ShowConfirmAsync("Warning", "Are you sure you want to permanently delete all purchase records? This action cannot be undone and does NOT revert stock levels.");
            if (!confirm) return;

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var snapshot = await db.Collection("purchases").GetSnapshotAsync();
                var batch = db.StartBatch();
                foreach (var doc in snapshot.Documents)
                {
                    batch.Delete(doc.Reference);
                }
                await batch.CommitAsync();
                await DialogHelper.ShowMessageAsync("Success", "All purchase records have been cleared.");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to clear history: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task DeletePurchaseAsync(PurchaseDisplayModel model)
        {
            if (model == null) return;
            bool confirm = await DialogHelper.ShowConfirmAsync("Confirm", $"Are you sure you want to delete purchase {model.Id}? This will revert stock levels.");
            if (!confirm) return;

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var batch = db.StartBatch();

                foreach (var item in model.Purchase.Items)
                {
                    var productRef = db.Collection("products").Document(item.ProductId);
                    batch.Update(productRef, "quantity", FieldValue.Increment(-item.Quantity));
                    // Simplified: lot number quantity reverting is skipped here similarly to HTML version, 
                    // ideally you'd read the document to revert specific lots.
                }

                batch.Delete(db.Collection("purchases").Document(model.Id));
                await batch.CommitAsync();
                await DialogHelper.ShowMessageAsync("Success", "Purchase deleted and stock reverted.");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to delete purchase: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void OpenPayDueDialog(PurchaseDisplayModel model)
        {
            if (model == null || model.DueAmount <= 0.5) return;
            SelectedPurchaseForPayment = model;
            PaymentAmount = model.DueAmount;
            IsPayDueDialogOpen = true;
        }

        [RelayCommand]
        private async Task ConfirmPaymentAsync()
        {
            if (SelectedPurchaseForPayment == null) return;
            if (PaymentAmount <= 0 || PaymentAmount > SelectedPurchaseForPayment.DueAmount)
            {
                await DialogHelper.ShowMessageAsync("Validation", "Invalid payment amount.");
                return;
            }

            IsProcessing = true;
            try
            {
                var db = _firestoreService.GetDb();
                var pRef = db.Collection("purchases").Document(SelectedPurchaseForPayment.Id);
                
                var updates = new Dictionary<string, object>
                {
                    { "paidAmount", FieldValue.Increment(PaymentAmount) },
                    { "dueAmount", FieldValue.Increment(-PaymentAmount) }
                };
                
                await pRef.UpdateAsync(updates);
                
                IsPayDueDialogOpen = false;
                await DialogHelper.ShowMessageAsync("Success", "Payment successful!");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to process payment: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        
        [RelayCommand]
        private void ClosePayDueDialog()
        {
            IsPayDueDialogOpen = false;
        }

        [RelayCommand]
        private void ShowDetails(PurchaseDisplayModel model)
        {
            if (model == null) return;
            SelectedPurchaseForDetails = model;
            IsDetailsDialogOpen = true;
        }

        [RelayCommand]
        private void CloseDetailsDialog()
        {
            IsDetailsDialogOpen = false;
        }

        [RelayCommand]
        private void PrintPurchaseReceipt()
        {
            if (SelectedPurchaseForDetails == null) return;
            PrintReceiptInternal(false);
        }

        [RelayCommand]
        private void ExportPurchasePdf()
        {
            if (SelectedPurchaseForDetails == null) return;
            PrintReceiptInternal(true);
        }

        private void PrintReceiptInternal(bool isPdf)
        {
            try
            {
                var pd = new System.Drawing.Printing.PrintDocument();
                
                if (isPdf)
                {
                    pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    pd.PrinterSettings.PrintToFile = true;
                    // Standard A4
                    pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1169);
                    
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "PDF Files (*.pdf)|*.pdf",
                        FileName = $"Purchase_Receipt_{SelectedPurchaseForDetails.Id}.pdf"
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        pd.PrinterSettings.PrintFileName = dialog.FileName;
                    }
                    else
                    {
                        return; // Cancelled
                    }
                }
                else
                {
                    // Thermal 80mm
                    int width = 280;
                    int height = 500 + (SelectedPurchaseForDetails.Purchase.Items.Count * 30);
                    pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Custom", width, height);
                }

                pd.PrintPage += (s, ev) => PrintPurchasePageEvent(ev, SelectedPurchaseForDetails, isPdf);
                pd.Print();

                _ = DialogHelper.ShowMessageAsync("Success", isPdf ? "Exported to PDF successfully." : "Purchase receipt sent to printer.");
            }
            catch (Exception ex)
            {
                _ = DialogHelper.ShowMessageAsync("Print Error", "Failed to print: " + ex.Message);
            }
        }

        private void PrintPurchasePageEvent(System.Drawing.Printing.PrintPageEventArgs e, PurchaseDisplayModel model, bool isPdf)
        {
            var g = e.Graphics;
            var fontTitle = new System.Drawing.Font("Arial", 16, System.Drawing.FontStyle.Bold);
            var fontSubTitle = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
            var fontBold = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            var fontRegular = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Regular);
            var centerFormat = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center };
            var rightFormat = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Far };

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

            g.DrawString("Baroda Computure And Sales", fontTitle, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin - leftMargin, 25), centerFormat);
            yPos += 30;
            g.DrawString("Singra, Natore\nMobile: +880 1714-637798", fontRegular, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin - leftMargin, 40), centerFormat);
            yPos += 45;

            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 10;

            g.DrawString("PURCHASE RECEIPT", fontSubTitle, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin - leftMargin, 20), centerFormat);
            yPos += 30;

            g.DrawString($"ID: {model.Id}", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
            g.DrawString($"Date: {model.Date:g}", fontRegular, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            g.DrawString($"Supplier: {model.Supplier}", fontBold, System.Drawing.Brushes.Black, leftMargin, yPos);
            yPos += 25;

            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            // Header
            float col1 = leftMargin;
            float col2 = isPdf ? rightMargin - 200 : rightMargin - 90;
            float col3 = isPdf ? rightMargin - 100 : rightMargin - 45;

            g.DrawString("Item", fontBold, System.Drawing.Brushes.Black, col1, yPos);
            g.DrawString("Qty", fontBold, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
            g.DrawString("Total", fontBold, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            foreach (var item in model.Purchase.Items)
            {
                var nameRect = new System.Drawing.RectangleF(col1, yPos, col2 - col1 - 10, 40);
                g.DrawString(item.Name, fontRegular, System.Drawing.Brushes.Black, nameRect);
                g.DrawString(item.Quantity.ToString(), fontRegular, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
                g.DrawString(item.Total.ToString("N2"), fontRegular, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
                
                // Estimate height
                var size = g.MeasureString(item.Name, fontRegular, (int)nameRect.Width);
                yPos += Math.Max(20, size.Height + 5);
            }

            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 10;

            g.DrawString("Subtotal:", fontRegular, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
            g.DrawString(model.Purchase.Subtotal.ToString("N2"), fontRegular, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            if (model.Purchase.Discount > 0)
            {
                g.DrawString("Discount:", fontRegular, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
                g.DrawString("-" + model.Purchase.Discount.ToString("N2"), fontRegular, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 20;
            }

            g.DrawString("Grand Total:", fontBold, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
            g.DrawString(model.Purchase.TotalAmount.ToString("N2"), fontBold, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 25;

            g.DrawString("Paid:", fontRegular, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
            g.DrawString(model.Purchase.PaidAmount.ToString("N2"), fontRegular, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            g.DrawString("Due:", fontBold, System.Drawing.Brushes.Black, col2, yPos, rightFormat);
            g.DrawString(model.Purchase.DueAmount.ToString("N2"), fontBold, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 30;

            g.DrawString("Thank you for your business!", fontRegular, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin - leftMargin, 20), centerFormat);
        }
    }
}
