using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public class Lot
    {
        [FirestoreProperty("lotNumber")]
        public string LotNumber { get; set; }

        [FirestoreProperty("quantity")]
        public int? Quantity { get; set; }

        [FirestoreProperty("buyPrice")]
        public double? BuyPrice { get; set; }
    }

    [FirestoreData]
    public class PriceTier
    {
        [FirestoreProperty("buy")]
        public double? Buy { get; set; }

        [FirestoreProperty("wholesale")]
        public double? Wholesale { get; set; }

        [FirestoreProperty("sell")]
        public double? Sell { get; set; }

        [FirestoreProperty("shop_wholesale")]
        public double? ShopWholesale { get; set; }

        [FirestoreProperty("shop_sell")]
        public double? ShopSell { get; set; }
    }

    [FirestoreData]
    public class Product
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("brand")]
        public string Brand { get; set; }

        [FirestoreProperty("barcode")]
        public string Barcode { get; set; }

        [FirestoreProperty("barcodeType")]
        public string BarcodeType { get; set; }

        [FirestoreProperty("category")]
        public string Category { get; set; }

        [FirestoreProperty("unit")]
        public string Unit { get; set; }

        [FirestoreProperty("quantity")]
        public int? Quantity { get; set; }

        [FirestoreProperty("alertQuantity")]
        public int? AlertQuantity { get; set; }

        [FirestoreProperty("buyPrice")]
        public double? BuyPrice { get; set; }

        [FirestoreProperty("wholesalePrice")]
        public double? WholesalePrice { get; set; }

        [FirestoreProperty("sellPrice")]
        public double? SellPrice { get; set; }

        [FirestoreProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; }

        [FirestoreProperty("packetPerBox")]
        public int? PacketPerBox { get; set; }

        [FirestoreProperty("pcsPerBox")]
        public int? PcsPerBox { get; set; }

        [FirestoreProperty("pcsPerPacket")]
        public int? PcsPerPacket { get; set; }

        [FirestoreProperty("createdAt")]
        public Google.Cloud.Firestore.Timestamp? CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Google.Cloud.Firestore.Timestamp? UpdatedAt { get; set; }

        [FirestoreProperty("lots")]
        public List<Lot> Lots { get; set; }

        [FirestoreProperty("priceTiers")]
        public Dictionary<string, PriceTier> PriceTiers { get; set; }

        private PriceTier GetPrimaryPriceTier()
        {
            if (PriceTiers == null || PriceTiers.Count == 0) return null;
            string u = (Unit ?? "").Trim().ToLowerInvariant();
            if (u == "box" && PriceTiers.ContainsKey("box")) return PriceTiers["box"];
            if (u == "kg" && PriceTiers.ContainsKey("kg")) return PriceTiers["kg"];
            if (u == "liter" && PriceTiers.ContainsKey("liter")) return PriceTiers["liter"];
            if (u == "packet" && PriceTiers.ContainsKey("pack")) return PriceTiers["pack"];
            if (PriceTiers.ContainsKey("pcs")) return PriceTiers["pcs"];
            if (PriceTiers.ContainsKey("box")) return PriceTiers["box"];
            if (PriceTiers.ContainsKey("kg")) return PriceTiers["kg"];
            if (PriceTiers.ContainsKey("liter")) return PriceTiers["liter"];
            if (PriceTiers.ContainsKey("pack")) return PriceTiers["pack"];
            foreach (var val in PriceTiers.Values) return val;
            return null;
        }

        // Helper properties for UI
        public double DisplayBuyPrice => GetPrimaryPriceTier()?.Buy ?? BuyPrice ?? 0;
        public double DisplayWholesalePrice => GetPrimaryPriceTier()?.Wholesale ?? WholesalePrice ?? 0;
        public double DisplaySellPrice => GetPrimaryPriceTier()?.Sell ?? SellPrice ?? 0;
        public double DisplayShopWholesalePrice => GetPrimaryPriceTier()?.ShopWholesale ?? WholesalePrice ?? 0;
        public double DisplayShopSellPrice => GetPrimaryPriceTier()?.ShopSell ?? SellPrice ?? 0;

        public List<Lot> DisplayLots
        {
            get
            {
                if (Lots != null && Lots.Count > 0) return Lots;
                return new List<Lot>
                {
                    new Lot
                    {
                        LotNumber = "Default Lot",
                        Quantity = Quantity ?? 0,
                        BuyPrice = DisplayBuyPrice
                    }
                };
            }
        }
    }
}
