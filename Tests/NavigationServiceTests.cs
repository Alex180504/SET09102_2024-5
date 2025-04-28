using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using SET09102_2024_5.Views;
using Xunit;

namespace SET09102_2024_5.Tests
{
    /// <summary>
    /// Tests for NavigationService class
    /// </summary>
    public class NavigationServiceTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILoggingService> _mockLoggingService;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public NavigationServiceTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLoggingService = new Mock<ILoggingService>();
            _mockServiceProvider = new Mock<IServiceProvider>();
        }

        /// <summary>
        /// Test NavigateToAsync with null route
        /// </summary>
        [Fact]
        public async Task NavigateToAsync_WithNullRoute_DoesNotNavigate()
        {
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            await navigationService.NavigateToAsync(null);

            // Assert
            _mockLoggingService.Verify(
                x => x.Warning(
                    It.Is<string>(m => m.Contains("empty route")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test NavigateToAsync with empty route
        /// </summary>
        [Fact]
        public async Task NavigateToAsync_WithEmptyRoute_DoesNotNavigate()
        {
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            await navigationService.NavigateToAsync(string.Empty);

            // Assert
            _mockLoggingService.Verify(
                x => x.Warning(
                    It.Is<string>(m => m.Contains("empty route")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test CanNavigateToRouteAsync with null route
        /// </summary>
        [Fact]
        public async Task CanNavigateToRouteAsync_WithNullRoute_ReturnsFalse()
        {
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            var result = await navigationService.CanNavigateToRouteAsync(null);

            // Assert
            Assert.False(result);
            _mockLoggingService.Verify(
                x => x.Warning(
                    It.Is<string>(m => m.Contains("empty route")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test CanNavigateToRouteAsync with public route
        /// </summary>
        [Fact]
        public async Task CanNavigateToRouteAsync_WithPublicRoute_ReturnsTrue()
        {
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            var result = await navigationService.CanNavigateToRouteAsync(RouteConstants.LoginPage);

            // Assert
            Assert.True(result);
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("publicly accessible")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test CanNavigateToRouteAsync with protected route when user is not authenticated
        /// </summary>
        [Fact]
        public async Task CanNavigateToRouteAsync_WithProtectedRouteAndUnauthenticated_ReturnsFalse()
        {
            // Arrange
            _mockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((User)null);

            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            var result = await navigationService.CanNavigateToRouteAsync(RouteConstants.MainPage);

            // Assert
            Assert.False(result);
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("requires authentication")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test CanNavigateToRouteAsync with admin route when user is authenticated but not admin
        /// </summary>
        [Fact]
        public async Task CanNavigateToRouteAsync_WithAdminRouteAndNotAdmin_ReturnsFalse()
        {
            // Arrange
            var testUser = new User { UserId = 1, Email = "test@example.com"};
            _mockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(testUser);
            _mockAuthService.Setup(x => x.IsInRoleAsync(It.IsAny<int>(), It.Is<string>(r => r == "Administrator")))
                .ReturnsAsync(false);

            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            var result = await navigationService.CanNavigateToRouteAsync(RouteConstants.AdminDashboardPage);

            // Assert
            Assert.False(result);
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("requires admin role")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test CanNavigateToRouteAsync with admin route when user is authenticated and is admin
        /// </summary>
        [Fact]
        public async Task CanNavigateToRouteAsync_WithAdminRouteAndIsAdmin_ReturnsTrue()
        {
            // Arrange
            var testUser = new User { UserId = 1, Email = "admin@example.com"};
            _mockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(testUser);
            _mockAuthService.Setup(x => x.IsInRoleAsync(It.IsAny<int>(), It.Is<string>(r => r == "Administrator")))
                .ReturnsAsync(true);

            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            var result = await navigationService.CanNavigateToRouteAsync(RouteConstants.AdminDashboardPage);

            // Assert
            Assert.True(result);
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("user has admin role")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test CanNavigateToRouteAsync with non-admin route when user is authenticated
        /// </summary>
        [Fact]
        public async Task CanNavigateToRouteAsync_WithNonAdminRouteAndAuthenticated_ReturnsTrue()
        {
            // Arrange
            var testUser = new User { UserId = 1, Email = "user@example.com"};
            _mockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(testUser);

            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            var result = await navigationService.CanNavigateToRouteAsync(RouteConstants.MainPage);

            // Assert
            Assert.True(result);
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("accessible to authenticated users")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test NavigateToViewAsync correctly converts view type to route
        /// </summary>
        [Fact]
        public async Task NavigateToViewAsync_ConvertsViewTypeToRoute()
        {
            // This test will check if NavigateToViewAsync correctly converts a view type to its route name
            
            // Since we can't directly test Shell navigation in a unit test environment,
            // we'll verify that the navigation service attempts to navigate to a route
            // that matches the view name
            
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Create a mock NavigationService that we can verify was called with the correct route
            var navigationServiceMock = new Mock<NavigationService>(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object) { CallBase = true };

            // Setup the mock to track calls to NavigateToAsync
            navigationServiceMock
                .Setup(x => x.NavigateToAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act - we need to use a class that derives from ViewBase
            await navigationServiceMock.Object.NavigateToViewAsync<TestViewBase>();

            // Assert
            // Check that the navigateToAsync was called with the correct route name
            navigationServiceMock.Verify(
                x => x.NavigateToAsync("TestViewBase"),
                Times.Once);
        }

        /// <summary>
        /// Test that EnableFlyoutAsync calls the right Shell method
        /// </summary>
        [Fact]
        public void EnableFlyoutAsync_LogsEnablingFlyoutMenu()
        {
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            navigationService.EnableFlyoutAsync();

            // Assert
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("Enabling flyout menu")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test that DisableFlyoutAsync calls the right Shell method
        /// </summary>
        [Fact]
        public void DisableFlyoutAsync_LogsDisablingFlyoutMenu()
        {
            // Arrange
            var navigationService = new NavigationService(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object);

            // Act
            navigationService.DisableFlyoutAsync();

            // Assert
            _mockLoggingService.Verify(
                x => x.Debug(
                    It.Is<string>(m => m.Contains("Disabling flyout menu")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Test that navigation helper methods call NavigateToAsync with correct route
        /// </summary>
        [Fact]
        public async Task NavigationHelperMethods_CallNavigateToAsyncWithCorrectRoute()
        {
            // Create a test-only subclass that allows us to verify navigation attempts
            var navigationServiceMock = new Mock<NavigationService>(
                _mockAuthService.Object,
                _mockLoggingService.Object,
                _mockServiceProvider.Object) { CallBase = true };

            // Setup the mock to track calls to NavigateToAsync
            navigationServiceMock
                .Setup(x => x.NavigateToAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var navigationService = navigationServiceMock.Object;

            // Test navigation helper methods
            await navigationService.NavigateToLoginAsync();
            navigationServiceMock.Verify(x => x.NavigateToAsync(RouteConstants.LoginPage), Times.Once);

            await navigationService.NavigateToRegisterAsync();
            navigationServiceMock.Verify(x => x.NavigateToAsync(RouteConstants.RegisterPage), Times.Once);

            await navigationService.NavigateToMainPageAsync();
            navigationServiceMock.Verify(x => x.NavigateToAsync(RouteConstants.MainPage), Times.Once);
        }
        
        // Test helper class that inherits from ViewBase for testing NavigateToViewAsync
        private class TestViewBase : ViewBase
        {
        }
    }
}