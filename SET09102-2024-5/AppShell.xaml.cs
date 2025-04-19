using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Services;
using SET09102_2024_5.Views;

namespace SET09102_2024_5;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        
        // Register routes for navigation
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));

        // Set binding context for logout command
        BindingContext = this;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _authService.Logout();
        
        // Disable flyout and navigate to login page
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        await Shell.Current.GoToAsync("//LoginPage");
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // Get the target page from the URI
        var targetPage = args.Target.Location.OriginalString;
        
        // Skip auth check for login and registration
        if (targetPage.Contains("LoginPage") || targetPage.Contains("RegisterPage"))
        {
            return;
        }

        // Check if user is authenticated
        var currentUser = _authService.GetCurrentUserAsync().Result;
        if (currentUser == null)
        {
            // Cancel the current navigation
            args.Cancel();
            
            // Redirect to login page
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            Shell.Current.GoToAsync("//LoginPage").Wait();
        }
        else
        {
            // Enable flyout for authenticated users
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
            
            // Role-based access control for admin pages
            if (targetPage.Contains("UserManagementPage"))
            {
                bool isAdmin = _authService.IsInRoleAsync(currentUser.UserId, "Administrator").Result;
                if (!isAdmin)
                {
                    args.Cancel();
                    // Optionally redirect with an error message
                    Shell.Current.DisplayAlert("Access Denied", "You need administrator privileges to access this page.", "OK");
                    Shell.Current.GoToAsync("//MainPage").Wait();
                }
            }
        }
    }
}
