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

namespace SET09102_2024_5
{
    public static class MauiProgram
    {
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
            string certPath = ExtractSslCertificate();
            connectionString = connectionString.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={certPath};");

            builder.Services.AddDbContext<SensorMonitoringContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Add memory cache for repository optimization
            builder.Services.AddMemoryCache();

            // Register repositories
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Register services
            builder.Services.AddScoped<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IAuthService, AuthService>(); // Singleton to maintain auth state
            builder.Services.AddSingleton<INavigationService, NavigationService>(); // Singleton for navigation service

            // Register app shell with navigation
            builder.Services.AddSingleton<AppShell>();

            // Register ViewModels
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            
            // Register Admin ViewModels
            builder.Services.AddTransient<RoleManagementViewModel>();
            builder.Services.AddTransient<UserRoleManagementViewModel>();

            // Register Views
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            
            // Register Admin Views
            builder.Services.AddTransient<AdminDashboardPage>();
            builder.Services.AddTransient<RoleManagementPage>();
            builder.Services.AddTransient<UserRoleManagementPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

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
}
