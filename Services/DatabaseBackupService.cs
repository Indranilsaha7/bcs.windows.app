using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace BarodaPOS.Services
{
    public class DatabaseBackupService
    {
        private static readonly string BackupFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarodaPOS", "LocalDb", "full_backup.json");
        private readonly FirestoreService _firestoreService;

        public DatabaseBackupService(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
            
            var dir = Path.GetDirectoryName(BackupFilePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public async Task PerformDailyBackupAsync()
        {
            var lastBackup = GetLastBackupTime();
            if (DateTime.Now.Date > lastBackup.Date)
            {
                await ExportDatabaseToJsonAsync(BackupFilePath);
                SaveLastBackupTime(DateTime.Now);
            }
        }

        public async Task<bool> ExportDatabaseToJsonAsync(string filePath)
        {
            try
            {
                if (!NetworkStateMonitor.Instance.IsOnline)
                {
                    return false; // Cannot backup if offline
                }

                var db = _firestoreService.GetDb();
                var fullDb = new Dictionary<string, object>();

                string[] collectionsToBackup = { "products", "sales", "purchases", "calendar_events", "tasks", "users", "suppliers", "customers" };

                foreach (var colName in collectionsToBackup)
                {
                    var colRef = db.Collection(colName);
                    var snapshot = await colRef.GetSnapshotAsync();
                    
                    var documents = new Dictionary<string, object>();
                    foreach (var doc in snapshot.Documents)
                    {
                        if (doc.Exists)
                        {
                            documents[doc.Id] = doc.ToDictionary();
                        }
                    }
                    fullDb[colName] = documents;
                }

                string json = JsonSerializer.Serialize(fullDb, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                
                // Keep local working copies fresh
                await DistributeToLocalCollectionsAsync(fullDb);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Backup failed: " + ex.Message);
                return false;
            }
        }

        private async Task DistributeToLocalCollectionsAsync(Dictionary<string, object> fullDb)
        {
            var baseDir = Path.GetDirectoryName(BackupFilePath);
            foreach (var kvp in fullDb)
            {
                string collectionName = kvp.Key;
                string json = JsonSerializer.Serialize(kvp.Value, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(baseDir, $"{collectionName}.json"), json);
            }
        }

        private DateTime GetLastBackupTime()
        {
            string timeFile = Path.Combine(Path.GetDirectoryName(BackupFilePath), "last_backup.txt");
            if (File.Exists(timeFile) && DateTime.TryParse(File.ReadAllText(timeFile), out DateTime time))
            {
                return time;
            }
            return DateTime.MinValue;
        }

        private void SaveLastBackupTime(DateTime time)
        {
            string timeFile = Path.Combine(Path.GetDirectoryName(BackupFilePath), "last_backup.txt");
            File.WriteAllText(timeFile, time.ToString("o"));
        }
    }
}
