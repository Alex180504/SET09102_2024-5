using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Services;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for the login page that handles user authentication
    /// </summary>
    /// <remarks>
    /// This ViewModel manages the login process by interfacing with the AuthService.
    /// It handles input validation, authentication requests, and navigation after successful login.
    /// </remarks>
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        
        /// <summary>
        /// Gets or sets the email address entered by the user
        /// </summary>
        [ObservableProperty]
        private string _email = string.Empty;
        
        /// <summary>
        /// Gets or sets the password entered by the user
        /// </summary>
        [ObservableProperty]
        private string _password = string.Empty;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginViewModel"/> class
        /// </summary>
        /// <param name="authService">Service for authentication operations</param>
        /// <param name="navigationService">Service for navigation operations</param>
        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Login";
        }

        /// <summary>
        /// Navigates to the registration page
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand]
        private Task RegisterAsync() => _navigationService.NavigateToRegisterAsync();

        /// <summary>
        /// Attempts to authenticate the user with the provided credentials
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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
        
        /// <summary>
        /// Determines whether the login operation can be executed based on input validation
        /// </summary>
        /// <returns>True if both email and password fields are non-empty; otherwise, false</returns>
        private bool CanLogin() => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password);

        // This ensures the login button enabled state updates when properties change
        partial void OnEmailChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
        partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    }
}