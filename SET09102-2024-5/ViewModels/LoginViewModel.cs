using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        
        [ObservableProperty]
        private string _email = string.Empty;
        
        [ObservableProperty]
        private string _password = string.Empty;
        
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Login";
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            await _navigationService.NavigateToRegisterAsync();
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Email and password are required.";
                return;
            }

            try
            {
                StartBusy("Logging in...");
                
                var user = await _authService.AuthenticateAsync(Email, Password);
                
                if (user != null)
                {
                    // Explicitly set the current user to ensure UserChanged event is triggered
                    _authService.SetCurrentUser(user);
                    
                    // Reset fields
                    Email = string.Empty;
                    Password = string.Empty;
                    ErrorMessage = string.Empty;
                    
                    // Navigate to the main page using the navigation service
                    await _navigationService.NavigateToMainPageAsync();
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
                EndBusy("Login");
            }
        }
    }
}