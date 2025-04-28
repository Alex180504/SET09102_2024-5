using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services.Common;
using SET09102_2024_5.Services.Security;

// Explicitly import the Preferences namespace
using Preferences = Microsoft.Maui.Storage.Preferences;

namespace SET09102_2024_5.Services
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly SensorMonitoringContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICacheManager _cacheManager;
        private readonly ITokenService _tokenService;
        private User _currentUser;
        private Task _initializationTask;
        
        // Keys for storing authentication data
        private const string AuthTokenKey = "AuthToken";
        private const string AuthCategory = "Authentication";
        
        // Cache keys
        private const string UserPermissionsCacheKeyPrefix = "user_permissions_";
        private const string UserRoleCacheKeyPrefix = "user_role_";

        public event EventHandler UserChanged;

        protected virtual void OnUserChanged()
        {
            UserChanged?.Invoke(this, EventArgs.Empty);
        }

        public AuthService(
            SensorMonitoringContext dbContext, 
            ILoggingService loggingService,
            IPasswordHasher passwordHasher,
            ICacheManager cacheManager,
            ITokenService tokenService)
            : base("Authentication Service", AuthCategory, loggingService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        protected override async Task InitializeInternalAsync()
        {
            _initializationTask = InitializeAuthenticationAsync();
            await _initializationTask;
            _initializationTask = null;
        }
        
        // Public method to ensure authentication is initialized before proceeding
        public async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
        
        // Method for loading saved user session
        private async Task InitializeAuthenticationAsync()
        {
            var result = await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    // Extract into separate method to improve readability
                    if (await TryRestoreUserSessionAsync())
                    {
                        return true;
                    }

                    _loggingService.Debug("No saved authentication session found", _serviceCategory);
                    return true;
                },
                _loggingService,
                _serviceCategory,
                "InitializeAuthentication",
                false
            );

            // If operation itself failed (not the session restoration)
            if (!result.Success)
            {
                ClearSavedUserSession();
                throw new Exception("Authentication initialization failed");
            }
        }

        // Updated method to handle user session restoration using tokens
        private async Task<bool> TryRestoreUserSessionAsync()
        {
            // Check if we have a saved token
            if (!Preferences.ContainsKey(AuthTokenKey))
            {
                return false;
            }

            try
            {
                // Get the saved token info
                string tokenJson = Preferences.Get(AuthTokenKey, string.Empty);
                
                if (string.IsNullOrEmpty(tokenJson))
                {
                    _loggingService.Warning("Empty token found in preferences", _serviceCategory);
                    ClearSavedUserSession();
                    return false;
                }
                
                var tokenInfo = JsonSerializer.Deserialize<TokenInfo>(tokenJson);
                
                if (tokenInfo == null)
                {
                    _loggingService.Warning("Failed to deserialize token", _serviceCategory);
                    ClearSavedUserSession();
                    return false;
                }
                
                // Validate the token
                bool isValid = await _tokenService.ValidateTokenAsync(tokenInfo);
                
                if (!isValid)
                {
                    _loggingService.Warning($"Invalid or expired token for user {tokenInfo.UserId}", _serviceCategory);
                    ClearSavedUserSession();
                    return false;
                }

                // Retrieve the user from database with proper error handling
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == tokenInfo.UserId && u.Email == tokenInfo.Email);
                
                if (user == null)
                {
                    _loggingService.Warning($"Saved user session not found in database: {tokenInfo.UserId}", _serviceCategory);
                    ClearSavedUserSession();
                    return false;
                }

                _loggingService.Info($"User {user.Email} session restored", _serviceCategory);
                _currentUser = user;
                OnUserChanged();
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error restoring user session", ex, _serviceCategory);
                ClearSavedUserSession();
                return false;
            }
        }
        
        // Updated method to save user session with token
        private async Task SaveUserSessionAsync(User user)
        {
            if (user != null && user.UserId > 0 && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    // Generate a token for the user
                    var tokenInfo = await _tokenService.GenerateTokenAsync(user.UserId, user.Email);
                    
                    // Save the token info as JSON
                    string tokenJson = JsonSerializer.Serialize(tokenInfo);
                    Preferences.Set(AuthTokenKey, tokenJson);
                    
                    _loggingService.Debug($"User session saved with token: {user.Email}", _serviceCategory);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error saving user session", ex, _serviceCategory);
                }
            }
        }
        
        // Updated method to clear saved user session
        private void ClearSavedUserSession()
        {
            try
            {
                if (Preferences.ContainsKey(AuthTokenKey))
                    Preferences.Remove(AuthTokenKey);
                    
                _loggingService.Debug("User session cleared", _serviceCategory);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error clearing user session", ex, _serviceCategory);
            }
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _loggingService.Warning("Authentication attempt with empty credentials", _serviceCategory);
                return null;
            }

            // Use regular ExecuteAsync instead of ExecuteWithRetryAsync
            var result = await ServiceOperations.ExecuteAsync<User>(
                async () =>
                {
                    var user = await _dbContext.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Email == email);

                    if (user == null)
                    {
                        _loggingService.Warning($"Authentication failed - user not found: {email}", _serviceCategory);
                        return null;
                    }

                    if (!_passwordHasher.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                    {
                        _loggingService.Warning($"Authentication failed - invalid password: {email}", _serviceCategory);
                        return null;
                    }

                    return user;
                },
                _loggingService,
                _serviceCategory,
                $"Authenticate({email})",
                null
            );

            if (result.Success && result.Value != null)
            {
                _currentUser = result.Value;
                
                // Save user session with token for persistent login
                await SaveUserSessionAsync(_currentUser);
                
                _loggingService.Info($"User authenticated successfully: {email}", _serviceCategory);
                OnUserChanged(); // Trigger the event when authentication changes
            }

            return result.Value;
        }

        public void Logout()
        {
            if (_currentUser != null)
            {
                _loggingService.Info($"Logging out user: {_currentUser.Email}", _serviceCategory);
                
                // Clear any cached permissions for the user
                InvalidateUserCache(_currentUser.UserId);
                
                _currentUser = null;
            }
            
            // Clear saved session data on logout
            ClearSavedUserSession();
            
            OnUserChanged(); // Trigger the event when logout occurs
        }

        public async void SetCurrentUser(User user)
        {
            var previousUser = _currentUser;
            _currentUser = user;
            
            if (user != null)
            {
                _loggingService.Info($"Setting current user: {user.Email}", _serviceCategory);
                // Save user session with token for persistent login
                await SaveUserSessionAsync(user);

                // Clear any cached permissions for the user in case they've changed
                InvalidateUserCache(user.UserId);
            }
            else 
            {
                if (previousUser != null)
                {
                    // Clear any cached permissions for the previous user
                    InvalidateUserCache(previousUser.UserId);
                }

                _loggingService.Info("Clearing current user", _serviceCategory);
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

        public async Task<bool> RegisterUserAsync(string firstName, string lastName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || 
                string.IsNullOrWhiteSpace(email) || 
                string.IsNullOrWhiteSpace(password))
            {
                _loggingService.Warning("Registration attempt with invalid data", _serviceCategory);
                return false;
            }

            var result = await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    // Check if user already exists
                    if (await _dbContext.Users.AnyAsync(u => u.Email == email))
                    {
                        _loggingService.Warning($"Registration failed - user already exists: {email}", _serviceCategory);
                        return false;
                    }

                    // Get guest role (create if doesn't exist)
                    var guestRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guest");
                    if (guestRole == null)
                    {
                        _loggingService.Info("Creating missing Guest role", _serviceCategory);
                        guestRole = new Role
                        {
                            RoleName = "Guest",
                            Description = "Limited access role for new users"
                        };
                        _dbContext.Roles.Add(guestRole);
                        await _dbContext.SaveChangesAsync();
                    }

                    // Create password hash and salt using the password hasher service
                    _passwordHasher.CreatePasswordHash(password, out string passwordHash, out string passwordSalt);

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
                    _loggingService.Info($"User registered successfully: {email}", _serviceCategory);
                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"RegisterUser({email})",
                false
            );

            return result.Success && result.Value;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (userId <= 0 || 
                string.IsNullOrWhiteSpace(currentPassword) || 
                string.IsNullOrWhiteSpace(newPassword))
            {
                _loggingService.Warning($"Invalid password change request for user ID: {userId}", _serviceCategory);
                return false;
            }

            var result = await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    var user = await _dbContext.Users.FindAsync(userId);
                    if (user == null)
                    {
                        _loggingService.Warning($"Password change failed - user not found: {userId}", _serviceCategory);
                        return false;
                    }

                    if (!_passwordHasher.VerifyPasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt))
                    {
                        _loggingService.Warning($"Password change failed - invalid current password: {userId}", _serviceCategory);
                        return false;
                    }

                    _passwordHasher.CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);
                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;

                    await _dbContext.SaveChangesAsync();
                    
                    // If this is the current user, we need to update their token
                    if (_currentUser != null && _currentUser.UserId == userId)
                    {
                        await SaveUserSessionAsync(user);
                    }
                    
                    _loggingService.Info($"Password changed successfully for user ID: {userId}", _serviceCategory);
                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"ChangePassword(userId: {userId})",
                false
            );

            return result.Success && result.Value;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(permissionName))
            {
                _loggingService.Warning($"Invalid permission check request: User {userId}, Permission '{permissionName}'", _serviceCategory);
                return false;
            }

            try
            {
                // Get user permissions from cache or database
                var permissions = await GetUserPermissionsAsync(userId);

                // Check if user is admin (has all permissions)
                if (permissions.Contains("*"))
                {
                    _loggingService.Debug($"Permission '{permissionName}' granted - user is Administrator: {userId}", _serviceCategory);
                    return true;
                }

                // Check for the specific permission
                bool hasPermission = permissions.Any(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
                _loggingService.Debug($"Permission '{permissionName}' for user {userId}: {hasPermission}", _serviceCategory);
                return hasPermission;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error checking permission '{permissionName}' for user {userId}", ex, _serviceCategory);
                return false;
            }
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(roleName))
            {
                _loggingService.Warning($"Invalid role check request: User {userId}, Role '{roleName}'", _serviceCategory);
                return false;
            }

            // Cache key for this user's role
            string cacheKey = $"{UserRoleCacheKeyPrefix}{userId}";

            try
            {
                // Try to get the role from cache or database
                string userRoleName = await _cacheManager.GetOrCreateAsync(
                    cacheKey,
                    async () => 
                    {
                        // Not in cache, fetch from database
                        var result = await ServiceOperations.ExecuteAsync<string>(
                            async () =>
                            {
                                var user = await _dbContext.Users
                                    .Include(u => u.Role)
                                    .FirstOrDefaultAsync(u => u.UserId == userId);

                                if (user == null || user.Role == null)
                                {
                                    _loggingService.Warning($"Role check failed - user or role not found: {userId}", _serviceCategory);
                                    return null;
                                }

                                return user.Role.RoleName;
                            },
                            _loggingService,
                            _serviceCategory,
                            $"GetUserRole(userId: {userId})",
                            null
                        );

                        return result.Value;
                    },
                    TimeSpan.FromMinutes(10) // Cache user roles for 10 minutes
                );

                if (userRoleName == null)
                {
                    return false;
                }

                bool isInRole = userRoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase);
                _loggingService.Debug($"User {userId} in role '{roleName}': {isInRole}", _serviceCategory);
                return isInRole;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error checking if user {userId} is in role '{roleName}'", ex, _serviceCategory);
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
                _loggingService.Warning($"Invalid user ID for permission list: {userId}", _serviceCategory);
                return new List<string>();
            }

            // Cache key for this user's permissions
            string cacheKey = $"{UserPermissionsCacheKeyPrefix}{userId}";
            
            try
            {
                // Try to get permissions from cache or database with a 5-minute expiration
                var permissions = await _cacheManager.GetOrCreateAsync(
                    cacheKey,
                    async () => 
                    {
                        // Not in cache, fetch from database
                        var result = await ServiceOperations.ExecuteAsync<List<string>>(
                            async () =>
                            {
                                var user = await _dbContext.Users
                                    .Include(u => u.Role)
                                    .ThenInclude(r => r.RolePrivileges)
                                    .ThenInclude(rp => rp.AccessPrivilege)
                                    .FirstOrDefaultAsync(u => u.UserId == userId);
                                
                                if (user == null || user.Role == null)
                                {
                                    _loggingService.Warning($"User or role not found for permission list: {userId}", _serviceCategory);
                                    return new List<string>();
                                }
                                
                                // If administrator, return a special indicator
                                if (user.Role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                                {
                                    _loggingService.Debug($"User {userId} is Administrator with all permissions", _serviceCategory);
                                    return new List<string> { "*" }; // * indicates all permissions
                                }
                                
                                // Get the list of permission names
                                var permissions = user.Role.RolePrivileges
                                    .Where(rp => rp.AccessPrivilege != null)
                                    .Select(rp => rp.AccessPrivilege.Name)
                                    .ToList();
                                
                                _loggingService.Debug($"Retrieved {permissions.Count} permissions for user {userId}", _serviceCategory);
                                return permissions;
                            },
                            _loggingService,
                            _serviceCategory,
                            $"GetUserPermissions(userId: {userId})",
                            new List<string>()
                        );
                        
                        return result.Value ?? new List<string>();
                    },
                    TimeSpan.FromMinutes(5) // Cache user permissions for 5 minutes
                );

                return permissions;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving permissions for user {userId}", ex, _serviceCategory);
                return new List<string>();
            }
        }
        
        // Helper method to invalidate all cached data for a user
        private void InvalidateUserCache(int userId)
        {
            _cacheManager.Remove($"{UserPermissionsCacheKeyPrefix}{userId}");
            _cacheManager.Remove($"{UserRoleCacheKeyPrefix}{userId}");
        }
    }
}