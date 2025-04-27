using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace SET09102_2024_5.ViewModels
{
    public partial class RoleManagementViewModel : BaseViewModel
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<AccessPrivilege> _privilegeRepository;
        private readonly IRepository<RolePrivilege> _rolePrivilegeRepository;
        private readonly IAuthService _authService;
        private readonly IMemoryCache _cache;

        // Keep track of roles that already had their privileges loaded
        private HashSet<int> _loadedRoleIds = new HashSet<int>();
        
        // Flag to track if there are unsaved changes
        private bool _hasUnsavedPrivilegeChanges = false;
        
        // Dictionary to track original state of privileges for a role
        private Dictionary<int, bool> _originalPrivilegeStates = new Dictionary<int, bool>();
        
        // ID of the currently selected role for privilege comparison
        private int? _currentRoleId = null;
        
        // Flag to prevent reloading privileges when user is making changes
        private bool _isUserModifyingPrivileges = false;

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

        // New property to track if there are pending changes
        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        // Message to display when changes are pending
        [ObservableProperty]
        private string _pendingChangesMessage = string.Empty;

        // Observable collection for users assigned to the selected role
        [ObservableProperty]
        private ObservableCollection<User> _roleUsers = new();

        // Currently selected user in the list
        [ObservableProperty]
        private User _selectedUser;
        
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
            
            // Subscribe to UserRoleChanged message to refresh users when changes are made
            // from the UserRoleManagementPage
            MessagingCenter.Subscribe<UserRoleManagementViewModel, UserRoleChangedMessage>(
                this, "UserRoleChanged", async (sender, message) => 
                {
                    // If we're currently viewing the role that the user was removed from or added to,
                    // reload the users list
                    if (SelectedRole != null && 
                        (SelectedRole.RoleId == message.OldRoleId || SelectedRole.RoleId == message.NewRoleId))
                    {
                        await LoadRoleUsersAsync(SelectedRole);
                    }
                });
        }

        // Command to switch between privileges and users tabs
        [RelayCommand]
        private async Task SetActiveSection(string section)
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
                        // Load users for the selected role when switching to Users tab
                        if (SelectedRole != null)
                        {
                            await LoadRoleUsersAsync(SelectedRole);
                        }
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
                
                // Always load privileges for UI display, even if no role is selected
                await LoadAllPrivilegesForDisplayAsync();
                
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
        
        // New method to load all privileges for the UI when no role is selected
        private async Task LoadAllPrivilegesForDisplayAsync()
        {
            if (AllPrivileges.Count == 0)
            {
                // Load all privileges first if they're not already loaded
                await LoadPrivilegesAsync();
            }
            
            if (RolePrivilegeGroups.Count > 0 || SelectedRole != null)
            {
                // Already have groups loaded or a selected role - no need to proceed
                return;
            }
            
            try
            {
                // Group privileges by module for display
                var groupedPrivileges = AllPrivileges
                    .GroupBy(p => p.ModuleName ?? "General")
                    .OrderBy(g => g.Key);
                
                var privilegeGroups = new List<PrivilegeModuleGroup>();
                
                // Create privilege groups
                foreach (var group in groupedPrivileges)
                {
                    var privilegeGroup = new PrivilegeModuleGroup
                    {
                        ModuleName = group.Key,
                        IsExpanded = true,
                        HasHeaderCheckbox = true
                    };
                    
                    // Add privileges to the group with unchecked state
                    foreach (var privilege in group.OrderBy(p => p.Name))
                    {
                        privilegeGroup.Privileges.Add(new AccessPrivilegeViewModel
                        {
                            AccessPrivilege = privilege,
                            IsAssigned = false // Initially unchecked when no role is selected
                        });
                    }
                    
                    privilegeGroup.UpdateGroupSelectionState();
                    privilegeGroups.Add(privilegeGroup);
                }
                
                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Clear existing groups first
                    RolePrivilegeGroups.Clear();
                    RolePrivileges.Clear();
                    
                    // Add the new groups
                    foreach (var group in privilegeGroups)
                    {
                        RolePrivilegeGroups.Add(group);
                        
                        // Also add to flat list for backward compatibility
                        foreach (var privilege in group.Privileges)
                        {
                            RolePrivileges.Add(privilege);
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Loaded {RolePrivilegeGroups.Count} privilege groups with {RolePrivileges.Count} privileges for display");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading privileges for display: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
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

        // Unified method to load role privileges - replaces both previous methods
        private async Task LoadRolePrivilegesAsync(Role role = null)
        {
            // Use SelectedRole if no specific role provided
            var targetRole = role ?? SelectedRole;
            if (targetRole == null) return;

            try
            {
                var rolePrivileges = await _rolePrivilegeRepository
                    .FindAsync(rp => rp.RoleId == targetRole.RoleId);
                
                var rolePrivilegeIds = rolePrivileges.Select(rp => rp.AccessPrivilegeId).ToHashSet();
                
                System.Diagnostics.Debug.WriteLine($"Found {rolePrivilegeIds.Count} privileges for role {targetRole.RoleName}");
                
                // Store the current role ID for later comparison
                _currentRoleId = targetRole.RoleId;
                
                // Reset tracking for unsaved changes only if explicitly loading a new role
                // or if the role is different from the current one
                if (role != null || _currentRoleId != targetRole.RoleId)
                {
                    _hasUnsavedPrivilegeChanges = false;
                    HasUnsavedChanges = false;
                    PendingChangesMessage = string.Empty;
                    _originalPrivilegeStates.Clear();
                }
                
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
                        
                        // Store the original state for checking changes later
                        // Only store if not already tracked (preserves user modifications)
                        if (!_originalPrivilegeStates.ContainsKey(privilege.AccessPrivilegeId))
                        {
                            _originalPrivilegeStates[privilege.AccessPrivilegeId] = isAssigned;
                        }
                        
                        var privilegeVm = new AccessPrivilegeViewModel
                        {
                            AccessPrivilege = privilege,
                            IsAssigned = isAssigned,
                            ParentGroup = privilegeGroup
                        };
                        
                        privilegeGroup.Privileges.Add(privilegeVm);
                        RolePrivileges.Add(privilegeVm);
                    }
                    
                    // Update group selection state
                    privilegeGroup.UpdateGroupSelectionState();
                    privilegeGroups.Add(privilegeGroup);
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
                
                // Remember that we've loaded this role's privileges
                if (!_loadedRoleIds.Contains(targetRole.RoleId))
                {
                    _loadedRoleIds.Add(targetRole.RoleId);
                }
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
                // Check if this is a protected role using our centralized helper
                if (await IsProtectedRoleOperation(SelectedRole, "modify"))
                    return;
                    
                // Get a fresh instance of the role from the repository to avoid tracking conflicts
                var dbRole = await _roleRepository.GetByIdAsync(SelectedRole.RoleId);
                if (dbRole != null)
                {
                    // Copy editable properties from the view model to the database entity
                    dbRole.RoleName = SelectedRole.RoleName;
                    dbRole.Description = SelectedRole.Description;
                    
                    // Update the database entity instead of the view model instance
                    _roleRepository.Update(dbRole);
                    await _roleRepository.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException($"Role with ID {SelectedRole.RoleId} could not be found in the database.");
                }
                
                // Update privileges
                await UpdateRolePrivilegesAsync();
                
                // Reset the user modification flag after saving
                _isUserModifyingPrivileges = false;
                
                // Clear unsaved changes tracking
                HasUnsavedChanges = false;
                _hasUnsavedPrivilegeChanges = false;
                PendingChangesMessage = string.Empty;
                
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
            // Check if this is a protected role using our centralized helper
            if (await IsProtectedRoleOperation(SelectedRole, "delete"))
                return;
                
            // Validate delete operation for additional constraints (like assigned users)
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
            
            // Set flag to indicate user is modifying privileges
            _isUserModifyingPrivileges = true;
            
            privilegeVm.IsAssigned = !privilegeVm.IsAssigned;
            privilegeVm.ParentGroup?.UpdateGroupSelectionState();
            CheckForUnsavedChanges();
            _hasUnsavedPrivilegeChanges = HasUnsavedChanges;
            SaveRoleCommand.NotifyCanExecuteChanged();
        }

        private void CheckForUnsavedChanges()
        {
            if (SelectedRole == null || !_originalPrivilegeStates.Any())
                return;
            int changedCount = 0;
            foreach (var privilege in RolePrivilegeGroups.SelectMany(g => g.Privileges))
            {
                int privilegeId = privilege.AccessPrivilege.AccessPrivilegeId;
                if (_originalPrivilegeStates.TryGetValue(privilegeId, out bool originalState) && privilege.IsAssigned != originalState)
                    changedCount++;
            }
            HasUnsavedChanges = changedCount > 0;
            PendingChangesMessage = changedCount > 0 ? $"{changedCount} privilege {(changedCount == 1 ? "change" : "changes")} pending - click 'Update Privileges' to apply" : string.Empty;
        }
        
        // Centralized helper method to handle protected role logic
        private async Task<bool> IsProtectedRoleOperation(Role role, string operation)
        {
            if (role == null) return false;
            
            // Check if the role is protected
            if (role.IsProtected)
            {
                string errorMessage = $"Cannot {operation} the protected role '{role.RoleName}'";
                await ShowAlert("Protected Role", errorMessage, "OK");
                return true;
            }
            
            return false;
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
            
            // If user is actively modifying privileges, don't reload them
            if (_isUserModifyingPrivileges)
            {
                System.Diagnostics.Debug.WriteLine("User is modifying privileges, skipping reload");
                return;
            }
            
            if (value != null)
            {
                System.Diagnostics.Debug.WriteLine($"Selected Role: {value.RoleName} (ID: {value.RoleId})");
                
                // Ensure we're on the privileges tab when a role is selected
                IsUsersTabSelected = false;
                
                // Only load privileges if they haven't been loaded already
                if (!_loadedRoleIds.Contains(value.RoleId))
                {
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
                                // BUT DON'T REPLACE SelectedRole as this would trigger OnSelectedRoleChanged again
                                if (value != completeRole)
                                {
                                    // Update properties we care about without triggering another property change
                                    if (value.Users == null && completeRole.Users != null)
                                        value.Users = completeRole.Users;
                                    if (string.IsNullOrEmpty(value.Description) && !string.IsNullOrEmpty(completeRole.Description))
                                        value.Description = completeRole.Description;
                                }
                            }
                            
                            // Now load role privileges with proper UI updating
                            await LoadRolePrivilegesAsync(value);
                            System.Diagnostics.Debug.WriteLine($"Loaded {RolePrivilegeGroups.Count} privilege groups for role");
                            
                            // Remember that we've loaded this role's privileges
                            _loadedRoleIds.Add(value.RoleId);
                            
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
                    System.Diagnostics.Debug.WriteLine($"Privileges for role {value.RoleId} already loaded, skipping reload");
                }
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

            // Check if there are unsaved changes and prompt the user
            if (HasUnsavedChanges)
            {
                bool shouldSave = await Confirm(
                    "Unsaved Changes", 
                    "You have unsaved privilege changes. Would you like to save them before switching roles?", 
                    "Save", "Discard");
                    
                if (shouldSave)
                {
                    // Save current role changes first
                    if (SelectedRole != null)
                    {
                        await SaveRoleAsync();
                    }
                }
            }

            // Clear the state only if we're selecting a different role
            if (SelectedRole == null || SelectedRole.RoleId != role.RoleId)
            {
                _hasUnsavedPrivilegeChanges = false;
                HasUnsavedChanges = false;
                PendingChangesMessage = string.Empty;
                _originalPrivilegeStates.Clear();
                _currentRoleId = role.RoleId;
            }

            // Set the selected role
            SelectedRole = role;
            IsUsersTabSelected = false;

            // Reset the user modifying flag when switching roles
            _isUserModifyingPrivileges = false;

            // Load privileges for the selected role
            await LoadRolePrivilegesAsync(role);

            // Notify UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                OnPropertyChanged(nameof(SelectedRole));
                OnPropertyChanged(nameof(IsUsersTabSelected));
                OnPropertyChanged(nameof(HasUnsavedChanges));
                OnPropertyChanged(nameof(PendingChangesMessage));
            });
        }

        private async Task LoadRolePrivilegesForRoleAsync(Role role)
        {
            if (role == null) return;
            try
            {
                var rolePrivileges = await _rolePrivilegeRepository.FindAsync(rp => rp.RoleId == role.RoleId);
                var rolePrivilegeIds = rolePrivileges.Select(rp => rp.AccessPrivilegeId).ToHashSet();

                // Always clear and reload
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RolePrivileges.Clear();
                    RolePrivilegeGroups.Clear();
                });

                // Group privileges by module
                var groupedPrivileges = AllPrivileges
                    .GroupBy(p => p.ModuleName ?? "General")
                    .OrderBy(g => g.Key)
                    .ToList();

                var privilegeGroups = new List<PrivilegeModuleGroup>();
                _originalPrivilegeStates.Clear();

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
                        _originalPrivilegeStates[privilege.AccessPrivilegeId] = isAssigned;
                        var privilegeVm = new AccessPrivilegeViewModel
                        {
                            AccessPrivilege = privilege,
                            IsAssigned = isAssigned,
                            ParentGroup = privilegeGroup
                        };
                        privilegeGroup.Privileges.Add(privilegeVm);
                        RolePrivileges.Add(privilegeVm);
                    }
                    privilegeGroup.UpdateGroupSelectionState();
                    privilegeGroups.Add(privilegeGroup);
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var group in privilegeGroups)
                        RolePrivilegeGroups.Add(group);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading privileges for role: {ex.Message}");
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

        // Method to load users for a specific role
        private async Task LoadRoleUsersAsync(Role role)
        {
            if (role == null) return;
            
            try
            {
                // Need to get a fresh instance of the role with all related data
                var completeRole = await _roleRepository.GetByIdAsync(role.RoleId);
                if (completeRole == null || completeRole.Users == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => RoleUsers.Clear());
                    return;
                }

                // Update the RoleUsers collection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RoleUsers.Clear();
                    foreach (var user in completeRole.Users.OrderBy(u => u.Email))
                    {
                        RoleUsers.Add(user);
                    }
                });
                
                System.Diagnostics.Debug.WriteLine($"Loaded {RoleUsers.Count} users for role {role.RoleName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading role users: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RemoveUserFromRoleAsync(User user)
        {
            if (user == null || SelectedRole == null) return;
            
            // Confirm removal
            bool confirm = await Confirm(
                "Confirm Remove User", 
                $"Are you sure you want to remove the user '{user.Email}' from role '{SelectedRole.RoleName}'?", 
                "Yes", "No");

            if (!confirm) return;

            await ExecuteAsync(async () =>
            {
                // Get a fresh instance of the user to avoid conflicts
                var userService = Application.Current.Handler.MauiContext.Services.GetService<IRepository<User>>();
                if (userService == null)
                {
                    await ShowAlert("Error", "User service not available", "OK");
                    return;
                }

                var dbUser = await userService.GetByIdAsync(user.UserId);
                if (dbUser == null)
                {
                    await ShowAlert("Error", "User not found", "OK");
                    return;
                }
                
                // Find a default role to assign
                var defaultRoles = await _roleRepository.FindAsync(r => 
                    r.RoleName.Equals("User", StringComparison.OrdinalIgnoreCase) ||
                    r.RoleName.Equals("Basic User", StringComparison.OrdinalIgnoreCase) ||
                    r.RoleName.Equals("Default", StringComparison.OrdinalIgnoreCase));
                    
                var defaultRole = defaultRoles.FirstOrDefault();
                if (defaultRole == null)
                {
                    await ShowAlert("Error", "Cannot remove user from role because no default role exists to assign them to.", "OK");
                    return;
                }
                
                // Update the user's role
                dbUser.RoleId = defaultRole.RoleId;
                dbUser.Role = defaultRole;
                
                userService.Update(dbUser);
                await userService.SaveChangesAsync();
                
                // Remove from local collection
                RoleUsers.Remove(user);
                
                await ShowAlert("Success", $"User {user.Email} has been removed from role {SelectedRole.RoleName}", "OK");
            }, "Removing user from role...", "Failed to remove user from role");
        }

        // Message object used to communicate role changes between ViewModels
        public class UserRoleChangedMessage
        {
            public int UserId { get; set; }
            public int OldRoleId { get; set; }
            public int NewRoleId { get; set; }
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