using SET09102_2024_5.Views;
using System;
using System.Collections.Generic;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Provides centralized registration of views and their route names
    /// </summary>
    public static class ViewRegistration
    {
        // Dictionary mapping route names to view types for easy lookup
        private static readonly Dictionary<string, Type> _routeViewMap = new Dictionary<string, Type>();
        
        /// <summary>
        /// Initializes the view registration system with standard routes
        /// </summary>
        public static void Initialize()
        {
            // Register all views and their corresponding routes
            RegisterRoute(RouteConstants.LoginPage, typeof(LoginPage));
            RegisterRoute(RouteConstants.MainPage, typeof(MainPage));
            RegisterRoute(RouteConstants.RegisterPage, typeof(RegisterPage));
            RegisterRoute(RouteConstants.MapPage, typeof(MapPage));
            RegisterRoute(RouteConstants.AdminDashboardPage, typeof(AdminDashboardPage));
            RegisterRoute(RouteConstants.RoleManagementPage, typeof(RoleManagementPage));
            RegisterRoute(RouteConstants.UserRoleManagementPage, typeof(UserRoleManagementPage));
        }

        /// <summary>
        /// Registers a route name with its corresponding view type
        /// </summary>
        /// <param name="routeName">The route name for navigation</param>
        /// <param name="pageType">The type of the page/view</param>
        public static void RegisterRoute(string routeName, Type pageType)
        {
            if (string.IsNullOrEmpty(routeName))
                throw new ArgumentException("Route name cannot be null or empty", nameof(routeName));
                
            if (pageType == null)
                throw new ArgumentNullException(nameof(pageType));
                
            if (!pageType.IsSubclassOf(typeof(ViewBase)))
                throw new ArgumentException("Page type must derive from ViewBase", nameof(pageType));
                
            _routeViewMap[routeName] = pageType;
        }

        /// <summary>
        /// Gets a view type for a given route name
        /// </summary>
        /// <param name="routeName">The route name to lookup</param>
        /// <returns>The view type for the route</returns>
        public static Type GetViewTypeForRoute(string routeName)
        {
            if (string.IsNullOrEmpty(routeName))
                throw new ArgumentException("Route name cannot be null or empty", nameof(routeName));
                
            if (_routeViewMap.TryGetValue(routeName, out Type pageType))
                return pageType;
                
            throw new KeyNotFoundException($"No view registered for route '{routeName}'");
        }
    }
}