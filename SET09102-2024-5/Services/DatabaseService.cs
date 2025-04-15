using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;

namespace SET09102_2024_5.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SensorMonitoringContext _dbContext;

        public DatabaseService(SensorMonitoringContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task InitializeDatabaseAsync()
        {
            // Apply any pending migrations
            await _dbContext.Database.MigrateAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _dbContext.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}