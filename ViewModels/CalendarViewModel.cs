using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarodaPOS.Models;
using BarodaPOS.Services;
using Google.Cloud.Firestore;

namespace BarodaPOS.ViewModels
{
    public partial class CalendarViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;

        public CalendarViewModel()
        {
            _firestoreService = new FirestoreService();
            Events = new ObservableCollection<CalendarEvent>();
            FilteredEvents = new ObservableCollection<CalendarEvent>();
            EventDate = DateTime.Today;
            _ = LoadEventsAsync();
        }

        [ObservableProperty]
        private ObservableCollection<CalendarEvent> _events;

        [ObservableProperty]
        private ObservableCollection<CalendarEvent> _filteredEvents;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isAddingEvent;

        [ObservableProperty]
        private string _newEventTitle;

        [ObservableProperty]
        private string _newEventDescription;
        
        [ObservableProperty]
        private DateTime _eventDate;

        [ObservableProperty]
        private DateTime? _selectedFilterDate;

        public bool IsDateSelected => SelectedFilterDate.HasValue;

        partial void OnSelectedFilterDateChanged(DateTime? value)
        {
            OnPropertyChanged(nameof(IsDateSelected));
            FilterEvents();
        }

        [RelayCommand]
        private void ClearDateFilter()
        {
            SelectedFilterDate = null;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterEvents();
        }

        [RelayCommand]
        public async Task LoadEventsAsync()
        {
            IsLoading = true;
            try
            {
                var db = _firestoreService.GetDb();
                var evs = await Task.Run(async () => {
                    var snapshot = await db.Collection("events").GetSnapshotAsync();
                    var list = new List<CalendarEvent>();
                    foreach (var doc in snapshot.Documents)
                    {
                        if (doc.Exists)
                        {
                            var ev = doc.ConvertTo<CalendarEvent>();
                            ev.Id = doc.Id;
                            ev.Date = ev.Date.ToLocalTime().Date; 
                            list.Add(ev);
                        }
                    }
                    return list;
                });
                
                Events = new ObservableCollection<CalendarEvent>(evs);
                
                FilterEvents();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to load events: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterEvents()
        {
            if (Events == null) return;
            
            var query = Events.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lowerSearch = SearchText.ToLower();
                query = query.Where(e => (e.Title != null && e.Title.ToLower().Contains(lowerSearch)) || 
                                         (e.Description != null && e.Description.ToLower().Contains(lowerSearch)));
            }

            if (SelectedFilterDate.HasValue)
            {
                var targetDate = SelectedFilterDate.Value.Date;
                query = query.Where(e => e.Date.Date == targetDate);
            }

            FilteredEvents = new ObservableCollection<CalendarEvent>(query.OrderByDescending(e => e.Date));
        }

        [RelayCommand]
        private void ShowAddEvent()
        {
            NewEventTitle = string.Empty;
            NewEventDescription = string.Empty;
            EventDate = DateTime.Today;
            IsAddingEvent = true;
        }

        [RelayCommand]
        private void CancelAddEvent()
        {
            IsAddingEvent = false;
        }

        [RelayCommand]
        private async Task SaveEventAsync()
        {
            if (string.IsNullOrWhiteSpace(NewEventTitle))
            {
                await DialogHelper.ShowMessageAsync("Validation", "Please enter an event title.");
                return;
            }

            try
            {
                var newEvent = new CalendarEvent
                {
                    Title = NewEventTitle,
                    Description = NewEventDescription ?? string.Empty,
                    Date = EventDate.ToUniversalTime(), 
                    IsCompleted = false
                };

                var db = _firestoreService.GetDb();
                var docRef = await db.Collection("events").AddAsync(newEvent);
                
                newEvent.Id = docRef.Id;
                newEvent.Date = EventDate.Date;
                
                Events.Add(newEvent);
                FilterEvents();
                
                IsAddingEvent = false;
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to save event: {ex.Message}");
            }
        }
        
        [RelayCommand]
        private async Task DeleteEventAsync(CalendarEvent ev)
        {
            if (ev == null) return;
            
            bool isConfirmed = await DialogHelper.ShowConfirmAsync("Confirm Delete", $"Are you sure you want to delete event '{ev.Title}'?");
            if (!isConfirmed) return;
            
            try
            {
                var db = _firestoreService.GetDb();
                await db.Collection("events").Document(ev.Id).DeleteAsync();
                
                Events.Remove(ev);
                FilterEvents();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to delete event: {ex.Message}");
            }
        }
    }
}
