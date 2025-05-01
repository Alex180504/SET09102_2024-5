using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Defines a comprehensive database service interface for sensor monitoring and management.
    /// This service provides data access operations for all major system entities including users,
    /// roles, access privileges, sensors, measurements, and incidents. It abstracts database
    /// operations from the rest of the application and provides connection management functionality.
    /// The interface extends IBaseService to inherit common service capabilities like initialization 
    /// and status reporting.
    /// </summary>
    public interface IDatabaseService : IBaseService
    {
        // Connection management
        Task<bool> TestConnectionAsync();
        Task<string> GetConnectionInfoAsync();

        // User management
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetAllUsersWithRolesAsync();
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserRoleAsync(int userId, int roleId);
        Task<bool> DeleteUserAsync(int userId);

        // Role management
        Task<List<Role>> GetAllRolesAsync();
        Task<Role> GetRoleByIdAsync(int roleId);
        Task<Role> GetRoleByNameAsync(string roleName);
        Task<bool> CreateRoleAsync(Role role);
        Task<bool> UpdateRoleAsync(Role role);
        Task<bool> DeleteRoleAsync(int roleId);

        // Access privilege management
        Task<List<AccessPrivilege>> GetAllAccessPrivilegesAsync();
        Task<List<RolePrivilege>> GetRolePrivilegesAsync(int roleId);
        Task<bool> UpdateRolePrivilegesAsync(int roleId, List<int> addedPrivilegeIds, List<int> removedPrivilegeIds);

        // Sensor data management
        Task<List<Sensor>> GetAllSensorsAsync();
        Task<Sensor> GetSensorByIdAsync(int sensorId);
        Task<List<Measurement>> GetSensorMeasurementsAsync(int sensorId, DateTime startDate, DateTime endDate);

        // Incident management
        Task<List<Incident>> GetIncidentsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Incident> GetIncidentByIdAsync(int incidentId);
    }
}
