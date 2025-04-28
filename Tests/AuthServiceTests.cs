// filepath: c:\Users\Owner\Downloads\MAUI_APP_DEV\SET09102_2024-5\Tests\AuthServiceTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using SET09102_2024_5.Services.Security;
using Xunit;

namespace SET09102_2024_5.Tests
{
    /// <summary>
    /// Tests for the AuthService class
    /// </summary>
    public class AuthServiceTests
    {
        // Mock dependencies for testing
        private readonly Mock<ILoggingService> _mockLoggingService;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly Mock<ICacheManager> _mockCacheManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly DbContextOptions<SensorMonitoringContext> _dbContextOptions;

        /// <summary>
        /// Constructor to set up common test dependencies
        /// </summary>
        public AuthServiceTests()
        {
            _mockLoggingService = new Mock<ILoggingService>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockCacheManager = new Mock<ICacheManager>();
            _mockTokenService = new Mock<ITokenService>();
            
            // Setup in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseInMemoryDatabase(databaseName: $"AuthServiceTestDb_{Guid.NewGuid()}")
                .Options;
        }

        /// <summary>
        /// Helper method to create SensorMonitoringContext for testing
        /// </summary>
        private SensorMonitoringContext CreateDbContext()
        {
            var context = new SensorMonitoringContext(_dbContextOptions);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Helper method to seed the test database with test data
        /// </summary>
        private async Task SeedTestDataAsync(SensorMonitoringContext context)
        {
            // Create test roles
            var adminRole = new Role
            {
                RoleId = 1,
                RoleName = "Administrator",
                Description = "Administrator role with all permissions"
            };
            
            var guestRole = new Role
            {
                RoleId = 2,
                RoleName = "Guest", 
                Description = "Limited access role for new users"
            };

            context.Roles.Add(adminRole);
            context.Roles.Add(guestRole);
            await context.SaveChangesAsync();

            // Setup password hash/salt for testing
            _mockPasswordHasher
                .Setup(ph => ph.CreatePasswordHash(It.IsAny<string>(), out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
                .Callback<string, string, string>((password, out string hash, out string salt) => 
                {
                    hash = "hashedPassword";
                    salt = "passwordSalt";
                });

            _mockPasswordHasher
                .Setup(ph => ph.VerifyPasswordHash(It.Is<string>(p => p == "correctPassword"), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            _mockPasswordHasher
                .Setup(ph => ph.VerifyPasswordHash(It.Is<string>(p => p != "correctPassword"), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            // Create test users
            var adminUser = new User
            {
                UserId = 1,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                PasswordHash = "hashedPassword",
                PasswordSalt = "passwordSalt",
                RoleId = adminRole.RoleId,
                Role = adminRole
            };

            var guestUser = new User
            {
                UserId = 2,
                FirstName = "Guest",
                LastName = "User",
                Email = "guest@example.com",
                PasswordHash = "hashedPassword",
                PasswordSalt = "passwordSalt",
                RoleId = guestRole.RoleId,
                Role = guestRole
            };

            context.Users.Add(adminUser);
            context.Users.Add(guestUser);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Test that AuthService initializes correctly
        /// </summary>
        [Fact]
        public async Task InitializeAsync_ShouldSetServiceAsReady()
        {
            // Arrange
            using var context = CreateDbContext();
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            
            // Act
            bool result = await authService.InitializeAsync();
            bool isReady = await authService.IsReadyAsync();
            
            // Assert
            Assert.True(result);
            Assert.True(isReady);
            Assert.Equal("Ready", authService.GetServiceStatus());
        }

        /// <summary>
        /// Test that GetServiceName returns the correct name
        /// </summary>
        [Fact]
        public void GetServiceName_ReturnsCorrectName()
        {
            // Arrange
            using var context = CreateDbContext();
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            
            // Act
            string serviceName = authService.GetServiceName();
            
            // Assert
            Assert.Equal("Authentication Service", serviceName);
        }

        /// <summary>
        /// Test that authentication with valid credentials returns a user
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnUser()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            _mockTokenService
                .Setup(ts => ts.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TokenInfo { UserId = 1, Email = "admin@example.com", Token = "valid-token", Expiry = DateTime.Now.AddDays(1) });
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            var user = await authService.AuthenticateAsync("admin@example.com", "correctPassword");
            
            // Assert
            Assert.NotNull(user);
            Assert.Equal("admin@example.com", user.Email);
            Assert.Equal("Admin", user.FirstName);
            Assert.Equal("User", user.LastName);
            Assert.Equal(1, user.UserId);
        }
        
        /// <summary>
        /// Test that authentication with invalid credentials returns null
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WithInvalidCredentials_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            var user = await authService.AuthenticateAsync("admin@example.com", "wrongPassword");
            
            // Assert
            Assert.Null(user);
        }
        
        /// <summary>
        /// Test that authentication with non-existent user returns null
        /// </summary>
        [Fact]
        public async Task AuthenticateAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            var user = await authService.AuthenticateAsync("nonexistent@example.com", "anyPassword");
            
            // Assert
            Assert.Null(user);
        }

        /// <summary>
        /// Test that registration with valid details succeeds
        /// </summary>
        [Fact]
        public async Task RegisterUserAsync_WithValidDetails_ShouldSucceed()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool result = await authService.RegisterUserAsync(
                "New", "User", "new.user@example.com", "newPassword");
            
            // Assert
            Assert.True(result);
            var newUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "new.user@example.com");
            Assert.NotNull(newUser);
            Assert.Equal("New", newUser.FirstName);
            Assert.Equal("User", newUser.LastName);
            Assert.Equal(2, newUser.RoleId); // Should be assigned Guest role
        }
        
        /// <summary>
        /// Test that registration with duplicate email fails
        /// </summary>
        [Fact]
        public async Task RegisterUserAsync_WithDuplicateEmail_ShouldFail()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool result = await authService.RegisterUserAsync(
                "Duplicate", "User", "admin@example.com", "password");
            
            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Test that changing password with valid credentials succeeds
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_WithValidCredentials_ShouldSucceed()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool result = await authService.ChangePasswordAsync(1, "correctPassword", "newPassword");
            
            // Assert
            Assert.True(result);
        }
        
        /// <summary>
        /// Test that changing password with invalid credentials fails
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_WithInvalidCredentials_ShouldFail()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool result = await authService.ChangePasswordAsync(1, "wrongPassword", "newPassword");
            
            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Test that IsInRoleAsync correctly identifies user roles
        /// </summary>
        [Fact]
        public async Task IsInRoleAsync_WithValidUser_ShouldReturnCorrectResult()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);

            // Setup cache manager mock
            _mockCacheManager
                .Setup(cm => cm.GetOrCreateAsync(
                    It.IsAny<string>(), 
                    It.IsAny<Func<Task<string>>>(), 
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync((string key, Func<Task<string>> factory, TimeSpan timeSpan) => factory().Result);
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool adminUserIsAdmin = await authService.IsInRoleAsync(1, "Administrator");
            bool adminUserIsGuest = await authService.IsInRoleAsync(1, "Guest");
            bool guestUserIsAdmin = await authService.IsInRoleAsync(2, "Administrator");
            bool guestUserIsGuest = await authService.IsInRoleAsync(2, "Guest");
            
            // Assert
            Assert.True(adminUserIsAdmin);
            Assert.False(adminUserIsGuest);
            Assert.False(guestUserIsAdmin);
            Assert.True(guestUserIsGuest);
        }

        /// <summary>
        /// Test that HasPermissionAsync works correctly for admin users (who have all permissions)
        /// </summary>
        [Fact]
        public async Task HasPermissionAsync_ForAdminUser_ShouldReturnTrue()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);

            // Setup cache manager mock to simulate admin permissions
            _mockCacheManager
                .Setup(cm => cm.GetOrCreateAsync<List<string>>(
                    It.IsAny<string>(), 
                    It.IsAny<Func<Task<List<string>>>>(), 
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<string> { "*" }); // * indicates all permissions for admin
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool result = await authService.HasPermissionAsync(1, "AnyPermission");
            
            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Test that HasPermissionAsync works correctly for regular users
        /// </summary>
        [Fact]
        public async Task HasPermissionAsync_ForRegularUser_ShouldReturnCorrectResult()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);

            // Setup cache manager mock to simulate regular user permissions
            _mockCacheManager
                .Setup(cm => cm.GetOrCreateAsync<List<string>>(
                    It.IsAny<string>(), 
                    It.IsAny<Func<Task<List<string>>>>(), 
                    It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<string> { "ViewDashboard", "ViewAlerts" });
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool hasPermission = await authService.HasPermissionAsync(2, "ViewDashboard");
            bool noPermission = await authService.HasPermissionAsync(2, "EditSettings");
            
            // Assert
            Assert.True(hasPermission);
            Assert.False(noPermission);
        }

        /// <summary>
        /// Test that GetCurrentUserAsync returns null when not authenticated
        /// </summary>
        [Fact]
        public async Task GetCurrentUserAsync_WhenNotAuthenticated_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateDbContext();
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            var currentUser = await authService.GetCurrentUserAsync();
            
            // Assert
            Assert.Null(currentUser);
        }

