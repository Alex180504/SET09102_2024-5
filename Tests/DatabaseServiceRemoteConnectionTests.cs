using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SET09102_2024_5.Data;
using SET09102_2024_5.Services;
using System.IO;
using System.Reflection;
using Xunit;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Tests
{
    public class DatabaseServiceRemoteConnectionTests
    {
        private readonly IConfiguration _configuration;
        private readonly IDatabaseService _databaseService;

        public DatabaseServiceRemoteConnectionTests()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var configStream = assembly.GetManifestResourceStream("Tests.appsettings.test.json");

                if (configStream == null)
                {
                    throw new InvalidOperationException("Could not find appsettings.test.json embedded resource.");
                }

                _configuration = new ConfigurationBuilder()
                    .AddJsonStream(configStream)
                    .Build();

                var serviceCollection = new ServiceCollection();
                string certPath = ExtractSslCertificateForTest();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                connectionString = connectionString.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={certPath};");
                serviceCollection.AddDbContext<SensorMonitoringContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
                serviceCollection.AddScoped<IDatabaseService, DatabaseService>();
                var serviceProvider = serviceCollection.BuildServiceProvider();
                _databaseService = serviceProvider.GetRequiredService<IDatabaseService>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize test: {ex.Message}", ex);
            }
        }

        [Fact]
        public async Task CanConnectToDatabase_ShouldReturnTrue_WhenCredentialsAreValid()
        {
            try
            {
                bool canConnect = await _databaseService.TestConnectionAsync();
                Assert.True(canConnect, "Failed to connect to the database. Check connection string and credentials.");
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Database connection test failed with exception: {ex.Message}");
            }

        }

        [Fact]
        public async Task InitializeDatabaseAsync_ShouldNotThrow_WhenDatabaseIsAccessible()
        {
            try
            {
                await _databaseService.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Database initialization failed with exception: {ex.Message}");
            }
        }

        private static string ExtractSslCertificateForTest()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var certStream = assembly.GetManifestResourceStream("Tests.DigiCertGlobalRootG2.crt.pem");
            if (certStream == null)
            {
                throw new InvalidOperationException("Could not find DigiCertGlobalRootG2.crt.pem embedded resource. " +
                    "Make sure the file exists and is set as an Embedded Resource in project properties.");
            }
            string tempPath = Path.Combine(Path.GetTempPath(), "DigiCertGlobalRootG2.crt.pem");
            using (var fileStream = File.Create(tempPath))
            {
                certStream.CopyTo(fileStream);
            }
            return tempPath;
        }
    }
}
