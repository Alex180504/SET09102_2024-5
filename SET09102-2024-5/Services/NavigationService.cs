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
    /// <summary>
    /// Provides navigation functionality between pages in the application
    /// </summary>
    public class NavigationService : BaseService, INavigationService
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private const string NavCategory = "Navigation";
        
        // Keep track of registered routes locally since Shell.Routes is not accessible
        private readonly HashSet<string> _registeredRoutes = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the NavigationService class
        /// </summary>
        /// <param name="authService">Authentication service for permission checks</param>
        /// <param name="loggingService">Logging service for diagnostic information</param>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public NavigationService(
            IAuthService authService, 
            ILoggingService loggingService,
            IServiceProvider serviceProvider)
            : base("Navigation Service", NavCategory, loggingService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        
        /// <summary>
        /// Initializes the navigation service by registering all common routes
        /// </summary>
        protected override async Task InitializeInternalAsync()
        {
            // Initialize the view registration system
            ViewRegistration.Initialize();
            
            // Pre-register common routes to avoid dynamic registration during navigation
            foreach (var route in RouteConstants.AllRoutes)
            {
                try
                {
                    var viewType = ViewRegistration.GetViewTypeForRoute(route);
                    RegisterRouteIfNeeded(route, viewType);
                }
                catch (Exception ex)
                {
                    _loggingService.Warning($"Could not pre-register route {route}: {ex.Message}", _serviceCategory);
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Navigates to a specified route with automatic flyout menu handling
        /// </summary>
        /// <param name="route">The route name to navigate to</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task NavigateToAsync(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                _loggingService.Warning("Attempted to navigate to empty route", _serviceCategory);
                return Task.CompletedTask;
            }

            // Determine route type and enable/disable flyout menu accordingly
            bool showFlyout = !IsPublicRoute(route);
            return NavigateInternalAsync(route, showFlyout);
        }
        
        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If back navigation fails, attempts to navigate to the main page</remarks>
        public Task GoBackAsync()
        {
            _loggingService.Debug("Navigating back", _serviceCategory);
            
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("..");
                    _loggingService.Info("Successfully navigated back", _serviceCategory);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error navigating back", ex, _serviceCategory);
                    
                    // Fallback - try to navigate to main page
                    try 
                    {
                        await NavigateToAsync(RouteConstants.MainPage);
                    }
                    catch (Exception fallbackEx)
                    {
                        _loggingService.Error("Fallback navigation failed", fallbackEx, _serviceCategory);
                    }
                }
            });
        }

        /// <summary>
        /// Navigates to the login page
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task NavigateToLoginAsync() => NavigateToAsync(RouteConstants.LoginPage);
        
        /// <summary>
        /// Navigates to the registration page
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task NavigateToRegisterAsync() => NavigateToAsync(RouteConstants.RegisterPage);
        
        /// <summary>
        /// Navigates to the main page
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task NavigateToMainPageAsync() => NavigateToAsync(RouteConstants.MainPage);
        
        /// <summary>
        /// Navigates to the admin dashboard with permission check
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>Redirects to login page if user is not authenticated or shows access denied if lacking permissions</remarks>
        public Task NavigateToAdminDashboardAsync() => NavigateToRouteWithPermissionCheckAsync(RouteConstants.AdminDashboardPage);
        
        /// <summary>
        /// Navigates to the role management page with permission check
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>Redirects to login page if user is not authenticated or shows access denied if lacking permissions</remarks>
        public Task NavigateToRoleManagementAsync() => NavigateToRouteWithPermissionCheckAsync(RouteConstants.RoleManagementPage);
        
        /// <summary>
        /// Navigates to the user role management page with permission check
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>Redirects to login page if user is not authenticated or shows access denied if lacking permissions</remarks>
        public Task NavigateToUserRoleManagementAsync() => NavigateToRouteWithPermissionCheckAsync(RouteConstants.UserRoleManagementPage);

        /// <summary>
        /// Navigates to a view by its type rather than route name
        /// </summary>
        /// <typeparam name="TView">The type of the view to navigate to</typeparam>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task NavigateToViewAsync<TView>() where TView : ViewBase
        {
            // Convert from view type to route name
            string viewName = typeof(TView).Name;
            string routeName = viewName;
            
            return NavigateToAsync(routeName);
        }

        /// <summary>
        /// Enables the Shell flyout menu
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task EnableFlyoutAsync()
        {
            _loggingService.Debug("Enabling flyout menu", _serviceCategory);
            return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout);
        }

        /// <summary>
        /// Disables the Shell flyout menu
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task DisableFlyoutAsync()
        {
            _loggingService.Debug("Disabling flyout menu", _serviceCategory);
            return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled);
        }

        /// <summary>
        /// Checks if navigation to a specific route is allowed based on authentication and roles
        /// </summary>
        /// <param name="route">The route to check permissions for</param>
        /// <returns>True if navigation is allowed, false otherwise</returns>
        public async Task<bool> CanNavigateToRouteAsync(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                _loggingService.Warning("Attempted to check permissions for empty route", _serviceCategory);
                return false;
            }

            try
            {
                // Public routes are always accessible
                if (IsPublicRoute(route))
                {
                    _loggingService.Debug($"Route {route} is publicly accessible", _serviceCategory);
                    return true;
                }
                    
                // Check if user is authenticated
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _loggingService.Debug($"Route {route} requires authentication - user not authenticated", _serviceCategory);
                    return false;
                }
                
                // Admin routes require administrator role
                if (IsAdminRoute(route))
                {
                    bool isAdmin = await _authService.IsInRoleAsync(currentUser.UserId, "Administrator");
                    
                    if (!isAdmin)
                    {
                        _loggingService.Debug($"Route {route} requires admin role - access denied", _serviceCategory);
                    }
                    else
                    {
                        _loggingService.Debug($"Route {route} is admin route - user has admin role", _serviceCategory);
                    }
                    
                    return isAdmin;
                }
                
                // All authenticated users can access other routes
                _loggingService.Debug($"Route {route} is accessible to authenticated users", _serviceCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error checking navigation permissions for route {route}", ex, _serviceCategory);
                return false;
            }
        }

        #region Private Helper Methods
        
        // Core navigation implementation
        private Task NavigateInternalAsync(string route, bool enableFlyout = false)
        {
            _loggingService.Debug($"Attempting to navigate to: {route} (flyout: {enableFlyout})", _serviceCategory);
            
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try 
                {
                    // Set flyout behavior
                    Shell.Current.FlyoutBehavior = enableFlyout ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;
                    
                    // Normalize the route
                    string normalizedRoute = NormalizeRoute(route);
                    _loggingService.Debug($"Normalized route: {normalizedRoute}", _serviceCategory);
                    
                    // Register route if needed
                    if (!_registeredRoutes.Contains(route))
                    {
                        try
                        {
                            Type viewType = ViewRegistration.GetViewTypeForRoute(route);
                            RegisterRouteIfNeeded(route, viewType);
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Warning($"Could not register route dynamically: {ex.Message}", _serviceCategory);
                        }
                    }
                    
                    await Shell.Current.GoToAsync(normalizedRoute);
                    _loggingService.Info($"Successfully navigated to: {route}", _serviceCategory);
                }
                catch (Exception ex)
                {
                    // Handle navigation errors
                    await HandleNavigationError(route, ex);
                }
            });
        }
        
        // Navigation with permission check
        private async Task NavigateToRouteWithPermissionCheckAsync(string route)
        {
            try
            {
                bool canNavigate = await CanNavigateToRouteAsync(route);
                
                if (canNavigate)
                {
                    _loggingService.Debug($"Permission check passed for route: {route}", _serviceCategory);
                    bool showFlyout = !IsPublicRoute(route);
                    await NavigateInternalAsync(route, showFlyout);
                }
                else
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    
                    if (currentUser == null)
                    {
                        _loggingService.Warning($"Navigation to {route} redirected to login - user not authenticated", _serviceCategory);
                        await NavigateToLoginAsync();
                    }
                    else
                    {
                        _loggingService.Warning($"Access denied for route {route}", _serviceCategory);
                        
                        // Show access denied message and redirect
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
                _loggingService.Error($"Error during permission-checked navigation to {route}", ex, _serviceCategory);
                
                // Handle navigation errors gracefully
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("Navigation Error", 
                        "An error occurred while navigating. Please try again.", "OK");
                });
            }
        }

        // Helper to register new routes for navigation
        private void RegisterRouteIfNeeded(string route, Type viewType)
        {
            if (string.IsNullOrEmpty(route) || viewType == null)
                return;

            try
            {
                if (!_registeredRoutes.Contains(route))
                {
                    _loggingService.Debug($"Registering route: {route} -> {viewType.Name}", _serviceCategory);
                    Routing.RegisterRoute(route, viewType);
                    _registeredRoutes.Add(route);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Failed to register route: {route}", ex, _serviceCategory);
            }
        }

        // Handle navigation errors with fallback strategy
        private async Task HandleNavigationError(string route, Exception ex)
        {
            _loggingService.Error($"Navigation error for route: {route}", ex, _serviceCategory);
            
            if (ex.Message.Contains("Global routes") || ex.Message.Contains("No matching route found"))
            {
                _loggingService.Debug("Attempting fallback navigation strategy", _serviceCategory);
                
                try
                {
                    await Shell.Current.GoToAsync("//MainPage");
                    await Task.Delay(50);
                    
                    // Try relative navigation after resetting
                    string relativeRoute = route.TrimStart('/');
                    if (!string.IsNullOrEmpty(relativeRoute))
                    {
                        await Shell.Current.GoToAsync(relativeRoute);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _loggingService.Error("Fallback navigation failed", fallbackEx, _serviceCategory);
                }
            }
        }

        // Make absolute route paths for navigation
        private string NormalizeRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
                return "//MainPage";
                
            string cleanRoute = route.TrimStart('/');
            return "///" + cleanRoute;
        }

        // Check if a route is public (no authentication required)
        private bool IsPublicRoute(string route)
        {
            return RouteConstants.PublicRoutes.Any(r => 
                route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase));
        }

        // Check if a route requires admin permissions
        private bool IsAdminRoute(string route)
        {
            return RouteConstants.AdminRoutes.Any(r => 
                route.EndsWith(r.TrimStart('/'), StringComparison.OrdinalIgnoreCase));
        }
        
        #endregion
    }
}