using AppointMan.Domain.Events;

namespace AppointMan.Domain.Interfaces
{
    /// <summary>
    /// Kalici kayit sonrasi domain event'lerini dis dunyaya tasir.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
