using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using BarodaPOS.Services;
using Google.Cloud.Firestore;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace BarodaPOS.ViewModels
{
    public class TopProductModel { public string Name { get; set; } public string Category { get; set; } public double UnitsSold { get; set; } }

    public class SaleReturnData
    {
        public double Refunded { get; set; } = 0;
        public bool HasItems { get; set; } = false;
        public Dictionary<string, double> ReturnedItems { get; set; } = new Dictionary<string, double>();
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;

        // Chart Properties
        [ObservableProperty] private ISeries[] _categorySeries;
        [ObservableProperty] private ISeries[] _stockSeries;
        [ObservableProperty] private ISeries[] _thirtyDaySalesSeries;
        [ObservableProperty] private Axis[] _thirtyDayXAxes;
        [ObservableProperty] private Axis[] _thirtyDayYAxes;


                [ObservableProperty] private ObservableCollection<TopProductModel> _topProducts = new ObservableCollection<TopProductModel>();

        [ObservableProperty] private ISeries[] _thirtyDaySaleDueSeries;
        [ObservableProperty] private ISeries[] _thirtyDaySaleReturnSeries;
        [ObservableProperty] private ISeries[] _thirtyDayPurchaseSeries;
        [ObservableProperty] private ISeries[] _thirtyDayPurchaseDueSeries;
        [ObservableProperty] private ISeries[] _thirtyDayPurchaseReturnSeries;

        [ObservableProperty] private ISeries[] _financialYearSalesSeries;
        [ObservableProperty] private ISeries[] _financialYearSaleDueSeries;
        [ObservableProperty] private ISeries[] _financialYearPurchaseSeries;
        [ObservableProperty] private ISeries[] _financialYearPurchaseDueSeries;
        [ObservableProperty] private ISeries[] _financialYearSellReturnSeries;
        [ObservableProperty] private ISeries[] _financialYearPurchaseReturnSeries;

        [ObservableProperty] private Axis[] _financialYearXAxes;
        [ObservableProperty] private Axis[] _financialYearYAxes;

        [ObservableProperty] private string _totalRevenue = "Loading...";
        [ObservableProperty] private string _totalPurchase = "Loading...";
        [ObservableProperty] private string _totalSalesCount = "Loading...";
        [ObservableProperty] private string _netProfit = "Loading...";
        [ObservableProperty] private string _totalPurchaseDue = "Loading...";
        [ObservableProperty] private string _totalSaleDue = "Loading...";
        [ObservableProperty] private string _totalOrders = "Loading...";
        [ObservableProperty] private string _totalSaleReturn = "Loading...";
        [ObservableProperty] private string _totalPurchaseReturn = "Loading...";
        [ObservableProperty] private string _totalProducts = "Loading...";
        [ObservableProperty] private string _inStockProducts = "Loading...";
        [ObservableProperty] private string _outOfStockProducts = "Loading...";
        [ObservableProperty] private string _categoriesCount = "Loading...";
        [ObservableProperty] private string _alertStockProducts = "Loading...";
        [ObservableProperty] private string _totalCustomers = "Loading...";
        [ObservableProperty] private string _totalSuppliers = "Loading...";
        [ObservableProperty] private string _totalEmployees = "Loading...";

        public DashboardViewModel()
        {
            _firestoreService = new FirestoreService();
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var db = _firestoreService.GetDb();
                bool isDark = false;
                App.Current.Dispatcher.Invoke(() => {
                    isDark = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;
                });

                await Task.Run(async () => 
                {
                    // 1. Fetch Products for Cost Calculation and Stats
                var productsSnap = await db.Collection("products").GetSnapshotAsync();
                int inStock = 0, alertStock = 0, outOfStock = 0;
                var categories = new HashSet<string>();
                var productsDict = new Dictionary<string, double>(); // productId -> buyPrice
                var productTiersDict = new Dictionary<string, Dictionary<string, double>>(); // productId -> unit -> buyPrice

                foreach (var doc in productsSnap.Documents)
                {
                    int qty = doc.ContainsField("Quantity") ? doc.GetValue<int>("Quantity") : 
                             (doc.ContainsField("Stock") ? doc.GetValue<int>("Stock") : 0);
                    int alert = doc.ContainsField("alertQuantity") ? doc.GetValue<int>("alertQuantity") : 
                               (doc.ContainsField("AlertQuantity") ? doc.GetValue<int>("AlertQuantity") : 0);
                    
                    if (qty <= 0) outOfStock++;
                    else if (qty <= alert) alertStock++;
                    else inStock++;

                    if (doc.ContainsField("category")) categories.Add(doc.GetValue<string>("category"));
                    else if (doc.ContainsField("Category")) categories.Add(doc.GetValue<string>("Category"));

                    double buyPrice = 0;
                    if (doc.ContainsField("buyPrice"))
                        buyPrice = Convert.ToDouble(doc.GetValue<object>("buyPrice") ?? 0);
                    
                    productsDict[doc.Id] = buyPrice;

                    // Tiers parsing if needed (assuming simple structure if present)
                    if (doc.ContainsField("priceTiers"))
                    {
                        var tiersMap = doc.GetValue<Dictionary<string, object>>("priceTiers");
                        if (tiersMap != null)
                        {
                            var unitDict = new Dictionary<string, double>();
                            foreach (var kvp in tiersMap)
                            {
                                if (kvp.Value is Dictionary<string, object> tierData && tierData.ContainsKey("buy"))
                                {
                                    unitDict[kvp.Key.ToLower()] = Convert.ToDouble(tierData["buy"]);
                                }
                            }
                            productTiersDict[doc.Id] = unitDict;
                        }
                    }
                }
                
                TotalProducts = productsSnap.Documents.Count.ToString();
                InStockProducts = inStock.ToString();
                OutOfStockProducts = outOfStock.ToString();
                AlertStockProducts = alertStock.ToString();
                CategoriesCount = categories.Count.ToString();

                // 2. Fetch Sell Returns to build the returnsMap
                var srSnap = await db.Collection("sellReturns").GetSnapshotAsync();
                var returnsMap = new Dictionary<string, SaleReturnData>();
                double totalSaleReturnAmt = 0;

                foreach (var doc in srSnap.Documents)
                {
                    string originalSaleId = doc.ContainsField("originalSaleId") ? doc.GetValue<string>("originalSaleId") : "";
                    double amountRefunded = doc.ContainsField("amountRefunded") ? Convert.ToDouble(doc.GetValue<object>("amountRefunded")) : 0;
                    totalSaleReturnAmt += amountRefunded;

                    if (!string.IsNullOrEmpty(originalSaleId))
                    {
                        if (!returnsMap.ContainsKey(originalSaleId))
                            returnsMap[originalSaleId] = new SaleReturnData();

                        returnsMap[originalSaleId].Refunded += amountRefunded;

                        if (doc.ContainsField("items"))
                        {
                            var itemsList = doc.GetValue<List<object>>("items");
                            if (itemsList != null && itemsList.Count > 0)
                            {
                                returnsMap[originalSaleId].HasItems = true;
                                foreach (var itemObj in itemsList)
                                {
                                    if (itemObj is Dictionary<string, object> itemDict)
                                    {
                                        string pid = "";
                                        if (itemDict.ContainsKey("productId")) pid = itemDict["productId"]?.ToString();
                                        else if (itemDict.ContainsKey("firestoreId")) pid = itemDict["firestoreId"]?.ToString();
                                        else if (itemDict.ContainsKey("id")) pid = itemDict["id"]?.ToString();

                                        double rQty = itemDict.ContainsKey("quantity") ? Convert.ToDouble(itemDict["quantity"]) : 0;

                                        if (!string.IsNullOrEmpty(pid))
                                        {
                                            if (!returnsMap[originalSaleId].ReturnedItems.ContainsKey(pid))
                                                returnsMap[originalSaleId].ReturnedItems[pid] = 0;
                                            returnsMap[originalSaleId].ReturnedItems[pid] += rQty;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                TotalSaleReturn = $"৳ {totalSaleReturnAmt:N2}";

                // 3. Process Sales (Revenue, Due, Net Profit)
                var salesSnap = await db.Collection("sales").GetSnapshotAsync();
                double totalAdjustedRevenue = 0;
                double totalSaleDue = 0;
                double netProfit = 0;
                int ordersCount = 0;
                int validSalesCount = 0;

                foreach (var doc in salesSnap.Documents)
                {
                    string source = doc.ContainsField("source") ? doc.GetValue<string>("source") : "";
                    string paymentStatus = doc.ContainsField("paymentStatus") ? doc.GetValue<string>("paymentStatus") : "Pending";
                    
                    if (source == "order")
                    {
                        ordersCount++;
                        if (paymentStatus == "Pending" || paymentStatus == "Processing" || paymentStatus == "Shipped" || paymentStatus == "Cancelled")
                        {
                            continue; // Skip these per the original logic filter
                        }
                    }

                    validSalesCount++;
                    double totalAmount = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                    double paidAmount = doc.ContainsField("paidAmount") ? Convert.ToDouble(doc.GetValue<object>("paidAmount")) : 0;
                    
                    SaleReturnData returnData = returnsMap.ContainsKey(doc.Id) ? returnsMap[doc.Id] : null;
                    double refunded = returnData?.Refunded ?? 0;
                    
                    // Adjusted Revenue
                    double adjustedRevenue = totalAmount - refunded;
                    totalAdjustedRevenue += adjustedRevenue;

                    // Due amount (ensuring only positive remaining balances)
                    double due = totalAmount - paidAmount;
                    if (due > 0)
                    {
                        totalSaleDue += due;
                    }

                    // Cost Calculation for Net Profit
                    double saleCost = 0;
                    if (doc.ContainsField("items"))
                    {
                        var itemsList = doc.GetValue<List<object>>("items");
                        if (itemsList != null)
                        {
                            var returnedItemsPool = returnData != null ? new Dictionary<string, double>(returnData.ReturnedItems) : new Dictionary<string, double>();

                            foreach (var itemObj in itemsList)
                            {
                                if (itemObj is Dictionary<string, object> itemDict)
                                {
                                    string pid = "";
                                    if (itemDict.ContainsKey("productId")) pid = itemDict["productId"]?.ToString();
                                    else if (itemDict.ContainsKey("firestoreId")) pid = itemDict["firestoreId"]?.ToString();
                                    else if (itemDict.ContainsKey("id")) pid = itemDict["id"]?.ToString();

                                    double qty = itemDict.ContainsKey("quantity") ? Convert.ToDouble(itemDict["quantity"]) : 0;
                                    double itemBuyPrice = 0;

                                    if (itemDict.ContainsKey("buyPrice"))
                                    {
                                        itemBuyPrice = Convert.ToDouble(itemDict["buyPrice"]);
                                    }
                                    
                                    if (itemBuyPrice <= 0 && !string.IsNullOrEmpty(pid) && productsDict.ContainsKey(pid))
                                    {
                                        string unit = itemDict.ContainsKey("unit") ? itemDict["unit"]?.ToString()?.ToLower() : "pcs";
                                        if (productTiersDict.ContainsKey(pid) && productTiersDict[pid].ContainsKey(unit))
                                        {
                                            itemBuyPrice = productTiersDict[pid][unit];
                                        }
                                        else
                                        {
                                            itemBuyPrice = productsDict[pid];
                                        }
                                    }

                                    double returnedQty = 0;
                                    if (!string.IsNullOrEmpty(pid) && returnedItemsPool.ContainsKey(pid))
                                    {
                                        returnedQty = Math.Min(qty, returnedItemsPool[pid]);
                                        returnedItemsPool[pid] -= returnedQty;
                                    }

                                    double actualQty = Math.Max(0, qty - returnedQty);
                                    saleCost += (itemBuyPrice * actualQty);
                                }
                            }
                        }
                    }

                    if (returnData != null && !returnData.HasItems && totalAmount > 0)
                    {
                        double refundRatio = Math.Min(1.0, refunded / totalAmount);
                        saleCost = saleCost * (1.0 - refundRatio);
                    }

                    netProfit += (adjustedRevenue - saleCost);
                }

                TotalSalesCount = validSalesCount.ToString();
                TotalRevenue = $"৳ {totalAdjustedRevenue:N2}";
                TotalSaleDue = $"৳ {totalSaleDue:N2}";
                TotalOrders = ordersCount.ToString();
                NetProfit = $"৳ {netProfit:N2}";


                // 4. Purchases & Purchase Returns
                var purchasesSnap = await db.Collection("purchases").GetSnapshotAsync();
                double totalPur = 0, totalPDue = 0;
                foreach (var doc in purchasesSnap.Documents)
                {
                    double amt = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                    double paid = doc.ContainsField("paidAmount") ? Convert.ToDouble(doc.GetValue<object>("paidAmount")) : 0;
                    totalPur += amt;
                    double due = amt - paid;
                    if (due > 0) totalPDue += due;
                }
                TotalPurchase = $"৳ {totalPur:N2}";
                TotalPurchaseDue = $"৳ {totalPDue:N2}";

                var prSnap = await db.Collection("purchaseReturns").GetSnapshotAsync();
                double prAmt = 0;
                foreach (var doc in prSnap.Documents)
                {
                    if (doc.ContainsField("amount")) prAmt += Convert.ToDouble(doc.GetValue<object>("amount"));
                }
                TotalPurchaseReturn = $"৳ {prAmt:N2}";

                // 5. People
                var custSnap = await db.Collection("customers").GetSnapshotAsync();
                TotalCustomers = custSnap.Documents.Count.ToString();

                var supSnap = await db.Collection("suppliers").GetSnapshotAsync();
                TotalSuppliers = supSnap.Documents.Count.ToString();

                var empSnap = await db.Collection("employees").GetSnapshotAsync();
                TotalEmployees = empSnap.Documents.Count.ToString();
                                // --- CHARTS LOGIC ---

                var textColor = isDark ? SKColor.Parse("#adb5bd") : SKColor.Parse("#495057");
                var textPaint = new SolidColorPaint(textColor);
                var chartColors = new[] { 
                    SKColor.Parse("#6366f1"), SKColor.Parse("#8b5cf6"), SKColor.Parse("#14b8a6"), 
                    SKColor.Parse("#f59e0b"), SKColor.Parse("#10b981"), SKColor.Parse("#3b82f6"), SKColor.Parse("#ef4444") 
                };

                // 1. Category Series
                var categoryCounts = new Dictionary<string, int>();
                foreach(var cat in categories) categoryCounts[cat] = 0;
                foreach(var doc in productsSnap.Documents) {
                    string cat = "Uncategorized";
                    if (doc.ContainsField("category")) cat = doc.GetValue<string>("category");
                    else if (doc.ContainsField("Category")) cat = doc.GetValue<string>("Category");
                    if(string.IsNullOrEmpty(cat)) cat = "Uncategorized";
                    
                    if(!categoryCounts.ContainsKey(cat)) categoryCounts[cat] = 0;
                    categoryCounts[cat]++;
                }
                
                var catSeries = new List<ISeries>();
                int colorIdx = 0;
                foreach(var kvp in categoryCounts) {
                    if(kvp.Value > 0) {
                        catSeries.Add(new PieSeries<int> { 
                            Values = new[] { kvp.Value }, 
                            Name = kvp.Key,
                            Fill = new SolidColorPaint(chartColors[colorIdx % chartColors.Length]),
                            InnerRadius = 50,
                            DataLabelsPaint = textPaint,
                            DataLabelsSize = 14,
                            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer
                        });
                        colorIdx++;
                    }
                }
                CategorySeries = catSeries.ToArray();

                // 2. Stock Series
                StockSeries = new ISeries[] {
                    new PieSeries<int> { 
                        Values = new[] { inStock }, 
                        Name = "In Stock", 
                        Fill = new SolidColorPaint(SKColor.Parse("#10b981")),
                        DataLabelsPaint = textPaint,
                        DataLabelsSize = 14,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer
                    },
                    new PieSeries<int> { 
                        Values = new[] { outOfStock }, 
                        Name = "Out of Stock", 
                        Fill = new SolidColorPaint(SKColor.Parse("#ef4444")),
                        DataLabelsPaint = textPaint,
                        DataLabelsSize = 14,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer
                    }
                };

                // 3. Thirty Day Sales
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var thirtyDaySalesMap = new Dictionary<string, double>();
                for (int i = 29; i >= 0; i--) {
                    thirtyDaySalesMap[DateTime.Now.AddDays(-i).ToString("MMM dd")] = 0;
                }

                foreach (var doc in salesSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= thirtyDaysAgo) {
                            string dayKey = date.ToString("MMM dd");
                            if (thirtyDaySalesMap.ContainsKey(dayKey)) {
                                double amt = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                                thirtyDaySalesMap[dayKey] += amt;
                            }
                        }
                    }
                }

                ThirtyDaySalesSeries = new ISeries[] {
                    new ColumnSeries<double> { 
                        Values = thirtyDaySalesMap.Values.ToArray(), 
                        Name = "Sales",
                        Fill = new SolidColorPaint(SKColor.Parse("#6366f1"))
                    }
                };

                ThirtyDayXAxes = new Axis[] {
                    new Axis { 
                        Labels = thirtyDaySalesMap.Keys.ToArray(),
                        LabelsPaint = textPaint,
                        TextSize = 12
                    }
                };
                
                ThirtyDayYAxes = new Axis[] {
                    new Axis { 
                        LabelsPaint = textPaint,
                        TextSize = 12
                    }
                };
// 4. Top Products Table
                var thirtyDaySaleDueMap = new Dictionary<string, double>();
                var thirtyDayPurchaseMap = new Dictionary<string, double>();
                var thirtyDayPurchaseDueMap = new Dictionary<string, double>();
                var thirtyDaySaleReturnMap = new Dictionary<string, double>();
                var thirtyDayPurchaseReturnMap = new Dictionary<string, double>();

                for (int i = 29; i >= 0; i--) {
                    string dKey = DateTime.Now.AddDays(-i).ToString("MMM dd");
                    thirtyDaySaleDueMap[dKey] = 0;
                    thirtyDayPurchaseMap[dKey] = 0;
                    thirtyDayPurchaseDueMap[dKey] = 0;
                    thirtyDaySaleReturnMap[dKey] = 0;
                    thirtyDayPurchaseReturnMap[dKey] = 0;
                }

                foreach (var doc in salesSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= thirtyDaysAgo) {
                            string dayKey = date.ToString("MMM dd");
                            if (thirtyDaySaleDueMap.ContainsKey(dayKey)) {
                                double amt = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                                double paid = doc.ContainsField("paidAmount") ? Convert.ToDouble(doc.GetValue<object>("paidAmount")) : 0;
                                double due = amt - paid;
                                if (due > 0) thirtyDaySaleDueMap[dayKey] += due;
                            }
                        }
                    }
                }

                foreach (var doc in purchasesSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= thirtyDaysAgo) {
                            string dayKey = date.ToString("MMM dd");
                            if (thirtyDayPurchaseMap.ContainsKey(dayKey)) {
                                double amt = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                                double paid = doc.ContainsField("paidAmount") ? Convert.ToDouble(doc.GetValue<object>("paidAmount")) : 0;
                                thirtyDayPurchaseMap[dayKey] += amt;
                                double due = doc.ContainsField("dueAmount") ? Convert.ToDouble(doc.GetValue<object>("dueAmount")) : (amt - paid);
                                if (due > 0) thirtyDayPurchaseDueMap[dayKey] += due;
                            }
                        }
                    }
                }

                foreach (var doc in srSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= thirtyDaysAgo) {
                            string dayKey = date.ToString("MMM dd");
                            if (thirtyDaySaleReturnMap.ContainsKey(dayKey)) {
                                double amt = doc.ContainsField("amountRefunded") ? Convert.ToDouble(doc.GetValue<object>("amountRefunded")) : 0;
                                thirtyDaySaleReturnMap[dayKey] += amt;
                            }
                        }
                    }
                }

                foreach (var doc in prSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= thirtyDaysAgo) {
                            string dayKey = date.ToString("MMM dd");
                            if (thirtyDayPurchaseReturnMap.ContainsKey(dayKey)) {
                                double amt = doc.ContainsField("amount") ? Convert.ToDouble(doc.GetValue<object>("amount")) : 0;
                                thirtyDayPurchaseReturnMap[dayKey] += amt;
                            }
                        }
                    }
                }

                ThirtyDaySaleDueSeries = new ISeries[] { new ColumnSeries<double> { Values = thirtyDaySaleDueMap.Values.ToArray(), Name = "Sale Due", Fill = new SolidColorPaint(SKColor.Parse("#f59e0b")) } };
                ThirtyDayPurchaseSeries = new ISeries[] { new ColumnSeries<double> { Values = thirtyDayPurchaseMap.Values.ToArray(), Name = "Purchase", Fill = new SolidColorPaint(SKColor.Parse("#3b82f6")) } };
                ThirtyDayPurchaseDueSeries = new ISeries[] { new ColumnSeries<double> { Values = thirtyDayPurchaseDueMap.Values.ToArray(), Name = "Purchase Due", Fill = new SolidColorPaint(SKColor.Parse("#ef4444")) } };
                ThirtyDaySaleReturnSeries = new ISeries[] { new ColumnSeries<double> { Values = thirtyDaySaleReturnMap.Values.ToArray(), Name = "Sale Return", Fill = new SolidColorPaint(SKColor.Parse("#f59e0b")) } };
                ThirtyDayPurchaseReturnSeries = new ISeries[] { new ColumnSeries<double> { Values = thirtyDayPurchaseReturnMap.Values.ToArray(), Name = "Purchase Return", Fill = new SolidColorPaint(SKColor.Parse("#14b8a6")) } };

                // Financial Year Logic
                int currentYear = DateTime.Now.Year;
                int startYear = DateTime.Now.Month < 7 ? currentYear - 1 : currentYear;
                var fyStartDate = new DateTime(startYear, 7, 1);
                var fyEndDate = new DateTime(startYear + 1, 6, 30, 23, 59, 59);

                var monthLabels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                var fyMonthLabels = new List<string>();
                for (int i = 0; i < 12; i++) {
                    fyMonthLabels.Add(monthLabels[(6 + i) % 12]);
                }

                var salesByMonth = new double[12];
                var saleDueByMonth = new double[12];
                var purchaseByMonth = new double[12];
                var purchaseDueByMonth = new double[12];
                var sellReturnByMonth = new double[12];
                var purchaseReturnByMonth = new double[12];

                foreach (var doc in salesSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= fyStartDate && date <= fyEndDate) {
                            int monthIdx = (date.Month + 5) % 12; // July is 0
                            double amt = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                            double due = doc.ContainsField("dueAmount") ? Convert.ToDouble(doc.GetValue<object>("dueAmount")) : (amt - (doc.ContainsField("paidAmount") ? Convert.ToDouble(doc.GetValue<object>("paidAmount")) : 0));
                            salesByMonth[monthIdx] += amt;
                            if (due > 0) saleDueByMonth[monthIdx] += due;
                        }
                    }
                }

                foreach (var doc in purchasesSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= fyStartDate && date <= fyEndDate) {
                            int monthIdx = (date.Month + 5) % 12;
                            double amt = doc.ContainsField("totalAmount") ? Convert.ToDouble(doc.GetValue<object>("totalAmount")) : 0;
                            double due = doc.ContainsField("dueAmount") ? Convert.ToDouble(doc.GetValue<object>("dueAmount")) : (amt - (doc.ContainsField("paidAmount") ? Convert.ToDouble(doc.GetValue<object>("paidAmount")) : 0));
                            purchaseByMonth[monthIdx] += amt;
                            if (due > 0) purchaseDueByMonth[monthIdx] += due;
                        }
                    }
                }

                foreach (var doc in srSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= fyStartDate && date <= fyEndDate) {
                            int monthIdx = (date.Month + 5) % 12;
                            double amt = doc.ContainsField("amountRefunded") ? Convert.ToDouble(doc.GetValue<object>("amountRefunded")) : 0;
                            sellReturnByMonth[monthIdx] += amt;
                        }
                    }
                }

                foreach (var doc in prSnap.Documents) {
                    if (doc.ContainsField("date")) {
                        var ts = doc.GetValue<Google.Cloud.Firestore.Timestamp>("date");
                        var date = ts.ToDateTime().ToLocalTime();
                        if (date >= fyStartDate && date <= fyEndDate) {
                            int monthIdx = (date.Month + 5) % 12;
                            double amt = doc.ContainsField("amount") ? Convert.ToDouble(doc.GetValue<object>("amount")) : 0;
                            purchaseReturnByMonth[monthIdx] += amt;
                        }
                    }
                }

                FinancialYearSalesSeries = new ISeries[] { new LineSeries<double> { Values = salesByMonth, Name = "Sales FY", Stroke = new SolidColorPaint(SKColor.Parse("#8b5cf6")) { StrokeThickness = 2 }, GeometryFill = null, GeometryStroke = null, Fill = new SolidColorPaint(SKColor.Parse("#338b5cf6")) } };
                FinancialYearSaleDueSeries = new ISeries[] { new LineSeries<double> { Values = saleDueByMonth, Name = "Sale Due FY", Stroke = new SolidColorPaint(SKColor.Parse("#f59e0b")) { StrokeThickness = 2 }, GeometryFill = null, GeometryStroke = null, Fill = new SolidColorPaint(SKColor.Parse("#33f59e0b")) } };
                FinancialYearPurchaseSeries = new ISeries[] { new LineSeries<double> { Values = purchaseByMonth, Name = "Purchase FY", Stroke = new SolidColorPaint(SKColor.Parse("#14b8a6")) { StrokeThickness = 2 }, GeometryFill = null, GeometryStroke = null, Fill = new SolidColorPaint(SKColor.Parse("#3314b8a6")) } };
                FinancialYearPurchaseDueSeries = new ISeries[] { new LineSeries<double> { Values = purchaseDueByMonth, Name = "Purchase Due FY", Stroke = new SolidColorPaint(SKColor.Parse("#3b82f6")) { StrokeThickness = 2 }, GeometryFill = null, GeometryStroke = null, Fill = new SolidColorPaint(SKColor.Parse("#333b82f6")) } };
                FinancialYearSellReturnSeries = new ISeries[] { new LineSeries<double> { Values = sellReturnByMonth, Name = "Sell Return FY", Stroke = new SolidColorPaint(SKColor.Parse("#ef4444")) { StrokeThickness = 2 }, GeometryFill = null, GeometryStroke = null, Fill = new SolidColorPaint(SKColor.Parse("#33ef4444")) } };
                FinancialYearPurchaseReturnSeries = new ISeries[] { new LineSeries<double> { Values = purchaseReturnByMonth, Name = "Purchase Return FY", Stroke = new SolidColorPaint(SKColor.Parse("#f59e0b")) { StrokeThickness = 2 }, GeometryFill = null, GeometryStroke = null, Fill = new SolidColorPaint(SKColor.Parse("#33f59e0b")) } };

                FinancialYearXAxes = new Axis[] { new Axis { Labels = fyMonthLabels.ToArray(), LabelsPaint = textPaint, TextSize = 12 } };
                FinancialYearYAxes = new Axis[] { new Axis { LabelsPaint = textPaint, TextSize = 12 } };