        /// <summary>
        /// Test that GetCurrentUserAsync returns the current user when authenticated
        /// </summary>
        [Fact]
        public async Task GetCurrentUserAsync_WhenAuthenticated_ShouldReturnUser()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            _mockTokenService
                .Setup(ts => ts.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TokenInfo { UserId = 1, Email = "admin@example.com", Token = "valid-token", Expiry = DateTime.Now.AddDays(1) });

            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Authenticate a user first
            var authenticatedUser = await authService.AuthenticateAsync("admin@example.com", "correctPassword");
            
            // Act
            var currentUser = await authService.GetCurrentUserAsync();
            
            // Assert
            Assert.NotNull(currentUser);
            Assert.Equal("admin@example.com", currentUser.Email);
        }

        /// <summary>
        /// Test that IsAuthenticatedAsync returns false when not authenticated
        /// </summary>
        [Fact]
        public async Task IsAuthenticatedAsync_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Arrange
            using var context = CreateDbContext();
            
            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Act
            bool isAuthenticated = await authService.IsAuthenticatedAsync();
            
            // Assert
            Assert.False(isAuthenticated);
        }
        
        /// <summary>
        /// Test that IsAuthenticatedAsync returns true when authenticated
        /// </summary>
        [Fact]
        public async Task IsAuthenticatedAsync_WhenAuthenticated_ShouldReturnTrue()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            _mockTokenService
                .Setup(ts => ts.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TokenInfo { UserId = 1, Email = "admin@example.com", Token = "valid-token", Expiry = DateTime.Now.AddDays(1) });

            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Authenticate a user first
            var user = await authService.AuthenticateAsync("admin@example.com", "correctPassword");
            
            // Act
            bool isAuthenticated = await authService.IsAuthenticatedAsync();
            
            // Assert
            Assert.True(isAuthenticated);
        }

        /// <summary>
        /// Test that UserChanged event is triggered on logout
        /// </summary>
        [Fact]
        public async Task Logout_ShouldTriggerUserChangedEvent()
        {
            // Arrange
            using var context = CreateDbContext();
            await SeedTestDataAsync(context);
            
            _mockTokenService
                .Setup(ts => ts.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new TokenInfo { UserId = 1, Email = "admin@example.com", Token = "valid-token", Expiry = DateTime.Now.AddDays(1) });

            var authService = new AuthService(context, _mockLoggingService.Object, 
                _mockPasswordHasher.Object, _mockCacheManager.Object, _mockTokenService.Object);
            await authService.InitializeAsync();
            
            // Authenticate a user first
            var user = await authService.AuthenticateAsync("admin@example.com", "correctPassword");
            
            // Setup event tracking
            bool eventTriggered = false;
            authService.UserChanged += (sender, args) => eventTriggered = true;
            
            // Act
            authService.Logout();
            
            // Assert
            Assert.True(eventTriggered);
            Assert.False(await authService.IsAuthenticatedAsync());
        }
    }
}