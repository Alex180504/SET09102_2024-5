using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Services;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for the user registration page
    /// </summary>
    /// <remarks>
    /// Manages user registration process including form validation and submission.
    /// Handles navigation after successful registration.
    /// </remarks>
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        
        /// <summary>
        /// Gets or sets the user's first name
        /// </summary>
        [ObservableProperty]
        private string _firstName = string.Empty;
        
        /// <summary>
        /// Gets or sets the user's last name
        /// </summary>
        [ObservableProperty]
        private string _lastName = string.Empty;
        
        /// <summary>
        /// Gets or sets the user's email address
        /// </summary>
        [ObservableProperty]
        private string _email = string.Empty;
        
        /// <summary>
        /// Gets or sets the user's chosen password
        /// </summary>
        [ObservableProperty]
        private string _password = string.Empty;
        
        /// <summary>
        /// Gets or sets the password confirmation value
        /// </summary>
        [ObservableProperty]
        private string _confirmPassword = string.Empty;
        
        /// <summary>
        /// Gets or sets a value indicating whether registration was successful
        /// </summary>
        [ObservableProperty]
        private bool _registrationSuccessful;

        /// <summary>
        /// Gets or sets a value indicating whether registration is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isRegistering;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterViewModel"/> class
        /// </summary>
        /// <param name="authService">Service for authentication operations</param>
        /// <param name="navigationService">Service for navigation operations</param>
        public RegisterViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            Title = "Register";
        }

        /// <summary>
        /// Registers a new user with the information provided in the form
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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
        
        /// <summary>
        /// Navigates back to the login page
        /// </summary>
        /// <returns>A task that represents the asynchronous navigation operation</returns>
        [RelayCommand]
        private Task GoToLoginAsync() => _navigationService.NavigateToLoginAsync();
        
        /// <summary>
        /// Determines if the registration operation can be executed based on form validation
        /// </summary>
        /// <returns>True if all required fields are filled and registration is not in progress; otherwise, false</returns>
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