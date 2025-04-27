using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using Microsoft.EntityFrameworkCore;

namespace SET09102_2024_5.ViewModels
{
    public partial class RoleManagementViewModel : BaseViewModel
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<AccessPrivilege> _privilegeRepository;
        private readonly IRepository<RolePrivilege> _rolePrivilegeRepository;
        private readonly IAuthService _authService;
        private readonly IMemoryCache _cache;

        // Tab control properties
        [ObservableProperty]
        private bool _isCreateRoleTab = true;

        [ObservableProperty]
        private bool _isManageRolesTab = false;

        // Properties for the privileges and users tabs in Manage Roles
        [ObservableProperty]
        private bool _isUsersTabSelected = false;

        [ObservableProperty]
        private ObservableCollection<Role> _roles = new();

        [ObservableProperty]
        private ObservableCollection<AccessPrivilege> _allPrivileges = new();

        [ObservableProperty]
        private Role _selectedRole;

        [ObservableProperty]
        private string _newRoleName;

        [ObservableProperty]
        private string _newRoleDescription;

        [ObservableProperty]
        private ObservableCollection<AccessPrivilegeViewModel> _rolePrivileges = new();

        // New property for privileges when creating a new role
        [ObservableProperty]
        private ObservableCollection<PrivilegeModuleGroup> _newRolePrivilegeGroups = new();

        // New property for privileges of existing role, organized by module
        [ObservableProperty]
        private ObservableCollection<PrivilegeModuleGroup> _rolePrivilegeGroups = new();

        public RoleManagementViewModel(
            IRepository<Role> roleRepository,
            IRepository<AccessPrivilege> privilegeRepository, 
            IRepository<RolePrivilege> rolePrivilegeRepository,
            IAuthService authService,
            IMemoryCache cache)
        {
            _roleRepository = roleRepository;
            _privilegeRepository = privilegeRepository;
            _rolePrivilegeRepository = rolePrivilegeRepository;
            _authService = authService;
            _cache = cache;

            Title = "Role Management";
            
            // Don't load data automatically in constructor
            // Let the view call InitializeDataAsync explicitly
        }

