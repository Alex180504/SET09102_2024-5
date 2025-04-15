using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SET09102_2024_5
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Load configuration
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("SET09102_2024_5.appsettings.json");

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            // Get connection string from configuration
            var connectionString = config.GetConnectionString("DefaultConnection");

            // Extract SSL certificate and save it to a temporary file
            string certPath = ExtractSslCertificate();
            connectionString = connectionString.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={certPath};");

            builder.Services.AddDbContext<SensorMonitoringContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Register repositories
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Register services
            builder.Services.AddScoped<IDatabaseService, DatabaseService>();

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
