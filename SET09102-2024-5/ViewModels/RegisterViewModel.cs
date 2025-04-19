using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        
        [ObservableProperty]
        private string firstName = string.Empty;
        
        [ObservableProperty]
        private string lastName = string.Empty;
        
        [ObservableProperty]
        private string email = string.Empty;
        
        [ObservableProperty]
        private string password = string.Empty;
        
        [ObservableProperty]
        private string confirmPassword = string.Empty;
        
        [ObservableProperty]
        private string errorMessage = string.Empty;
        
        [ObservableProperty]
        private bool isRegistering;
        
        [ObservableProperty]
        private bool registrationSuccessful;

        public RegisterViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Register";
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            // Validate input
            if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName) ||
                string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || 
                string.IsNullOrEmpty(ConfirmPassword))
            {
                ErrorMessage = "All fields are required.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            IsRegistering = true;
            
            try
            {
                var success = await _authService.RegisterUserAsync(
                    FirstName, LastName, Email, Password);
                
                if (success)
                {
                    // Registration successful
                    RegistrationSuccessful = true;
                    
                    // Reset fields
                    FirstName = string.Empty;
                    LastName = string.Empty;
                    Email = string.Empty;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;
                    ErrorMessage = string.Empty;
                    
                    // Optional: Navigate back to login or directly to another page
                    await Task.Delay(2000); // Show success message for 2 seconds
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    ErrorMessage = "Registration failed. Email may already be in use.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration error: {ex.Message}";
            }
            finally
            {
                IsRegistering = false;
            }
        }
        
        [RelayCommand]
        private async Task GoToLoginAsync()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}