using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for managing user role assignments and role privileges
    /// </summary>
    /// <remarks>
    /// This ViewModel provides functionality for:
    /// 1. Assigning roles to users
    /// 2. Viewing and modifying privileges assigned to roles
    /// 3. Searching for specific users
    /// </remarks>
    public partial class UserRoleManagementViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;
        private CancellationTokenSource? _statusMessageCts;

        /// <summary>
        /// Gets or sets the collection of all users in the system
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        /// <summary>
        /// Gets or sets the currently selected user
        /// </summary>
        [ObservableProperty]
        private User? _selectedUser;

        /// <summary>
        /// Gets or sets the collection of all available roles for assignment
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Role> _availableRoles = new();

        /// <summary>
        /// Gets or sets the user's original role before any changes
        /// </summary>
        [ObservableProperty]
        private Role? _originalRole;

        /// <summary>
        /// Gets or sets the collection of all roles in the system
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Role> _roles = new();

        /// <summary>
        /// Gets or sets the currently selected role for privilege management
        /// </summary>
        [ObservableProperty]
        private Role? _selectedRole;

        /// <summary>
        /// Gets or sets the collection of privileges for the selected role
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PrivilegeViewModel> _rolePrivileges = new();

        /// <summary>
        /// Gets or sets the collection of privileges grouped by module
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PrivilegeGroup> _groupedPrivileges = new();
        
        /// <summary>
        /// Gets or sets the current search term for filtering users
        /// </summary>
        [ObservableProperty]
        private string _searchTerm = string.Empty;

        /// <summary>
        /// Gets or sets the status message to display to the user
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether a refresh operation is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isRefreshing;

        /// <summary>
        /// Gets or sets a value indicating whether a save operation is in progress
        /// </summary>
        [ObservableProperty] 
        private bool _isSaving;
        
        /// <summary>
        /// Gets or sets a message describing modified privileges
        /// </summary>
        [ObservableProperty]
        private string _modifiedPrivilegesMessage = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the current role assignment can be saved
        /// </summary>
        public bool CanSaveRoleAssignment => SelectedUser != null && SelectedRole != null && 
                                           (!SelectedUser.Role?.RoleId.Equals(SelectedRole.RoleId) ?? true);

        /// <summary>
        /// Gets a value indicating whether privilege changes can be saved
        /// </summary>
        public bool CanSaveChanges => HasPrivilegeChanges && SelectedRole != null && !SelectedRole.IsProtected;
        
        /// <summary>
        /// Gets a value indicating whether any privileges have been modified
        /// </summary>
        private bool HasPrivilegeChanges => RolePrivileges?.Any(p => p.IsModified) == true;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRoleManagementViewModel"/> class
        /// </summary>
        /// <param name="databaseService">Service for database operations</param>
        /// <param name="navigationService">Service for navigation operations</param>
        public UserRoleManagementViewModel(IDatabaseService databaseService, INavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            
            Title = "User Access Management";
        }

        /// <summary>
        /// Sets the status message and schedules it to be cleared after 3 seconds
        /// </summary>
        /// <param name="message">The message to display</param>
        private void SetStatusMessageWithTimeout(string message)
        {
            // Cancel any existing timer
            _statusMessageCts?.Cancel();
            _statusMessageCts = new CancellationTokenSource();
            
            // Set the new message
            StatusMessage = message;
            
            // Start a new timer to clear the message after 3 seconds
            Task.Delay(3000, _statusMessageCts.Token).ContinueWith(t => 
            {
                if (!t.IsCanceled)
                {
                    // Clear the message on the UI thread
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => 
                    {
                        StatusMessage = string.Empty;
                    });
                }
            }, TaskScheduler.Current);
        }

        /// <summary>
        /// Loads all users and roles from the database
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;

            try
            {
                StartBusy("Loading data...");
                IsRefreshing = true;
                
                // Load all users
                var users = await _databaseService.GetAllUsersWithRolesAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                // Load all roles for dropdown selection
                var roles = await _databaseService.GetAllRolesAsync();
                AvailableRoles.Clear();
                foreach (var role in roles)
                {
                    AvailableRoles.Add(role);
                }

                // Clear selection
                SelectedUser = null;
                SelectedRole = null;

                SetStatusMessageWithTimeout($"Loaded {Users.Count} users and {AvailableRoles.Count} roles");
            }
            catch (Exception ex)
            {
                SetStatusMessageWithTimeout($"Error loading data: {ex.Message}");
            }
            finally
            {
                EndBusy("User Access Management");
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Searches for users based on the current search term
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand]
        private async Task SearchAsync()
        {
            if (IsBusy) return;

            try
            {
                StartBusy("Searching...");
                
                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    // If search is empty, just reload all users
                    await LoadDataAsync();
                    return;
                }

                var searchTermLower = SearchTerm.ToLowerInvariant();
                
                // Filter users based on search term
                var allUsers = await _databaseService.GetAllUsersWithRolesAsync();
                var filteredUsers = allUsers.Where(u => 
                    u.Email.ToLowerInvariant().Contains(searchTermLower) ||
                    (u.Email != null && u.Email.ToLowerInvariant().Contains(searchTermLower)) ||
                    (u.Role != null && u.Role.RoleName.ToLowerInvariant().Contains(searchTermLower))
                ).ToList();

                // Update observable collection
                Users.Clear();
                foreach (var user in filteredUsers)
                {
                    Users.Add(user);
                }

                SetStatusMessageWithTimeout($"Found {Users.Count} users matching '{SearchTerm}'");
            }
            catch (Exception ex)
            {
                SetStatusMessageWithTimeout($"Error during search: {ex.Message}");
            }
            finally
            {
                EndBusy("User Access Management");
            }
        }

        /// <summary>
        /// Loads the role for a specific user
        /// </summary>
        /// <param name="user">The user to load the role for</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand]
        private async Task LoadUserRoleAsync(User user)
        {
            if (user == null) return;

            try
            {
                StartBusy($"Loading role for {user.Email}...");
                
                SelectedUser = user;
                OriginalRole = user.Role;
                SelectedRole = user.Role;

                SetStatusMessageWithTimeout($"Loaded role for {user.Email}");
            }
            catch (Exception ex)
            {
                SetStatusMessageWithTimeout($"Error loading user role: {ex.Message}");
            }
            finally
            {
                EndBusy("User Access Management");
            }
        }

        /// <summary>
        /// Loads the privileges for a specific role
        /// </summary>
        /// <param name="role">The role to load privileges for</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand]
        private async Task LoadRolePrivilegesAsync(Role role)
        {
            if (role == null) return;

            try
            {
                StartBusy($"Loading privileges for {role.RoleName}...");
                
                SelectedRole = role;
                RolePrivileges.Clear();
                GroupedPrivileges.Clear();
                
                // Get all available privileges
                var allPrivileges = await _databaseService.GetAllAccessPrivilegesAsync();
                
                // Get the role's assigned privileges
                var rolePrivileges = await _databaseService.GetRolePrivilegesAsync(role.RoleId);
                var assignedPrivilegeIds = rolePrivileges.Select(rp => rp.AccessPrivilegeId).ToHashSet();
                
                // Group privileges by module
                var privilegeGroups = allPrivileges
                    .OrderBy(p => p.ModuleName)
                    .ThenBy(p => p.Name)
                    .GroupBy(p => p.ModuleName ?? "General")
                    .ToList();
                
                // Add privileges to flat collection for data binding
                foreach (var group in privilegeGroups)
                {
                    foreach (var privilege in group)
                    {
                        var isAssigned = assignedPrivilegeIds.Contains(privilege.AccessPrivilegeId);
                        RolePrivileges.Add(new PrivilegeViewModel(privilege, isAssigned));
                    }
                }

                // Create grouped collection for UI
                foreach (var group in privilegeGroups)
                {
                    var privilegeViewModels = group.Select(p => 
                        new PrivilegeViewModel(p, assignedPrivilegeIds.Contains(p.AccessPrivilegeId))
                    ).ToList();
                    
                    GroupedPrivileges.Add(new PrivilegeGroup(
                        group.Key,
                        new ObservableCollection<PrivilegeViewModel>(privilegeViewModels)
                    ));
                }
                
                SetStatusMessageWithTimeout($"Loaded {RolePrivileges.Count} privileges for {role.RoleName}");
                
                // Reset the modified status
                ModifiedPrivilegesMessage = string.Empty;
            }
            catch (Exception ex)
            {
                SetStatusMessageWithTimeout($"Error loading privileges: {ex.Message}");
            }
            finally
            {
                EndBusy("User Access Management");
            }
        }

        /// <summary>
        /// Toggles the selection state of a privilege
        /// </summary>
        /// <param name="privilege">The privilege to toggle</param>
        [RelayCommand]
        private void TogglePrivilege(PrivilegeViewModel privilege)
        {
            if (privilege == null || SelectedRole?.IsProtected == true) return;
            
            // Toggle the assigned state
            privilege.IsAssigned = !privilege.IsAssigned;
            privilege.IsModified = true;
            
            // Update the notification about changes
            var modifiedCount = RolePrivileges.Count(p => p.IsModified);
            if (modifiedCount > 0)
            {
                ModifiedPrivilegesMessage = $"{modifiedCount} privilege changes pending";
            }
            else
            {
                ModifiedPrivilegesMessage = string.Empty;
            }
            
            // Notify that save command can execute
            OnPropertyChanged(nameof(CanSaveChanges));
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Saves changes to role privileges
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand(CanExecute = nameof(CanSaveChanges))]
        private async Task SaveChangesAsync()
        {
            if (SelectedRole == null || !HasPrivilegeChanges)
            {
                SetStatusMessageWithTimeout("No changes to save.");
                return;
            }

            if (SelectedRole.IsProtected)
            {
                SetStatusMessageWithTimeout("Cannot modify privileges for protected roles.");
                return;
            }

            try
            {
                StartBusy("Saving privilege changes...");
                
                var modifiedPrivileges = RolePrivileges.Where(p => p.IsModified).ToList();
                var addedPrivileges = modifiedPrivileges.Where(p => p.IsAssigned).Select(p => p.Privilege.AccessPrivilegeId).ToList();
                var removedPrivileges = modifiedPrivileges.Where(p => !p.IsAssigned).Select(p => p.Privilege.AccessPrivilegeId).ToList();
                
                // Update the role privileges in the database
                bool success = await _databaseService.UpdateRolePrivilegesAsync(
                    SelectedRole.RoleId, 
                    addedPrivileges, 
                    removedPrivileges);
                
                if (success)
                {
                    // Reset modified status
                    foreach (var privilege in RolePrivileges)
                    {
                        privilege.IsModified = false;
                    }
                    
                    ModifiedPrivilegesMessage = string.Empty;
                    SetStatusMessageWithTimeout($"Successfully updated privileges for {SelectedRole.RoleName}");
                    
                    // Notify property changes
                    OnPropertyChanged(nameof(CanSaveChanges));
                    SaveChangesCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    SetStatusMessageWithTimeout("Failed to save privilege changes.");
                }
            }
            catch (Exception ex)
            {
                SetStatusMessageWithTimeout($"Error saving privileges: {ex.Message}");
            }
            finally
            {
                EndBusy("User Access Management");
            }
        }

        /// <summary>
        /// Saves a new role assignment for the selected user
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand(CanExecute = nameof(CanSaveRoleAssignment))]
        private async Task SaveUserRoleAsync()
        {
            if (SelectedUser == null || SelectedRole == null)
            {
                SetStatusMessageWithTimeout("User or role not selected.");
                return;
            }

            try
            {
                StartBusy("Updating user role...");
                
                // Store the previous role for potential update in UI
                var previousRole = SelectedUser.Role;
                
                // Update the user's role in the database
                bool success = await _databaseService.UpdateUserRoleAsync(
                    SelectedUser.UserId,
                    SelectedRole.RoleId);
                
                if (success)
                {
                    // Update the user object locally
                    SelectedUser.Role = SelectedRole;
                    OriginalRole = SelectedRole;
                    
                    // Update the user in the list
                    int index = Users.IndexOf(Users.FirstOrDefault(u => u.UserId == SelectedUser.UserId));
                    if (index >= 0)
                    {
                        Users[index] = SelectedUser;
                    }
                    
                    SetStatusMessageWithTimeout($"Successfully updated role for {SelectedUser.Email} to {SelectedRole.RoleName}");
                    
                    // Notify command can execute
                    OnPropertyChanged(nameof(CanSaveRoleAssignment));
                    SaveUserRoleCommand.NotifyCanExecuteChanged();
                    
                    // If the user was previously in another role, notify any interested components
                    // This allows the RoleManagementViewModel to refresh its users list if it's open
                    if (previousRole != null && previousRole.RoleId != SelectedRole.RoleId)
                    {
                        MessagingCenter.Send(this, "UserRoleChanged", new UserRoleChangedMessage 
                        { 
                            UserId = SelectedUser.UserId,
                            OldRoleId = previousRole.RoleId,
                            NewRoleId = SelectedRole.RoleId
                        });
                    }
                }
                else
                {
                    SetStatusMessageWithTimeout("Failed to update user role.");
                }
            }
            catch (Exception ex)
            {
                SetStatusMessageWithTimeout($"Error updating user role: {ex.Message}");
            }
            finally
            {
                EndBusy("User Access Management");
            }
        }

        partial void OnSelectedRoleChanged(Role? value)
        {
            // Notify can save property when the selected role changes
            OnPropertyChanged(nameof(CanSaveRoleAssignment));
            SaveUserRoleCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Clears the status message manually
        /// </summary>
        [RelayCommand]
        private void ClearStatusMessage()
        {
            StatusMessage = string.Empty;
            
            // Also cancel any existing timeout
            _statusMessageCts?.Cancel();
        }
        
        /// <summary>
        /// Refreshes the data by reloading users and roles
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }
        
        /// <summary>
        /// Initializes the view model by loading data
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// Cleans up any resources when the page is unloaded
        /// </summary>
        public void Cleanup()
        {
            _statusMessageCts?.Cancel();
            _statusMessageCts?.Dispose();
            _statusMessageCts = null;
        }
    }

    /// <summary>
    /// Represents a privilege with selection state for UI binding
    /// </summary>
    public class PrivilegeViewModel : ObservableObject
    {
        private bool _isAssigned;
        private bool _isModified;

        /// <summary>
        /// Gets the underlying access privilege
        /// </summary>
        public AccessPrivilege Privilege { get; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this privilege is assigned to the role
        /// </summary>
        public bool IsAssigned 
        { 
            get => _isAssigned;
            set => SetProperty(ref _isAssigned, value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether this privilege has been modified
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivilegeViewModel"/> class
        /// </summary>
        /// <param name="privilege">The underlying access privilege</param>
        /// <param name="isAssigned">Whether the privilege is assigned to the role</param>
        public PrivilegeViewModel(AccessPrivilege privilege, bool isAssigned)
        {
            Privilege = privilege;
            _isAssigned = isAssigned;
            _isModified = false;
        }
    }

    /// <summary>
    /// Represents a group of privileges for a specific module
    /// </summary>
    public class PrivilegeGroup : ObservableObject
    {
        /// <summary>
        /// Gets the name of the module this group represents
        /// </summary>
        public string ModuleName { get; }
        
        /// <summary>
        /// Gets the collection of privileges in this module
        /// </summary>
        public ObservableCollection<PrivilegeViewModel> Privileges { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivilegeGroup"/> class
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <param name="privileges">The collection of privileges in this module</param>
        public PrivilegeGroup(string moduleName, ObservableCollection<PrivilegeViewModel> privileges)
        {
            ModuleName = moduleName;
            Privileges = privileges;
        }
    }

    /// <summary>
    /// Message object used to communicate role changes between ViewModels
    /// </summary>
    public class UserRoleChangedMessage
    {
        /// <summary>
        /// Gets or sets the ID of the user whose role was changed
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
}