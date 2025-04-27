using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Services
{
    public interface INavigationService : IBaseService
    {
        // Basic navigation
        Task NavigateToAsync(string route);
        Task GoBackAsync();
        
        // Authentication-related navigation
        Task NavigateToLoginAsync();
        Task NavigateToRegisterAsync();
        Task NavigateToMainPageAsync();
        
        // Role-based navigation
        Task NavigateToAdminDashboardAsync();
        Task NavigateToRoleManagementAsync();
        Task NavigateToUserRoleManagementAsync();
        
        // UI state management
        Task EnableFlyoutAsync();
        Task DisableFlyoutAsync();
        
        // Check navigation permissions
        Task<bool> CanNavigateToRouteAsync(string route);
    }
}