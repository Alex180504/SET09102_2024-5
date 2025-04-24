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

        // Enhanced navigation helper method with improved route handling
        private Task NavigateInternalAsync(string route, bool enableFlyout = false)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (enableFlyout)
                    await EnableFlyoutAsync();
                else
                    await DisableFlyoutAsync();
                
                try 
                {
                    // Normalize the route format for better compatibility
                    string normalizedRoute = NormalizeRoute(route);
                    await Shell.Current.GoToAsync(normalizedRoute);
                }
                catch (Exception ex)
                {
                    // Enhanced error handling for different navigation scenarios
                    if (ex.Message.Contains("Global routes currently cannot be the only page on the stack"))
                    {
                        await HandleGlobalRouteError(route);
                    }
                    else if (ex.Message.Contains("No matching route found"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Route not found: {route}. Check if the route is registered in AppShell.");
                        // Fallback to main page
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    else
                    {
                        // For other types of navigation errors, log and rethrow
                        System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        // Helper method to handle the specific global route error
        private async Task HandleGlobalRouteError(string route)
        {
            // Force navigation to MainPage first, then to the desired route
            await Shell.Current.GoToAsync("//MainPage");
            
            // Strip leading slashes for relative navigation after main page
            string relativeRoute = route.TrimStart('/');
            if (!string.IsNullOrEmpty(relativeRoute))
            {
                await Task.Delay(100); // Small delay to ensure navigation stability
                await Shell.Current.GoToAsync(relativeRoute);
            }
        }

        // Helper method to normalize route format
        private string NormalizeRoute(string route)
        {
            // Strip any existing slashes to avoid confusion
            string cleanRoute = route.TrimStart('/');

            // Convert admin routes to use proper shell navigation syntax
            if (RouteConstants.AdminRoutes.Any(r => r.TrimStart('/') == cleanRoute))
            {
                // Ensure admin routes use absolute paths with three slashes
                return "///" + cleanRoute;
            }
            
            // Check if it's a public route
            if (RouteConstants.PublicRoutes.Any(r => r.TrimStart('/') == cleanRoute))
            {
                // Use three slashes for public routes as well
                return "///" + cleanRoute;
            }
            
            // For main page, use three slashes
            if (cleanRoute == "MainPage" || cleanRoute == RouteConstants.MainPage.TrimStart('/'))
            {
                return "///MainPage";
            }
            
            // For any other routes, ensure they have proper three-slash prefix
            return route.StartsWith("///") ? route : "///" + cleanRoute;
        }

        public Task NavigateToAsync(string route)
        {
            // Use internal navigation with proper error handling
            return NavigateInternalAsync(route, !RouteConstants.PublicRoutes.Contains(route));
        }

        public Task NavigateToLoginAsync()
        {
            return NavigateInternalAsync(RouteConstants.Login, false);
        }

        public Task NavigateToRegisterAsync()
        {
            return NavigateInternalAsync(RouteConstants.Register, false);
        }

        public Task NavigateToMainPageAsync()
        {
            return NavigateInternalAsync(RouteConstants.MainPage, true);
        }

        // Optimized implementation for admin routes
        public Task NavigateToAdminDashboardAsync()
        {
            return CheckAndNavigateAsync(RouteConstants.AdminDashboard);
        }
        
        public Task NavigateToRoleManagementAsync()
        {
            return CheckAndNavigateAsync(RouteConstants.RoleManagement);
        }
        
        public Task NavigateToUserRoleManagementAsync()
        {
            return CheckAndNavigateAsync(RouteConstants.UserRoleManagement);
        }

        // Enhanced navigation method with improved permission checking and error handling
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
                        // Not logged in, redirect to login
                        await NavigateToLoginAsync();
                    }
                    else
                    {
                        // Show access denied message with more details
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Shell.Current.DisplayAlert("Access Denied", 
                                "You don't have the required permissions to access this page. " +
                                "Please contact an administrator if you need access.", "OK");
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
                        "An error occurred while navigating to the requested page. " +
                        "Please try again.", "OK");
                    System.Diagnostics.Debug.WriteLine($"Navigation error in CheckAndNavigateAsync: {ex.Message}");
                    await NavigateToMainPageAsync();
                });
            }
        }

        // Optimized to avoid unnecessary Task creation
        public Task EnableFlyoutAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(() => 
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout);
        }

        // Optimized to avoid unnecessary Task creation
        public Task DisableFlyoutAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(() => 
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled);
        }

        public async Task<bool> CanNavigateToRouteAsync(string route)
        {
            // Performance optimization: early return for public routes
            if (RouteConstants.PublicRoutes.Any(r => route.Contains(r.Replace("/", ""))))
                return true;
                
            // Check if user is authenticated
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                return false; // Not authenticated and not a public route
            }
            
            // Check admin routes with improved comparison
            if (RouteConstants.AdminRoutes.Any(r => 
                route.Equals(r, StringComparison.OrdinalIgnoreCase) || 
                route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase)))
            {
                return await _authService.IsInRoleAsync(currentUser.UserId, "Administrator");
            }
            
            // All authenticated users can access other routes
            return true;
        }
    }
}