using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Interface for cache management operations
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// Retrieves an item from the cache, or creates and caches it if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The type of item to cache</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="factory">The function to create the item if not in cache</param>
        /// <param name="absoluteExpirationRelativeToNow">Optional expiration time</param>
        /// <returns>The cached or newly created item</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpirationRelativeToNow = null);
        
        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        void Remove(string key);
        
        /// <summary>
        /// Invalidates all cached items that match a pattern
        /// </summary>
        /// <param name="keyPrefix">The prefix pattern to match</param>
        void InvalidateByPrefix(string keyPrefix);
    }
}