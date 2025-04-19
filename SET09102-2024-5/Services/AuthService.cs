using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class AuthService : IAuthService
    {
        private readonly SensorMonitoringContext _dbContext;
        private User _currentUser;

        public AuthService(SensorMonitoringContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> RegisterUserAsync(string firstName, string lastName, string email, string password)
        {
            // Check if user already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == email))
            {
                return false;
            }

            // Get guest role (create if doesn't exist)
            var guestRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guest");
            if (guestRole == null)
            {
                guestRole = new Role
                {
                    RoleName = "Guest"
                };
                _dbContext.Roles.Add(guestRole);
                await _dbContext.SaveChangesAsync();
            }

            // Create password hash and salt
            CreatePasswordHash(password, out string passwordHash, out string passwordSalt);

            // Create new user
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RoleId = guestRole.RoleId,
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return null;

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            await _dbContext.SaveChangesAsync();

            _currentUser = user;
            return user;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (!VerifyPasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt))
                return false;

            CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role == null)
                return false;

            return user.Role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase);
        }

        public Task<User> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        private static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            
            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            
            return computedHash == storedHash;
        }
    }
}