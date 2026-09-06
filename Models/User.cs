using Google.Cloud.Firestore;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty("Username")]
        public string Username { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("PasswordHash")]
        public string PasswordHash { get; set; }

        [FirestoreProperty("password")]
        public string Password { get; set; }

        [FirestoreProperty("CodeHash")]
        public string CodeHash { get; set; }

        [FirestoreProperty("code")]
        public string Code { get; set; }

        [FirestoreProperty("Role")]
        public string Role { get; set; }

        [FirestoreProperty("role")]
        public string LowercaseRole { get; set; }
    }
}
