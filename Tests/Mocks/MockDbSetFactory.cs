using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Mocks
{
    /// <summary>
    /// Utility class for creating mock DbSet implementations for unit testing
    /// </summary>
    public static class MockDbSetFactory
    {
        /// <summary>
        /// Creates a mock DbSet from a list of entities with basic query capabilities
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="data">The collection of entities to include in the mock</param>
        /// <param name="idPropertyName">The name of the property that serves as the entity's ID (default: "Id")</param>
        /// <returns>A mocked DbSet</returns>
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data, string idPropertyName = "Id") where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            // Setup IQueryable interface
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            // Setup FindAsync
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).Returns<object[]>(ids =>
            {
                var id = ids[0];
                var entityType = typeof(T);
                var prop = entityType.GetProperty(idPropertyName);

                if (prop == null)
                {
                    throw new ArgumentException($"Entity type {entityType.Name} does not have a property named {idPropertyName}");
                }

                var propType = prop.PropertyType;
                object convertedId = Convert.ChangeType(id, propType);

                return new ValueTask<T>(data.FirstOrDefault(e =>
                    prop.GetValue(e)?.Equals(convertedId) ?? false
                ));
            });

            return mockSet;
        }

        /// <summary>
        /// Sets up EF Core operations on a DbSet mock (Include, AsNoTracking, FirstOrDefaultAsync, ToListAsync)
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="mockSet">The mock DbSet to enhance</param>
        /// <param name="data">The source data collection</param>
        /// <returns>The enhanced mock DbSet</returns>
        public static Mock<DbSet<T>> SetupAdvancedOperations<T>(Mock<DbSet<T>> mockSet, List<T> data) where T : class
        {
            // Setup Include/AsNoTracking to return same mock for chaining
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
            mockSet.Setup(m => m.AsNoTracking()).Returns(mockSet.Object);

            // Setup ToListAsync
            mockSet.Setup(m => m.ToListAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(data);

            // Setup FirstOrDefaultAsync with predicate
            mockSet.Setup(m => m.FirstOrDefaultAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken token) =>
                      data.AsQueryable().FirstOrDefault(predicate));

            return mockSet;
        }

        /// <summary>
        /// Creates a mock DbSet for tests dependent on EF Core
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="data">The source data collection</param>
        /// <param name="idPropertyName">The name of the primary key property</param>
        /// <returns>A DbSet object with mocked functionality</returns>
        public static DbSet<T> CreateMockDbSetWithAdvancedOperations<T>(List<T> data, string idPropertyName = "Id") where T : class
        {
            var mockSet = CreateMockDbSet(data, idPropertyName);
            SetupAdvancedOperations(mockSet, data);
            return mockSet.Object;
        }
    }
}
