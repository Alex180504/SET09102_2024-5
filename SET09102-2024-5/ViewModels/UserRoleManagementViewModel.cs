using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public class UserRoleManagementViewModel : BaseViewModel
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IAuthService _authService;

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();
        public ObservableCollection<Role> Roles { get; } = new ObservableCollection<Role>();

        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                if (_selectedUser != null)
                {
                    SelectedRole = Roles.FirstOrDefault(r => r.RoleId == _selectedUser.RoleId);
                }
                OnPropertyChanged(nameof(CanSaveChanges));
                ((Command)SaveChangesCommand).ChangeCanExecute();
            }
        }

        private Role _selectedRole;
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveChanges));
                ((Command)SaveChangesCommand).ChangeCanExecute();
            }
        }

        public bool CanSaveChanges => SelectedUser != null && SelectedRole != null;

        public ICommand SaveChangesCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        
        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
            }
        }

        public UserRoleManagementViewModel(
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IAuthService authService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _authService = authService;

            SaveChangesCommand = new Command(async () => await SaveChanges(), () => CanSaveChanges);
            RefreshCommand = new Command(async () => await LoadData());
            SearchCommand = new Command(async () => await FilterUsers());

            // Load data on startup
            Task.Run(async () => await LoadData());
        }

        private async Task LoadData()
        {
            if (IsBusy)
                return;
                
            try
            {
                StartBusy("Loading users and roles...");
                
                // Clear existing collections
                Users.Clear();
                Roles.Clear();
                
                // Load roles first
                var roles = await _roleRepository.GetAllAsync().ConfigureAwait(false);
                
                // Load users with proper error handling
                var users = await _userRepository.GetAllAsync().ConfigureAwait(false);
                
                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var role in roles)
                    {
                        Roles.Add(role);
                    }
                    
                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error", 
                        $"Failed to load user data: {ex.Message}", 
                        "OK");
                });
                System.Diagnostics.Debug.WriteLine($"Error loading user data: {ex}");
            }
            finally
            {
                EndBusy("User Role Management");
            }
        }
        
        private async Task FilterUsers()
        {
            if (IsBusy)
                return;
                
            try
            {
                StartBusy("Searching users...");
                
                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    await LoadData().ConfigureAwait(false);
                    return;
                }
                
                // Keep existing roles
                var allUsers = await _userRepository.GetAllAsync().ConfigureAwait(false);
                
                var term = SearchTerm.ToLower();
                var filteredUsers = allUsers.Where(u => 
                    u.FirstName?.ToLower().Contains(term) == true || 
                    u.LastName?.ToLower().Contains(term) == true || 
                    u.Email?.ToLower().Contains(term) == true).ToList();
                    
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Users.Clear();
                    foreach (var user in filteredUsers)
                    {
                        Users.Add(user);
                    }
                });
            }
            finally
            {
                EndBusy("User Role Management");
            }
        }

        private async Task SaveChanges()
        {
            if (IsBusy || SelectedUser == null || SelectedRole == null)
                return;

            // Prevent changing own role if current user is an admin
            var currentUser = await _authService.GetCurrentUserAsync().ConfigureAwait(false);
            if (currentUser?.UserId == SelectedUser.UserId && 
                currentUser?.Role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true &&
                !SelectedRole.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Warning", 
                        "You cannot remove your own Administrator role.", 
                        "OK");
                });
                return;
            }

            try
            {
                StartBusy("Saving changes...");
                
                // Update role assignment
                SelectedUser.RoleId = SelectedRole.RoleId;
                SelectedUser.Role = SelectedRole;

                _userRepository.Update(SelectedUser);
                await _userRepository.SaveChangesAsync().ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Success", 
                        $"Role for user {SelectedUser.FirstName} {SelectedUser.LastName} updated successfully.", 
                        "OK");
                });
                
                // Refresh the data to ensure UI is up-to-date
                await LoadData().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error", 
                        $"Failed to update user role: {ex.Message}", 
                        "OK");
                });
                System.Diagnostics.Debug.WriteLine($"Error updating user role: {ex}");
            }
            finally
            {
                EndBusy("User Role Management");
            }
        }
    }
}