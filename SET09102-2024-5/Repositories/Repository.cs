using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Data.Repositories
{
    /// <summary>
    /// Generic repository implementation that provides standard data access operations for entities
    /// with integrated caching. This class implements the repository pattern to abstract data access
    /// logic and provide a consistent, high-performance API for entity operations.
    /// 
    /// Features:
    /// - Memory caching for read operations with 5-minute sliding expiration
    /// - Automatic cache invalidation on write operations
    /// - Performance optimizations using EF Core's AsNoTracking for read-only queries
    /// - Async operations with ConfigureAwait(false) for better performance in UI contexts
    /// 
    /// This base repository can be extended by specialized repositories for specific entity types
    /// when additional query capabilities are needed.
    /// </summary>
    /// <typeparam name="T">The entity type this repository works with. Must be a reference type.</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SensorMonitoringContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the Repository class with the specified database context and memory cache.
        /// </summary>
        /// <param name="context">The database context used for entity operations.</param>
        /// <param name="cache">The memory cache used to store frequently accessed entities.</param>
        public Repository(SensorMonitoringContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        /// <summary>
        /// Retrieves an entity by its identifier with caching support.
        /// Returns null if no entity is found with the specified ID.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to retrieve.</param>
        /// <returns>The entity if found; otherwise, null.</returns>
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

        /// <summary>
        /// Retrieves all entities of type T with caching support.
        /// Uses AsNoTracking for better performance in read-only scenarios.
        /// </summary>
        /// <returns>A collection of all entities of type T.</returns>
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

        /// <summary>
        /// Finds entities that match the specified predicate expression.
        /// Results are not cached as predicate-based queries are typically unique.
        /// Uses AsNoTracking for better performance in read-only scenarios.
        /// </summary>
        /// <param name="predicate">The expression used to filter entities.</param>
        /// <returns>A collection of entities that match the predicate.</returns>
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            // Don't cache query results as they're likely to be different each time
            return await _context.Set<T>()
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a new entity to the database context.
        /// Note: Changes are not persisted until SaveChangesAsync is called.
        /// Automatically invalidates related cache entries.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity).ConfigureAwait(false);
            InvalidateCache();
        }

        /// <summary>
        /// Adds a collection of entities to the database context.
        /// Note: Changes are not persisted until SaveChangesAsync is called.
        /// Automatically invalidates related cache entries.
        /// </summary>
        /// <param name="entities">The collection of entities to add.</param>
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities).ConfigureAwait(false);
            InvalidateCache();
        }

        /// <summary>
        /// Updates an existing entity in the database context.
        /// Note: Changes are not persisted until SaveChangesAsync is called.
        /// Automatically invalidates related cache entries.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
            InvalidateCache();
        }

        /// <summary>
        /// Marks an entity for deletion from the database context.
        /// Note: Changes are not persisted until SaveChangesAsync is called.
        /// Automatically invalidates related cache entries.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
            InvalidateCache();
        }

        /// <summary>
        /// Marks a collection of entities for deletion from the database context.
        /// Note: Changes are not persisted until SaveChangesAsync is called.
        /// Automatically invalidates related cache entries.
        /// </summary>
        /// <param name="entities">The collection of entities to remove.</param>
        public void RemoveRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
            InvalidateCache();
        }

        /// <summary>
        /// Persists all pending changes to the database.
        /// This method must be called after Add, Update, Remove operations to save changes.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Invalidates cache entries related to the current entity type.
        /// Called automatically after operations that modify data.
        /// </summary>
        private void InvalidateCache()
        {
            // Create a pattern to remove related cache entries for this type
            var pattern = $"{typeof(T).Name}_";

            // Remove all matching entries
            _cache.Remove($"{pattern}all");
        }
    }
}