// 4. Top Products Table
                var productSales = new Dictionary<string, double>();
                foreach (var doc in salesSnap.Documents) {
                    if (doc.ContainsField("items")) {
                        var itemsList = doc.GetValue<List<object>>("items");
                        if (itemsList != null) {
                            foreach (var itemObj in itemsList) {
                                if (itemObj is Dictionary<string, object> itemDict) {
                                    string pid = "";
                                    if (itemDict.ContainsKey("productId")) pid = itemDict["productId"]?.ToString();
                                    else if (itemDict.ContainsKey("firestoreId")) pid = itemDict["firestoreId"]?.ToString();
                                    
                                    double qty = itemDict.ContainsKey("quantity") ? Convert.ToDouble(itemDict["quantity"]) : 0;
                                    if(!string.IsNullOrEmpty(pid)) {
                                        if(!productSales.ContainsKey(pid)) productSales[pid] = 0;
                                        productSales[pid] += qty;
                                    }
                                }
                            }
                        }
                    }
                }
                
                var top = productSales.OrderByDescending(x => x.Value).Take(5);
                App.Current.Dispatcher.Invoke(() => {
                    TopProducts.Clear();
                    foreach(var item in top) {
                        string name = "Unknown";
                        string cat = "N/A";
                        // Find product by id
                        foreach(var pdoc in productsSnap.Documents) {
                            if(pdoc.Id == item.Key) {
                                name = pdoc.ContainsField("name") ? pdoc.GetValue<string>("name") : "Unknown";
                                cat = pdoc.ContainsField("category") ? pdoc.GetValue<string>("category") : "N/A";
                                break;
                            }
                        }
                        TopProducts.Add(new TopProductModel { Name = name, Category = cat, UnitsSold = item.Value });
                    }
                });


                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }
    }
}



