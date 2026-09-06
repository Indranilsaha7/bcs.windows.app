using Google.Cloud.Firestore;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public partial class SaleItem : ObservableObject
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

        [FirestoreProperty("unit")]
        public string Unit { get; set; }

        [FirestoreProperty("lotNumber")]
        public string LotNumber { get; set; }

        [FirestoreProperty("price")]
        public double Price { get; set; }

        [FirestoreProperty("buyPrice")]
        public double BuyPrice { get; set; }

        [FirestoreProperty("saleType")]
        public string SaleType { get; set; }
        
        public double Total => Quantity * Price;
    }

    [FirestoreData]
    public class Sale
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("source")]
        public string Source { get; set; }

        [FirestoreProperty("customer")]
        public Dictionary<string, object> Customer { get; set; }

        [FirestoreProperty("date")]
        public Google.Cloud.Firestore.Timestamp Date { get; set; }

        public System.DateTime DateValue => Date.ToDateTime().ToLocalTime();

        [FirestoreProperty("soldBy")]
        public Dictionary<string, object> SoldBy { get; set; }
        
        [FirestoreProperty("saleMode")]
        public string SaleMode { get; set; }

        [FirestoreProperty("items")]
        public List<SaleItem> Items { get; set; }

        [FirestoreProperty("subtotal")]
        public double Subtotal { get; set; }

        [FirestoreProperty("discount")]
        public double Discount { get; set; }

        [FirestoreProperty("tax")]
        public double Tax { get; set; }

        [FirestoreProperty("shipping")]
        public double Shipping { get; set; }

        [FirestoreProperty("carry")]
        public double Carry { get; set; }

        [FirestoreProperty("maintenance")]
        public double Maintenance { get; set; }

        [FirestoreProperty("pointsUsed")]
        public double PointsUsed { get; set; }

        [FirestoreProperty("pointsEarned")]
        public double PointsEarned { get; set; }

        [FirestoreProperty("paymentMethod")]
        public string PaymentMethod { get; set; }

        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [FirestoreProperty("paidAmount")]
        public double PaidAmount { get; set; }

        [FirestoreProperty("dueAmount")]
        public double DueAmount { get; set; }

        [FirestoreProperty("paymentStatus")]
        public string PaymentStatus { get; set; }

        [FirestoreProperty("createdAt")]
        [ServerTimestamp]
        public Google.Cloud.Firestore.Timestamp? CreatedAt { get; set; }
    }
}
