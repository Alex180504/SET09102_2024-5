using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public class RoleManagementViewModel : BaseViewModel
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<AccessPrivilege> _privilegeRepository;
        private readonly IRepository<RolePrivilege> _rolePrivilegeRepository;
        private readonly IAuthService _authService;

        public ObservableCollection<Role> Roles { get; set; }
        public ObservableCollection<AccessPrivilege> AllPrivileges { get; set; }

        private Role _selectedRole;
        public Role SelectedRole 
        { 
            get => _selectedRole; 
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
                _ = LoadRolePrivilegesAsync();
            }
        }

        private string _newRoleName;
        public string NewRoleName
        {
            get => _newRoleName;
            set
            {
                _newRoleName = value;
                OnPropertyChanged();
            }
        }

        private string _newRoleDescription;
        public string NewRoleDescription
        {
            get => _newRoleDescription;
            set
            {
                _newRoleDescription = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AccessPrivilegeViewModel> RolePrivileges { get; set; }

        public ICommand CreateRoleCommand { get; }
        public ICommand SaveRoleCommand { get; }
        public ICommand DeleteRoleCommand { get; }
        public ICommand TogglePrivilegeCommand { get; }

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

            Roles = new ObservableCollection<Role>();
            AllPrivileges = new ObservableCollection<AccessPrivilege>();
            RolePrivileges = new ObservableCollection<AccessPrivilegeViewModel>();

            CreateRoleCommand = new Command(async () => await CreateRole());
            SaveRoleCommand = new Command(async () => await SaveRole(), () => SelectedRole != null);
            DeleteRoleCommand = new Command(async () => await DeleteRole(), () => SelectedRole != null);
            TogglePrivilegeCommand = new Command<AccessPrivilegeViewModel>(async (p) => await TogglePrivilege(p));

            // Initialize data sequentially to avoid DbContext concurrency issues
            _ = InitializeDataAsync();
        }

        // New method to sequentially load data without concurrent DbContext operations
        private async Task InitializeDataAsync()
        {
            await LoadRolesAsync();
            await LoadPrivilegesAsync();
        }

        private async Task LoadRolesAsync()
        {
            IsBusy = true;
            try
            {
                var roles = await _roleRepository.GetAllAsync();
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadPrivilegesAsync()
        {
            IsBusy = true;
            try
            {
                var privileges = await _privilegeRepository.GetAllAsync();
                AllPrivileges.Clear();
                foreach (var privilege in privileges)
                {
                    AllPrivileges.Add(privilege);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadRolePrivilegesAsync()
        {
            if (SelectedRole == null) return;

            IsBusy = true;
            try
            {
                var rolePrivileges = await _rolePrivilegeRepository.FindAsync(rp => rp.RoleId == SelectedRole.RoleId);
                var rolePrivilegeIds = rolePrivileges.Select(rp => rp.AccessPrivilegeId);

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
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateRole()
        {
            if (string.IsNullOrWhiteSpace(NewRoleName))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Role name is required", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var role = new Role
                {
                    RoleName = NewRoleName,
                    Description = NewRoleDescription
                };

                await _roleRepository.AddAsync(role);
                await _roleRepository.SaveChangesAsync();

                // Clear inputs
                NewRoleName = string.Empty;
                NewRoleDescription = string.Empty;
                OnPropertyChanged(nameof(NewRoleName));
                OnPropertyChanged(nameof(NewRoleDescription));

                // Reload roles
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create role: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveRole()
        {
            if (SelectedRole == null) return;

            IsBusy = true;
            try
            {
                _roleRepository.Update(SelectedRole);
                await _roleRepository.SaveChangesAsync();

                // Update role privileges
                var existingPrivileges = await _rolePrivilegeRepository.FindAsync(rp => rp.RoleId == SelectedRole.RoleId);
                var existingPrivilegeIds = existingPrivileges.Select(rp => rp.AccessPrivilegeId).ToList();

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
                    await _rolePrivilegeRepository.AddRangeAsync(newRolePrivileges);
                }

                await _rolePrivilegeRepository.SaveChangesAsync();

                await Application.Current.MainPage.DisplayAlert("Success", "Role updated successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save role: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteRole()
        {
            if (SelectedRole == null) return;

            // Check if this is an admin role or there are users with this role
            try
            {
                // Don't allow deleting the Administrator role
                if (SelectedRole.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "The Administrator role cannot be deleted", "OK");
                    return;
                }

                // Check if any users have this role
                bool hasUsers = SelectedRole.Users?.Any() == true;
                if (hasUsers)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Cannot delete a role that is assigned to users", "OK");
                    return;
                }

                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete", 
                    $"Are you sure you want to delete the role '{SelectedRole.RoleName}'?", 
                    "Yes", "No");

                if (!confirm) return;

                IsBusy = true;

                // Remove all role privileges first
                var rolePrivileges = await _rolePrivilegeRepository.FindAsync(rp => rp.RoleId == SelectedRole.RoleId);
                _rolePrivilegeRepository.RemoveRange(rolePrivileges);
                await _rolePrivilegeRepository.SaveChangesAsync();

                // Now remove the role
                _roleRepository.Remove(SelectedRole);
                await _roleRepository.SaveChangesAsync();

                SelectedRole = null;
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete role: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TogglePrivilege(AccessPrivilegeViewModel privilegeVm)
        {
            if (SelectedRole == null || privilegeVm == null) return;

            privilegeVm.IsAssigned = !privilegeVm.IsAssigned;
        }
    }

    public class AccessPrivilegeViewModel : BaseViewModel
    {
        private AccessPrivilege _accessPrivilege;
        public AccessPrivilege AccessPrivilege
        {
            get => _accessPrivilege;
            set
            {
                _accessPrivilege = value;
                OnPropertyChanged();
            }
        }

        private bool _isAssigned;
        public bool IsAssigned
        {
            get => _isAssigned;
            set
            {
                _isAssigned = value;
                OnPropertyChanged();
            }
        }
    }
}