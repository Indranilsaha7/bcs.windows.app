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
    public class SaleService
    {
        private readonly FirestoreService _firestore;
        private readonly NetworkSyncManager _syncManager;
        private readonly string _localDbPath;
        private readonly string _queuePath;

        public SaleService(FirestoreService firestore, NetworkSyncManager syncManager)
        {
            _firestore = firestore;
            _syncManager = syncManager;
            
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb");
            Directory.CreateDirectory(baseDir);
            _localDbPath = Path.Combine(baseDir, "sales.json");
            _queuePath = Path.Combine(baseDir, "offline_queue.json");
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try 
                {
                    var db = _firestore.GetDb();
                    var snapshot = await db.Collection("sales").GetSnapshotAsync();
                    var sales = new List<Sale>();
                    foreach (var doc in snapshot.Documents)
                    {
                        if (doc.Exists)
                        {
                            var sale = doc.ConvertTo<Sale>();
                            sale.Id = doc.Id;
                            sales.Add(sale);
                        }
                    }
                    
                    var dict = sales.ToDictionary(s => s.Id, s => s);
                    await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
                    
                    return sales;
                }
                catch { }
            }
            
            if (!File.Exists(_localDbPath)) return new List<Sale>();
            string json = await File.ReadAllTextAsync(_localDbPath);
            var dictLocal = JsonSerializer.Deserialize<Dictionary<string, Sale>>(json);
            var salesLocal = new List<Sale>();
            if (dictLocal != null)
            {
                foreach (var kvp in dictLocal)
                {
                    var sale = kvp.Value;
                    sale.Id = kvp.Key;
                    salesLocal.Add(sale);
                }
            }
            return salesLocal;
        }

        public async Task SaveSaleAsync(Sale sale)
        {
            if (string.IsNullOrEmpty(sale.Id))
                sale.Id = Guid.NewGuid().ToString("N");
            
            var dict = new Dictionary<string, Sale>();
            if (File.Exists(_localDbPath))
            {
                string json = await File.ReadAllTextAsync(_localDbPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, Sale>>(json) ?? dict;
            }
            dict[sale.Id] = sale;
            await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));

            bool success = false;
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try
                {
                    var db = _firestore.GetDb();
                    await db.Collection("sales").Document(sale.Id).SetAsync(sale, SetOptions.MergeAll);
                    success = true;
                }
                catch { }
            }

            if (!success)
            {
                await EnqueueMutationAsync("sales", "Set", sale.Id, sale);
            }
            
            await _syncManager.SendSyncPayloadAsync(new { Action = "SyncCollection", Collection = "sales", Data = dict });
        }

        public async Task DeleteSaleAsync(string saleId)
        {
            var dict = new Dictionary<string, Sale>();
            if (File.Exists(_localDbPath))
            {
                string json = await File.ReadAllTextAsync(_localDbPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, Sale>>(json) ?? dict;
            }
            
            if (dict.Remove(saleId))
            {
                await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }

            bool success = false;
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try
                {
                    var db = _firestore.GetDb();
                    await db.Collection("sales").Document(saleId).DeleteAsync();
                    success = true;
                }
                catch { }
            }

            if (!success)
            {
                await EnqueueMutationAsync("sales", "Delete", saleId, null);
            }
            
            await _syncManager.SendSyncPayloadAsync(new { Action = "SyncCollection", Collection = "sales", Data = dict });
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

