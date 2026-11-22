using System.Linq.Expressions;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Tests.Fakes;

/// <summary>
/// Amac: handler'larin exception mesajlarini sizdirmadigini dogrulamak. Bu repository
/// bilincli olarak ayirt edilebilir bir mesaj firlatir; test bu mesajin istemciye
/// ulasmadigini kontrol eder.
/// </summary>
public sealed class ThrowingRepository<T> : IRepository<T> where T : class, IAggregateRoot
{
    private const string Secret = "secret-internal-detail";

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task<T?> GetByIdWithIncludesAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes)
        => throw new InvalidOperationException(Secret);

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task<IReadOnlyList<T>> FindWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        string[] includes,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);

    public Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(Secret);
}
