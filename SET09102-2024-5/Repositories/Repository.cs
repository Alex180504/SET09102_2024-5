using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;

namespace SET09102_2024_5.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SensorMonitoringContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public Repository(SensorMonitoringContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            string cacheKey = $"{typeof(T).Name}_id_{id}";
            
            if (!_cache.TryGetValue(cacheKey, out T entity))
            {
                entity = await _context.Set<T>().FindAsync(id).ConfigureAwait(false);
                if (entity != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(_cacheExpiration);
                    _cache.Set(cacheKey, entity, cacheOptions);
                }
            }
            
            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            string cacheKey = $"{typeof(T).Name}_all";
            
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<T> entities))
            {
                // Use AsNoTracking for read-only scenarios which improves performance
                entities = await _context.Set<T>()
                    .AsNoTracking()
                    .ToListAsync()
                    .ConfigureAwait(false);
                
                if (entities != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(_cacheExpiration);
                    _cache.Set(cacheKey, entities, cacheOptions);
                }
            }
            
            return entities;
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            // Don't cache query results as they're likely to be different each time
            return await _context.Set<T>()
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity).ConfigureAwait(false);
            InvalidateCache();
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities).ConfigureAwait(false);
            InvalidateCache();
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
            InvalidateCache();
        }

        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
            InvalidateCache();
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
            InvalidateCache();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        
        // Helper method to invalidate cache for this entity type
        private void InvalidateCache()
        {
            // Create a pattern to remove related cache entries for this type
            var pattern = $"{typeof(T).Name}_";
            
            // Remove all matching entries
            // We can't enumerate through IMemoryCache directly, so we'll use a more targeted approach
            _cache.Remove($"{pattern}all");
            
            // For applications that need more granular cache invalidation, consider implementing
            // a cache key tracking system to keep track of all cache keys by entity type
        }
    }
}