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
    public class UserRoleManagementViewModel : BaseViewModel
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IAuthService _authService;

        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<Role> Roles { get; set; }

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
            }
        }

        public bool CanSaveChanges => SelectedUser != null && SelectedRole != null;

        public ICommand SaveChangesCommand { get; }
        public ICommand RefreshCommand { get; }

        public UserRoleManagementViewModel(
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IAuthService authService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _authService = authService;

            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();

            SaveChangesCommand = new Command(async () => await SaveChanges(), () => CanSaveChanges);
            RefreshCommand = new Command(async () => await LoadData());

            LoadData();
        }

        private async Task LoadData()
        {
            IsBusy = true;
            try
            {
                var users = await _userRepository.GetAllAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

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

        private async Task SaveChanges()
        {
            if (SelectedUser == null || SelectedRole == null) return;

            // Prevent changing own role if current user is an admin
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser?.UserId == SelectedUser.UserId && 
                currentUser?.Role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) == true &&
                !SelectedRole.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Warning", 
                    "You cannot remove your own Administrator role.", 
                    "OK");
                return;
            }

            IsBusy = true;
            try
            {
                SelectedUser.RoleId = SelectedRole.RoleId;
                SelectedUser.Role = SelectedRole; // Update the navigation property

                _userRepository.Update(SelectedUser);
                await _userRepository.SaveChangesAsync();

                await Application.Current.MainPage.DisplayAlert(
                    "Success", 
                    $"Role for user {SelectedUser.FirstName} {SelectedUser.LastName} updated successfully.", 
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error", 
                    $"Failed to update user role: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}