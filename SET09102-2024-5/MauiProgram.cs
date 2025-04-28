using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;


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
                .UseSkiaSharp()
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
#if DEBUG
                ConnectionString = config.GetConnectionString("LocalConnection");
#else
                ConnectionString = config.GetConnectionString("DefaultConnection");
#endif
                var backupFolder = Path.Combine(FileSystem.CacheDirectory, "Backups");
                Directory.CreateDirectory(backupFolder);
                builder.Services.AddSingleton(new BackupOptions
                {
                    ScheduleTime = TimeSpan.FromHours(2),
                    KeepLatestBackups = 7,
                    BackupFolder = backupFolder
                });
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

                // Your DbContext (scoped)
                builder.Services.AddDbContextFactory<SensorMonitoringContext>(opts =>
    opts.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString)));
                builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();

                // Repositories (scoped)
                builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                builder.Services.AddScoped<ISensorRepository, SensorRepository>();
                builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();

                // Services (scoped, not singleton)
                builder.Services.AddScoped<IDatabaseService, DatabaseService>();
                builder.Services.AddSingleton<IMainThreadService, MainThreadService>();
                builder.Services.AddScoped<ISensorService, SensorService>();
                builder.Services.AddSingleton<IDialogService, DialogService>();
                builder.Services.AddSingleton<SchedulerService>();
                builder.Services.AddSingleton<IDialogService, DialogService>();
                builder.Services.AddSingleton<IBackupService>(
                _ => new MySqlBackupService(ConnectionString, backupFolder));
                builder.Services.AddSingleton<HttpClient>();


                builder.Services.AddScoped<IDataQualityService, MockDataQualityService>();
                builder.Services.AddTransient<DataQualityViewModel>();
                builder.Services.AddTransient<DataQualityPage>();


                // ViewModels & Views
                builder.Services.AddTransient<MainPageViewModel>();
                builder.Services.AddTransient<SensorManagementViewModel>();
                builder.Services.AddTransient<SensorOperationalStatusViewModel>();
                builder.Services.AddTransient<SensorIncidentLogViewModel>();
                builder.Services.AddTransient<SensorLocatorViewModel>();
                builder.Services.AddTransient<SensorLocatorPage>();
                builder.Services.AddTransient<MapViewModel>();
                builder.Services.AddTransient<MainPage>();
                builder.Services.AddTransient<SensorManagementPage>();
                builder.Services.AddTransient<SensorOperationalStatusPage>();
                builder.Services.AddTransient<SensorIncidentPage>();
                builder.Services.AddTransient<MapPage>();
                builder.Services.AddTransient<DataStoragePage>();
                builder.Services.AddTransient<DataStorageViewModel>();

                builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);



#if DEBUG
                builder.Logging.AddDebug();
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Application initialization error: {ex.Message}");
            }

            var app = builder.Build();
            var scheduler = app.Services.GetRequiredService<SchedulerService>();
            scheduler.Start();
            return app;
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
