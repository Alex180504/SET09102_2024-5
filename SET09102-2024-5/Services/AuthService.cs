using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using SET09102_2024_5.Data;
using SET09102_2024_5.Models;

// Explicitly import the Preferences namespace
using Preferences = Microsoft.Maui.Storage.Preferences;

namespace SET09102_2024_5.Services
{
    public class AuthService : IAuthService
    {
        private readonly SensorMonitoringContext _dbContext;
        private User _currentUser;
        private Task _initializationTask;
        
        // Keys for storing authentication data
        private const string UserIdKey = "AuthUserId";
        private const string UserEmailKey = "AuthUserEmail";

        public event EventHandler UserChanged;

        protected virtual void OnUserChanged()
        {
            UserChanged?.Invoke(this, EventArgs.Empty);
        }

        public AuthService(SensorMonitoringContext dbContext)
        {
            _dbContext = dbContext;
            
            // Initialize the authentication state properly with awaitable task
            _initializationTask = InitializeAuthenticationAsync();
        }
        
        // Public method to ensure authentication is initialized before proceeding
        public async Task EnsureInitializedAsync()
        {
            if (_initializationTask != null)
            {
                try
                {
                    await _initializationTask;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Authentication initialization error: {ex.Message}");
                    // Reinitialize in case of failure
                    _initializationTask = InitializeAuthenticationAsync();
                    await _initializationTask;
                }
                finally
                {
                    _initializationTask = null;
                }
            }
        }
        
        // Renamed and improved method for loading saved user session
        private async Task InitializeAuthenticationAsync()
        {
            try
            {
                // Check if we have saved credentials
                if (Preferences.ContainsKey(UserIdKey) && Preferences.ContainsKey(UserEmailKey))
                {
                    int userId = Preferences.Get(UserIdKey, -1);
                    string email = Preferences.Get(UserEmailKey, string.Empty);
                    
                    if (userId > 0 && !string.IsNullOrEmpty(email))
                    {
                        try
                        {
                            // Retrieve the user from database with proper error handling
                            var user = await _dbContext.Users
                                .Include(u => u.Role)
                                .FirstOrDefaultAsync(u => u.UserId == userId && u.Email == email);
                            
                            if (user != null)
                            {
                                // Set as current user
                                _currentUser = user;
                                OnUserChanged();
                            }
                            else
                            {
                                // User not found in database, clear preferences
                                ClearSavedUserSession();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Database error during authentication: {ex.Message}");
                            ClearSavedUserSession();
                        }
                    }
                    else
                    {
                        // Invalid stored data, clear preferences
                        ClearSavedUserSession();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved session: {ex.Message}");
                // If there's any error, clear the saved session
                ClearSavedUserSession();
                throw; // Rethrow to allow proper handling by caller
            }
        }
        
        // Improved method to save user session with validation
        private void SaveUserSession(User user)
        {
            if (user != null && user.UserId > 0 && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    Preferences.Set(UserIdKey, user.UserId);
                    Preferences.Set(UserEmailKey, user.Email);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving user session: {ex.Message}");
                    // Silent failure - don't crash the app for preference storage issues
                }
            }
        }
        
        // Method to clear saved user session with error handling
        private void ClearSavedUserSession()
        {
            try
            {
                if (Preferences.ContainsKey(UserIdKey))
                    Preferences.Remove(UserIdKey);
                    
                if (Preferences.ContainsKey(UserEmailKey))
                    Preferences.Remove(UserEmailKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing user session: {ex.Message}");
                // Silent failure - don't crash the app for preference removal issues
            }
        }

        public async Task<bool> RegisterUserAsync(string firstName, string lastName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || 
                string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                // Check if user already exists
                if (await _dbContext.Users.AnyAsync(u => u.Email == email))
                {
                    return false;
                }

                // Get guest role (create if doesn't exist)
                var guestRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guest");
                if (guestRole == null)
                {
                    guestRole = new Role
                    {
                        RoleName = "Guest"
                    };
                    _dbContext.Roles.Add(guestRole);
                    await _dbContext.SaveChangesAsync();
                }

                // Create password hash and salt
                CreatePasswordHash(password, out string passwordHash, out string passwordSalt);

                // Create new user
                var user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = guestRole.RoleId,
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering user: {ex.Message}");
                return false;
            }
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                    return null;

                if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                    return null;

                _currentUser = user;
                
                // Save user session for persistent login
                SaveUserSession(user);
                
                OnUserChanged(); // Trigger the event when authentication changes
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return null;
            }
        }

        // Rest of the methods remain unchanged but with proper input validation and error handling
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (userId <= 0 || 
                string.IsNullOrWhiteSpace(currentPassword) || 
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                    return false;

                if (!VerifyPasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt))
                    return false;

                CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionName))
            {
                return false;
            }

            try
            {
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .ThenInclude(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.AccessPrivilege)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null || user.Role == null)
                    return false;

                // Check if user has admin role (admin role has all permissions)
                if (user.Role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Check if the user's role has the specific permission
                return user.Role.RolePrivileges.Any(rp => 
                    rp.AccessPrivilege != null &&
                    rp.AccessPrivilege.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking permission: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            try
            {
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null || user.Role == null)
                    return false;

                return user.Role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking role: {ex.Message}");
                return false;
            }
        }

        public async Task<User> GetCurrentUserAsync()
        {
            // Ensure authentication is initialized before accessing current user
            await EnsureInitializedAsync();
            return _currentUser;
        }

        public void SetCurrentUser(User user)
        {
            var previousUser = _currentUser;
            _currentUser = user;
            
            // Save user session for persistent login if a new user is set
            if (user != null)
            {
                SaveUserSession(user);
            }
            else 
            {
                ClearSavedUserSession();
            }
            
            // Only trigger the event if the user actually changed
            if ((previousUser == null && user != null) || 
                (previousUser != null && user == null) ||
                (previousUser != null && user != null && previousUser.UserId != user.UserId))
            {
                OnUserChanged();
            }
        }

        public void Logout()
        {
            _currentUser = null;
            
            // Clear saved session data on logout
            ClearSavedUserSession();
            
            OnUserChanged(); // Trigger the event when logout occurs
        }

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                
                using var hmac = new HMACSHA512(saltBytes);
                var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                
                return computedHash == storedHash;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verifying password hash: {ex.Message}");
                return false;
            }
        }
    }
}