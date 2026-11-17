using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AppointMan.Domain.Interfaces;

namespace AppointMan.Domain.Interfaces
{
    public interface IRepository<T> where T : class, IAggregateRoot
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<T?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the entities matching <paramref name="predicate"/> with the requested
        /// navigation properties eagerly loaded.
        /// </summary>
        /// <param name="predicate">Filter applied to the entity set.</param>
        /// <param name="includes">
        /// Navigation paths to load. Nested paths use dot notation, for example
        /// <c>"ParentCategory.Translations"</c>, which is required because a single level
        /// <c>Include</c> cannot populate grandchildren.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching entities with their navigations populated.</returns>
        Task<IReadOnlyList<T>> FindWithIncludesAsync(
            Expression<Func<T, bool>> predicate,
            string[] includes,
            CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    }
}