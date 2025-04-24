using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace SET09102_2024_5.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IAuthService _authService;

        public NavigationService(IAuthService authService)
        {
            _authService = authService;
        }

        // Simplified navigation helper method with improved route normalization
        private Task NavigateInternalAsync(string route, bool enableFlyout = false)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try 
                {
                    // Set flyout behavior
                    Shell.Current.FlyoutBehavior = enableFlyout ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;
                    
                    // Normalize the route once
                    string normalizedRoute = NormalizeRoute(route);
                    await Shell.Current.GoToAsync(normalizedRoute);
                }
                catch (Exception ex)
                {
                    // Handle all navigation errors in one place
                    await HandleNavigationError(route, ex);
                }
            });
        }

        // Consolidated navigation error handling
        private async Task HandleNavigationError(string route, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message} for route: {route}");
            
            if (ex.Message.Contains("Global routes") || ex.Message.Contains("No matching route found"))
            {
                // Handle common shell navigation issues by resetting to main page first
                try
                {
                    await Shell.Current.GoToAsync("//MainPage");
                    await Task.Delay(50); // Small delay for UI to catch up
                    
                    // Try relative navigation after resetting to main page
                    string relativeRoute = route.TrimStart('/');
                    if (!string.IsNullOrEmpty(relativeRoute))
                    {
                        await Shell.Current.GoToAsync(relativeRoute);
                    }
                }
                catch (Exception fallbackEx)
                {
                    // If even the fallback fails, just log it
                    System.Diagnostics.Debug.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                }
            }
        }

        // Simplified route normalization
        private string NormalizeRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
                return "//MainPage";
                
            // Strip any existing slashes
            string cleanRoute = route.TrimStart('/');
            
            // All routes use absolute paths
            return "///" + cleanRoute;
        }

        public Task NavigateToAsync(string route)
        {
            bool isPublicRoute = RouteConstants.PublicRoutes.Any(r => 
                route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase));
                
            return NavigateInternalAsync(route, !isPublicRoute);
        }

        public Task NavigateToLoginAsync() => NavigateInternalAsync(RouteConstants.Login, false);

        public Task NavigateToRegisterAsync() => NavigateInternalAsync(RouteConstants.Register, false);

        public Task NavigateToMainPageAsync() => NavigateInternalAsync(RouteConstants.MainPage, true);

        // Consolidated implementation for admin routes
        public Task NavigateToAdminDashboardAsync() => CheckAndNavigateAsync(RouteConstants.AdminDashboard);
        
        public Task NavigateToRoleManagementAsync() => CheckAndNavigateAsync(RouteConstants.RoleManagement);
        
        public Task NavigateToUserRoleManagementAsync() => CheckAndNavigateAsync(RouteConstants.UserRoleManagement);

        // Optimized navigation with permission check
        private async Task CheckAndNavigateAsync(string route)
        {
            try
            {
                bool canNavigate = await CanNavigateToRouteAsync(route);
                
                if (canNavigate)
                {
                    await NavigateInternalAsync(route, true);
                }
                else
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    
                    if (currentUser == null)
                    {
                        await NavigateToLoginAsync();
                    }
                    else
                    {
                        // Show access denied message
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Shell.Current.DisplayAlert("Access Denied", 
                                "You don't have the required permissions to access this page.", "OK");
                            await NavigateToMainPageAsync();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle navigation errors gracefully
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("Navigation Error", 
                        "An error occurred while navigating. Please try again.", "OK");
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                });
            }
        }

        // Simplified flyout behavior methods
        public Task EnableFlyoutAsync() => 
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout);

        public Task DisableFlyoutAsync() => 
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled);

        public async Task<bool> CanNavigateToRouteAsync(string route)
        {
            // Public routes are always accessible
            if (RouteConstants.PublicRoutes.Any(r => 
                route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
                
            // Check if user is authenticated
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return false;
            }
            
            // Admin routes require administrator role
            if (RouteConstants.AdminRoutes.Any(r => 
                route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase)))
            {
                return await _authService.IsInRoleAsync(currentUser.UserId, "Administrator");
            }
            
            // All authenticated users can access other routes
            return true;
        }
    }
}