using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using SET09102_2024_5.Views;
using System.Collections.Generic;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IAuthService _authService;
        private readonly ILoggingService _loggingService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized = false;
        private const string NavCategory = "Navigation";
        private const string ServiceName = "Navigation Service";
        
        // Keep track of registered routes locally since Shell.Routes is not accessible
        private readonly HashSet<string> _registeredRoutes = new HashSet<string>();

        public NavigationService(
            IAuthService authService, 
            ILoggingService loggingService,
            IServiceProvider serviceProvider)
        {
            _authService = authService;
            _loggingService = loggingService;
            _serviceProvider = serviceProvider;
        }
        
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;
                
            _loggingService.Info("Initializing navigation service", NavCategory);
            
            try
            {
                // Initialize the view registration system
                ViewRegistration.Initialize();
                _isInitialized = true;
                _loggingService.Info("Navigation service initialized successfully", NavCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Failed to initialize navigation service", ex, NavCategory);
                return false;
            }
        }
        
        public Task<bool> IsReadyAsync()
        {
            return Task.FromResult(_isInitialized);
        }
        
        public string GetServiceStatus()
        {
            return _isInitialized ? "Ready" : "Not Ready";
        }
        
        public string GetServiceName()
        {
            return ServiceName;
        }
        
        public async Task CleanupAsync()
        {
            _loggingService.Info("Cleaning up navigation service", NavCategory);
            // No specific cleanup needed for this service
            await Task.CompletedTask;
        }

        // Enhanced navigation method with lazy loading of views for better performance
        private Task NavigateInternalAsync(string route, bool enableFlyout = false)
        {
            _loggingService.Debug($"Attempting to navigate to: {route} (flyout: {enableFlyout})", NavCategory);
            
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try 
                {
                    // Set flyout behavior
                    Shell.Current.FlyoutBehavior = enableFlyout ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;
                    
                    // Normalize the route once
                    string normalizedRoute = NormalizeRoute(route);
                    _loggingService.Debug($"Normalized route: {normalizedRoute}", NavCategory);
                    
                    // Use the ViewRegistration system to resolve the view
                    if (!_registeredRoutes.Contains(route))
                    {
                        try
                        {
                            Type viewType = ViewRegistration.GetViewTypeForRoute(route);
                            RegisterRouteIfNeeded(route, viewType);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warning($"Could not register route dynamically: {ex.Message}", NavCategory);
                        }
                    }
                    
                    await Shell.Current.GoToAsync(normalizedRoute);
                    _loggingService.Info($"Successfully navigated to: {route}", NavCategory);
                }
                catch (Exception ex)
                {
                    // Handle all navigation errors in one place
                    await HandleNavigationError(route, ex);
                }
            });
        }

        // Register a route dynamically if it's not already registered
        private void RegisterRouteIfNeeded(string route, Type viewType)
        {
            try
            {
                if (!_registeredRoutes.Contains(route))
                {
                    _loggingService.Debug($"Registering route dynamically: {route} -> {viewType.Name}", NavCategory);
                    
                    // Create view using dependency injection for proper lifecycle management
                    Routing.RegisterRoute(route, viewType);
                    _registeredRoutes.Add(route);
                    _loggingService.Debug($"Route registered successfully: {route}", NavCategory);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Failed to register route: {route}", ex, NavCategory);
            }
        }

        // Consolidated navigation error handling
        private async Task HandleNavigationError(string route, Exception ex)
        {
            _loggingService.Error($"Navigation error for route: {route}", ex, NavCategory);
            
            if (ex.Message.Contains("Global routes") || ex.Message.Contains("No matching route found"))
            {
                _loggingService.Debug("Attempting fallback navigation strategy", NavCategory);
                
                // Handle common shell navigation issues by resetting to main page first
                try
                {
                    await Shell.Current.GoToAsync("//MainPage");
                    await Task.Delay(50); // Small delay for UI to catch up
                    
                    // Try relative navigation after resetting to main page
                    string relativeRoute = route.TrimStart('/');
                    if (!string.IsNullOrEmpty(relativeRoute))
                    {
                        _loggingService.Debug($"Attempting relative navigation to: {relativeRoute}", NavCategory);
                        await Shell.Current.GoToAsync(relativeRoute);
                        _loggingService.Info($"Fallback navigation to {relativeRoute} successful", NavCategory);
                    }
                }
                catch (Exception fallbackEx)
                {
                    // If even the fallback fails, just log it
                    _loggingService.Error("Fallback navigation failed", fallbackEx, NavCategory);
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
        
        public Task GoBackAsync()
        {
            _loggingService.Debug("Navigating back", NavCategory);
            
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("..");
                    _loggingService.Info("Successfully navigated back", NavCategory);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error navigating back", ex, NavCategory);
                    
                    // Fallback - try to navigate to main page
                    try 
                    {
                        await NavigateToMainPageAsync();
                    }
                    catch (Exception fallbackEx)
                    {
                        _loggingService.Error("Fallback navigation failed", fallbackEx, NavCategory);
                    }
                }
            });
        }

        public Task NavigateToLoginAsync() => NavigateInternalAsync(RouteConstants.LoginPage, false);

        public Task NavigateToRegisterAsync() => NavigateInternalAsync(RouteConstants.RegisterPage, false);

        public Task NavigateToMainPageAsync() => NavigateInternalAsync(RouteConstants.MainPage, true);

        // Consolidated implementation for admin routes
        public Task NavigateToAdminDashboardAsync() => CheckAndNavigateAsync(RouteConstants.AdminDashboardPage);
        
        public Task NavigateToRoleManagementAsync() => CheckAndNavigateAsync(RouteConstants.RoleManagementPage);
        
        public Task NavigateToUserRoleManagementAsync() => CheckAndNavigateAsync(RouteConstants.UserRoleManagementPage);

        // Add a generic way to navigate to any view by ViewModel type for better maintainability
        public Task NavigateToViewAsync<TView>() where TView : ViewBase
        {
            Type viewType = typeof(TView);
            string viewName = viewType.Name;
            
            // Convert from view name to route name (e.g., LoginPage -> LoginPage)
            string routeName = viewName;
            
            return NavigateToAsync(routeName);
        }

        // Optimized navigation with permission check
        private async Task CheckAndNavigateAsync(string route)
        {
            try
            {
                _loggingService.Debug($"Checking navigation permissions for route: {route}", NavCategory);
                bool canNavigate = await CanNavigateToRouteAsync(route);
                
                if (canNavigate)
                {
                    _loggingService.Debug($"Permission check passed for route: {route}", NavCategory);
                    await NavigateInternalAsync(route, true);
                }
                else
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    
                    if (currentUser == null)
                    {
                        _loggingService.Warning($"Navigation to {route} redirected to login - user not authenticated", NavCategory);
                        await NavigateToLoginAsync();
                    }
                    else
                    {
                        _loggingService.Warning($"Access denied for route {route} - insufficient permissions", NavCategory);
                        
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
                _loggingService.Error($"Error during permission-checked navigation to {route}", ex, NavCategory);
                
                // Handle navigation errors gracefully
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("Navigation Error", 
                        "An error occurred while navigating. Please try again.", "OK");
                });
            }
        }

        // Simplified flyout behavior methods
        public Task EnableFlyoutAsync()
        {
            _loggingService.Debug("Enabling flyout menu", NavCategory);
            return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout);
        }

        public Task DisableFlyoutAsync()
        {
            _loggingService.Debug("Disabling flyout menu", NavCategory);
            return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled);
        }

        public async Task<bool> CanNavigateToRouteAsync(string route)
        {
            try
            {
                // Public routes are always accessible
                if (RouteConstants.PublicRoutes.Any(r => 
                    route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase)))
                {
                    _loggingService.Debug($"Route {route} is publicly accessible", NavCategory);
                    return true;
                }
                    
                // Check if user is authenticated
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _loggingService.Debug($"Route {route} requires authentication - user not authenticated", NavCategory);
                    return false;
                }
                
                // Admin routes require administrator role
                if (RouteConstants.AdminRoutes.Any(r => 
                    route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase)))
                {
                    bool isAdmin = await _authService.IsInRoleAsync(currentUser.UserId, "Administrator");
                    
                    if (!isAdmin)
                    {
                        _loggingService.Debug($"Route {route} requires admin role - access denied", NavCategory);
                    }
                    else
                    {
                        _loggingService.Debug($"Route {route} is admin route - user has admin role", NavCategory);
                    }
                    
                    return isAdmin;
                }
                
                // All authenticated users can access other routes
                _loggingService.Debug($"Route {route} is accessible to authenticated users", NavCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error checking navigation permissions for route {route}", ex, NavCategory);
                return false;
            }
        }
    }
}