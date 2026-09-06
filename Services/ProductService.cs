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
    public class ProductService
    {
        private readonly FirestoreService _firestore;
        private readonly NetworkSyncManager _syncManager;
        private readonly string _localDbPath;
        private readonly string _queuePath;

        public ProductService(FirestoreService firestore, NetworkSyncManager syncManager)
        {
            _firestore = firestore;
            _syncManager = syncManager;
            
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb");
            Directory.CreateDirectory(baseDir);
            _localDbPath = Path.Combine(baseDir, "products.json");
            _queuePath = Path.Combine(baseDir, "offline_queue.json");
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try 
                {
                    var db = _firestore.GetDb();
                    var snapshot = await db.Collection("products").GetSnapshotAsync().ConfigureAwait(false);
                    var products = new List<Product>();
                    foreach (var doc in snapshot.Documents)
                    {
                        if (doc.Exists)
                        {
                            var product = doc.ConvertTo<Product>();
                            product.Id = doc.Id;
                            products.Add(product);
                        }
                    }
                    
                    var dict = products.ToDictionary(p => p.Id, p => p);
                    await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
                    
                    return products;
                }
                catch { }
            }
            
            if (!File.Exists(_localDbPath)) return new List<Product>();
            string json = await File.ReadAllTextAsync(_localDbPath).ConfigureAwait(false);
            var dictLocal = JsonSerializer.Deserialize<Dictionary<string, Product>>(json);
            var productsLocal = new List<Product>();
            if (dictLocal != null)
            {
                foreach (var kvp in dictLocal)
                {
                    var product = kvp.Value;
                    product.Id = kvp.Key;
                    productsLocal.Add(product);
                }
            }
            return productsLocal;
        }

        public async Task SaveProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.Id))
                product.Id = Guid.NewGuid().ToString("N");
            
            var dict = new Dictionary<string, Product>();
            if (File.Exists(_localDbPath))
            {
                string json = await File.ReadAllTextAsync(_localDbPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, Product>>(json) ?? dict;
            }
            dict[product.Id] = product;
            await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));

            bool success = false;
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try
                {
                    var db = _firestore.GetDb();
                    await db.Collection("products").Document(product.Id).SetAsync(product, SetOptions.MergeAll);
                    success = true;
                }
                catch { }
            }

            if (!success)
            {
                await EnqueueMutationAsync("products", "Set", product.Id, product);
            }
            
            await _syncManager.SendSyncPayloadAsync(new { Action = "SyncCollection", Collection = "products", Data = dict });
        }

        public async Task DeleteProductAsync(string productId)
        {
            var dict = new Dictionary<string, Product>();
            if (File.Exists(_localDbPath))
            {
                string json = await File.ReadAllTextAsync(_localDbPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, Product>>(json) ?? dict;
            }
            
            if (dict.Remove(productId))
            {
                await File.WriteAllTextAsync(_localDbPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }

            bool success = false;
            if (NetworkStateMonitor.Instance.IsOnline)
            {
                try
                {
                    var db = _firestore.GetDb();
                    await db.Collection("products").Document(productId).DeleteAsync();
                    success = true;
                }
                catch { }
            }

            if (!success)
            {
                await EnqueueMutationAsync("products", "Delete", productId, null);
            }
            
            await _syncManager.SendSyncPayloadAsync(new { Action = "SyncCollection", Collection = "products", Data = dict });
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
