using Rezervise.Domain.Interfaces;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Rezervise.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction is not null || _context.Database.CurrentTransaction is not null)
                throw new InvalidOperationException("Zaten aktif bir transaction var.");

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction is null)
                throw new InvalidOperationException("Aktif transaction yok.");

            try
            {
                // Kayit, exception cevirisi ve domain event dispatch'i AppDbContext'te toplanir.
                await _context.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                try
                {
                    await _transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Rollback hatasi asil hatayi mask elemeli.
                }

                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction is null)
                return;

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public IRepository<T> Repository<T>() where T : class, IAggregateRoot
        {
            ThrowIfDisposed();

            return new GenericRepository<T>(_context);
        }

        /// <summary>
        /// Verilen isi belirtilen izolasyon seviyesinde tek transaction icinde
        /// calistirir, basarili olursa commit eder, hata olursa geri alir.
        /// Rezervasyon gibi aralik cakismasi riski tasiyan akislarda
        /// <see cref="IsolationLevel.Serializable"/> kullanilir.
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>Donus degeri olmayan isler icin ayni davranis.</summary>
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            return ExecuteInTransactionAsync(async token =>
            {
                await operation(token);
                return true;
            }, isolationLevel, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Transaction bu birime ait; DbContext scoped oldugu icin
                // yasam dongusunu DI konteyneri yonetir, burada dispose edilmez.
                _transaction?.Dispose();
                _transaction = null;
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));
        }
    }
}
