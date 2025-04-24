using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using SET09102_2024_5.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace SET09102_2024_5
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        
        public App(IServiceProvider services)
        {
            InitializeComponent();

            // Get services from the service provider
            _authService = services.GetRequiredService<IAuthService>();
            _navigationService = services.GetRequiredService<INavigationService>();
            
            // Create AppShell with the required services
            MainPage = services.GetRequiredService<AppShell>();
            
            // Initialize authentication when app starts
            // We'll call this in OnStart() lifecycle method
        }
        
        protected override async void OnStart()
        {
            base.OnStart();
            
            try
            {
                // Initialize authentication properly when the app is started
                await InitializeAuthenticationAsync();
            }
            catch (Exception ex)
            {
                // Log the exception and handle it gracefully
                System.Diagnostics.Debug.WriteLine($"Authentication initialization error: {ex.Message}");
                
                // Navigate to login page as fallback in case of authentication error
                await _navigationService.NavigateToLoginAsync();
            }
        }
        
        // Authentication initialization method with proper error handling
        private async Task InitializeAuthenticationAsync()
        {
            try
            {
                // This will trigger loading the saved user credentials if they exist
                var currentUser = await _authService.GetCurrentUserAsync();
                
                // If we have a valid user from saved session, configure app accordingly
                if (currentUser != null)
                {
                    await _navigationService.NavigateToMainPageAsync();
                }
                else
                {
                    await _navigationService.NavigateToLoginAsync();
                }
            }
            catch (Exception ex)
            {
                // Properly handle and rethrow the exception so it can be caught by the caller
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                throw;
            }
        }
        
        // Parameterless constructor for design-time support only
        public App()
        {
            InitializeComponent();
            // This will only be called in design time or when services aren't available
        }
    }
}
