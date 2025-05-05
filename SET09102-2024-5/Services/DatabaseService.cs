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
using System.Linq;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Provides comprehensive database operations for sensor monitoring system components.
    /// This service implements IDatabaseService and handles all data access operations including
    /// connection management, user/role administration, access control, sensor data management,
    /// and incident tracking. It uses Entity Framework Core to interact with the database
    /// and includes robust error handling with detailed logging.
    /// </summary>
    public class DatabaseService : BaseService, IDatabaseService
    {
        private readonly SensorMonitoringContext _dbContext;
        private const string DbCategory = "Database";

        /// <summary>
        /// Initializes a new instance of the DatabaseService class.
        /// </summary>
        /// <param name="dbContext">The Entity Framework database context for sensor monitoring.</param>
        /// <param name="loggingService">The logging service for recording operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown if dbContext is null.</exception>
        public DatabaseService(
            SensorMonitoringContext dbContext,
            ILoggingService loggingService)
            : base("Database Service", DbCategory, loggingService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Initializes the database service by testing the connection and setting up initial data.
        /// This method is called by the base service's initialization process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if database connection fails.</exception>
        protected override async Task InitializeInternalAsync()
        {
            // Test connection
            bool connectionSuccess = await TestConnectionAsync();

            if (!connectionSuccess)
            {
                _loggingService.Error("Database connection failed during initialization", null, _serviceCategory);
                throw new Exception("Database connection failed");
            }

            // Seed the database if needed
            await InitializeDatabaseAsync();
        }

        /// <summary>
        /// Ensures the database exists and is initialized with required seed data.
        /// This method creates the database if it doesn't exist and populates default roles and users.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Rethrows any exceptions that occur during initialization.</exception>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _loggingService.Debug("Ensuring database is created", _serviceCategory);
                await _dbContext.Database.EnsureCreatedAsync();
                await SeedRolesAsync();
                _loggingService.Info("Database initialized successfully", _serviceCategory);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error initializing database", ex, _serviceCategory);
                throw;
            }
        }

        /// <summary>
        /// Tests the database connection by executing a simple query.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the connection test was successful; otherwise, false.
        /// </returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _loggingService.Debug("Testing database connection", _serviceCategory);
                // Try to execute a simple query
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
                _loggingService.Info("Database connection test successful", _serviceCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Database connection test failed", ex, _serviceCategory);
                return false;
            }
        }

        /// <summary>
        /// Retrieves information about the current database connection.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a string with connection details.
        /// </returns>
        public async Task<string> GetConnectionInfoAsync()
        {
            return await ServiceOperations.ExecuteAsync<string>(async () =>
            {
                var connection = _dbContext.Database.GetDbConnection();
                string dbName = connection.Database;
                string dataSource = connection.DataSource;

                return $"Connection: {dataSource}, Database: {dbName}";
            }, _loggingService, _serviceCategory, "GetConnectionInfo", "Unable to retrieve connection information").ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves all users from the database with their associated roles.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of User objects including their Role information.
        /// Returns an empty list if no users are found or an error occurs.
        /// </returns>
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await ServiceOperations.ExecuteAsync<List<User>>(
                async () => await _dbContext.Users
                    .Include(u => u.Role)
                    .ToListAsync(),
                _loggingService,
                _serviceCategory,
                "GetAllUsers",
                new List<User>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves all users from the database with their associated roles.
        /// Functionally equivalent to GetAllUsersAsync but named differently for clarity in different contexts.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of User objects including their Role information.
        /// Returns an empty list if no users are found or an error occurs.
        /// </returns>
        public async Task<List<User>> GetAllUsersWithRolesAsync()
        {
            return await ServiceOperations.ExecuteAsync<List<User>>(
                async () => await _dbContext.Users
                    .Include(u => u.Role)
                    .ToListAsync(),
                _loggingService,
                _serviceCategory,
                "GetAllUsersWithRoles",
                new List<User>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves a specific user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the User object if found; otherwise, null.
        /// </returns>
        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await ServiceOperations.ExecuteAsync<User>(
                async () =>
                {
                    var user = await _dbContext.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user == null)
                        _loggingService.Warning($"User with ID {userId} not found", _serviceCategory);

                    return user;
                },
                _loggingService,
                _serviceCategory,
                $"GetUserById({userId})",
                null
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves a specific user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the User object if found; otherwise, null.
        /// Returns null immediately if the provided email is null or empty.
        /// </returns>
        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _loggingService.Warning("Attempted to get user with empty email", _serviceCategory);
                return null;
            }

            return await ServiceOperations.ExecuteAsync<User>(
                async () =>
                {
                    var user = await _dbContext.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Email == email);

                    if (user == null)
                        _loggingService.Warning($"User with email {email} not found", _serviceCategory);

                    return user;
                },
                _loggingService,
                _serviceCategory,
                $"GetUserByEmail({email})",
                null
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Updates a user's role assignment.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to update.</param>
        /// <param name="roleId">The unique identifier of the role to assign to the user.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the update was successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method will not allow changing the role of a user with the Administrator role
        /// if that role is marked as protected.
        /// </remarks>
        public async Task<bool> UpdateUserRoleAsync(int userId, int roleId)
        {
            return await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    _loggingService.Info($"Updating role for user {userId} to role {roleId}", _serviceCategory);

                    var user = await _dbContext.Users.FindAsync(userId);
                    if (user == null)
                    {
                        _loggingService.Warning($"Role update failed - user {userId} not found", _serviceCategory);
                        return false;
                    }

                    var role = await _dbContext.Roles.FindAsync(roleId);
                    if (role == null)
                    {
                        _loggingService.Warning($"Role update failed - role {roleId} not found", _serviceCategory);
                        return false;
                    }

                    // Check if current user role is Administrator, which is protected
                    var currentRole = await _dbContext.Roles.FindAsync(user.RoleId);
                    if (currentRole != null && currentRole.IsProtected &&
                        currentRole.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                    {
                        _loggingService.Warning($"Role update failed - cannot change role for Administrator user {userId}", _serviceCategory);
                        return false;
                    }

                    user.RoleId = roleId;
                    _dbContext.Users.Update(user);
                    await _dbContext.SaveChangesAsync();

                    _loggingService.Info($"Role updated successfully for user {userId}", _serviceCategory);
                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"UpdateUserRole({userId}, {roleId})",
                false
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Deletes a user from the system.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to delete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the deletion was successful; otherwise, false.
        /// </returns>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            return await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    _loggingService.Info($"Deleting user with ID: {userId}", _serviceCategory);

                    var user = await _dbContext.Users.FindAsync(userId);
                    if (user == null)
                    {
                        _loggingService.Warning($"User delete failed - user {userId} not found", _serviceCategory);
                        return false;
                    }

                    _dbContext.Users.Remove(user);
                    await _dbContext.SaveChangesAsync();

                    _loggingService.Info($"User {userId} deleted successfully", _serviceCategory);
                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"DeleteUser({userId})",
                false
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves all roles defined in the system.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of Role objects.
        /// Returns an empty list if no roles are found or an error occurs.
        /// </returns>
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await ServiceOperations.ExecuteAsync<List<Role>>(
                async () => await _dbContext.Roles.ToListAsync(),
                _loggingService,
                _serviceCategory,
                "GetAllRoles",
                new List<Role>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves a specific role by its unique identifier.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the Role object if found; otherwise, null.
        /// </returns>
        public async Task<Role> GetRoleByIdAsync(int roleId)
        {
            return await ServiceOperations.ExecuteAsync<Role>(
                async () =>
                {
                    var role = await _dbContext.Roles.FindAsync(roleId);
                    if (role == null)
                        _loggingService.Warning($"Role with ID {roleId} not found", _serviceCategory);

                    return role;
                },
                _loggingService,
                _serviceCategory,
                $"GetRoleById({roleId})",
                null
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves a specific role by its name (case-insensitive).
        /// </summary>
        /// <param name="roleName">The name of the role to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the Role object if found; otherwise, null.
        /// Returns null immediately if the provided role name is null or empty.
        /// </returns>
        public async Task<Role> GetRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                _loggingService.Warning("Attempted to get role with empty name", _serviceCategory);
                return null;
            }

            return await ServiceOperations.ExecuteAsync<Role>(
                async () =>
                {
                    var role = await _dbContext.Roles
                        .FirstOrDefaultAsync(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));

                    if (role == null)
                        _loggingService.Warning($"Role with name '{roleName}' not found", _serviceCategory);

                    return role;
                },
                _loggingService,
                _serviceCategory,
                $"GetRoleByName({roleName})",
                null
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Creates a new role in the system.
        /// </summary>
        /// <param name="role">The role object to create.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the creation was successful; otherwise, false.
        /// Returns false immediately if the provided role is null or has an empty name.
        /// </returns>
        /// <remarks>
        /// This method will not create a role if another role with the same name already exists.
        /// </remarks>
        public async Task<bool> CreateRoleAsync(Role role)
        {
            if (role == null || string.IsNullOrWhiteSpace(role.RoleName))
            {
                _loggingService.Warning("Attempted to create invalid role", _serviceCategory);
                return false;
            }

            return await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    // Check if role with the same name already exists
                    bool exists = await _dbContext.Roles
                        .AnyAsync(r => r.RoleName.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));

                    if (exists)
                    {
                        _loggingService.Warning($"Role creation failed - role '{role.RoleName}' already exists", _serviceCategory);
                        return false;
                    }

                    _dbContext.Roles.Add(role);
                    await _dbContext.SaveChangesAsync();

                    _loggingService.Info($"Role '{role.RoleName}' created successfully with ID {role.RoleId}", _serviceCategory);
                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"CreateRole({role?.RoleName})",
                false
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Updates an existing role in the system.
        /// </summary>
        /// <param name="role">The role object with updated information.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the update was successful; otherwise, false.
        /// Returns false immediately if the provided role is null, has an invalid ID, or has an empty name.
        /// </returns>
        /// <remarks>
        /// This method will not allow renaming a protected role.
        /// </remarks>
        public async Task<bool> UpdateRoleAsync(Role role)
        {
            if (role == null || role.RoleId <= 0 || string.IsNullOrWhiteSpace(role.RoleName))
            {
                _loggingService.Warning("Attempted to update invalid role", _serviceCategory);
                return false;
            }

            return await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    // Check if role exists
                    var existingRole = await _dbContext.Roles.FindAsync(role.RoleId);
                    if (existingRole == null)
                    {
                        _loggingService.Warning($"Role update failed - role ID {role.RoleId} not found", _serviceCategory);
                        return false;
                    }

                    // Check if the role is protected and prevent name changes
                    if (existingRole.IsProtected && existingRole.RoleName != role.RoleName)
                    {
                        _loggingService.Warning($"Cannot rename protected role: {existingRole.RoleName}", _serviceCategory);
                        return false;
                    }

                    // Update fields
                    existingRole.RoleName = role.RoleName;
                    existingRole.Description = role.Description;

                    _dbContext.Roles.Update(existingRole);
                    await _dbContext.SaveChangesAsync();

                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"UpdateRole({role.RoleId}, {role.RoleName})",
                false
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Deletes a role from the system.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to delete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the deletion was successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method will not delete a role if:
        /// - The role does not exist
        /// - The role is marked as protected
        /// - The role is assigned to any users
        /// </remarks>
        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            return await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    var role = await _dbContext.Roles.FindAsync(roleId);
                    if (role == null)
                    {
                        _loggingService.Warning($"Role delete failed - role ID {roleId} not found", _serviceCategory);
                        return false;
                    }

                    // Don't allow deletion of protected roles
                    if (role.IsProtected)
                    {
                        _loggingService.Warning($"Cannot delete protected role: {role.RoleName}", _serviceCategory);
                        return false;
                    }

                    // Check if any users have this role
                    bool hasUsers = await _dbContext.Users.AnyAsync(u => u.RoleId == roleId);
                    if (hasUsers)
                    {
                        _loggingService.Warning($"Cannot delete role ID {roleId} - it is assigned to users", _serviceCategory);
                        return false;
                    }

                    _dbContext.Roles.Remove(role);
                    await _dbContext.SaveChangesAsync();

                    return true;
                },
                _loggingService,
                _serviceCategory,
                $"DeleteRole({roleId})",
                false
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves all sensors from the system.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of Sensor objects.
        /// Returns an empty list if no sensors are found or an error occurs.
        /// </returns>
        public async Task<List<Sensor>> GetAllSensorsAsync()
        {
            return await ServiceOperations.ExecuteAsync<List<Sensor>>(
                async () => await _dbContext.Sensors.ToListAsync(),
                _loggingService,
                _serviceCategory,
                "GetAllSensors",
                new List<Sensor>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves a specific sensor by its unique identifier.
        /// </summary>
        /// <param name="sensorId">The unique identifier of the sensor to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the Sensor object if found; otherwise, null.
        /// </returns>
        public async Task<Sensor> GetSensorByIdAsync(int sensorId)
        {
            return await ServiceOperations.ExecuteAsync<Sensor>(
                async () => await _dbContext.Sensors.FindAsync(sensorId),
                _loggingService,
                _serviceCategory,
                $"GetSensorById({sensorId})",
                null
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves all measurements for a specific sensor within a date range.
        /// </summary>
        /// <param name="sensorId">The unique identifier of the sensor.</param>
        /// <param name="startDate">The start date for the measurement range.</param>
        /// <param name="endDate">The end date for the measurement range.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of Measurement objects ordered by timestamp.
        /// Returns an empty list if no measurements are found or an error occurs.
        /// </returns>
        public async Task<List<Measurement>> GetSensorMeasurementsAsync(int sensorId, DateTime startDate, DateTime endDate)
        {
            return await ServiceOperations.ExecuteAsync<List<Measurement>>(
                async () =>
                {
                    var measurements = await _dbContext.Measurements
                        .Include(m => m.PhysicalQuantity)
                        .Where(m => m.PhysicalQuantity.SensorId == sensorId &&
                                    m.Timestamp >= startDate &&
                                    m.Timestamp <= endDate)
                        .OrderBy(m => m.Timestamp)
                        .ToListAsync();

                    return measurements;
                },
                _loggingService,
                _serviceCategory,
                $"GetSensorMeasurements(sensorId: {sensorId}, {startDate} - {endDate})",
                new List<Measurement>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves incidents from the system, optionally filtered by date range.
        /// </summary>
        /// <param name="startDate">Optional start date for filtering incidents.</param>
        /// <param name="endDate">Optional end date for filtering incidents.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of Incident objects ordered by the latest measurement timestamp.
        /// Returns an empty list if no incidents are found or an error occurs.
        /// </returns>
        public async Task<List<Incident>> GetIncidentsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await ServiceOperations.ExecuteAsync<List<Incident>>(
                async () =>
                {
                    IQueryable<Incident> query = _dbContext.Incidents
                        .Include(i => i.IncidentMeasurements)
                        .ThenInclude(im => im.Measurement);

                    if (startDate.HasValue || endDate.HasValue)
                    {
                        query = query.Where(i => i.IncidentMeasurements.Any(im =>
                            (!startDate.HasValue || im.Measurement.Timestamp >= startDate.Value) &&
                            (!endDate.HasValue || im.Measurement.Timestamp <= endDate.Value)));
                    }

                    var incidents = await query.ToListAsync();

                    // Client-side ordering for complex expressions
                    return incidents.OrderByDescending(i =>
                        i.IncidentMeasurements != null && i.IncidentMeasurements.Any() &&
                        i.IncidentMeasurements.Any(im => im.Measurement != null && im.Measurement.Timestamp.HasValue) ?
                        i.IncidentMeasurements
                            .Where(im => im.Measurement != null && im.Measurement.Timestamp.HasValue)
                            .Max(im => im.Measurement.Timestamp.Value) :
                        DateTime.MinValue)
                    .ToList();
                },
                _loggingService,
                _serviceCategory,
                "GetIncidents",
                new List<Incident>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves a specific incident by its unique identifier.
        /// </summary>
        /// <param name="incidentId">The unique identifier of the incident to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the Incident object if found; otherwise, null.
        /// </returns>
        public async Task<Incident> GetIncidentByIdAsync(int incidentId)
        {
            return await ServiceOperations.ExecuteAsync<Incident>(
                async () =>
                {
                    var incident = await _dbContext.Incidents
                        .Include(i => i.IncidentMeasurements)
                        .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

                    return incident;
                },
                _loggingService,
                _serviceCategory,
                $"GetIncidentById({incidentId})",
                null
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Seeds the database with default roles and an admin user if they don't already exist.
        /// Creates Administrator, User, and Guest roles with appropriate descriptions and protection settings.
        /// Also creates a default administrator user with predefined credentials.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Rethrows any exceptions that occur during the seeding process.</exception>
        private async Task SeedRolesAsync()
        {
            // Only seed if roles table is empty
            if (await _dbContext.Roles.AnyAsync())
            {
                _loggingService.Debug("Skipping role seeding - roles already exist", _serviceCategory);
                return;
            }

            try
            {
                _loggingService.Info("Seeding default roles and admin user", _serviceCategory);

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

                _loggingService.Info("Default roles created successfully", _serviceCategory);

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

                _loggingService.Info("Default admin user created successfully", _serviceCategory);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error seeding default roles and admin user", ex, _serviceCategory);
                throw;
            }
        }

        /// <summary>
        /// Creates a password hash and salt for secure storage of user credentials.
        /// Uses HMACSHA512 to generate a cryptographically strong hash and salt pair.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <param name="passwordHash">Output parameter that will contain the generated hash.</param>
        /// <param name="passwordSalt">Output parameter that will contain the generated salt.</param>
        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// Retrieves all access privileges defined in the system, ordered by module name and privilege name.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of AccessPrivilege objects.
        /// Returns an empty list if no privileges are found or an error occurs.
        /// </returns>
        public async Task<List<AccessPrivilege>> GetAllAccessPrivilegesAsync()
        {
            return await ServiceOperations.ExecuteAsync<List<AccessPrivilege>>(
                async () =>
                {
                    var privileges = await _dbContext.AccessPrivileges
                        .OrderBy(p => p.ModuleName)
                        .ThenBy(p => p.Name)
                        .ToListAsync();

                    return privileges;
                },
                _loggingService,
                _serviceCategory,
                "GetAllAccessPrivileges",
                new List<AccessPrivilege>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Retrieves all privileges assigned to a specific role.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a list of RolePrivilege objects with their associated AccessPrivilege details.
        /// Returns an empty list if no privileges are found for the role or an error occurs.
        /// </returns>
        public async Task<List<RolePrivilege>> GetRolePrivilegesAsync(int roleId)
        {
            return await ServiceOperations.ExecuteAsync<List<RolePrivilege>>(
                async () =>
                {
                    var rolePrivileges = await _dbContext.RolePrivileges
                        .Include(rp => rp.AccessPrivilege)
                        .Where(rp => rp.RoleId == roleId)
                        .ToListAsync();

                    return rolePrivileges;
                },
                _loggingService,
                _serviceCategory,
                $"GetRolePrivileges({roleId})",
                new List<RolePrivilege>()
            ).ContinueWith(t => t.Result.Value);
        }

        /// <summary>
        /// Updates the privileges assigned to a role by adding and/or removing specific privileges.
        /// All changes are performed within a transaction to ensure data consistency.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role.</param>
        /// <param name="addedPrivilegeIds">List of privilege IDs to add to the role.</param>
        /// <param name="removedPrivilegeIds">List of privilege IDs to remove from the role.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the update was successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method will not modify privileges for protected roles.
        /// It handles both adding new privileges and removing existing privileges in a single transaction.
        /// </remarks>
        public async Task<bool> UpdateRolePrivilegesAsync(int roleId, List<int> addedPrivilegeIds, List<int> removedPrivilegeIds)
        {
            return await ServiceOperations.ExecuteAsync<bool>(
                async () =>
                {
                    // Check if role exists
                    var role = await _dbContext.Roles.FindAsync(roleId);
                    if (role == null)
                    {
                        _loggingService.Warning($"Privilege update failed - role ID {roleId} not found", _serviceCategory);
                        return false;
                    }

                    // Don't allow modifying privileges for protected roles
                    if (role.IsProtected)
                    {
                        _loggingService.Warning($"Cannot modify privileges for protected role: {role.RoleName}", _serviceCategory);
                        return false;
                    }

                    // Use a database transaction to ensure all operations succeed or fail together
                    using var transaction = await _dbContext.Database.BeginTransactionAsync();

                    try
                    {
                        // Remove privileges
                        if (removedPrivilegeIds != null && removedPrivilegeIds.Any())
                        {
                            _loggingService.Debug($"Removing {removedPrivilegeIds.Count} privileges from role ID {roleId}", _serviceCategory);

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
                            _loggingService.Debug($"Adding {addedPrivilegeIds.Count} privileges to role ID {roleId}", _serviceCategory);

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
                                    _loggingService.Warning($"Privilege ID {privilegeId} does not exist - skipping", _serviceCategory);
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

                        _loggingService.Info($"Role privileges updated successfully for role ID {roleId}", _serviceCategory);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction if any part fails
                        await transaction.RollbackAsync();
                        _loggingService.Error($"Error updating privileges for role ID {roleId}", ex, _serviceCategory);
                        return false;
                    }
                },
                _loggingService,
                _serviceCategory,
                $"UpdateRolePrivileges(roleId: {roleId})",
                false
            ).ContinueWith(t => t.Result.Value);
        }
    }
}
