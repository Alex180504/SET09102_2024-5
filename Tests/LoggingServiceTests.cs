using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Moq;
using SET09102_2024_5.Services;
using Xunit;

namespace SET09102_2024_5.Tests
{
    /// <summary>
    /// Tests for the LoggingService class
    /// </summary>
    public class LoggingServiceTests
    {
        /// <summary>
        /// Test that InitializeAsync correctly initializes the service
        /// </summary>
        [Fact]
        public async Task InitializeAsync_ShouldSetServiceAsReady()
        {
            // Arrange
            var loggingService = new LoggingService();
            
            // Act
            bool result = await loggingService.InitializeAsync();
            bool isReady = await loggingService.IsReadyAsync();
            
            // Assert
            Assert.True(result);
            Assert.True(isReady);
            Assert.Equal("Ready", loggingService.GetServiceStatus());
        }
        
        /// <summary>
        /// Test that GetServiceName returns the correct name
        /// </summary>
        [Fact]
        public void GetServiceName_ReturnsCorrectName()
        {
            // Arrange
            var loggingService = new LoggingService();
            
            // Act
            string serviceName = loggingService.GetServiceName();
            
            // Assert
            Assert.Equal("Logging Service", serviceName);
        }
        
        /// <summary>
        /// Test that logging methods don't throw exceptions
        /// </summary>
        [Fact]
        public async Task LoggingMethods_ShouldNotThrow()
        {
            // Arrange
            var loggingService = new LoggingService();
            await loggingService.InitializeAsync();
            
            // Act & Assert - these should not throw exceptions
            loggingService.Debug("Debug message");
            loggingService.Info("Info message");
            loggingService.Warning("Warning message");
            loggingService.Error("Error message");
            loggingService.Error("Error with exception", new Exception("Test exception"));
            
            // Additional verification that the service is still operational
            Assert.True(await loggingService.IsReadyAsync());
        }
        
        /// <summary>
        /// Test that logging with custom categories works
        /// </summary>
        [Fact]
        public async Task LoggingWithCategories_ShouldNotThrow()
        {
            // Arrange
            var loggingService = new LoggingService();
            await loggingService.InitializeAsync();
            
            // Act & Assert - these should not throw exceptions
            loggingService.Debug("Debug message", "TestCategory");
            loggingService.Info("Info message", "TestCategory");
            loggingService.Warning("Warning message", "TestCategory");
            loggingService.Error("Error message", null, "TestCategory");
            
            // Additional verification that the service is still operational
            Assert.True(await loggingService.IsReadyAsync());
        }
        
        /// <summary>
        /// Test that CleanupAsync doesn't throw exceptions
        /// </summary>
        [Fact]
        public async Task CleanupAsync_ShouldNotThrow()
        {
            // Arrange
            var loggingService = new LoggingService();
            await loggingService.InitializeAsync();
            
            // Log some messages
            loggingService.Info("Test message before cleanup");
            
            // Act & Assert - should not throw exception
            await loggingService.CleanupAsync();
            
            // Additional verification that the service is still operational
            Assert.True(await loggingService.IsReadyAsync());
        }
    }
}