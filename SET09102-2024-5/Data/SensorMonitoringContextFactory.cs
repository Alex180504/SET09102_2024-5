﻿// Data/SensorMonitoringContextFactory.cs
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
            // Get connection string from embedded resource instead of physical file
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("SET09102_2024_5.appsettings.json");

            if (stream == null)
            {
                throw new InvalidOperationException(
                    "Could not find appsettings.json embedded resource. " +
                    "Make sure it exists and its Build Action is set to 'Embedded resource'.");
            }

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            // Get the connection string
            var conn = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Handle SSL certificate
            if (!string.IsNullOrEmpty(MauiProgram.CertPath))
            {
                conn = conn.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={MauiProgram.CertPath};");
            }
            else
            {
                try
                {
                    string certPath = ExtractSslCertificate();
                    conn = conn.Replace("SslCa=DigiCertGlobalRootG2.crt.pem;", $"SslCa={certPath};");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not extract SSL certificate: {ex.Message}");
                }
            }

            // Configure the context options
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
