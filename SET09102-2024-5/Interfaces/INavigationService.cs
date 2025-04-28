using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Views;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Provides navigation functionality between pages, abstracting MAUI's navigation
    /// </summary>
    public interface INavigationService : IBaseService
    {
        /// <summary>
        /// Navigate to a route by name
        /// </summary>
        Task NavigateToAsync(string route);
        
        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        Task GoBackAsync();
        
        /// <summary>
        /// Navigate to the login page
        /// </summary>
        Task NavigateToLoginAsync();
        
        /// <summary>
        /// Navigate to the registration page
        /// </summary>
        Task NavigateToRegisterAsync();
        
        /// <summary>
        /// Navigate to the main page
        /// </summary>
        Task NavigateToMainPageAsync();
        
        /// <summary>
        /// Navigate to the admin dashboard
        /// </summary>
        Task NavigateToAdminDashboardAsync();
        
        /// <summary>
        /// Navigate to the role management page
        /// </summary>
        Task NavigateToRoleManagementAsync();
        
        /// <summary>
        /// Navigate to the user role management page
        /// </summary>
        Task NavigateToUserRoleManagementAsync();
        
        /// <summary>
        /// Navigate to a view by its type
        /// </summary>
        Task NavigateToViewAsync<TView>() where TView : ViewBase;
        
        /// <summary>
        /// Enable the flyout menu
        /// </summary>
        Task EnableFlyoutAsync();
        
        /// <summary>
        /// Disable the flyout menu
        /// </summary>
        Task DisableFlyoutAsync();
        
        /// <summary>
        /// Check if navigation to a specific route is allowed
        /// </summary>
        Task<bool> CanNavigateToRouteAsync(string route);
    }
}

