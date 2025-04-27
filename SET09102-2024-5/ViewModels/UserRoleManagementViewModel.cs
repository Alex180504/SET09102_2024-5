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
    public partial class UserRoleManagementViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;
        private CancellationTokenSource? _statusMessageCts;

        // Properties for user management
        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private ObservableCollection<Role> _availableRoles = new();

        [ObservableProperty]
        private Role? _originalRole;

        // Properties for role management
        [ObservableProperty]
        private ObservableCollection<Role> _roles = new();

        [ObservableProperty]
        private Role? _selectedRole;

        [ObservableProperty]
        private ObservableCollection<PrivilegeViewModel> _rolePrivileges = new();

        [ObservableProperty]
        private ObservableCollection<PrivilegeGroup> _groupedPrivileges = new();
        
        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty] 
        private bool _isSaving;
        
        [ObservableProperty]
        private string _modifiedPrivilegesMessage = string.Empty;

        // For role assignment
        public bool CanSaveRoleAssignment => SelectedUser != null && SelectedRole != null && 
                                           (!SelectedUser.Role?.RoleId.Equals(SelectedRole.RoleId) ?? true);

        // For role privileges
        public bool CanSaveChanges => HasPrivilegeChanges && SelectedRole != null && !SelectedRole.IsProtected;
        
        private bool HasPrivilegeChanges => RolePrivileges?.Any(p => p.IsModified) == true;

        public UserRoleManagementViewModel(IDatabaseService databaseService, INavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            
            Title = "User Access Management";
        }

        // Sets the status message and schedules it to be cleared after 3 seconds
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

        [RelayCommand]
        private void ClearStatusMessage()
        {
            StatusMessage = string.Empty;
            
            // Also cancel any existing timeout
            _statusMessageCts?.Cancel();
        }
        
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }
        
        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        // Clean up any resources when the page is unloaded
        public void Cleanup()
        {
            _statusMessageCts?.Cancel();
            _statusMessageCts?.Dispose();
            _statusMessageCts = null;
        }
    }

    public class PrivilegeViewModel : ObservableObject
    {
        private bool _isAssigned;
        private bool _isModified;

        public AccessPrivilege Privilege { get; }
        
        public bool IsAssigned 
        { 
            get => _isAssigned;
            set => SetProperty(ref _isAssigned, value);
        }
        
        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        public PrivilegeViewModel(AccessPrivilege privilege, bool isAssigned)
        {
            Privilege = privilege;
            _isAssigned = isAssigned;
            _isModified = false;
        }
    }

    public class PrivilegeGroup : ObservableObject
    {
        public string ModuleName { get; }
        public ObservableCollection<PrivilegeViewModel> Privileges { get; }

        public PrivilegeGroup(string moduleName, ObservableCollection<PrivilegeViewModel> privileges)
        {
            ModuleName = moduleName;
            Privileges = privileges;
        }
    }
}