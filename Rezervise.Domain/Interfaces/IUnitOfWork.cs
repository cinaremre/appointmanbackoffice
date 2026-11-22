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
        /// Verilen isi belirtilen izolasyon seviyesinde tek bir transaction icinde
        /// calistirir, basarili olursa commit eder, hata olursa geri alir.
        /// Rezervasyon gibi aralik cakismasi riski tasiyan akislarda
        /// <see cref="IsolationLevel.Serializable"/> kullanilir.
        /// </summary>
        /// <param name="operation">Transaction icinde calistirilacak is.</param>
        /// <param name="isolationLevel">Transaction izolasyon seviyesi.</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        /// <returns>Islemin sonucu.</returns>
        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);

        /// <summary>Donus degeri olmayan isler icin ayni davranis.</summary>
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);
    }
}