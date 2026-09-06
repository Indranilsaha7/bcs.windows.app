using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Cloud.Firestore;
using BarodaPOS.Models;
using BarodaPOS.Services;

namespace BarodaPOS.ViewModels
{
    public partial class TaskViewModel : ObservableObject
    {
        private readonly FirestoreService _firestoreService;
        private readonly FirestoreDb _db;

        [ObservableProperty] private ObservableCollection<AppTask> _tasks = new();
        [ObservableProperty] private string _newTaskTitle = "";
        [ObservableProperty] private bool _isLoading;

        public TaskViewModel()
        {
            _firestoreService = new FirestoreService();
            _db = _firestoreService.GetDb();
            _ = LoadTasksAsync();
        }

        [RelayCommand]
        public async Task LoadTasksAsync()
        {
            IsLoading = true;
            try
            {
                var snap = await _db.Collection("tasks").OrderByDescending("createdAt").GetSnapshotAsync();
                Tasks.Clear();
                foreach (var doc in snap.Documents)
                {
                    var task = doc.ConvertTo<AppTask>();
                    task.Id = doc.Id;
                    
                    // Attach property changed to auto-save completion status
                    task.PropertyChanged += async (s, e) =>
                    {
                        if (e.PropertyName == nameof(AppTask.IsCompleted))
                        {
                            await ToggleTaskCompletionAsync(task);
                        }
                    };
                    
                    Tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to load tasks: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AddTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;

            IsLoading = true;
            try
            {
                var newTask = new AppTask
                {
                    Title = NewTaskTitle.Trim(),
                    IsCompleted = false
                };

                var docRef = await _db.Collection("tasks").AddAsync(newTask);
                
                // Reload to get the proper ID and timestamp, or just manually insert at top
                await LoadTasksAsync();
                
                NewTaskTitle = "";
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to add task: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task DeleteTaskAsync(AppTask task)
        {
            if (task == null || string.IsNullOrEmpty(task.Id)) return;
            
            bool confirm = await DialogHelper.ShowConfirmAsync("Confirm Delete", $"Are you sure you want to delete '{task.Title}'?");
            if (!confirm) return;

            IsLoading = true;
            try
            {
                await _db.Collection("tasks").Document(task.Id).DeleteAsync();
                Tasks.Remove(task);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessageAsync("Error", $"Failed to delete task: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleTaskCompletionAsync(AppTask task)
        {
            if (task == null || string.IsNullOrEmpty(task.Id)) return;
            try
            {
                await _db.Collection("tasks").Document(task.Id).UpdateAsync("isCompleted", task.IsCompleted);
            }
            catch (Exception ex)
            {
                // Revert locally if failed
                task.IsCompleted = !task.IsCompleted; 
                await DialogHelper.ShowMessageAsync("Error", $"Failed to update task: {ex.Message}");
            }
        }
    }
}
