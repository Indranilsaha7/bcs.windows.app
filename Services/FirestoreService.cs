using Google.Cloud.Firestore;
using System;
using System.IO;

namespace BarodaPOS.Services
{
    public class FirestoreService
    {
        private FirestoreDb _db;
        private string _projectId;

        public FirestoreService()
        {
            // Set up Google Application Credentials for Firestore
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-config.json");
            
            // Fallback for when running via 'dotnet run' where BaseDirectory might not have the file if not copied
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Environment.CurrentDirectory, "firebase-config.json");
            }

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Could not find Firebase config file at {configPath}");
            }

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", configPath);

            string json = File.ReadAllText(configPath);
            _projectId = ExtractProjectId(json);

            _db = FirestoreDb.Create(_projectId);
        }

        public FirestoreDb GetDb()
        {
            return _db;
        }

        public async Task<string> CallCloudFunctionAsync(string functionName, object payload, string region = "asia-south1")
        {
            if (string.IsNullOrEmpty(_projectId)) return null;

            string url = $"https://{region}-{_projectId}.cloudfunctions.net/{functionName}";

            using (var client = new System.Net.Http.HttpClient())
            {
                var content = new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { data = payload }), 
                    System.Text.Encoding.UTF8, 
                    "application/json");

                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<bool> TriggerEmailAsync(string to, string subject, string body)
        {
            // Write to the mail_queue collection to activate the processMailQueue trigger in Firestore
            var mailData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "status", "pending" },
                { "to_group", "specific" },
                { "to_emails", new[] { to } },
                { "message", new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "subject", subject },
                        { "text", body },
                        { "html", $"<p>{body.Replace(Environment.NewLine, "<br/>")}</p>" }
                    }
                },
                { "createdAt", FieldValue.ServerTimestamp }
            };

            await _db.Collection("mail_queue").AddAsync(mailData);
            return true;
        }

        public async Task<bool> TriggerAdminEmailAsync(string subject, string htmlBody)
        {
            var settings = await GetSettingsAsync();
            var adminEmails = new System.Collections.Generic.List<string>();

            if (settings.ContainsKey("adminEmails") && settings["adminEmails"] is System.Collections.Generic.IEnumerable<object> emailsObj)
            {
                foreach (var email in emailsObj)
                {
                    adminEmails.Add(email.ToString());
                }
            }
            else if (settings.ContainsKey("adminEmail"))
            {
                adminEmails.Add(settings["adminEmail"].ToString());
            }

            if (adminEmails.Count == 0)
            {
                return false; // No admins configured
            }

            var mailData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "status", "pending" },
                { "to_group", "specific" },
                { "to_emails", adminEmails },
                { "message", new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "subject", subject },
                        { "text", "Please view this email in an HTML-compatible client." },
                        { "html", htmlBody }
                    }
                },
                { "createdAt", FieldValue.ServerTimestamp }
            };

            await _db.Collection("mail_queue").AddAsync(mailData);
            return true;
        }

        public async Task<System.Collections.Generic.Dictionary<string, object>> GetSettingsAsync()
        {
            var docRef = _db.Collection("settings").Document("notifications");
            var snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                return snapshot.ToDictionary();
            }
            return new System.Collections.Generic.Dictionary<string, object>();
        }

        public async Task UpdateSettingsAsync(System.Collections.Generic.Dictionary<string, object> settings)
        {
            var docRef = _db.Collection("settings").Document("notifications");
            await docRef.SetAsync(settings, SetOptions.MergeAll);
        }

        private string ExtractProjectId(string json)
        {
            // Simple string manipulation to get project_id from json without loading NewtonSoft.Json
            int idx = json.IndexOf("\"project_id\"");
            if (idx == -1) return "baroda-pos"; // fallback
            int colonIdx = json.IndexOf(":", idx);
            int startQuote = json.IndexOf("\"", colonIdx);
            int endQuote = json.IndexOf("\"", startQuote + 1);
            return json.Substring(startQuote + 1, endQuote - startQuote - 1);
        }
    }
}
