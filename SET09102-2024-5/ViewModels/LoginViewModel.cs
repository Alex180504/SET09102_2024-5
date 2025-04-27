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
        
        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Login";
        }

        [RelayCommand]
        private Task RegisterAsync() => _navigationService.NavigateToRegisterAsync();

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (!CanLogin())
            {
                ErrorMessage = "Email and password are required.";
                return;
            }

            await ExecuteAsync(async () =>
            {
                var user = await _authService.AuthenticateAsync(Email, Password);
                
                if (user != null)
                {
                    // Explicitly set the current user to ensure UserChanged event is triggered
                    _authService.SetCurrentUser(user);
                    
                    // Reset fields
                    Email = string.Empty;
                    Password = string.Empty;
                    ClearError();
                    
                    // Navigate to the main page using the navigation service
                    await _navigationService.NavigateToMainPageAsync();
                }
                else
                {
                    ErrorMessage = "Invalid email or password.";
                }
            }, "Logging in...", "Authentication error", "Login");
        }
        
        private bool CanLogin() => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password);

        // This ensures the login button enabled state updates when properties change
        partial void OnEmailChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
        partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    }
}