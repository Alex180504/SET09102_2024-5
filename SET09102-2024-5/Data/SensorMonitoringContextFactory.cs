// Data/SensorMonitoringContextFactory.cs
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SET09102_2024_5.Data
{
    public class SensorMonitoringContextFactory
        : IDesignTimeDbContextFactory<SensorMonitoringContext>
    {
        public SensorMonitoringContext CreateDbContext(string[] args)
        {
            // 1) build config from appsettings.json
            var basePath = Directory.GetCurrentDirectory();
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // 2) grab your connection string
            var conn = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // 3) If we have a certificate path from MauiProgram, use it
            if (!string.IsNullOrEmpty(MauiProgram.CertPath))
            {
                conn = conn.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={MauiProgram.CertPath};");
            }
            // Otherwise, try to extract the certificate ourselves
            else
            {
                try 
                {
                    // For design-time tools that don't use MauiProgram
                    string certPath = ExtractSslCertificate();
                    conn = conn.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={certPath};");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not extract SSL certificate: {ex.Message}");
                }
            }

            // 4) configure the MySQL/MariaDB provider
            var opts = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseMySql(conn, ServerVersion.AutoDetect(conn));

            return new SensorMonitoringContext(opts.Options);
        }

        private string ExtractSslCertificate()
        {
            // Similar implementation to MauiProgram
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var certStream = assembly.GetManifestResourceStream("SET09102_2024_5.DigiCertGlobalRootG2.crt.pem");

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
