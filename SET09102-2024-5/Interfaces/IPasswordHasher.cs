using System;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Interface for password hashing operations
    /// </summary>
    public interface IPasswordHasher : IDisposable
    {
        /// <summary>
        /// Creates a secure hash and salt for a password
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <param name="passwordHash">The output password hash</param>
        /// <param name="passwordSalt">The output password salt</param>
        void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt);
        
        /// <summary>
        /// Verifies a password against a stored hash and salt
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <param name="storedHash">The previously stored hash</param>
        /// <param name="storedSalt">The previously stored salt</param>
        /// <returns>True if the password is correct, otherwise false</returns>
        bool VerifyPasswordHash(string password, string storedHash, string storedSalt);
    }
}