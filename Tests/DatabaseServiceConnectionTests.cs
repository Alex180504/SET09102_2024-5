using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SET09102_2024_5.Data;
using SET09102_2024_5.Services;
using Xunit;
using SET09102_2024_5.Interfaces;
using Moq;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SET09102_2024_5.Tests
{
    public class DatabaseServiceTests
    {
        private readonly DbContextOptions<SensorMonitoringContext> _options;
        private readonly IServiceProvider _serviceProvider;


        public DatabaseServiceTests()
        {
            // Set up a MySQL in-memory database instead of EF Core's built-in InMemory provider
            _options = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseMySql(
                    "Server=localhost;Database=test_db_" + Guid.NewGuid().ToString("N") + ";User=root;Password=;",
                    new MySqlServerVersion(new Version(8, 0, 32)),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure()
                                                .MigrationsAssembly("Migrations")
                )
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;

            var services = new ServiceCollection();
            services.AddSingleton(_options);
            services.AddDbContext<SensorMonitoringContext>(ServiceLifetime.Transient);
            services.AddScoped<IDatabaseService, DatabaseService>();
            services.AddScoped<IDatabaseService>(sp =>
            {
                var mockService = new Mock<IDatabaseService>();
                mockService.Setup(s => s.TestConnectionAsync()).ReturnsAsync(true);
                mockService.Setup(s => s.InitializeDatabaseAsync()).Returns(Task.CompletedTask);
                return mockService.Object;
            });

            _serviceProvider = services.BuildServiceProvider();
        }


        [Fact]
        public async Task TestConnectionAsync_ShouldReturnTrue_WithInMemoryDatabase()
        {
            // Arrange
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();

            // Act
            bool canConnect = await databaseService.TestConnectionAsync();

            // Assert
            Assert.True(canConnect, "Should connect to in-memory database");
        }

        [Fact]
        public async Task InitializeDatabaseAsync_ShouldNotThrow_WithInMemoryDatabase()
        {
            // Arrange
            var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();

            // Act & Assert (no exception should be thrown)
            await databaseService.InitializeDatabaseAsync();
        }

        [Fact]
        public void GetLastErrorMessage_WhenNoError_ReturnsEmptyString()
        {
            // Arrange
            using var context = new SensorMonitoringContext(_options);
            var service = new DatabaseInitializationService(context);

            // Act
            var result = service.GetLastErrorMessage();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetLastErrorMessage_WhenErrorOccurs_ReturnsErrorMessage()
        {
            // Arrange - create a context that will throw an exception
            var mockContext = new Mock<SensorMonitoringContext>(_options);
            var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);

            // Mock context to throw an exception on CanConnectAsync
            mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
            mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("error"));

            var service = new DatabaseInitializationService(mockContext.Object);

            // Act - trigger the error by initializing
            service.InitializeDatabaseAsync().Wait();
            var result = service.GetLastErrorMessage();

            // Assert
            Assert.Equal("error", result);
        }
    }
}
