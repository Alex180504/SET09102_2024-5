using System;
using System.Threading.Tasks;
using SET09102_2024_5.Services.Security;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Interface for token generation and validation operations
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a new secure token for the specified user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="email">The user's email</param>
        /// <param name="expiry">The token expiration time</param>
        /// <returns>A TokenInfo object containing the token and expiration</returns>
        Task<TokenInfo> GenerateTokenAsync(int userId, string email, DateTime? expiry = null);
        
        /// <summary>
        /// Validates a token and returns the associated user information
        /// </summary>
        /// <param name="tokenInfo">The token info to validate</param>
        /// <returns>True if the token is valid, false otherwise</returns>
        Task<bool> ValidateTokenAsync(TokenInfo tokenInfo);
    }
}