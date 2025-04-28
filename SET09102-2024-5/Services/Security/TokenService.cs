using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services.Security
{
    /// <summary>
    /// Service for generating and validating secure authentication tokens
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly ILoggingService _loggingService;
        private const string TokenCategory = "TokenService";
        
        public TokenService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }
        
        /// <summary>
        /// Generates a new secure token for the specified user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="email">The user's email</param>
        /// <param name="expiry">The token expiration time</param>
        /// <returns>A TokenInfo object containing the token and expiration</returns>
        public Task<TokenInfo> GenerateTokenAsync(int userId, string email, DateTime? expiry = null)
        {
            try
            {
                // Generate a random value for the token
                var randomBytes = new byte[32]; // 256 bits
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                
                // Create base string using user ID and email
                string baseString = $"{userId}:{email}:{Convert.ToBase64String(randomBytes)}";
                
                // Create a hash of the base string for the token
                string tokenValue;
                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(baseString));
                    tokenValue = Convert.ToBase64String(hash);
                }
                
                // Default token expiration is 30 days from now if not specified
                DateTime tokenExpiry = expiry ?? DateTime.UtcNow.AddDays(30);
                
                var tokenInfo = new TokenInfo
                {
                    UserId = userId,
                    Email = email,
                    Token = tokenValue,
                    Expires = tokenExpiry
                };
                
                _loggingService?.Debug($"Token generated for user {userId}", TokenCategory);
                return Task.FromResult(tokenInfo);
            }
            catch (Exception ex)
            {
                _loggingService?.Error("Error generating token", ex, TokenCategory);
                throw;
            }
        }
        
        /// <summary>
        /// Validates a token and returns the associated user information
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <param name="userId">The expected user ID</param>
        /// <param name="email">The expected email</param>
        /// <returns>True if the token is valid, false otherwise</returns>
        public Task<bool> ValidateTokenAsync(TokenInfo tokenInfo)
        {
            if (tokenInfo == null)
            {
                _loggingService?.Warning("Null token provided for validation", TokenCategory);
                return Task.FromResult(false);
            }
            
            try
            {
                // Check if token has expired
                if (tokenInfo.Expires < DateTime.UtcNow)
                {
                    _loggingService?.Warning($"Token expired for user {tokenInfo.UserId}", TokenCategory);
                    return Task.FromResult(false);
                }
                
                // In a more complete implementation, we would validate the token against
                // a stored value in a database or secure storage
                
                _loggingService?.Debug($"Token validated successfully for user {tokenInfo.UserId}", TokenCategory);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _loggingService?.Error("Error validating token", ex, TokenCategory);
                return Task.FromResult(false);
            }
        }
    }
    
    /// <summary>
    /// Contains information about an authentication token
    /// </summary>
    public class TokenInfo
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
    }
}