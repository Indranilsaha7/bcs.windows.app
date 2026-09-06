using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows;
using BarodaPOS.Models;
using BarodaPOS.Services;

namespace BarodaPOS.Views
{
    public partial class SaleDetailsWindow : Window
    {
        private Sale _sale;
        private int _bottomMargin = 50;
        
        public SaleDetailsWindow(Sale sale)
        {
            InitializeComponent();
            _sale = sale;
            DataContext = _sale;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void PrintReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrintController = new StandardPrintController();
                
                var firestoreService = new FirestoreService();
                var settings = await firestoreService.GetSettingsAsync();
                if (settings.ContainsKey("defaultPrinter") && settings["defaultPrinter"] != null)
                {
                    pd.PrinterSettings.PrinterName = settings["defaultPrinter"].ToString();
                }

                if (settings.ContainsKey("bottomMargin") && settings["bottomMargin"] != null)
                {
                    _bottomMargin = Convert.ToInt32(settings["bottomMargin"]);
                }

                // Dynamically calculate height so long receipts don't get cut off by default 11-inch page height
                int estimatedHeight = 450; // Header + Footer approximate height
                if (_sale.Items != null)
                {
                    estimatedHeight += (_sale.Items.Count * 15) + 50;
                }
                estimatedHeight += _bottomMargin;
                
                int paperWidth = pd.DefaultPageSettings.PaperSize.Width;
                if (paperWidth < 100) paperWidth = 284; // fallback for 80mm
                
                pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Custom", paperWidth, estimatedHeight);

                pd.PrintPage += Pd_PrintPage;
                pd.Print();
                
                Wpf.Ui.Controls.MessageBox mb = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Success",
                    Content = "Receipt sent to printer.",
                    CloseButtonText = "OK"
                };
                await mb.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to print: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font titleFont = new Font("Arial", 12, System.Drawing.FontStyle.Bold);
            Font regularFont = new Font("Arial", 9, System.Drawing.FontStyle.Regular);
            Font boldFont = new Font("Arial", 9, System.Drawing.FontStyle.Bold);
            
            float yPos = 10;
            float leftMargin = 5;
            float rightMargin = 275;
            
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            StringFormat rightFormat = new StringFormat { Alignment = StringAlignment.Far };

            g.DrawString("BARODA COMPUTERS & SALES", titleFont, Brushes.Black, new RectangleF(leftMargin, yPos, rightMargin, 20), centerFormat);
            yPos += 20;
            g.DrawString("Singra, Natore\nMobile: +880 1714-637798", regularFont, Brushes.Black, new RectangleF(leftMargin, yPos, rightMargin, 30), centerFormat);
            yPos += 35;
            
            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            g.DrawString($"Sale ID: {_sale.Id}", regularFont, Brushes.Black, leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Date: {_sale.DateValue.ToString("MMM dd, yyyy HH:mm")}", regularFont, Brushes.Black, leftMargin, yPos);
            yPos += 15;
            if (_sale.Customer != null && _sale.Customer.ContainsKey("name"))
            {
                g.DrawString($"Customer: {_sale.Customer["name"]}", regularFont, Brushes.Black, leftMargin, yPos);
                yPos += 15;
            }
            
            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            g.DrawString("Item", boldFont, Brushes.Black, leftMargin, yPos);
            g.DrawString("Qty", boldFont, Brushes.Black, leftMargin + 130, yPos);
            g.DrawString("Total", boldFont, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;

            if (_sale.Items != null)
            {
                foreach (var item in _sale.Items)
                {
                    string itemName = item.Name;
                    if (itemName.Length > 20) itemName = itemName.Substring(0, 20) + "..";
                    
                    g.DrawString(itemName, regularFont, Brushes.Black, leftMargin, yPos);
                    g.DrawString(item.Quantity.ToString(), regularFont, Brushes.Black, leftMargin + 130, yPos);
                    g.DrawString(item.Total.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                    yPos += 15;
                }
            }

            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            g.DrawString("Subtotal:", regularFont, Brushes.Black, leftMargin, yPos);
            g.DrawString(_sale.Subtotal.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 15;

            if (_sale.Discount > 0)
            {
                g.DrawString("Discount:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("-" + _sale.Discount.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }
            if (_sale.Tax > 0)
            {
                g.DrawString("Tax:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("+" + _sale.Tax.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }
            if (_sale.Shipping > 0)
            {
                g.DrawString("Shipping:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("+" + _sale.Shipping.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }
            if (_sale.Carry > 0)
            {
                g.DrawString("Carry Charge:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("+" + _sale.Carry.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }
            if (_sale.Maintenance > 0)
            {
                g.DrawString("Maintenance:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("+" + _sale.Maintenance.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }
            if (_sale.PointsUsed > 0)
            {
                g.DrawString("Points Used:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("-" + _sale.PointsUsed.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }
            if (_sale.PointsEarned > 0)
            {
                g.DrawString("Points Earned:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString("+" + _sale.PointsEarned.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }

            g.DrawString("Grand Total:", boldFont, Brushes.Black, leftMargin, yPos);
            g.DrawString(_sale.TotalAmount.ToString("N2"), boldFont, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 20;
            
            g.DrawString("Paid:", regularFont, Brushes.Black, leftMargin, yPos);
            g.DrawString(_sale.PaidAmount.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
            yPos += 15;

            if (_sale.DueAmount > 0)
            {
                g.DrawString("Due:", regularFont, Brushes.Black, leftMargin, yPos);
                g.DrawString(_sale.DueAmount.ToString("N2"), regularFont, Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 15;
            }

            yPos += 10;
            g.DrawLine(Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 10;
            
            g.DrawString("Thank you for shopping!", regularFont, Brushes.Black, new RectangleF(leftMargin, yPos, rightMargin, 20), centerFormat);
            
            // Bottom Padding for auto-cutter / bottleneck
            yPos += _bottomMargin;
            g.DrawString(".", regularFont, Brushes.White, leftMargin, yPos);
        }
    }
}
