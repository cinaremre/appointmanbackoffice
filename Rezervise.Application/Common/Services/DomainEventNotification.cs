using Rezervise.Domain.Events;
using MediatR;

namespace Rezervise.Application.Common.Services
{
    /// <summary>
    /// Domain event'lerini MediatR hattına taşıyan adaptor.
    /// Domain katmanı MediatR'a bağımlı olmadığı için (IDomainEvent : INotification değildir)
    /// event'ler bu sarmalayıcı üzerinden yayınlanır. Handler'lar
    /// <see cref="INotificationHandler{TNotification}"/> ile bu tipi dinler ve
    /// <see cref="DomainEvent"/> üzerinden gerçek event'e ulaşır.
    /// </summary>
    public sealed class DomainEventNotification : INotification
    {
        /// <summary>
        /// Sarmalanan domain event'i.
        /// </summary>
        public IDomainEvent DomainEvent { get; }

        /// <summary>
        /// Adaptoru oluşturur.
        /// </summary>
        /// <param name="domainEvent">Yayınlanacak domain event'i.</param>
        /// <exception cref="ArgumentNullException">Event null öldüğünda fırlatılır.</exception>
        public DomainEventNotification(IDomainEvent domainEvent)
        {
            DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
        }
    }
}
