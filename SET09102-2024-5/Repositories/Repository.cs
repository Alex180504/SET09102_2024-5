using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace SET09102_2024_5.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SensorMonitoringContext _context;
        private static readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public Repository(SensorMonitoringContext context)
        {
            _context = context;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            string cacheKey = $"{typeof(T).Name}_id_{id}";
            if (_cache.TryGetValue(cacheKey, out var cachedItem))
            {
                return (T)cachedItem;
            }

            var entity = await _context.Set<T>().FindAsync(id).ConfigureAwait(false);
            if (entity != null)
            {
                // Add to cache with expiration
                _cache.TryAdd(cacheKey, entity);
                // Schedule removal from cache
                _ = Task.Delay(_cacheExpiration).ContinueWith(_ => {
                    object? removed;
                    _cache.TryRemove(cacheKey, out removed);
                });
            }
            
            return entity;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            string cacheKey = $"{typeof(T).Name}_all";
            if (_cache.TryGetValue(cacheKey, out var cachedItems))
            {
                return (IEnumerable<T>)cachedItems;
            }

            // Use AsNoTracking for read-only scenarios which improves performance
            var entities = await _context.Set<T>()
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
                
            // Add to cache with expiration
            _cache.TryAdd(cacheKey, entities);
            // Schedule removal from cache
            _ = Task.Delay(_cacheExpiration).ContinueWith(_ => {
                object? removed;
                _cache.TryRemove(cacheKey, out removed);
            });
            
            return entities;
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            // Use AsNoTracking for read-only queries to improve performance
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
            // Remove all cached items for this entity type
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith($"{typeof(T).Name}_")).ToList();
            foreach (var key in keysToRemove)
            {
                object? removed;
                _cache.TryRemove(key, out removed);
            }
        }
    }
}