using System.Collections.Concurrent;
using System.Data;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Tests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public int SaveCount { get; private set; }
    public IsolationLevel? LastIsolationLevel { get; private set; }
    public int TransactionCount { get; private set; }

    public FakeRepository<T> RepositoryFor<T>() where T : class, IAggregateRoot
        => (FakeRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new FakeRepository<T>());

    public IRepository<T> Repository<T>() where T : class, IAggregateRoot => RepositoryFor<T>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveCount++;
        return Task.FromResult(0);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        TransactionCount++;
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        TransactionCount++;
        LastIsolationLevel = isolationLevel;
        var result = await operation(cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async token =>
        {
            await operation(token);
            return true;
        }, isolationLevel, cancellationToken);
    }

    public void Dispose()
    {
    }
}
