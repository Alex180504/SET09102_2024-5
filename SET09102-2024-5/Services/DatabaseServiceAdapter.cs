using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Models;
using SET09102_2024_5.Interfaces;
using Microsoft.Maui.Controls;

namespace SET09102_2024_5.Services
{
    public class DatabaseServiceAdapter : Interfaces.IDatabaseService
    {
        private readonly DatabaseService _databaseService;
        private bool _isReady = false;
        private const string ServiceName = "Database Service";

        public DatabaseServiceAdapter(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                await _databaseService.InitializeAsync();
                _isReady = true;
                return true;
            }
            catch (Exception)
            {
                _isReady = false;
                return false;
            }
        }

        public Task<bool> IsReadyAsync()
        {
            return Task.FromResult(_isReady);
        }

        public string GetServiceStatus()
        {
            return _isReady ? "Ready" : "Not Ready";
        }

        public string GetServiceName()
        {
            return ServiceName;
        }
        
        public Task CleanupAsync() => _databaseService.CleanupAsync();
        
        public Task<bool> TestConnectionAsync() => _databaseService.TestConnectionAsync();
        
        public Task<string> GetConnectionInfoAsync() => _databaseService.GetConnectionInfoAsync();
        
        public Task<List<User>> GetAllUsersAsync() => _databaseService.GetAllUsersAsync();
        
        public Task<List<User>> GetAllUsersWithRolesAsync() => _databaseService.GetAllUsersWithRolesAsync();
        
        public Task<User> GetUserByIdAsync(int userId) => _databaseService.GetUserByIdAsync(userId);
        
        public Task<User> GetUserByEmailAsync(string email) => _databaseService.GetUserByEmailAsync(email);
        
        public Task<bool> UpdateUserRoleAsync(int userId, int roleId) => _databaseService.UpdateUserRoleAsync(userId, roleId);
        
        public Task<bool> DeleteUserAsync(int userId) => _databaseService.DeleteUserAsync(userId);
        
        public Task<List<Role>> GetAllRolesAsync() => _databaseService.GetAllRolesAsync();
        
        public Task<Role> GetRoleByIdAsync(int roleId) => _databaseService.GetRoleByIdAsync(roleId);
        
        public Task<Role> GetRoleByNameAsync(string roleName) => _databaseService.GetRoleByNameAsync(roleName);
        
        public Task<bool> CreateRoleAsync(Role role) => _databaseService.CreateRoleAsync(role);
        
        public Task<bool> UpdateRoleAsync(Role role) => _databaseService.UpdateRoleAsync(role);
        
        public Task<bool> DeleteRoleAsync(int roleId) => _databaseService.DeleteRoleAsync(roleId);
        
        public Task<List<AccessPrivilege>> GetAllAccessPrivilegesAsync() => _databaseService.GetAllAccessPrivilegesAsync();
        
        public Task<List<RolePrivilege>> GetRolePrivilegesAsync(int roleId) => _databaseService.GetRolePrivilegesAsync(roleId);
        
        public Task<bool> UpdateRolePrivilegesAsync(int roleId, List<int> addedPrivilegeIds, List<int> removedPrivilegeIds) => 
            _databaseService.UpdateRolePrivilegesAsync(roleId, addedPrivilegeIds, removedPrivilegeIds);
        
        public Task<List<Sensor>> GetAllSensorsAsync() => _databaseService.GetAllSensorsAsync();
        
        public Task<Sensor> GetSensorByIdAsync(int sensorId) => _databaseService.GetSensorByIdAsync(sensorId);
        
        public Task<List<Measurement>> GetSensorMeasurementsAsync(int sensorId, DateTime startDate, DateTime endDate) => 
            _databaseService.GetSensorMeasurementsAsync(sensorId, startDate, endDate);
        
        public Task<List<Incident>> GetIncidentsAsync(DateTime? startDate = null, DateTime? endDate = null) => 
            _databaseService.GetIncidentsAsync(startDate, endDate);
        
        public Task<Incident> GetIncidentByIdAsync(int incidentId) => _databaseService.GetIncidentByIdAsync(incidentId);
    }
}