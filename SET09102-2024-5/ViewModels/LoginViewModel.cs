using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Services;
using SET09102_2024_5.Views;

namespace SET09102_2024_5.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        
        [ObservableProperty]
        private string email = string.Empty;
        
        [ObservableProperty]
        private string password = string.Empty;
        
        [ObservableProperty]
        private string errorMessage = string.Empty;
        
        [ObservableProperty]
        private bool isAuthenticating;

        public ICommand RegisterCommand { get; }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            RegisterCommand = new AsyncRelayCommand(NavigateToRegistrationAsync);
            Title = "Login";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Email and password are required.";
                return;
            }

            IsAuthenticating = true;
            
            try
            {
                var user = await _authService.AuthenticateAsync(Email, Password);
                
                if (user != null)
                {
                    // Reset fields
                    Email = string.Empty;
                    Password = string.Empty;
                    ErrorMessage = string.Empty;
                    
                    // Navigate to the main page after successful authentication
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    ErrorMessage = "Invalid email or password.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Authentication error: {ex.Message}";
            }
            finally
            {
                IsAuthenticating = false;
            }
        }
        
        private async Task NavigateToRegistrationAsync()
        {
            // Navigate to registration page
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}