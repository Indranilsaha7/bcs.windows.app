using CommunityToolkit.Mvvm.ComponentModel;
using Google.Cloud.Firestore;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public partial class AppTask : ObservableObject
    {
        [FirestoreDocumentId]
        public string Id { get; set; }
        
        [FirestoreProperty("title")]
        public string Title { get; set; }
        
        private bool _isCompleted;
        [FirestoreProperty("isCompleted")]
        public bool IsCompleted 
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
        
        [FirestoreProperty("createdAt")]
        [ServerTimestamp]
        public Timestamp? CreatedAt { get; set; }
    }
}
