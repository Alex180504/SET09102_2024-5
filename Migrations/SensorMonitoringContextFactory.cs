using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SET09102_2024_5.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Reflection;

namespace SET09102_2024_5.Migrations
{
    /// <summary>
    /// Factory class for creating DbContext instances at design time.
    /// This is used by EF Core migration tools when generating or applying migrations.
    /// Implements IDesignTimeDbContextFactory to provide a properly configured DbContext
    /// without requiring dependency injection.
    /// </summary>
    public class SensorMonitoringContextFactory : IDesignTimeDbContextFactory<SensorMonitoringContext>
    {
        /// <summary>
        /// Creates a new instance of a SensorMonitoringContext using configuration from appsettings.json.
        /// </summary>
        public SensorMonitoringContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Extract and use SSL certificate
            var certPath = ExtractSslCertificate();
            connectionString = connectionString.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={certPath};");

            var optionsBuilder = new DbContextOptionsBuilder<SensorMonitoringContext>();
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 32));

            // Specify the migrations assembly
            optionsBuilder.UseMySql(connectionString, serverVersion,
                options => options.MigrationsAssembly("Migrations"));

            return new SensorMonitoringContext(optionsBuilder.Options);
        }

        private string ExtractSslCertificate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var certStream = assembly.GetManifestResourceStream("Migrations.DigiCertGlobalRootG2.crt.pem");

            if (certStream == null)
            {
                throw new InvalidOperationException("Could not find DigiCertGlobalRootG2.crt.pem embedded resource.");
            }

            // Create temp file for the certificate
            string tempPath = Path.Combine(Path.GetTempPath(), "DigiCertGlobalRootG2.crt.pem");

            using (var fileStream = File.Create(tempPath))
            {
                certStream.CopyTo(fileStream);
            }

            return tempPath;
        }
    }
}
