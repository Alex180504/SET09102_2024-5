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

            // 3) configure the MySQL/MariaDB provider
            var opts = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseMySql(conn, ServerVersion.AutoDetect(conn));

            return new SensorMonitoringContext(opts.Options);
        }
    }
}
