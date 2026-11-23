using System;
using System.Data;

namespace Rezervise.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        IRepository<T> Repository<T>() where T : class, IAggregateRoot;

        /// <summary>
        /// Verilen isi belirtilen izolasyon seviyesinde tek bir transaction içinde
        /// çalıştırır, başarılı olursa commit eder, hata olursa geri alır.
        /// Rezervasyon gibi aralık çakışması riski taşıyan akışlarda
        /// <see cref="IsolationLevel.Serializable"/> kullanılır.
        /// </summary>
        /// <param name="operation">Transaction içinde çalıştırılacak is.</param>
        /// <param name="isolationLevel">Transaction izolasyon seviyesi.</param>
        /// <param name="cancellationToken">İptal tokeni.</param>
        /// <returns>İşlemin sonucu.</returns>
        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);

        /// <summary>Dönüş değeri olmayan işler için aynı davranış.</summary>
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);
    }
}