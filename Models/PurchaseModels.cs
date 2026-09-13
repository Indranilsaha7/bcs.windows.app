using Google.Cloud.Firestore;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public class Supplier
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("shopName")]
        public string ShopName { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        [FirestoreProperty("address")]
        public string Address { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }
    }

    [FirestoreData]
    public partial class PurchaseItem : ObservableObject
    {
        [FirestoreProperty("productId")]
        public string ProductId { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        private int _quantity;
        [FirestoreProperty("quantity")]
        public int Quantity 
        { 
            get => _quantity; 
            set 
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        [FirestoreProperty("cost")]
        public double Cost { get; set; }

        [FirestoreProperty("purchaseUnit")]
        public string PurchaseUnit { get; set; }

        [FirestoreProperty("lotNumber")]
        public string LotNumber { get; set; }

        private string _targetSupplierId;
        [FirestoreProperty("targetSupplierId")]
        public string TargetSupplierId 
        {
            get => _targetSupplierId;
            set => SetProperty(ref _targetSupplierId, value);
        }

        private string _targetSupplierName;
        [FirestoreProperty("targetSupplierName")]
        public string TargetSupplierName 
        {
            get => _targetSupplierName;
            set => SetProperty(ref _targetSupplierName, value);
        }
        
        // Not saved to Firestore in Purchase document, but used locally during the transaction
        public Dictionary<string, PriceTier> NewPriceTiers { get; set; }
        
        public double Total => Quantity * Cost;
    }

    [FirestoreData]
    public class Purchase
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("supplier")]
        public string Supplier { get; set; }

        [FirestoreProperty("date")]
        public Google.Cloud.Firestore.Timestamp Date { get; set; }

        [FirestoreProperty("purchasedBy")]
        public Dictionary<string, object> PurchasedBy { get; set; }

        [FirestoreProperty("items")]
        public List<PurchaseItem> Items { get; set; }

        [FirestoreProperty("subtotal")]
        public double Subtotal { get; set; }

        [FirestoreProperty("discount")]
        public double Discount { get; set; }

        [FirestoreProperty("tax")]
        public double Tax { get; set; }

        [FirestoreProperty("shipping")]
        public double Shipping { get; set; }

        [FirestoreProperty("carryCharge")]
        public double CarryCharge { get; set; }

        [FirestoreProperty("maintenance")]
        public double Maintenance { get; set; }

        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [FirestoreProperty("paidAmount")]
        public double PaidAmount { get; set; }

        [FirestoreProperty("dueAmount")]
        public double DueAmount { get; set; }

        [FirestoreProperty("createdAt")]
        [ServerTimestamp]
        public Google.Cloud.Firestore.Timestamp? CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        [ServerTimestamp]
        public Google.Cloud.Firestore.Timestamp? UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class PurchaseReturnItem : ObservableObject
    {
        [FirestoreProperty("productId")]
        public string ProductId { get; set; }

        private int _quantity;

        [FirestoreProperty("quantity")]
        public int Quantity 
        {
            get => _quantity;
            set 
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        [FirestoreProperty("cost")]
        public double Cost { get; set; }
        
        public double Total => Quantity * Cost;
    }

    [FirestoreData]
    public class PurchaseReturn
    {
        [FirestoreDocumentId]
        public string Id { get; set; }
        
        [FirestoreProperty("originalPurchaseId")]
        public string OriginalPurchaseId { get; set; }

        [FirestoreProperty("items")]
        public List<PurchaseReturnItem> Items { get; set; }

        [FirestoreProperty("amount")]
        public double Amount { get; set; }

        [FirestoreProperty("date")]
        public Google.Cloud.Firestore.Timestamp Date { get; set; }
        
        [FirestoreProperty("notes")]
        public string Notes { get; set; }

        [FirestoreProperty("processedBy")]
        public Dictionary<string, object> ProcessedBy { get; set; }

        [FirestoreProperty("createdAt")]
        [ServerTimestamp]
        public Google.Cloud.Firestore.Timestamp? CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        [ServerTimestamp]
        public Google.Cloud.Firestore.Timestamp? UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class PrePurchase
    {
        [FirestoreDocumentId]
        public string Id { get; set; }
        
        [FirestoreProperty("supplierId")]
        public string SupplierId { get; set; }
        
        [FirestoreProperty("supplierName")]
        public string SupplierName { get; set; }

        [FirestoreProperty("date")]
        public Timestamp Date { get; set; }

        [FirestoreProperty("items")]
        public List<PurchaseItem> Items { get; set; }
        
        [FirestoreProperty("status")]
        public string Status { get; set; } // "Pending", "Completed"
        
        [FirestoreProperty("totalEstimatedAmount")]
        public double TotalEstimatedAmount { get; set; }
    }
}
