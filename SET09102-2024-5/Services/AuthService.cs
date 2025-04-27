using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

// Explicitly import the Preferences namespace
using Preferences = Microsoft.Maui.Storage.Preferences;

namespace SET09102_2024_5.Services
{
    public class AuthService : IAuthService
    {
        private readonly SensorMonitoringContext _dbContext;
        private readonly ILoggingService _loggingService;
        private User _currentUser;
        private Task _initializationTask;
        private bool _isInitialized = false;
        private const string ServiceName = "Authentication Service";
        
        // Keys for storing authentication data
        private const string UserIdKey = "AuthUserId";
        private const string UserEmailKey = "AuthUserEmail";
        private const string AuthCategory = "Authentication";

        public event EventHandler UserChanged;

        protected virtual void OnUserChanged()
        {
            UserChanged?.Invoke(this, EventArgs.Empty);
        }

        public AuthService(SensorMonitoringContext dbContext, ILoggingService loggingService)
        {
            _dbContext = dbContext;
            _loggingService = loggingService;
        }

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;
                
            _loggingService.Info("Initializing authentication service", AuthCategory);
            _initializationTask = InitializeAuthenticationAsync();
            
            try
            {
                await _initializationTask;
                _isInitialized = true;
                _loggingService.Info("Authentication service initialized successfully", AuthCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Failed to initialize authentication", ex, AuthCategory);
                return false;
            }
            finally
            {
                _initializationTask = null;
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
            _loggingService.Info("Cleaning up authentication service", AuthCategory);
            // No specific cleanup needed for this service
            await Task.CompletedTask;
        }
        
        // Public method to ensure authentication is initialized before proceeding
        public async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
        
        // Renamed and improved method for loading saved user session
        private async Task InitializeAuthenticationAsync()
        {
            try
            {
                _loggingService.Debug("Loading saved authentication session", AuthCategory);
                
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
                                _loggingService.Info($"User {user.Email} session restored", AuthCategory);
                                // Set as current user
                                _currentUser = user;
                                OnUserChanged();
                            }
                            else
                            {
                                _loggingService.Warning($"Saved user session not found in database: {userId}", AuthCategory);
                                // User not found in database, clear preferences
                                ClearSavedUserSession();
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.Error("Database error during authentication restoration", ex, AuthCategory);
                            ClearSavedUserSession();
                        }
                    }
                    else
                    {
                        _loggingService.Warning("Invalid stored authentication data", AuthCategory);
                        // Invalid stored data, clear preferences
                        ClearSavedUserSession();
                    }
                }
                else
                {
                    _loggingService.Debug("No saved authentication session found", AuthCategory);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error loading saved session", ex, AuthCategory);
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
                    _loggingService.Debug($"User session saved: {user.Email}", AuthCategory);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error saving user session", ex, AuthCategory);
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
                    
                _loggingService.Debug("User session cleared", AuthCategory);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error clearing user session", ex, AuthCategory);
            }
        }

