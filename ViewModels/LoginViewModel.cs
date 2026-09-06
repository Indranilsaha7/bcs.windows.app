using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using BarodaPOS.Services;
using System.Windows;

namespace BarodaPOS.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _username;

        [ObservableProperty]
        private string _code;

        // Note: Password usually shouldn't be bound directly due to security in WPF, 
        // but we'll manage it via an interface or code-behind for simplicity.
        public string Password { private get; set; }

        public Action OnLoginSuccessful { get; set; }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        public Action<bool, string> OnLoginResult { get; set; }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Code))
            {
                OnLoginResult?.Invoke(false, "Please enter username, password, and code.");
                return;
            }

            bool success = await _authService.LoginAsync(Username, Password, Code);
            if (success)
            {
                OnLoginResult?.Invoke(true, "Login Successful! Redirecting...");
                
                // Add a small delay to let the animation play before hiding the frame
                await Task.Delay(1500); 
                OnLoginSuccessful?.Invoke();
            }
            else
            {
                OnLoginResult?.Invoke(false, "Invalid credentials.");
            }
        }
    }
}
