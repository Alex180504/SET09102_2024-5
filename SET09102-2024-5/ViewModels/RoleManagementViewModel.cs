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
    /// <summary>
    /// ViewModel for managing user roles and their associated privileges
    /// </summary>
    /// <remarks>
    /// This ViewModel handles the creation, editing, and deletion of roles,
    /// as well as managing the privileges assigned to each role.
    /// It provides functionality for viewing users assigned to roles and
    /// organizing privileges by module for better management.
    /// </remarks>
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

        /// <summary>
        /// Gets or sets a value indicating whether the create role tab is active
        /// </summary>
        [ObservableProperty]
        private bool _isCreateRoleTab = true;

        /// <summary>
        /// Gets or sets a value indicating whether the manage roles tab is active
        /// </summary>
        [ObservableProperty]
        private bool _isManageRolesTab = false;

        /// <summary>
        /// Gets or sets a value indicating whether the users tab is selected in the manage roles view
        /// </summary>
        [ObservableProperty]
        private bool _isUsersTabSelected = false;

        /// <summary>
        /// Gets or sets the collection of all roles in the system
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Role> _roles = new();

        /// <summary>
        /// Gets or sets the collection of all available privileges in the system
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AccessPrivilege> _allPrivileges = new();

        /// <summary>
        /// Gets or sets the currently selected role
        /// </summary>
        [ObservableProperty]
        private Role _selectedRole;

        /// <summary>
        /// Gets or sets the name for a new role being created
        /// </summary>
        [ObservableProperty]
        private string _newRoleName;

        /// <summary>
        /// Gets or sets the description for a new role being created
        /// </summary>
        [ObservableProperty]
        private string _newRoleDescription;

        /// <summary>
        /// Gets or sets the collection of privileges associated with the selected role
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AccessPrivilegeViewModel> _rolePrivileges = new();

        /// <summary>
        /// Gets or sets the collection of privilege groups for a new role, organized by module
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PrivilegeModuleGroup> _newRolePrivilegeGroups = new();

        /// <summary>
        /// Gets or sets the collection of privilege groups for an existing role, organized by module
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PrivilegeModuleGroup> _rolePrivilegeGroups = new();

        /// <summary>
        /// Gets or sets a value indicating whether there are unsaved changes
        /// </summary>
        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        /// <summary>
        /// Gets or sets a message describing pending changes
        /// </summary>
        [ObservableProperty]
        private string _pendingChangesMessage = string.Empty;

        /// <summary>
        /// Gets or sets the collection of users assigned to the selected role
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<User> _roleUsers = new();

        /// <summary>
        /// Gets or sets the currently selected user in the role users list
        /// </summary>
        [ObservableProperty]
        private User _selectedUser;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RoleManagementViewModel"/> class
        /// </summary>
        /// <param name="roleRepository">Repository for accessing role data</param>
        /// <param name="privilegeRepository">Repository for accessing privilege data</param>
        /// <param name="rolePrivilegeRepository">Repository for accessing role-privilege mappings</param>
        /// <param name="authService">Service for authentication operations</param>
        /// <param name="cache">Memory cache for improved performance</param>
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

        /// <summary>
        /// Sets the active section tab (privileges or users) in the manage roles view
        /// </summary>
        /// <param name="section">The section to activate: "privileges" or "users"</param>
        /// <returns>A task that represents the asynchronous operation</returns>
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

        /// <summary>
        /// Initializes the data for the view model, loading roles, privileges, and their relationships
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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

        /// <summary>
        /// Toggles selection of all privileges in a module group for a new role
        /// </summary>
        /// <param name="group">The privilege module group to toggle</param>
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

        /// <summary>
        /// Creates a new role with the specified name, description, and privileges
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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

        /// <summary>
        /// Resets the privilege selections for the new role
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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

        /// <summary>
        /// Determines whether a new role can be created based on validation rules
        /// </summary>
        /// <returns>True if the role can be created; otherwise, false</returns>
        private bool CanCreateRole() => !string.IsNullOrWhiteSpace(NewRoleName);

        /// <summary>
        /// Updates the selected role with any changes to name, description, and privileges
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand(CanExecute = nameof(CanUpdateRole))]
        private async Task UpdateRoleAsync()
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
                    
                    // Update the database entity
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
            }, "Updating role...", "Failed to update role");
        }
        
        /// <summary>
        /// Determines whether the selected role can be updated
        /// </summary>
        /// <returns>True if the role can be updated; otherwise, false</returns>
        private bool CanUpdateRole() => SelectedRole != null && !string.IsNullOrWhiteSpace(SelectedRole.RoleName);

        /// <summary>
        /// Saves changes to the selected role and its privileges
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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
        
        /// <summary>
        /// Determines whether the selected role can be saved
        /// </summary>
        /// <returns>True if the role can be saved; otherwise, false</returns>
        private bool CanSaveRole() => SelectedRole != null;

        /// <summary>
        /// Deletes the selected role after confirmation and validation
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
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
        
        /// <summary>
        /// Determines whether the selected role can be deleted
        /// </summary>
        /// <returns>True if the role can be deleted; otherwise, false</returns>
        private bool CanDeleteRole() => SelectedRole != null;
        
        /// <summary>
        /// Toggles the selection state of all privileges in a module group for an existing role
        /// </summary>
        /// <param name="group">The privilege module group to toggle</param>
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

        /// <summary>
        /// Toggles the selection state of an individual privilege for a role
        /// </summary>
        /// <param name="privilegeVm">The privilege view model to toggle</param>
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

        /// <summary>
        /// Selects a role and loads its associated privileges and users
        /// </summary>
        /// <param name="role">The role to select</param>
        /// <returns>A task that represents the asynchronous operation</returns>
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

        /// <summary>
        /// Switches between the create role and manage roles tabs
        /// </summary>
        /// <param name="tab">The tab to switch to: "create" or "manage"</param>
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

        /// <summary>
        /// Removes a user from the currently selected role
        /// </summary>
        /// <param name="user">The user to remove from the role</param>
        /// <returns>A task that represents the asynchronous operation</returns>
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

        /// <summary>
        /// Message object used to communicate role changes between ViewModels
        /// </summary>
        public class UserRoleChangedMessage
        {
            /// <summary>
            /// Gets or sets the ID of the user whose role changed
            /// </summary>
            public int UserId { get; set; }
            
            /// <summary>
            /// Gets or sets the ID of the user's previous role
            /// </summary>
            public int OldRoleId { get; set; }
            
            /// <summary>
            /// Gets or sets the ID of the user's new role
            /// </summary>
            public int NewRoleId { get; set; }
        }

        /// <summary>
        /// Loads all roles from the database
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Loads all access privileges from the database
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Loads privileges for the specified role, or for the currently selected role if not specified
        /// </summary>
        /// <param name="role">Optional role to load privileges for</param>
        /// <returns>A task representing the asynchronous operation</returns>
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
                    
                    // Also update the flat list for backward compatibility
                    foreach (var privilege in RolePrivilegeGroups.SelectMany(g => g.Privileges))
                    {
                        RolePrivileges.Add(privilege);
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

        /// <summary>
        /// Updates the privileges assigned to the currently selected role
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
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

        /// <summary>
        /// Validates if a role can be deleted, checking constraints
        /// </summary>
        /// <returns>Error message if validation fails; otherwise, empty string</returns>
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
        
        /// <summary>
        /// Deletes all privileges associated with the currently selected role
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
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
    }

    /// <summary>
    /// ViewModel representation of an access privilege with selection state
    /// </summary>
    public partial class AccessPrivilegeViewModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the underlying access privilege
        /// </summary>
        [ObservableProperty]
        private AccessPrivilege _accessPrivilege;

        /// <summary>
        /// Gets or sets a value indicating whether this privilege is assigned to the role
        /// </summary>
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

        /// <summary>
        /// Gets or sets the parent group this privilege belongs to
        /// </summary>
        public PrivilegeModuleGroup ParentGroup { get; set; }
    }

    /// <summary>
    /// Groups access privileges by module for organized display and management
    /// </summary>
    public partial class PrivilegeModuleGroup : ObservableObject
    {
        /// <summary>
        /// Gets or sets the name of the module this group represents
        /// </summary>
        [ObservableProperty]
        private string _moduleName;

        /// <summary>
        /// Gets or sets a value indicating whether this group is expanded in the UI
        /// </summary>
        [ObservableProperty]
        private bool _isExpanded;

        /// <summary>
        /// Gets or sets a value indicating whether this group has a header checkbox for selecting all privileges
        /// </summary>
        [ObservableProperty]
        private bool _hasHeaderCheckbox;

        /// <summary>
        /// Gets or sets a value indicating whether all privileges in this group are selected
        /// </summary>
        [ObservableProperty]
        private bool _areAllPrivilegesSelected;

        /// <summary>
        /// Gets or sets a value indicating whether some (but not all) privileges in this group are selected
        /// </summary>
        [ObservableProperty]
        private bool _areSomePrivilegesSelected;

        /// <summary>
        /// Gets the collection of privileges in this module group
        /// </summary>
        public ObservableCollection<AccessPrivilegeViewModel> Privileges { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivilegeModuleGroup"/> class
        /// </summary>
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

        /// <summary>
        /// Updates the group selection state based on the selection state of child privileges
        /// </summary>
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