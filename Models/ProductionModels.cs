using System;
using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public class ProductionRecord
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("date")]
        public object Date { get; set; } // ServerTimestamp

        [FirestoreProperty("finishedProductId")]
        public string FinishedProductId { get; set; }

        [FirestoreProperty("finishedProductName")]
        public string FinishedProductName { get; set; }

        [FirestoreProperty("quantityProduced")]
        public double QuantityProduced { get; set; }

        [FirestoreProperty("batchNumber")]
        public string BatchNumber { get; set; }

        [FirestoreProperty("note")]
        public string Note { get; set; }

        [FirestoreProperty("unitCost")]
        public double UnitCost { get; set; }

        [FirestoreProperty("totalCost")]
        public double TotalCost { get; set; }

        [FirestoreProperty("producedBy")]
        public Dictionary<string, object> ProducedBy { get; set; }

        [FirestoreProperty("rawMaterials")]
        public List<ProductionRawMaterial> RawMaterials { get; set; } = new();
    }

    [FirestoreData]
    public class ProductionRawMaterial
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("cost")]
        public double Cost { get; set; }

        [FirestoreProperty("qtyPerUnit")]
        public double QtyPerUnit { get; set; }

        [FirestoreProperty("quantity")]
        public double Quantity { get; set; } // Total consumed
    }
}
