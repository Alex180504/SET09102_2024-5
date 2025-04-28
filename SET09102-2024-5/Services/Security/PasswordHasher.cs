using System;
using System.Security.Cryptography;
using System.Text;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services.Security
{
    /// <summary>
    /// Provides utility methods for secure password hashing and verification
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private bool _disposed = false;
        private readonly ILoggingService _loggingService;

        public PasswordHasher(ILoggingService loggingService = null)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Creates a secure hash and salt for a password
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <param name="passwordHash">The output password hash</param>
        /// <param name="passwordSalt">The output password salt</param>
        /// <exception cref="ArgumentNullException">Thrown if password is null</exception>
        public void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            try
            {
                // Use HMACSHA512 for better security than MD5 or SHA1
                using var hmac = new HMACSHA512();
                passwordSalt = Convert.ToBase64String(hmac.Key);
                passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
            catch (Exception ex)
            {
                _loggingService?.Error("Error creating password hash", ex, "Security");
                throw new CryptographicException("Password hashing failed", ex);
            }
        }

        /// <summary>
        /// Verifies a password against a stored hash and salt
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <param name="storedHash">The previously stored hash</param>
        /// <param name="storedSalt">The previously stored salt</param>
        /// <returns>True if the password is correct, otherwise false</returns>
        public bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
                return false;

            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                
                using var hmac = new HMACSHA512(saltBytes);
                var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                
                return computedHash == storedHash;
            }
            catch (Exception ex)
            {
                _loggingService?.Error("Error verifying password hash", ex, "Security");
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}