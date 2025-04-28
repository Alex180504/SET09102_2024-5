using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Services;
using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Views;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.Maui;
using SET09102_2024_5.Views.Controls;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Features.HistoricalData.ViewModels;
using SET09102_2024_5.Models;
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
            
            try
            {
                builder
                    .UseMauiApp<App>()
                    .UseMauiCommunityToolkit()
                    .UseSkiaSharp()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                        fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                    });

                // Load configuration
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("SET09102_2024_5.appsettings.json");

                if (stream == null)
                {
                    throw new InvalidOperationException("Could not find appsettings.json embedded resource.");
                }

                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                // Get connection string from configuration
                var connectionString = config.GetConnectionString("DefaultConnection") 
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

                // Extract SSL certificate and save it to a temporary file
                CertPath = ExtractSslCertificate();
                ConnectionString = connectionString.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={CertPath};");

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
                builder.Services.AddSingleton<IDatabaseInitializationService>(serviceProvider => 
                    new DatabaseInitializationService(
                        serviceProvider.GetRequiredService<DbContextOptions<SensorMonitoringContext>>(),
                        serviceProvider.GetRequiredService<ILoggingService>()));

                // Add memory cache for repository optimization
                builder.Services.AddMemoryCache();

                // Register repositories
                builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

                // Register services - removed adapter pattern for simplicity
                builder.Services.AddScoped<IDatabaseService, DatabaseService>(); 
                builder.Services.AddSingleton<ILoggingService, LoggingService>(); 
                builder.Services.AddSingleton<IAuthService, AuthService>();
                builder.Services.AddSingleton<IDialogService, DialogService>();
                builder.Services.AddSingleton<IMainThreadService, MainThreadService>();
                builder.Services.AddSingleton<IPasswordHasher, Services.Security.PasswordHasher>();
                builder.Services.AddSingleton<ICacheManager, Services.Cache.CacheManager>();
                builder.Services.AddSingleton<ITokenService, Services.Security.TokenService>();
                
                // Register backup service and related dependencies
                var backupFolder = Path.Combine(FileSystem.AppDataDirectory, "Backups");
                builder.Services.AddSingleton(new BackupOptions
                {
                    BackupFolder = backupFolder,
                    KeepLatestBackups = 5, // Default value
                    ScheduleTime = new TimeSpan(3, 0, 0) // Default 3 AM
                });
                
                builder.Services.AddSingleton<IBackupService>(provider => 
                    new MySqlBackupService(ConnectionString, backupFolder));
                
                builder.Services.AddSingleton<SchedulerService>();
                
                // Register sensor services and repositories
                builder.Services.AddScoped<ISensorRepository, SensorRepository>();
                builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
                builder.Services.AddScoped<ISensorService, SensorService>();
                
                // Register navigation services
                builder.Services.AddSingleton<INavigationService, NavigationService>();
                builder.Services.AddSingleton<ViewModelLocator>();
                builder.Services.AddSingleton<ViewLifecycleManager>();

                // Register app shell with navigation
                builder.Services.AddSingleton<AppShell>();

                // Register ViewModels - all are transient for better memory management
                // Core ViewModels
                builder.Services.AddTransient<MainPageViewModel>();
                builder.Services.AddTransient<LoginViewModel>();
                builder.Services.AddTransient<RegisterViewModel>();
                builder.Services.AddTransient<MapViewModel>();
                builder.Services.AddTransient<SensorManagementViewModel>();
                builder.Services.AddTransient<SensorLocatorViewModel>();
                builder.Services.AddTransient<SensorOperationalStatusViewModel>();
                builder.Services.AddTransient<HistoricalDataViewModel>();
                builder.Services.AddTransient<DataStorageViewModel>();
                
                // Admin ViewModels
                builder.Services.AddTransient<RoleManagementViewModel>();
                builder.Services.AddTransient<UserRoleManagementViewModel>();
                
                // Register Reusable UI components
                RegisterControls(builder.Services);

                // Register Views - all views are transient to minimize memory usage
                // Core Views
                builder.Services.AddTransient<MainPage>();
                builder.Services.AddTransient<LoginPage>();
                builder.Services.AddTransient<RegisterPage>();
                builder.Services.AddTransient<MapPage>();
                builder.Services.AddTransient<SensorManagementPage>();
                builder.Services.AddTransient<SensorLocatorPage>();
                builder.Services.AddTransient<SensorOperationalStatusPage>();
                builder.Services.AddTransient<HistoricalDataPage>();
                builder.Services.AddTransient<DataStoragePage>();
                
                // Admin Views
                builder.Services.AddTransient<AdminDashboardPage>();
                builder.Services.AddTransient<RoleManagementPage>();
                builder.Services.AddTransient<UserRoleManagementPage>();
                
                // Note: Routes are registered by NavigationService during initialization
                // and managed by ViewRegistration class rather than duplicating them here

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
        
        /// <summary>
        /// Register all reusable UI controls
        /// </summary>
        private static void RegisterControls(IServiceCollection services)
        {
            // Register common controls used across the application
            services.AddTransient<PageHeaderView>();
            services.AddTransient<EmptyStateView>();
            services.AddTransient<LoadingOverlay>();
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
}
