using Rezervise.Domain.Events;

namespace Rezervise.Domain.Interfaces
{
    /// <summary>
    /// Kalıcı kayıt sonrası domain event'lerini dış dünyaya taşır.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
