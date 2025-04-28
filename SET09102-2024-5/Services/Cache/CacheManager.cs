using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services.Cache
{
    /// <summary>
    /// Provides centralized caching functionality for the application
    /// </summary>
    public class CacheManager : ICacheManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILoggingService _loggingService;
        private const string CacheCategory = "Cache";

        public CacheManager(IMemoryCache memoryCache, ILoggingService loggingService)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _loggingService = loggingService;
        }

        /// <summary>
        /// Retrieves an item from the cache, or creates and caches it if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The type of item to cache</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="factory">The function to create the item if not in cache</param>
        /// <param name="absoluteExpirationRelativeToNow">Optional expiration time</param>
        /// <returns>The cached or newly created item</returns>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            // Try to get the item from cache first
            if (_memoryCache.TryGetValue(key, out T cachedItem))
            {
                _loggingService?.Debug($"Cache hit: {key}", CacheCategory);
                return cachedItem;
            }

            // Not in cache, create the item
            _loggingService?.Debug($"Cache miss: {key}", CacheCategory);
            var item = await factory();

            // Cache the item
            var cacheOptions = new MemoryCacheEntryOptions();
            if (absoluteExpirationRelativeToNow.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow.Value;
            }
            else
            {
                // Default expiration time
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            }

            _memoryCache.Set(key, item, cacheOptions);
            _loggingService?.Debug($"Item cached: {key}", CacheCategory);

            return item;
        }

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _loggingService?.Debug($"Item removed from cache: {key}", CacheCategory);
        }

        /// <summary>
        /// Invalidates all cached items that match a pattern
        /// </summary>
        /// <param name="keyPrefix">The prefix pattern to match</param>
        public void InvalidateByPrefix(string keyPrefix)
        {
            // Unfortunately, MemoryCache doesn't provide a direct way to enumerate keys by prefix
            // This method is a placeholder for more sophisticated implementations
            _loggingService?.Debug($"Cache invalidation by prefix not fully implemented: {keyPrefix}", CacheCategory);
        }
    }
}