        public async Task<bool> RegisterUserAsync(string firstName, string lastName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || 
                string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password))
            {
                _loggingService.Warning("Registration attempt with invalid data", AuthCategory);
                return false;
            }

            try
            {
                _loggingService.Info($"Attempting to register user: {email}", AuthCategory);
                
                // Check if user already exists
                if (await _dbContext.Users.AnyAsync(u => u.Email == email))
                {
                    _loggingService.Warning($"Registration failed - user already exists: {email}", AuthCategory);
                    return false;
                }

                // Get guest role (create if doesn't exist)
                var guestRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guest");
                if (guestRole == null)
                {
                    _loggingService.Info("Creating missing Guest role", AuthCategory);
                    guestRole = new Role
                    {
                        RoleName = "Guest",
                        Description = "Limited access role for new users"
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
                _loggingService.Info($"User registered successfully: {email}", AuthCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error registering user: {email}", ex, AuthCategory);
                return false;
            }
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _loggingService.Warning("Authentication attempt with empty credentials", AuthCategory);
                return null;
            }

            try
            {
                _loggingService.Debug($"Authentication attempt: {email}", AuthCategory);
                
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    _loggingService.Warning($"Authentication failed - user not found: {email}", AuthCategory);
                    return null;
                }

                if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    _loggingService.Warning($"Authentication failed - invalid password: {email}", AuthCategory);
                    return null;
                }

                _currentUser = user;
                
                // Save user session for persistent login
                SaveUserSession(user);
                
                _loggingService.Info($"User authenticated successfully: {email}", AuthCategory);
                OnUserChanged(); // Trigger the event when authentication changes
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Authentication error", ex, AuthCategory);
                return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (userId <= 0 || 
                string.IsNullOrWhiteSpace(currentPassword) || 
                string.IsNullOrWhiteSpace(newPassword))
            {
                _loggingService.Warning($"Invalid password change request for user ID: {userId}", AuthCategory);
                return false;
            }

            try
            {
                _loggingService.Debug($"Password change attempt for user ID: {userId}", AuthCategory);
                
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    _loggingService.Warning($"Password change failed - user not found: {userId}", AuthCategory);
                    return false;
                }

                if (!VerifyPasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    _loggingService.Warning($"Password change failed - invalid current password: {userId}", AuthCategory);
                    return false;
                }

                CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                await _dbContext.SaveChangesAsync();
                _loggingService.Info($"Password changed successfully for user ID: {userId}", AuthCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error changing password for user ID: {userId}", ex, AuthCategory);
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionName))
            {
                _loggingService.Warning($"Invalid permission check request: User {userId}, Permission '{permissionName}'", AuthCategory);
                return false;
            }

            try
            {
                _loggingService.Debug($"Checking permission '{permissionName}' for user ID: {userId}", AuthCategory);
                
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .ThenInclude(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.AccessPrivilege)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null || user.Role == null)
                {
                    _loggingService.Warning($"Permission check failed - user or role not found: {userId}", AuthCategory);
                    return false;
                }

                // Check if user has admin role (admin role has all permissions)
                if (user.Role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    _loggingService.Debug($"Permission '{permissionName}' granted - user is Administrator: {userId}", AuthCategory);
                    return true;
                }

                // Check if the user's role has the specific permission
                bool hasPermission = user.Role.RolePrivileges.Any(rp => 
                    rp.AccessPrivilege != null &&
                    rp.AccessPrivilege.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
                
                _loggingService.Debug($"Permission '{permissionName}' for user {userId}: {hasPermission}", AuthCategory);
                return hasPermission;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error checking permission '{permissionName}' for user {userId}", ex, AuthCategory);
                return false;
            }
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(roleName))
            {
                _loggingService.Warning($"Invalid role check request: User {userId}, Role '{roleName}'", AuthCategory);
                return false;
            }

            try
            {
                _loggingService.Debug($"Checking if user {userId} is in role '{roleName}'", AuthCategory);
                
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null || user.Role == null)
                {
                    _loggingService.Warning($"Role check failed - user or role not found: {userId}", AuthCategory);
                    return false;
                }

                bool isInRole = user.Role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase);
                _loggingService.Debug($"User {userId} in role '{roleName}': {isInRole}", AuthCategory);
                return isInRole;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error checking if user {userId} is in role '{roleName}'", ex, AuthCategory);
                return false;
            }
        }

        public async Task<User> GetCurrentUserAsync()
        {
            // Ensure authentication is initialized before accessing current user
            await EnsureInitializedAsync();
            return _currentUser;
        }
        
        public async Task<bool> IsAuthenticatedAsync()
        {
            await EnsureInitializedAsync();
            return _currentUser != null;
        }
        
        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            if (userId <= 0)
            {
                _loggingService.Warning($"Invalid user ID for permission list: {userId}", AuthCategory);
                return new List<string>();
            }
            
            try
            {
                _loggingService.Debug($"Getting permissions for user ID: {userId}", AuthCategory);
                
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .ThenInclude(r => r.RolePrivileges)
                    .ThenInclude(rp => rp.AccessPrivilege)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                
                if (user == null || user.Role == null)
                {
                    _loggingService.Warning($"User or role not found for permission list: {userId}", AuthCategory);
                    return new List<string>();
                }
                
                // If administrator, return a special indicator
                if (user.Role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    _loggingService.Debug($"User {userId} is Administrator with all permissions", AuthCategory);
                    return new List<string> { "*" }; // * indicates all permissions
                }
                
                // Get the list of permission names
                var permissions = user.Role.RolePrivileges
                    .Where(rp => rp.AccessPrivilege != null)
                    .Select(rp => rp.AccessPrivilege.Name)
                    .ToList();
                
                _loggingService.Debug($"Retrieved {permissions.Count} permissions for user {userId}", AuthCategory);
                return permissions;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error getting permissions for user {userId}", ex, AuthCategory);
                return new List<string>();
            }
        }

        public void SetCurrentUser(User user)
        {
            var previousUser = _currentUser;
            _currentUser = user;
            
            if (user != null)
            {
                _loggingService.Info($"Setting current user: {user.Email}", AuthCategory);
                // Save user session for persistent login if a new user is set
                SaveUserSession(user);
            }
            else 
            {
                _loggingService.Info("Clearing current user", AuthCategory);
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
            if (_currentUser != null)
            {
                _loggingService.Info($"Logging out user: {_currentUser.Email}", AuthCategory);
            }
            
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
                // Use static method since we don't have access to the logger here
                System.Diagnostics.Debug.WriteLine($"Error verifying password hash: {ex.Message}");
                return false;
            }
        }
    }
}