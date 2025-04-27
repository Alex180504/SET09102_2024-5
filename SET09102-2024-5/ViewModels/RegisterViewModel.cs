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
        private readonly INavigationService _navigationService;
        
        [ObservableProperty]
        private string _firstName = string.Empty;
        
        [ObservableProperty]
        private string _lastName = string.Empty;
        
        [ObservableProperty]
        private string _email = string.Empty;
        
        [ObservableProperty]
        private string _password = string.Empty;
        
        [ObservableProperty]
        private string _confirmPassword = string.Empty;
        
        [ObservableProperty]
        private bool _registrationSuccessful;

        [ObservableProperty]
        private bool _isRegistering;

        public RegisterViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Register";
        }

        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task RegisterAsync()
        {
            if (!CanRegister())
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
            
            await ExecuteAsync(async () => 
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
                    ClearError();
                    
                    // Show success message briefly before navigation
                    await Task.Delay(1500);
                    await _navigationService.NavigateToLoginAsync();
                }
                else
                {
                    ErrorMessage = "Registration failed. Email may already be in use.";
                }
            }, "Registering account...", "Registration error", "Register");
            
            IsRegistering = false;
        }
        
        [RelayCommand]
        private Task GoToLoginAsync() => _navigationService.NavigateToLoginAsync();
        
        private bool CanRegister() => 
            !string.IsNullOrEmpty(FirstName) && 
            !string.IsNullOrEmpty(LastName) &&
            !string.IsNullOrEmpty(Email) && 
            !string.IsNullOrEmpty(Password) && 
            !string.IsNullOrEmpty(ConfirmPassword) &&
            !IsRegistering;
            
        // Update command can execute state when properties change
        partial void OnFirstNameChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();
        partial void OnLastNameChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();
        partial void OnEmailChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();
        partial void OnPasswordChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();
        partial void OnConfirmPasswordChanged(string value) => RegisterCommand.NotifyCanExecuteChanged();
    }
}