        // Command to switch between privileges and users tabs
        [RelayCommand]
        private void SetActiveSection(string section)
        {
            if (string.IsNullOrWhiteSpace(section)) return;

            try {
                bool oldValue = IsUsersTabSelected;
                
                switch (section.ToLower())
                {
                    case "privileges":
                        IsUsersTabSelected = false;
                        break;
                    case "users":
                        IsUsersTabSelected = true;
                        break;
                    default:
                        return;
                }
                
                // Only trigger property change if the value actually changed
                if (oldValue != IsUsersTabSelected)
                {
                    System.Diagnostics.Debug.WriteLine($"Tab changed to: {section}, IsUsersTabSelected={IsUsersTabSelected}, RolePrivilegeGroups.Count={RolePrivilegeGroups.Count}");
                    OnPropertyChanged(nameof(IsUsersTabSelected));
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error in SetActiveSection: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task InitializeDataAsync()
        {
            // If already busy, ensure we reset it first to clear any stuck overlays
            if (IsBusy)
            {
                System.Diagnostics.Debug.WriteLine("InitializeDataAsync: Resetting stuck IsBusy state");
                IsBusy = false;
                await Task.Delay(100); // Small delay to ensure UI updates
            }
            
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeDataAsync: Starting data load");
                ErrorMessage = string.Empty;
                IsBusy = true;
                
                // Clear cache entries for relevant types to ensure fresh data
                _cache.Remove("Role_all");
                _cache.Remove("AccessPrivilege_all");
                
                await LoadRolesAsync();
                await LoadPrivilegesAsync();
                await LoadNewRolePrivilegeGroupsAsync(); // Load privilege groups for new role creation
                
                if (SelectedRole != null)
                {
                    await LoadRolePrivilegesAsync();
                }
                
                System.Diagnostics.Debug.WriteLine("InitializeDataAsync: Data load completed successfully");
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error initializing data: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                ErrorMessage = $"Failed to load role management data: {ex.Message}";
            }
            finally
            {
                // Always ensure IsBusy is reset
                System.Diagnostics.Debug.WriteLine("InitializeDataAsync: Resetting IsBusy in finally block");
                IsBusy = false;
            }
        }

        // Load privilege groups for new role creation organized by module
        private async Task LoadNewRolePrivilegeGroupsAsync()
        {
            if (!AllPrivileges.Any())
            {
                System.Diagnostics.Debug.WriteLine("No privileges available to group");
                return;
            }
            
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Group privileges by ModuleName
                    var groupedPrivileges = AllPrivileges
                        .GroupBy(p => p.ModuleName ?? "General")
                        .OrderBy(g => g.Key);
                    
                    NewRolePrivilegeGroups.Clear();
                    
                    foreach (var group in groupedPrivileges)
                    {
                        var privilegeGroup = new PrivilegeModuleGroup
                        {
                            ModuleName = group.Key,
                            IsExpanded = true,
                            HasHeaderCheckbox = true
                        };
                        
                        foreach (var privilege in group.OrderBy(p => p.Name))
                        {
                            privilegeGroup.Privileges.Add(new AccessPrivilegeViewModel
                            {
                                AccessPrivilege = privilege,
                                IsAssigned = false // Initially unselected
                            });
                        }
                        
                        NewRolePrivilegeGroups.Add(privilegeGroup);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Created {NewRolePrivilegeGroups.Count} privilege groups");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading privilege groups: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        // Toggle all privileges in a module group for new role creation
        [RelayCommand]
        private void ToggleModuleGroupSelection(PrivilegeModuleGroup group)
        {
            if (group == null) return;
            
            bool newState = !group.AreAllPrivilegesSelected;
            
            foreach (var privilege in group.Privileges)
            {
                privilege.IsAssigned = newState;
            }
            
            // Update the group's selection state
            group.UpdateGroupSelectionState();
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                // Directly get from database to avoid caching issues
                var roles = await _roleRepository.GetAllAsync();
                
                if (roles == null || !roles.Any())
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Roles.Clear();
                    });
                    System.Diagnostics.Debug.WriteLine("No roles found in database");
                    return;
                }
                
                // Include related entities if needed
                foreach (var role in roles)
                {
                    // Make sure Users collection is loaded
                    if (role.Users == null)
                    {
                        role.Users = new List<User>();
                    }
                }
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Roles.Clear();
                    foreach (var role in roles)
                    {
                        Roles.Add(role);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {Roles.Count} roles successfully");
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error loading roles: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw; // Rethrow to be caught by the calling method
            }
        }

        private async Task LoadPrivilegesAsync()
        {
            try 
            {
                var privileges = await _privilegeRepository.GetAllAsync();
                
                if (privileges == null || !privileges.Any())
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AllPrivileges.Clear();
                    });
                    System.Diagnostics.Debug.WriteLine("No privileges found in database");
                    return;
                }
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AllPrivileges.Clear();
                    foreach (var privilege in privileges)
                    {
                        AllPrivileges.Add(privilege);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {AllPrivileges.Count} privileges successfully");
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error loading privileges: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw; // Rethrow to be caught by the calling method
            }
        }

        private async Task LoadRolePrivilegesAsync()
        {
            if (SelectedRole == null) return;

            try
            {
                var rolePrivileges = await _rolePrivilegeRepository
                    .FindAsync(rp => rp.RoleId == SelectedRole.RoleId);
                
                var rolePrivilegeIds = rolePrivileges.Select(rp => rp.AccessPrivilegeId).ToHashSet();
                
                System.Diagnostics.Debug.WriteLine($"Found {rolePrivilegeIds.Count} privileges for role {SelectedRole.RoleName}");
                
                // Initialize empty lists first to prevent null reference exceptions
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    RolePrivileges.Clear();
                    RolePrivilegeGroups.Clear();
                });

                // Group privileges by module for the organized view
                var groupedPrivileges = AllPrivileges
                    .GroupBy(p => p.ModuleName ?? "General")
                    .OrderBy(g => g.Key)
                    .ToList();

                // Process in background to avoid UI thread blocking
                var privilegeGroups = new List<PrivilegeModuleGroup>();
                foreach (var group in groupedPrivileges)
                {
                    var privilegeGroup = new PrivilegeModuleGroup
                    {
                        ModuleName = group.Key,
                        IsExpanded = true,
                        HasHeaderCheckbox = true
                    };
                    
                    foreach (var privilege in group.OrderBy(p => p.Name))
                    {
                        var isAssigned = rolePrivilegeIds.Contains(privilege.AccessPrivilegeId);
                        privilegeGroup.Privileges.Add(new AccessPrivilegeViewModel
                        {
                            AccessPrivilege = privilege,
                            IsAssigned = isAssigned
                        });
                        
                        // Also add to flat list for backward compatibility
                        RolePrivileges.Add(new AccessPrivilegeViewModel
                        {
                            AccessPrivilege = privilege,
                            IsAssigned = isAssigned
                        });
                    }
                    
                    // Update group selection state
                    privilegeGroup.UpdateGroupSelectionState();
                    privilegeGroups.Add(privilegeGroup);
                    System.Diagnostics.Debug.WriteLine($"Prepared group '{group.Key}' with {privilegeGroup.Privileges.Count} privileges");
                }

                // Update UI on main thread when all data is ready
                await MainThread.InvokeOnMainThreadAsync(() => 
                {
                    foreach (var group in privilegeGroups)
                    {
                        RolePrivilegeGroups.Add(group);
                    }
                    
                    // Force property change notifications to refresh the UI
                    OnPropertyChanged(nameof(RolePrivileges));
                    OnPropertyChanged(nameof(RolePrivilegeGroups));
                    
                    System.Diagnostics.Debug.WriteLine($"UI updated with {RolePrivilegeGroups.Count} privilege groups containing {RolePrivileges.Count} total privileges");
                    
                    // Ensure we're on privileges tab
                    IsUsersTabSelected = false;
                    OnPropertyChanged(nameof(IsUsersTabSelected));
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error loading role privileges: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw; // Rethrow to be caught by the calling ExecuteAsync method
            }
        }

        [RelayCommand(CanExecute = nameof(CanCreateRole))]
        private async Task CreateRoleAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Check if a role with this name already exists - use ToList() to execute the query client-side
                // This forces client evaluation to avoid the EF Core translation issue with String.Equals
                var roles = await _roleRepository.GetAllAsync();
                var existingRole = roles.FirstOrDefault(r => 
                    r.RoleName.Equals(NewRoleName, StringComparison.OrdinalIgnoreCase));
                    
                if (existingRole != null)
                {
                    await ShowAlert("Error", $"A role with the name '{NewRoleName}' already exists. Please use a different name.", "OK");
                    return;
                }
                
                // Create new role
                var role = new Role
                {
                    RoleName = NewRoleName,
                    Description = NewRoleDescription
                };

                await _roleRepository.AddAsync(role);
                await _roleRepository.SaveChangesAsync();

                // Assign privileges from groups to the new role
                var selectedPrivileges = NewRolePrivilegeGroups
                    .SelectMany(g => g.Privileges)
                    .Where(p => p.IsAssigned)
                    .Select(p => new RolePrivilege
                    {
                        RoleId = role.RoleId,
                        AccessPrivilegeId = p.AccessPrivilege.AccessPrivilegeId
                    })
                    .ToList();

                if (selectedPrivileges.Any())
                {
                    await _rolePrivilegeRepository.AddRangeAsync(selectedPrivileges);
                    await _rolePrivilegeRepository.SaveChangesAsync();
                }

                // Clear inputs and reset selections
                NewRoleName = string.Empty;
                NewRoleDescription = string.Empty;
                await ResetNewRolePrivilegeSelections(); // Reset selections
                
                await LoadRolesAsync();
                await ShowAlert("Success", "Role created successfully", "OK");
            }, "Creating role...", "Failed to create role");
        }

        // Reset privilege selections for new role
        private async Task ResetNewRolePrivilegeSelections()
        {
            foreach (var group in NewRolePrivilegeGroups)
            {
                foreach (var privilege in group.Privileges)
                {
                    privilege.IsAssigned = false;
                }
                group.UpdateGroupSelectionState();
            }
        }

        private bool CanCreateRole() => !string.IsNullOrWhiteSpace(NewRoleName);

        [RelayCommand(CanExecute = nameof(CanSaveRole))]
        private async Task SaveRoleAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Update role details
                _roleRepository.Update(SelectedRole);
                await _roleRepository.SaveChangesAsync();
                
                // Update privileges
                await UpdateRolePrivilegesAsync();
                
                await ShowAlert("Success", "Role updated successfully", "OK");
            }, "Saving role...", "Failed to save role");
        }
        
        private bool CanSaveRole() => SelectedRole != null;

        private async Task UpdateRolePrivilegesAsync()
        {
            if (SelectedRole == null) return;
            
            // Get existing role privileges
            var existingPrivileges = await _rolePrivilegeRepository
                .FindAsync(rp => rp.RoleId == SelectedRole.RoleId);
            
            var existingPrivilegeIds = existingPrivileges.Select(rp => rp.AccessPrivilegeId).ToList();

            // Get selected privileges from grouped privileges
            var selectedPrivilegeVms = RolePrivilegeGroups
                .SelectMany(g => g.Privileges)
                .Where(p => p.IsAssigned)
                .ToList();
                
            var selectedPrivilegeIds = selectedPrivilegeVms
                .Select(p => p.AccessPrivilege.AccessPrivilegeId)
                .ToList();

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
        }

        [RelayCommand(CanExecute = nameof(CanDeleteRole))]
        private async Task DeleteRoleAsync()
        {
            // Validate delete operation
            string validationError = await ValidateRoleDeletion();
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

            await ExecuteAsync(async () =>
            {
                // Delete role privileges first
                await DeleteRolePrivilegesAsync();
                
                // Delete the role
                _roleRepository.Remove(SelectedRole);
                await _roleRepository.SaveChangesAsync();
                
                // Clear selection and reload
                SelectedRole = null;
                await LoadRolesAsync();
            }, "Deleting role...", "Failed to delete role");
        }
        
        private bool CanDeleteRole() => SelectedRole != null;
        
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
        
        private async Task DeleteRolePrivilegesAsync()
        {
            if (SelectedRole == null) return;
            
            var rolePrivileges = await _rolePrivilegeRepository
                .FindAsync(rp => rp.RoleId == SelectedRole.RoleId);
                
            if (rolePrivileges.Any())
            {
                _rolePrivilegeRepository.RemoveRange(rolePrivileges);
                await _rolePrivilegeRepository.SaveChangesAsync();
            }
        }

        [RelayCommand]
        private void ToggleRoleModuleGroupSelection(PrivilegeModuleGroup group)
        {
            if (group == null || SelectedRole == null || SelectedRole.IsProtected) return;
            
            bool newState = !group.AreAllPrivilegesSelected;
            
            foreach (var privilege in group.Privileges)
            {
                privilege.IsAssigned = newState;
            }
            
            // Update the group's selection state
            group.UpdateGroupSelectionState();
        }

        [RelayCommand]
        private void TogglePrivilege(AccessPrivilegeViewModel privilegeVm)
        {
            if (SelectedRole == null || privilegeVm == null || SelectedRole.IsProtected) return;
            
            // Toggle the assigned state
            privilegeVm.IsAssigned = !privilegeVm.IsAssigned;
            
            // Update the group's selection state if this privilege belongs to a group
            if (privilegeVm.ParentGroup != null)
            {
                privilegeVm.ParentGroup.UpdateGroupSelectionState();
            }
            
            System.Diagnostics.Debug.WriteLine($"Toggled privilege: {privilegeVm.AccessPrivilege.Name} to {privilegeVm.IsAssigned}");
            
            // Notify that save command can execute
            SaveRoleCommand.NotifyCanExecuteChanged();
        }
        
        // Helper methods for UI operations
        private static Task<bool> Confirm(string title, string message, string accept, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(async () => 
                await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel));
        }
        
        private static Task ShowAlert(string title, string message, string accept)
        {
            return MainThread.InvokeOnMainThreadAsync(async () => 
                await Application.Current.MainPage.DisplayAlert(title, message, accept));
        }
        
        // Update command notification when properties change
        partial void OnNewRoleNameChanged(string value) => CreateRoleCommand.NotifyCanExecuteChanged();
        partial void OnSelectedRoleChanged(Role value)
        {
            SaveRoleCommand.NotifyCanExecuteChanged();
            DeleteRoleCommand.NotifyCanExecuteChanged();
            
            if (value != null)
            {
                System.Diagnostics.Debug.WriteLine($"Selected Role: {value.RoleName} (ID: {value.RoleId})");
                
                // Ensure we're on the privileges tab when a role is selected
                IsUsersTabSelected = false;
                
                // Load role privileges with proper data loading
                ExecuteAsync(async () => 
                {
                    try
                    {
                        // First, ensure the role has its related data fully loaded
                        var completeRole = await _roleRepository.GetByIdAsync(value.RoleId);
                        if (completeRole != null)
                        {
                            // Update selected role with complete data if needed
                            if (value != completeRole)
                            {
                                SelectedRole = completeRole;
                            }
                        }
                        
                        // Now load role privileges with proper UI updating
                        await LoadRolePrivilegesAsync();
                        System.Diagnostics.Debug.WriteLine($"Loaded {RolePrivilegeGroups.Count} privilege groups for role");
                        
                        // Force UI update after loading privileges
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            // Explicitly notify that privileges have changed
                            OnPropertyChanged(nameof(RolePrivileges));
                            OnPropertyChanged(nameof(RolePrivilegeGroups));
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading role privileges: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        }
                    }
                }, 
                "Loading role privileges...", 
                "Failed to load role privileges");
            }
            else
            {
                RolePrivileges.Clear();
                RolePrivilegeGroups.Clear();
            }
        }

        [RelayCommand]
        private async Task SelectRoleAsync(Role role)
        {
            if (role == null) return;
            
            try {
                // Set the selected role
                SelectedRole = role;
                
                // Always show privileges tab when selecting a role
                IsUsersTabSelected = false;
                
                System.Diagnostics.Debug.WriteLine($"Role selected: {role.RoleName} - Showing privileges tab (IsUsersTabSelected={IsUsersTabSelected})");
                
                // Force UI update after changing tabs
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Explicitly notify both properties changed
                    OnPropertyChanged(nameof(SelectedRole));
                    OnPropertyChanged(nameof(IsUsersTabSelected));
                });
                
                // Force layout update by triggering a small delay
                await Task.Delay(50);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error in SelectRoleAsync: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SwitchTab(string tab)
        {
            if (string.IsNullOrWhiteSpace(tab)) return;

            switch (tab.ToLower())
            {
                case "create":
                    IsCreateRoleTab = true;
                    IsManageRolesTab = false;
                    break;
                case "manage":
                    IsCreateRoleTab = false;
                    IsManageRolesTab = true;
                    break;
                default:
                    break;
            }
        }
    }

    public partial class AccessPrivilegeViewModel : ObservableObject
    {
        [ObservableProperty]
        private AccessPrivilege _accessPrivilege;

        [ObservableProperty]
        private bool _isAssigned;

        partial void OnIsAssignedChanged(bool value)
        {
            // If this is within a group, update the parent group selection status
            if (ParentGroup != null)
            {
                ParentGroup.UpdateGroupSelectionState();
            }
        }

        // Reference to parent group if this privilege belongs to one
        public PrivilegeModuleGroup ParentGroup { get; set; }
    }

    public partial class PrivilegeModuleGroup : ObservableObject
    {
        [ObservableProperty]
        private string _moduleName;

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _hasHeaderCheckbox;

        [ObservableProperty]
        private bool _areAllPrivilegesSelected;

        [ObservableProperty]
        private bool _areSomePrivilegesSelected;

        public ObservableCollection<AccessPrivilegeViewModel> Privileges { get; } = new();

        public PrivilegeModuleGroup()
        {
            // Set parent reference for each privilege
            Privileges.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (AccessPrivilegeViewModel item in e.NewItems)
                    {
                        item.ParentGroup = this;
                    }
                }
            };
        }

        // Update group selection state based on child privileges
        public void UpdateGroupSelectionState()
        {
            if (!Privileges.Any())
            {
                AreAllPrivilegesSelected = false;
                AreSomePrivilegesSelected = false;
                return;
            }

            int selectedCount = Privileges.Count(p => p.IsAssigned);
            AreAllPrivilegesSelected = selectedCount == Privileges.Count;
            AreSomePrivilegesSelected = selectedCount > 0 && selectedCount < Privileges.Count;
        }
    }
}