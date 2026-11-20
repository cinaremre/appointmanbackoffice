using System.Linq.Expressions;
using AppointMan.Domain.Interfaces;

namespace AppointMan.Tests.Fakes;

public sealed class FakeRepository<T> : IRepository<T> where T : class, IAggregateRoot
{
    private readonly List<T> _items;

    public FakeRepository(IEnumerable<T>? items = null)
    {
        _items = items?.ToList() ?? new List<T>();
    }

    public IReadOnlyList<T> Items => _items.AsReadOnly();

    private static Guid GetId(T entity)
        => typeof(T).GetProperty("Id")?.GetValue(entity) is Guid id ? id : Guid.Empty;

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_items.FirstOrDefault(x => GetId(x) == id));
    }

    public Task<T?> GetByIdWithIncludesAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes)
        => GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<T>>(_items.ToList());
    }

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<T>>(_items.Where(predicate.Compile()).ToList());
    }

    public Task<IReadOnlyList<T>> FindWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        string[] includes,
        CancellationToken cancellationToken = default)
        => FindAsync(predicate, cancellationToken);

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_items.Any(predicate.Compile()));
    }

    public Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(predicate is null ? _items.Count : _items.Count(predicate.Compile()));
    }
}
