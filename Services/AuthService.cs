using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;
using Google.Cloud.Firestore;
using BarodaPOS.Models;
using System.Security.Cryptography;

namespace BarodaPOS.Services
{
    public class AuthService
    {
        private readonly FirestoreService _firestoreService;

        public AuthService(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public async Task<bool> LoginAsync(string username, string password, string code)
        {
            try
            {
                var db = _firestoreService.GetDb();
                var usersCollection = db.Collection("users");
                var query = usersCollection.WhereEqualTo("Username", username);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    // Try "name" instead of "Username"
                    query = usersCollection.WhereEqualTo("name", username);
                    snapshot = await query.GetSnapshotAsync();
                    
                    if (snapshot.Documents.Count == 0)
                        return false; // User not found
                }

                var userDoc = snapshot.Documents.First();
                var user = userDoc.ConvertTo<User>();

                // Check stored values from either schema
                string storedPass = !string.IsNullOrEmpty(user.PasswordHash) ? user.PasswordHash : user.Password;
                string storedCode = !string.IsNullOrEmpty(user.CodeHash) ? user.CodeHash : user.Code;

                // Smart Authentication: Check if stored password/code is plain text and needs hashing
                bool needsPasswordUpdate = false;
                bool isPasswordValid = false;
                
                if (storedPass == password)
                {
                    isPasswordValid = true;
                    needsPasswordUpdate = true;
                }
                else
                {
                    isPasswordValid = VerifyHash(password, storedPass);
                }

                if (!isPasswordValid) return false;

                bool needsCodeUpdate = false;
                bool isCodeValid = false;

                if (storedCode == code)
                {
                    isCodeValid = true;
                    needsCodeUpdate = true;
                }
                else
                {
                    isCodeValid = VerifyHash(code, storedCode);
                }

                if (!isCodeValid) return false;

                // Update database if plain text was found to secure it with Argon2id
                if (needsPasswordUpdate || needsCodeUpdate)
                {
                    var updates = new System.Collections.Generic.Dictionary<string, object>();
                    if (needsPasswordUpdate)
                        updates["PasswordHash"] = HashString(password);
                    if (needsCodeUpdate)
                        updates["CodeHash"] = HashString(code);
                    
                    await userDoc.Reference.UpdateAsync(updates);
                }

                return true;
            }
            catch (Exception ex)
            {
                // In a real app we'd log this exception
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task SeedAdminUserAsync()
        {
            try
            {
                var db = _firestoreService.GetDb();
                var usersCollection = db.Collection("users");
                var snapshot = await usersCollection.WhereEqualTo("Username", "admin").GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    var user = new User
                    {
                        Username = "admin",
                        PasswordHash = HashString("admin"),
                        CodeHash = HashString("1234"),
                        Role = "Admin"
                    };
                    
                    await usersCollection.AddAsync(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error seeding admin: " + ex.Message);
            }
        }

        // Helper method to hash a plain text string using Argon2id
        public string HashString(string plainText, byte[] salt = null)
        {
            if (salt == null)
            {
                salt = CreateSalt();
            }

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(plainText));
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8; // 8 cores
            argon2.MemorySize = 65536; // 64 MB
            argon2.Iterations = 4;

            var hash = argon2.GetBytes(16);
            
            // Format: salt:hash (Base64)
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        // Helper method to verify a plain text string against a stored hash
        public bool VerifyHash(string plainText, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            // Support standard PHC format like $argon2d$v=19$m=1024,t=1,p=1$SALT$HASH
            if (storedHash.StartsWith("$argon2"))
            {
                var parts = storedHash.Split('$');
                if (parts.Length < 6) return false;

                string type = parts[1];
                var paramsDict = parts[3].Split(',')
                    .Select(p => p.Split('='))
                    .ToDictionary(p => p[0], p => int.Parse(p[1]));

                int memorySize = paramsDict.ContainsKey("m") ? paramsDict["m"] : 65536;
                int iterations = paramsDict.ContainsKey("t") ? paramsDict["t"] : 4;
                int parallelism = paramsDict.ContainsKey("p") ? paramsDict["p"] : 8;

                // Standard Base64 might not have padding (=), so add it
                string PadBase64(string b64) => b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');

                byte[] salt = Convert.FromBase64String(PadBase64(parts[4]));
                byte[] expectedHash = Convert.FromBase64String(PadBase64(parts[5]));

                Argon2 argon2;
                if (type == "argon2d") argon2 = new Argon2d(Encoding.UTF8.GetBytes(plainText));
                else if (type == "argon2id") argon2 = new Argon2id(Encoding.UTF8.GetBytes(plainText));
                else if (type == "argon2i") argon2 = new Argon2i(Encoding.UTF8.GetBytes(plainText));
                else return false;

                using (argon2)
                {
                    argon2.Salt = salt;
                    argon2.MemorySize = memorySize;
                    argon2.Iterations = iterations;
                    argon2.DegreeOfParallelism = parallelism;

                    byte[] computedHash = argon2.GetBytes(expectedHash.Length);
                    return computedHash.SequenceEqual(expectedHash);
                }
            }

            // Fallback for previous custom format: salt:hash
            var customParts = storedHash.Split(':');
            if (customParts.Length == 2)
            {
                var salt = Convert.FromBase64String(customParts[0]);
                var hashToVerify = customParts[1];

                var computedHash = HashString(plainText, salt).Split(':')[1];
                return hashToVerify == computedHash;
            }

            return false;
        }

        private byte[] CreateSalt()
        {
            var buffer = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return buffer;
        }
    }
}
