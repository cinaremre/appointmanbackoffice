using Rezervise.Domain.Events;
using MediatR;

namespace Rezervise.Application.Common.Services
{
    /// <summary>
    /// Domain event'lerini MediatR hattina tasiyan adaptor.
    /// Domain katmani MediatR'a bagimli olmadigi icin (IDomainEvent : INotification degildir)
    /// event'ler bu sarmalayici uzerinden yayinlanir. Handler'lar
    /// <see cref="INotificationHandler{TNotification}"/> ile bu tipi dinler ve
    /// <see cref="DomainEvent"/> uzerinden gercek event'e ulasir.
    /// </summary>
    public sealed class DomainEventNotification : INotification
    {
        /// <summary>
        /// Sarmalanan domain event'i.
        /// </summary>
        public IDomainEvent DomainEvent { get; }

        /// <summary>
        /// Adaptoru olusturur.
        /// </summary>
        /// <param name="domainEvent">Yayinlanacak domain event'i.</param>
        /// <exception cref="ArgumentNullException">Event null oldugunda firlatilir.</exception>
        public DomainEventNotification(IDomainEvent domainEvent)
        {
            DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
        }
    }
}
