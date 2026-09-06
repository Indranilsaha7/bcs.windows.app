using System.Windows;

namespace BarodaPOS.Views
{
    public partial class SupplierDetailsWindow : Window
    {
        public SupplierDetailsWindow()
        {
            InitializeComponent();
            
            // Apply the current theme
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(this);
        }

        private int _bottomMargin = 0;

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void PrintPurchaseLog_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as BarodaPOS.ViewModels.SupplierDisplayModel;
            if (model == null) return;

            try
            {
                var pd = new System.Drawing.Printing.PrintDocument();
                pd.PrintController = new System.Drawing.Printing.StandardPrintController();
                
                var firestoreService = new BarodaPOS.Services.FirestoreService();
                var settings = await firestoreService.GetSettingsAsync();
                if (settings.ContainsKey("defaultPrinter") && settings["defaultPrinter"] != null)
                {
                    pd.PrinterSettings.PrinterName = settings["defaultPrinter"].ToString();
                }

                if (settings.ContainsKey("bottomMargin") && settings["bottomMargin"] != null)
                {
                    _bottomMargin = System.Convert.ToInt32(settings["bottomMargin"]);
                }

                // Dynamically calculate height so long logs don't get cut off by default 11-inch page height
                int estimatedHeight = 350; // Header + Footer approximate height
                if (model.Purchases != null)
                {
                    estimatedHeight += (model.Purchases.Count * 15) + 50;
                }
                estimatedHeight += _bottomMargin;
                
                int paperWidth = pd.DefaultPageSettings.PaperSize.Width;
                if (paperWidth < 100) paperWidth = 284; // fallback for 80mm
                
                pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Custom", paperWidth, estimatedHeight);

                pd.PrintPage += (s, args) => PrintPurchaseLogPage(s, args, model);
                pd.Print();
                
                Wpf.Ui.Controls.MessageBox mb = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Success",
                    Content = "Purchase log sent to printer.",
                    CloseButtonText = "OK"
                };
                await mb.ShowDialogAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Failed to print: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintPurchaseLogPage(object sender, System.Drawing.Printing.PrintPageEventArgs e, BarodaPOS.ViewModels.SupplierDisplayModel model)
        {
            var g = e.Graphics;
            var fontTitle = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold);
            var fontSubTitle = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            var fontRegular = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Regular);
            var fontSmall = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular);
            var centerFormat = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center };
            var rightFormat = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Far };

            float yPos = 10;
            float leftMargin = 10;
            float rightMargin = 270;
            float center = (leftMargin + rightMargin) / 2;

            g.DrawString("BARODA COMPUTER & SALES", fontTitle, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin, 20), centerFormat);
            yPos += 25;
            g.DrawString("Singra, Natore\nMobile: +880 1714-637798", fontRegular, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin, 30), centerFormat);
            yPos += 35;
            
            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            g.DrawString("PURCHASE LOG", fontSubTitle, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin, 20), centerFormat);
            yPos += 20;

            g.DrawString($"Supplier: {model.Name}", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
            yPos += 15;
            if (!string.IsNullOrEmpty(model.ShopName))
            {
                g.DrawString($"Shop: {model.ShopName}", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
                yPos += 15;
            }
            g.DrawString($"Date Printed: {System.DateTime.Now.ToString("dd MMM yyyy")}", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
            yPos += 15;

            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            if (model.Purchases != null && model.Purchases.Count > 0)
            {
                g.DrawString("Date", fontSubTitle, System.Drawing.Brushes.Black, leftMargin, yPos);
                g.DrawString("ID", fontSubTitle, System.Drawing.Brushes.Black, leftMargin + 80, yPos);
                g.DrawString("Amt", fontSubTitle, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
                yPos += 20;

                var sortedPurchases = System.Linq.Enumerable.OrderBy(model.Purchases, p => p.Date.ToDateTime().ToLocalTime());
                foreach (var p in sortedPurchases)
                {
                    string pDate = p.Date.ToDateTime().ToLocalTime().ToString("dd-MMM-yy");
                    string pId = p.Id != null ? (p.Id.Length > 8 ? p.Id.Substring(0,8) : p.Id) : "N/A";
                    
                    g.DrawString(pDate, fontSmall, System.Drawing.Brushes.Black, leftMargin, yPos);
                    g.DrawString(pId, fontSmall, System.Drawing.Brushes.Black, leftMargin + 80, yPos);
                    g.DrawString(p.TotalAmount.ToString("N0"), fontSmall, System.Drawing.Brushes.Black, rightMargin, yPos, rightFormat);
                    yPos += 15;
                }
            }
            else
            {
                g.DrawString("No purchase history found.", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
                yPos += 20;
            }

            g.DrawLine(System.Drawing.Pens.Black, leftMargin, yPos, rightMargin, yPos);
            yPos += 5;

            g.DrawString($"Total Purchased: {model.TotalPurchased.ToString("N2")} BDT", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Total Due: {model.TotalDue.ToString("N2")} BDT", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
            yPos += 15;
            g.DrawString($"Total Returned: {model.TotalReturned.ToString("N2")} BDT", fontRegular, System.Drawing.Brushes.Black, leftMargin, yPos);
            yPos += 25;

            g.DrawString("Thank you for your business!", fontRegular, System.Drawing.Brushes.Black, new System.Drawing.RectangleF(leftMargin, yPos, rightMargin, 20), centerFormat);
            yPos += 20;

            // Thermal paper cutter margin fix
            yPos += _bottomMargin;
            g.DrawString(".", fontRegular, System.Drawing.Brushes.White, leftMargin, yPos);

            e.HasMorePages = false;
        }
    }
}
