    using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Services
{
    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly DbContextOptions<SensorMonitoringContext> _dbContextOptions;
        private readonly ILoggingService _loggingService;

        public string ConnectionString { get; private set; }

        public DatabaseInitializationService(
            DbContextOptions<SensorMonitoringContext> dbContextOptions,
            ILoggingService loggingService)
        {
            _dbContextOptions = dbContextOptions;
            _loggingService = loggingService;
            ConnectionString = MauiProgram.ConnectionString;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _loggingService.Info("Initializing database", "Database");
                using var context = new SensorMonitoringContext(_dbContextOptions);
                await context.Database.EnsureCreatedAsync();
                _loggingService.Info("Database initialization successful", "Database");
            }
            catch (Exception ex)
            {
                _loggingService.Error("Database initialization failed", ex, "Database");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _loggingService.Debug("Testing database connection", "Database");
                using var context = new SensorMonitoringContext(_dbContextOptions);
                // Try to execute a simple query
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                _loggingService.Info("Database connection test successful", "Database");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Database connection test failed", ex, "Database");
                return false;
            }
        }
    }
}