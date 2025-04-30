using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Generic repository interface that provides standard data access operations for entities.
    /// Implements the repository pattern to abstract data access logic and provide a consistent API
    /// for working with different entity types in the application.
    /// </summary>
    /// <typeparam name="T">The entity type this repository works with. Must be a reference type.</typeparam>
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task<int> SaveChangesAsync();
    }
}
