using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using SET09102_2024_5.Services;
using SET09102_2024_5.Views;
using SET09102_2024_5.Interfaces;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SET09102_2024_5;

public partial class AppShell : Shell, INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private bool _isAdmin;

    public bool IsAdmin
    {
        get => _isAdmin;
        set
        {
            if (_isAdmin != value)
            {
                _isAdmin = value;
                OnPropertyChanged(nameof(IsAdmin));
            }
        }
    }

    public AppShell(IAuthService authService, INavigationService navigationService)
    {
        InitializeComponent();
        _authService = authService;
        _navigationService = navigationService;
        
        // Register routes using a centralized approach
        RegisterRoutes();
        
        // Set binding context for logout command and IsAdmin property
        BindingContext = this;

        // Subscribe to auth state changes
        _authService.UserChanged += OnUserChanged!;

        // Check initial admin status
        _ = CheckAdminStatus();
    }

    // Centralized route registration to avoid redundancy
    private void RegisterRoutes()
    {
        // Register standard routes
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        
        // Register sensor pages
        Routing.RegisterRoute(nameof(SensorManagementPage), typeof(SensorManagementPage));
        
        // Register admin routes
        Routing.RegisterRoute(nameof(AdminDashboardPage), typeof(AdminDashboardPage));
        Routing.RegisterRoute(nameof(RoleManagementPage), typeof(RoleManagementPage));
        Routing.RegisterRoute(nameof(UserRoleManagementPage), typeof(UserRoleManagementPage));
    }

    private async void OnUserChanged(object? sender, EventArgs e)
    {
        await CheckAdminStatus();
    }

    private async Task CheckAdminStatus()
    {
        var currentUser = await _authService.GetCurrentUserAsync();
        IsAdmin = currentUser != null && await _authService.IsInRoleAsync(currentUser.UserId, "Administrator");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _authService.Logout();
        await _navigationService.NavigateToLoginAsync();
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        try
        {
            base.OnNavigating(args);

            // Get the target page from the URI
            var targetPage = args.Target.Location.OriginalString;
            
            // Skip auth check for login and registration
            if (IsPublicRoute(targetPage))
            {
                return;
            }

            // Defer to NavigationService for permission checks
            Task.Run(async () => {
                try {
                    // Use existing navigation service method to check permissions
                    bool canNavigate = await _navigationService.CanNavigateToRouteAsync(targetPage);
                    
                    if (!canNavigate)
                    {
                        // Cancel the current navigation
                        args.Cancel();
                        
                        var currentUser = await _authService.GetCurrentUserAsync();
                        await MainThread.InvokeOnMainThreadAsync(async () => {
                            if (currentUser == null)
                            {
                                // Redirect to login if not authenticated
                                await _navigationService.NavigateToLoginAsync();
                            }
                            else
                            {
                                // Show access denied message for authenticated users without proper permissions
                                await Shell.Current.DisplayAlert("Access Denied", 
                                    "You don't have the required permissions to access this page.", "OK");
                                await _navigationService.NavigateToMainPageAsync();
                            }
                        });
                    }
                    else if (!IsPublicRoute(targetPage))
                    {
                        // Enable flyout for authenticated users on non-login pages
                        await _navigationService.EnableFlyoutAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Shell navigation error: {ex}");
        }
    }
    
    // Helper method to check if route is a public route (login or register)
    private bool IsPublicRoute(string route)
    {
        return RouteConstants.PublicRoutes.Any(r => route.Contains(r.Replace("/", "")));
    }
}
