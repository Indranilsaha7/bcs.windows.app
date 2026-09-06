using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BarodaPOS.Models;
using Google.Cloud.Firestore;

namespace BarodaPOS.Services
{
    public class PurchaseService
    {
        private readonly FirestoreService _firestore;
        private readonly NetworkSyncManager _syncManager;
        private readonly string _localDbPath;
        private readonly string _queuePath;

        public PurchaseService(FirestoreService firestore, NetworkSyncManager syncManager)
        {
            _firestore = firestore;
            _syncManager = syncManager;
            
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb");
            Directory.CreateDirectory(baseDir);
            _localDbPath = Path.Combine(baseDir, "purchases.json");
            _queuePath = Path.Combine(baseDir, "offline_queue.json");
        }

        public async Task<List<Purchase>> GetAllPurchasesAsync()
        {
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try 
                {
                    var db = _firestore.GetDb();
                    var snapshot = await db.Collection("purchases").GetSnapshotAsync();
                    var purchases = new List<Purchase>();
                    foreach (var doc in snapshot.Documents)
                    {
                        if (doc.Exists)
                        {
                            var purchase = doc.ConvertTo<Purchase>();
                            purchase.Id = doc.Id;
                            purchases.Add(purchase);
                        }
                    }
                    
                    var dict = purchases.ToDictionary(p => p.Id, p => p);
                    await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
                    
                    return purchases;
                }
                catch { }
            }
            
            if (!File.Exists(_localDbPath)) return new List<Purchase>();
            string json = await File.ReadAllTextAsync(_localDbPath);
            var dictLocal = JsonSerializer.Deserialize<Dictionary<string, Purchase>>(json);
            var purchasesLocal = new List<Purchase>();
            if (dictLocal != null)
            {
                foreach (var kvp in dictLocal)
                {
                    var purchase = kvp.Value;
                    purchase.Id = kvp.Key;
                    purchasesLocal.Add(purchase);
                }
            }
            return purchasesLocal;
        }

        public async Task SavePurchaseAsync(Purchase purchase)
        {
            if (string.IsNullOrEmpty(purchase.Id))
                purchase.Id = Guid.NewGuid().ToString("N");
            
            var dict = new Dictionary<string, Purchase>();
            if (File.Exists(_localDbPath))
            {
                string json = await File.ReadAllTextAsync(_localDbPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, Purchase>>(json) ?? dict;
            }
            dict[purchase.Id] = purchase;
            await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));

            bool success = false;
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try
                {
                    var db = _firestore.GetDb();
                    await db.Collection("purchases").Document(purchase.Id).SetAsync(purchase, SetOptions.MergeAll);
                    success = true;
                }
                catch { }
            }

            if (!success)
            {
                await EnqueueMutationAsync("purchases", "Set", purchase.Id, purchase);
            }
            
            await _syncManager.SendSyncPayloadAsync(new { Action = "SyncCollection", Collection = "purchases", Data = dict });
        }

        public async Task DeletePurchaseAsync(string purchaseId)
        {
            var dict = new Dictionary<string, Purchase>();
            if (File.Exists(_localDbPath))
            {
                string json = await File.ReadAllTextAsync(_localDbPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, Purchase>>(json) ?? dict;
            }
            
            if (dict.Remove(purchaseId))
            {
                await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }

            bool success = false;
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try
                {
                    var db = _firestore.GetDb();
                    await db.Collection("purchases").Document(purchaseId).DeleteAsync();
                    success = true;
                }
                catch { }
            }

            if (!success)
            {
                await EnqueueMutationAsync("purchases", "Delete", purchaseId, null);
            }
            
            await _syncManager.SendSyncPayloadAsync(new { Action = "SyncCollection", Collection = "purchases", Data = dict });
        }

        private async Task EnqueueMutationAsync(string collection, string action, string docId, object data)
        {
            var queue = new List<Dictionary<string, object>>();
            if (File.Exists(_queuePath))
            {
                string json = await File.ReadAllTextAsync(_queuePath);
                queue = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json) ?? queue;
            }
            
            queue.Add(new Dictionary<string, object>
            {
                { "collection", collection },
                { "action", action },
                { "docId", docId },
                { "data", data != null ? JsonSerializer.Serialize(data) : null },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            });
            
            await File.WriteAllTextAsync(_queuePath, JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}

