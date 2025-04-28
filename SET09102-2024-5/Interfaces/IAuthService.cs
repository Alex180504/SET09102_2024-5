using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface IAuthService : IBaseService
    {
        event EventHandler UserChanged;
        
        Task<bool> RegisterUserAsync(string firstName, string lastName, string email, string password);
        Task<User> AuthenticateAsync(string email, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<User> GetCurrentUserAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<List<string>> GetUserPermissionsAsync(int userId);
        void SetCurrentUser(User user);
        void Logout();
        
        // Added method to invalidate user cache - critical for roles/permissions updates
        void InvalidateUserCache(int userId);
    }
}