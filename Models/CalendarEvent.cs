using Google.Cloud.Firestore;
using System;

namespace BarodaPOS.Models
{
    [FirestoreData]
    public class CalendarEvent
    {
        public string Id { get; set; }

        [FirestoreProperty("title")]
        public string Title { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; }

        [FirestoreProperty("date")]
        public DateTime Date { get; set; }
        
        [FirestoreProperty("isCompleted")]
        public bool IsCompleted { get; set; }
    }
}
