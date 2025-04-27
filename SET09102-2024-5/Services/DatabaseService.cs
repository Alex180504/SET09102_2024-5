using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SET09102_2024_5.Data;
using SET09102_2024_5.Models;
using SET09102_2024_5.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SET09102_2024_5.Services
{
    public class DatabaseService : IBaseService
    {
        private readonly SensorMonitoringContext _dbContext;
        private readonly ILoggingService _loggingService;
        private bool _isInitialized = false;
        private const string DbCategory = "Database";
        private const string ServiceName = "Database Service";

        public DatabaseService(
            SensorMonitoringContext dbContext,
            ILoggingService loggingService)
        {
            _dbContext = dbContext;
            _loggingService = loggingService;
        }
        
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;
                
            _loggingService.Info("Initializing database service", DbCategory);
            
            try
            {
                // Test connection
                bool connectionSuccess = await TestConnectionAsync();
                
                if (!connectionSuccess)
                {
                    _loggingService.Error("Database connection failed during initialization", null, DbCategory);
                    return false;
                }

                // Database is initialized
                _isInitialized = true;
                _loggingService.Info("Database service initialized successfully", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Failed to initialize database service", ex, DbCategory);
                return false;
            }
        }
        
        public Task<bool> IsReadyAsync()
        {
            return Task.FromResult(_isInitialized);
        }
        
        public string GetServiceStatus()
        {
            return _isInitialized ? "Ready" : "Not Ready";
        }
        
        public string GetServiceName()
        {
            return ServiceName;
        }
        
        public async Task CleanupAsync()
        {
            _loggingService.Info("Cleaning up database service", DbCategory);
            await Task.CompletedTask;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _loggingService.Debug("Ensuring database is created", DbCategory);
                await _dbContext.Database.EnsureCreatedAsync();
                await SeedRolesAsync();
                _loggingService.Info("Database initialized successfully", DbCategory);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error initializing database", ex, DbCategory);
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _loggingService.Debug("Testing database connection", DbCategory);
                // Try to execute a simple query
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
                _loggingService.Info("Database connection test successful", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Database connection test failed", ex, DbCategory);
                return false;
            }
        }
        
        public async Task<string> GetConnectionInfoAsync()
        {
            try
            {
                var connection = _dbContext.Database.GetDbConnection();
                string dbName = connection.Database;
                string dataSource = connection.DataSource;
                
                _loggingService.Debug($"Retrieving connection info: {dataSource}/{dbName}", DbCategory);
                return $"Connection: {dataSource}, Database: {dbName}";
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving connection information", ex, DbCategory);
                return "Unable to retrieve connection information";
            }
        }

        // User management methods
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                _loggingService.Debug("Retrieving all users", DbCategory);
                var users = await _dbContext.Users
                    .Include(u => u.Role)
                    .ToListAsync();
                    
                _loggingService.Debug($"Retrieved {users.Count} users", DbCategory);
                return users;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving all users", ex, DbCategory);
                return new List<User>();
            }
        }

        public async Task<List<User>> GetAllUsersWithRolesAsync()
        {
            try
            {
                _loggingService.Debug("Retrieving all users with roles", DbCategory);
                var users = await _dbContext.Users
                    .Include(u => u.Role)
                    .ToListAsync();
                    
                _loggingService.Debug($"Retrieved {users.Count} users with their roles", DbCategory);
                return users;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving users with roles", ex, DbCategory);
                return new List<User>();
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                _loggingService.Debug($"Retrieving user by ID: {userId}", DbCategory);
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                    
                if (user == null)
                {
                    _loggingService.Warning($"User with ID {userId} not found", DbCategory);
                }
                
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving user with ID {userId}", ex, DbCategory);
                return null;
            }
        }
        
        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _loggingService.Warning("Attempted to get user with empty email", DbCategory);
                return null;
            }
            
            try
            {
                _loggingService.Debug($"Retrieving user by email: {email}", DbCategory);
                var user = await _dbContext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);
                    
                if (user == null)
                {
                    _loggingService.Warning($"User with email {email} not found", DbCategory);
                }
                
                return user;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving user with email {email}", ex, DbCategory);
                return null;
            }
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, int roleId)
        {
            try
            {
                _loggingService.Info($"Updating role for user {userId} to role {roleId}", DbCategory);
                
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    _loggingService.Warning($"Role update failed - user {userId} not found", DbCategory);
                    return false;
                }

                var role = await _dbContext.Roles.FindAsync(roleId);
                if (role == null)
                {
                    _loggingService.Warning($"Role update failed - role {roleId} not found", DbCategory);
                    return false;
                }

                // Check if current user role is Administrator, which is protected
                var currentRole = await _dbContext.Roles.FindAsync(user.RoleId);
                if (currentRole != null && currentRole.IsProtected && 
                    currentRole.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    _loggingService.Warning($"Role update failed - cannot change role for Administrator user {userId}", DbCategory);
                    return false;
                }

                user.RoleId = roleId;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info($"Role updated successfully for user {userId}", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error updating role for user {userId} to {roleId}", ex, DbCategory);
                return false;
            }
        }
        
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                _loggingService.Info($"Deleting user with ID: {userId}", DbCategory);
                
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    _loggingService.Warning($"User delete failed - user {userId} not found", DbCategory);
                    return false;
                }

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info($"User {userId} deleted successfully", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error deleting user {userId}", ex, DbCategory);
                return false;
            }
        }

        // Role management methods
        public async Task<List<Role>> GetAllRolesAsync()
        {
            try
            {
                _loggingService.Debug("Retrieving all roles", DbCategory);
                var roles = await _dbContext.Roles.ToListAsync();
                _loggingService.Debug($"Retrieved {roles.Count} roles", DbCategory);
                return roles;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving all roles", ex, DbCategory);
                return new List<Role>();
            }
        }

        public async Task<Role> GetRoleByIdAsync(int roleId)
        {
            try
            {
                _loggingService.Debug($"Retrieving role by ID: {roleId}", DbCategory);
                var role = await _dbContext.Roles.FindAsync(roleId);
                
                if (role == null)
                {
                    _loggingService.Warning($"Role with ID {roleId} not found", DbCategory);
                }
                
                return role;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving role with ID {roleId}", ex, DbCategory);
                return null;
            }
        }
        
        public async Task<Role> GetRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                _loggingService.Warning("Attempted to get role with empty name", DbCategory);
                return null;
            }
            
            try
            {
                _loggingService.Debug($"Retrieving role by name: {roleName}", DbCategory);
                var role = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
                    
                if (role == null)
                {
                    _loggingService.Warning($"Role with name '{roleName}' not found", DbCategory);
                }
                
                return role;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving role with name '{roleName}'", ex, DbCategory);
                return null;
            }
        }
        
        public async Task<bool> CreateRoleAsync(Role role)
        {
            if (role == null || string.IsNullOrWhiteSpace(role.RoleName))
            {
                _loggingService.Warning("Attempted to create invalid role", DbCategory);
                return false;
            }
            
            try
            {
                _loggingService.Info($"Creating new role: {role.RoleName}", DbCategory);
                
                // Check if role with the same name already exists
                bool exists = await _dbContext.Roles
                    .AnyAsync(r => r.RoleName.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));
                    
                if (exists)
                {
                    _loggingService.Warning($"Role creation failed - role '{role.RoleName}' already exists", DbCategory);
                    return false;
                }
                
                _dbContext.Roles.Add(role);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info($"Role '{role.RoleName}' created successfully with ID {role.RoleId}", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error creating role '{role.RoleName}'", ex, DbCategory);
                return false;
            }
        }
        
        public async Task<bool> UpdateRoleAsync(Role role)
        {
            if (role == null || role.RoleId <= 0 || string.IsNullOrWhiteSpace(role.RoleName))
            {
                _loggingService.Warning("Attempted to update invalid role", DbCategory);
                return false;
            }
            
            try
            {
                _loggingService.Info($"Updating role ID {role.RoleId}: {role.RoleName}", DbCategory);
                
                // Check if role exists
                var existingRole = await _dbContext.Roles.FindAsync(role.RoleId);
                if (existingRole == null)
                {
                    _loggingService.Warning($"Role update failed - role ID {role.RoleId} not found", DbCategory);
                    return false;
                }
                
                // Check if the role is protected and prevent name changes
                if (existingRole.IsProtected && existingRole.RoleName != role.RoleName)
                {
                    _loggingService.Warning($"Cannot rename protected role: {existingRole.RoleName}", DbCategory);
                    return false;
                }
                
                // Update fields
                existingRole.RoleName = role.RoleName;
                existingRole.Description = role.Description;
                
                _dbContext.Roles.Update(existingRole);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info($"Role ID {role.RoleId} updated successfully", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error updating role ID {role.RoleId}", ex, DbCategory);
                return false;
            }
        }
        
        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            try
            {
                _loggingService.Info($"Deleting role with ID: {roleId}", DbCategory);
                
                var role = await _dbContext.Roles.FindAsync(roleId);
                if (role == null)
                {
                    _loggingService.Warning($"Role delete failed - role ID {roleId} not found", DbCategory);
                    return false;
                }
                
                // Don't allow deletion of protected roles
                if (role.IsProtected)
                {
                    _loggingService.Warning($"Cannot delete protected role: {role.RoleName}", DbCategory);
                    return false;
                }
                
                // Check if any users have this role
                bool hasUsers = await _dbContext.Users.AnyAsync(u => u.RoleId == roleId);
                if (hasUsers)
                {
                    _loggingService.Warning($"Cannot delete role ID {roleId} - it is assigned to users", DbCategory);
                    return false;
                }
                
                _dbContext.Roles.Remove(role);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info($"Role ID {roleId} deleted successfully", DbCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error deleting role ID {roleId}", ex, DbCategory);
                return false;
            }
        }
        
        // Sensor data management methods
        public async Task<List<Sensor>> GetAllSensorsAsync()
        {
            try
            {
                _loggingService.Debug("Retrieving all sensors", DbCategory);
                var sensors = await _dbContext.Sensors.ToListAsync();
                _loggingService.Debug($"Retrieved {sensors.Count} sensors", DbCategory);
                return sensors;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving all sensors", ex, DbCategory);
                return new List<Sensor>();
            }
        }
        
        public async Task<Sensor> GetSensorByIdAsync(int sensorId)
        {
            try
            {
                _loggingService.Debug($"Retrieving sensor by ID: {sensorId}", DbCategory);
                var sensor = await _dbContext.Sensors.FindAsync(sensorId);
                
                if (sensor == null)
                {
                    _loggingService.Warning($"Sensor with ID {sensorId} not found", DbCategory);
                }
                
                return sensor;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving sensor with ID {sensorId}", ex, DbCategory);
                return null;
            }
        }
        
        public async Task<List<Measurement>> GetSensorMeasurementsAsync(int sensorId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _loggingService.Debug($"Retrieving measurements for sensor {sensorId} between {startDate} - {endDate}", DbCategory);
                
                var measurements = await _dbContext.Measurements
                    .Include(m => m.PhysicalQuantity)
                    .Where(m => m.PhysicalQuantity.SensorId == sensorId && 
                                m.Timestamp >= startDate && 
                                m.Timestamp <= endDate)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
                
                _loggingService.Debug($"Retrieved {measurements.Count} measurements for sensor {sensorId}", DbCategory);
                return measurements;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving measurements for sensor {sensorId}", ex, DbCategory);
                return new List<Measurement>();
            }
        }
        
        // Incident management methods
        public async Task<List<Incident>> GetIncidentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                _loggingService.Debug("Retrieving incidents with filters", DbCategory);
                
                IQueryable<Incident> query = _dbContext.Incidents
                    .Include(i => i.IncidentMeasurements)
                    .ThenInclude(im => im.Measurement);
                
                if (startDate.HasValue || endDate.HasValue)
                {
                    // Join with measurements to filter by date if needed
                    query = query.Where(i => i.IncidentMeasurements.Any(im => 
                        (!startDate.HasValue || im.Measurement.Timestamp >= startDate.Value) &&
                        (!endDate.HasValue || im.Measurement.Timestamp <= endDate.Value)));
                }
                
                // Execute the query first to avoid expression tree lambda issues
                var incidents = await query.ToListAsync();
                
                // Then perform client-side ordering with the full lambda expression
                var orderedIncidents = incidents
                    .OrderByDescending(i => 
                        i.IncidentMeasurements != null && i.IncidentMeasurements.Any() && 
                        i.IncidentMeasurements.Any(im => im.Measurement != null && im.Measurement.Timestamp.HasValue) ?
                        i.IncidentMeasurements
                            .Where(im => im.Measurement != null && im.Measurement.Timestamp.HasValue)
                            .Max(im => im.Measurement.Timestamp.Value) :
                        DateTime.MinValue)
                    .ToList();
                    
                _loggingService.Debug($"Retrieved {orderedIncidents.Count} incidents", DbCategory);
                return orderedIncidents;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving incidents", ex, DbCategory);
                return new List<Incident>();
            }
        }
        
        public async Task<Incident> GetIncidentByIdAsync(int incidentId)
        {
            try
            {
                _loggingService.Debug($"Retrieving incident by ID: {incidentId}", DbCategory);
                var incident = await _dbContext.Incidents
                    .Include(i => i.IncidentMeasurements)
                    .FirstOrDefaultAsync(i => i.IncidentId == incidentId);
                
                if (incident == null)
                {
                    _loggingService.Warning($"Incident with ID {incidentId} not found", DbCategory);
                }
                
                return incident;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving incident with ID {incidentId}", ex, DbCategory);
                return null;
            }
        }

        private async Task SeedRolesAsync()
        {
            // Only seed if roles table is empty
            if (await _dbContext.Roles.AnyAsync())
            {
                _loggingService.Debug("Skipping role seeding - roles already exist", DbCategory);
                return;
            }

            try
            {
                _loggingService.Info("Seeding default roles and admin user", DbCategory);
                
                // Create default roles
                var adminRole = new Role
                {
                    RoleName = "Administrator",
                    Description = "Full system access with all privileges",
                    IsProtected = true
                };

                var userRole = new Role
                {
                    RoleName = "User",
                    Description = "Standard user access"
                };

                var guestRole = new Role
                {
                    RoleName = "Guest",
                    Description = "Limited read-only access"
                };

                _dbContext.Roles.Add(adminRole);
                _dbContext.Roles.Add(userRole);
                _dbContext.Roles.Add(guestRole);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info("Default roles created successfully", DbCategory);

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
                
                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
                
                _loggingService.Info("Default admin user created successfully", DbCategory);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error seeding default roles and admin user", ex, DbCategory);
                throw;
            }
        }
        
        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        // Access privilege management methods
        public async Task<List<AccessPrivilege>> GetAllAccessPrivilegesAsync()
        {
            try
            {
                _loggingService.Debug("Retrieving all access privileges", DbCategory);
                var privileges = await _dbContext.AccessPrivileges
                    .OrderBy(p => p.ModuleName)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
                    
                _loggingService.Debug($"Retrieved {privileges.Count} access privileges", DbCategory);
                return privileges;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error retrieving access privileges", ex, DbCategory);
                return new List<AccessPrivilege>();
            }
        }

        public async Task<List<RolePrivilege>> GetRolePrivilegesAsync(int roleId)
        {
            try
            {
                _loggingService.Debug($"Retrieving privileges for role ID: {roleId}", DbCategory);
                
                var rolePrivileges = await _dbContext.RolePrivileges
                    .Include(rp => rp.AccessPrivilege)
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();
                    
                _loggingService.Debug($"Retrieved {rolePrivileges.Count} privileges for role ID {roleId}", DbCategory);
                return rolePrivileges;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error retrieving privileges for role ID {roleId}", ex, DbCategory);
                return new List<RolePrivilege>();
            }
        }

        public async Task<bool> UpdateRolePrivilegesAsync(int roleId, List<int> addedPrivilegeIds, List<int> removedPrivilegeIds)
        {
            try
            {
                _loggingService.Info($"Updating privileges for role ID {roleId}", DbCategory);
                
                // Check if role exists
                var role = await _dbContext.Roles.FindAsync(roleId);
                if (role == null)
                {
                    _loggingService.Warning($"Privilege update failed - role ID {roleId} not found", DbCategory);
                    return false;
                }
                
                // Don't allow modifying privileges for protected roles
                if (role.IsProtected)
                {
                    _loggingService.Warning($"Cannot modify privileges for protected role: {role.RoleName}", DbCategory);
                    return false;
                }
                
                // Use a database transaction to ensure all operations succeed or fail together
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    // Remove privileges
                    if (removedPrivilegeIds != null && removedPrivilegeIds.Any())
                    {
                        _loggingService.Debug($"Removing {removedPrivilegeIds.Count} privileges from role ID {roleId}", DbCategory);
                        
                        var rolesToRemove = await _dbContext.RolePrivileges
                            .Where(rp => rp.RoleId == roleId && removedPrivilegeIds.Contains(rp.AccessPrivilegeId))
                            .ToListAsync();
                            
                        _dbContext.RolePrivileges.RemoveRange(rolesToRemove);
                        
                        // Save after removing to avoid conflicts with additions
                        await _dbContext.SaveChangesAsync();
                    }
                    
                    // Add privileges
                    if (addedPrivilegeIds != null && addedPrivilegeIds.Any())
                    {
                        _loggingService.Debug($"Adding {addedPrivilegeIds.Count} privileges to role ID {roleId}", DbCategory);
                        
                        // Only add privileges that don't already exist
                        var existingPrivilegeIds = await _dbContext.RolePrivileges
                            .Where(rp => rp.RoleId == roleId)
                            .Select(rp => rp.AccessPrivilegeId)
                            .ToListAsync();
                            
                        // Filter out privileges that already exist
                        var newPrivilegeIds = addedPrivilegeIds
                            .Except(existingPrivilegeIds)
                            .ToList();
                        
                        // Create a batch of new role privileges to add
                        var newRolePrivileges = new List<RolePrivilege>();
                            
                        foreach (var privilegeId in newPrivilegeIds)
                        {
                            // Check if privilege exists
                            if (await _dbContext.AccessPrivileges.AnyAsync(ap => ap.AccessPrivilegeId == privilegeId))
                            {
                                newRolePrivileges.Add(new RolePrivilege
                                {
                                    RoleId = roleId,
                                    AccessPrivilegeId = privilegeId
                                });
                            }
                            else
                            {
                                _loggingService.Warning($"Privilege ID {privilegeId} does not exist - skipping", DbCategory);
                            }
                        }
                        
                        // Add all new privileges in one operation
                        if (newRolePrivileges.Any())
                        {
                            await _dbContext.RolePrivileges.AddRangeAsync(newRolePrivileges);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    
                    // Commit the transaction only after all operations have succeeded
                    await transaction.CommitAsync();
                    
                    _loggingService.Info($"Role privileges updated successfully for role ID {roleId}", DbCategory);
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback the transaction if any part fails
                    await transaction.RollbackAsync();
                    _loggingService.Error($"Error updating privileges for role ID {roleId}", ex, DbCategory);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error updating privileges for role ID {roleId}", ex, DbCategory);
                return false;
            }
        }
    }
}