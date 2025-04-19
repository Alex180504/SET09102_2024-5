using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Models;
using System.Security.Cryptography;

namespace SET09102_2024_5.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SensorMonitoringContext _context;

        public DatabaseService(SensorMonitoringContext context)
        {
            _context = context;
        }

        public async Task InitializeDatabaseAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            await SeedRolesAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Try to execute a simple query
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task SeedRolesAsync()
        {
            // Only seed if roles table is empty
            if (await _context.Roles.AnyAsync())
                return;

            // Create default roles
            var adminRole = new Role
            {
                RoleName = "Administrator"
            };

            var userRole = new Role
            {
                RoleName = "User"
            };

            var guestRole = new Role
            {
                RoleName = "Guest"
            };

            _context.Roles.Add(adminRole);
            _context.Roles.Add(userRole);
            _context.Roles.Add(guestRole);
            await _context.SaveChangesAsync();

            // Create a default admin user
            CreatePasswordHash("Admin@123", out string passwordHash, out string passwordSalt);
            
            var adminUser = new User
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@system.com",
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RoleId = adminRole.RoleId
            };
            
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
        }
        
        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}