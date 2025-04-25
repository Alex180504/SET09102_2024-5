using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Services;
using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Views;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.Maui;
using SET09102_2024_5.Interfaces;


namespace SET09102_2024_5
{
    public static class MauiProgram
    {
        public static string ConnectionString { get; private set; }
        public static string CertPath { get; private set; }
        public static bool IsDatabaseConnectionSuccessful { get; private set; } = false;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            try
            {
                // Load configuration
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("SET09102_2024_5.appsettings.json");

                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                // Get connection string from configuration
                ConnectionString = config.GetConnectionString("DefaultConnection");

                // Extract SSL certificate and save it to a temporary file
                CertPath = ExtractSslCertificate();
                ConnectionString = ConnectionString.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={CertPath};");

                // Register a factory for DbContextOptions rather than the DbContext itself
                builder.Services.AddSingleton<DbContextOptions<SensorMonitoringContext>>(serviceProvider =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<SensorMonitoringContext>();
                    try
                    {
                        // Use a specific server version instead of auto-detect to avoid connection
                        var serverVersion = new MySqlServerVersion(new Version(8, 0, 32));
                        optionsBuilder.UseMySql(ConnectionString, serverVersion);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error configuring DbContext options: {ex.Message}");
                        // Still need to configure the options even if there's an error
                        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
                        optionsBuilder.UseMySql(ConnectionString, serverVersion);
                    }
                    return optionsBuilder.Options;
                });

                // Register the context itself
                builder.Services.AddScoped<SensorMonitoringContext>();

                // Register database initialization service
                builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();

                // Register repositories
                builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

                // Register services
                builder.Services.AddScoped<IDatabaseService, DatabaseService>();

                // Register ViewModels
                builder.Services.AddTransient<MainPageViewModel>();
                builder.Services.AddTransient<SensorManagementViewModel>();

                // Register Views
                builder.Services.AddTransient<MainPage>();
                builder.Services.AddTransient<SensorManagementPage>();

                builder.Services.AddSingleton<IMainThreadService, MainThreadService>();
                builder.Services.AddSingleton<IDialogService, DialogService>();
                builder.Services.AddSingleton<INavigationService, NavigationService>();

#if DEBUG
                builder.Logging.AddDebug();
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Application initialization error: {ex.Message}");
            }

            return builder.Build();
        }

        private static string ExtractSslCertificate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var certStream = assembly.GetManifestResourceStream("SET09102_2024_5.DigiCertGlobalRootG2.crt.pem");

            if (certStream == null)
            {
                throw new InvalidOperationException("Could not find DigiCertGlobalRootG2.crt.pem embedded resource.");
            }

            // Create temp file for the certificate
            string tempPath = Path.Combine(FileSystem.CacheDirectory, "DigiCertGlobalRootG2.crt.pem");

            using (var fileStream = File.Create(tempPath))
            {
                certStream.CopyTo(fileStream);
            }

            return tempPath;
        }
    }

    public interface IDatabaseInitializationService
    {
        Task<bool> InitializeDatabaseAsync();
        bool IsDatabaseAvailable { get; }
        string GetLastErrorMessage();
    }

    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly SensorMonitoringContext _dbContext;
        private bool _isDatabaseAvailable = false;
        private string _lastErrorMessage = string.Empty;

        public bool IsDatabaseAvailable => _isDatabaseAvailable;

        public DatabaseInitializationService(SensorMonitoringContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string GetLastErrorMessage() => _lastErrorMessage;

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                // Try to connect to the database
                bool canConnect = await _dbContext.Database.CanConnectAsync();
                _isDatabaseAvailable = canConnect;
                return canConnect;
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Database connection error: {ex.Message}");
                _isDatabaseAvailable = false;
                return false;
            }
        }
    }
}
