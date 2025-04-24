using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public partial class RoleManagementViewModel : BaseViewModel
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<AccessPrivilege> _privilegeRepository;
        private readonly IRepository<RolePrivilege> _rolePrivilegeRepository;
        private readonly IAuthService _authService;

        public ObservableCollection<Role> Roles { get; private set; } = new ObservableCollection<Role>();
        public ObservableCollection<AccessPrivilege> AllPrivileges { get; private set; } = new ObservableCollection<AccessPrivilege>();

        [ObservableProperty]
        private Role _selectedRole;

        [ObservableProperty]
        private string _newRoleName;

        [ObservableProperty]
        private string _newRoleDescription;

        public ObservableCollection<AccessPrivilegeViewModel> RolePrivileges { get; private set; } = new ObservableCollection<AccessPrivilegeViewModel>();

        public ICommand CreateRoleCommand { get; }
        public ICommand SaveRoleCommand { get; }
        public ICommand DeleteRoleCommand { get; }
        public ICommand TogglePrivilegeCommand { get; }
        public ICommand RefreshCommand { get; }

        public RoleManagementViewModel(
            IRepository<Role> roleRepository,
            IRepository<AccessPrivilege> privilegeRepository, 
            IRepository<RolePrivilege> rolePrivilegeRepository,
            IAuthService authService)
        {
            _roleRepository = roleRepository;
            _privilegeRepository = privilegeRepository;
            _rolePrivilegeRepository = rolePrivilegeRepository;
            _authService = authService;

            // Use the newer RelayCommand pattern for async commands
            CreateRoleCommand = new AsyncRelayCommand(CreateRole);
            SaveRoleCommand = new AsyncRelayCommand(SaveRole, () => SelectedRole != null);
            DeleteRoleCommand = new AsyncRelayCommand(DeleteRole, () => SelectedRole != null);
            TogglePrivilegeCommand = new AsyncRelayCommand<AccessPrivilegeViewModel>(TogglePrivilege);
            RefreshCommand = new AsyncRelayCommand(InitializeDataAsync);

            // Initialize data sequentially to avoid DbContext concurrency issues
            Task.Run(InitializeDataAsync);
        }

        // Sequential data loading to prevent DbContext concurrency issues
        private async Task InitializeDataAsync()
        {
            await LoadRolesAsync();
            await LoadPrivilegesAsync();
            
            if (SelectedRole != null)
            {
                await LoadRolePrivilegesAsync();
            }
        }

        private async Task LoadRolesAsync()
        {
            if (IsBusy) return;
            
            try
            {
                StartBusy("Loading roles...");
                var roles = await _roleRepository.GetAllAsync().ConfigureAwait(false);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Roles.Clear();
                    foreach (var role in roles)
                    {
                        Roles.Add(role);
                    }
                });
            }
            catch (Exception ex)
            {
                await HandleError("Error loading roles", ex);
            }
            finally
            {
                EndBusy("Role Management");
            }
        }

        private async Task LoadPrivilegesAsync()
        {
            if (IsBusy) return;
            
            try
            {
                StartBusy("Loading privileges...");
                var privileges = await _privilegeRepository.GetAllAsync().ConfigureAwait(false);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AllPrivileges.Clear();
                    foreach (var privilege in privileges)
                    {
                        AllPrivileges.Add(privilege);
                    }
                });
            }
            catch (Exception ex)
            {
                await HandleError("Error loading privileges", ex);
            }
            finally
            {
                EndBusy("Role Management");
            }
        }

        private async Task LoadRolePrivilegesAsync()
        {
            if (SelectedRole == null || IsBusy) return;

            try
            {
                StartBusy("Loading role privileges...");
                var rolePrivileges = await _rolePrivilegeRepository
                    .FindAsync(rp => rp.RoleId == SelectedRole.RoleId)
                    .ConfigureAwait(false);
                
                var rolePrivilegeIds = rolePrivileges.Select(rp => rp.AccessPrivilegeId).ToHashSet();

                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    // Create view models for all privileges, marking those assigned to this role
                    RolePrivileges.Clear();
                    foreach (var privilege in AllPrivileges)
                    {
                        RolePrivileges.Add(new AccessPrivilegeViewModel
                        {
                            AccessPrivilege = privilege,
                            IsAssigned = rolePrivilegeIds.Contains(privilege.AccessPrivilegeId)
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                await HandleError("Error loading role privileges", ex);
            }
            finally
            {
                EndBusy("Role Management");
            }
        }

        private async Task CreateRole()
        {
            if (string.IsNullOrWhiteSpace(NewRoleName))
            {
                await ShowAlert("Validation Error", "Role name is required", "OK");
                return;
            }

            if (IsBusy) return;

            try
            {
                StartBusy("Creating role...");
                
                var role = new Role
                {
                    RoleName = NewRoleName,
                    Description = NewRoleDescription
                };

                await _roleRepository.AddAsync(role).ConfigureAwait(false);
                await _roleRepository.SaveChangesAsync().ConfigureAwait(false);

                // Clear inputs
                NewRoleName = string.Empty;
                NewRoleDescription = string.Empty;
                
                await LoadRolesAsync().ConfigureAwait(false);
                await ShowAlert("Success", "Role created successfully", "OK");
            }
            catch (Exception ex)
            {
                await HandleError("Failed to create role", ex);
            }
            finally
            {
                EndBusy("Role Management");
            }
        }

        private async Task SaveRole()
        {
            if (SelectedRole == null || IsBusy) return;

            try
            {
                StartBusy("Saving role...");
                
                // Update role details
                _roleRepository.Update(SelectedRole);
                await _roleRepository.SaveChangesAsync().ConfigureAwait(false);
                
                // Update privileges (moved to separate method for readability)
                await UpdateRolePrivilegesAsync().ConfigureAwait(false);
                
                await ShowAlert("Success", "Role updated successfully", "OK");
            }
            catch (Exception ex)
            {
                await HandleError("Failed to save role", ex);
            }
            finally
            {
                EndBusy("Role Management");
            }
        }
        
        // Extracted method to make SaveRole more readable
        private async Task UpdateRolePrivilegesAsync()
        {
            if (SelectedRole == null) return;
            
            // Get existing role privileges
            var existingPrivileges = await _rolePrivilegeRepository
                .FindAsync(rp => rp.RoleId == SelectedRole.RoleId)
                .ConfigureAwait(false);
            
            var existingPrivilegeIds = existingPrivileges.Select(rp => rp.AccessPrivilegeId).ToList();

            // Get selected privileges
            var selectedPrivilegeVms = RolePrivileges.Where(rp => rp.IsAssigned).ToList();
            var selectedPrivilegeIds = selectedPrivilegeVms.Select(rp => rp.AccessPrivilege.AccessPrivilegeId).ToList();

            // Remove privileges that were unselected
            var toRemove = existingPrivileges.Where(rp => !selectedPrivilegeIds.Contains(rp.AccessPrivilegeId)).ToList();
            if (toRemove.Any())
            {
                _rolePrivilegeRepository.RemoveRange(toRemove);
            }

            // Add newly selected privileges
            var newPrivilegeIds = selectedPrivilegeIds.Except(existingPrivilegeIds).ToList();
            if (newPrivilegeIds.Any())
            {
                var newRolePrivileges = newPrivilegeIds.Select(id => new RolePrivilege
                {
                    RoleId = SelectedRole.RoleId,
                    AccessPrivilegeId = id
                });
                await _rolePrivilegeRepository.AddRangeAsync(newRolePrivileges).ConfigureAwait(false);
            }

            await _rolePrivilegeRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task DeleteRole()
        {
            if (SelectedRole == null || IsBusy) return;

            try
            {
                // Validate delete operation
                string validationError = await ValidateRoleDeletion().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(validationError))
                {
                    await ShowAlert("Cannot Delete Role", validationError, "OK");
                    return;
                }

                // Confirm deletion
                bool confirm = await Confirm(
                    "Confirm Delete", 
                    $"Are you sure you want to delete the role '{SelectedRole.RoleName}'?", 
                    "Yes", "No");

                if (!confirm) return;

                StartBusy("Deleting role...");
                
                // Delete role privileges first
                await DeleteRolePrivilegesAsync().ConfigureAwait(false);
                
                // Delete the role
                _roleRepository.Remove(SelectedRole);
                await _roleRepository.SaveChangesAsync().ConfigureAwait(false);
                
                // Clear selection and reload
                SelectedRole = null;
                await LoadRolesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HandleError("Failed to delete role", ex);
            }
            finally
            {
                EndBusy("Role Management");
            }
        }
        
        // Extract role validation logic for better testability and readability
        private async Task<string> ValidateRoleDeletion()
        {
            if (SelectedRole == null) return "No role selected";
            
            // Don't allow deleting the Administrator role
            if (SelectedRole.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return "The Administrator role cannot be deleted";
            }

            // Check if any users have this role
            bool hasUsers = SelectedRole.Users?.Any() == true;
            if (hasUsers)
            {
                return "Cannot delete a role that is assigned to users";
            }
            
            return string.Empty; // No validation errors
        }
        
        // Extract privilege deletion logic to make DeleteRole more readable
        private async Task DeleteRolePrivilegesAsync()
        {
            if (SelectedRole == null) return;
            
            var rolePrivileges = await _rolePrivilegeRepository
                .FindAsync(rp => rp.RoleId == SelectedRole.RoleId)
                .ConfigureAwait(false);
                
            if (rolePrivileges.Any())
            {
                _rolePrivilegeRepository.RemoveRange(rolePrivileges);
                await _rolePrivilegeRepository.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private Task TogglePrivilege(AccessPrivilegeViewModel privilegeVm)
        {
            if (SelectedRole == null || privilegeVm == null) return Task.CompletedTask;
            
            privilegeVm.IsAssigned = !privilegeVm.IsAssigned;
            return Task.CompletedTask;
        }
        
        // Helper methods for UI operations
        private Task<bool> Confirm(string title, string message, string accept, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(async () => 
                await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel));
        }
        
        private Task ShowAlert(string title, string message, string accept)
        {
            return MainThread.InvokeOnMainThreadAsync(async () => 
                await Application.Current.MainPage.DisplayAlert(title, message, accept));
        }
        
        private Task HandleError(string operation, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{operation}: {ex}");
            return MainThread.InvokeOnMainThreadAsync(async () => 
                await Application.Current.MainPage.DisplayAlert("Error", $"{operation}: {ex.Message}", "OK"));
        }
        
        // Override setter to handle selection change
        [ObservableProperty]
        private Role _selectedRoleField;
        partial void OnSelectedRoleChanged(Role value)
        {
            if (value != null)
            {
                Task.Run(() => LoadRolePrivilegesAsync());
            }
            else
            {
                RolePrivileges.Clear();
            }
        }
    }

    public partial class AccessPrivilegeViewModel : ObservableObject
    {
        [ObservableProperty]
        private AccessPrivilege _accessPrivilege;

        [ObservableProperty]
        private bool _isAssigned;
    }
}