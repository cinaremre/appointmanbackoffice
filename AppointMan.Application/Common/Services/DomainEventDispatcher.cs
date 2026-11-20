using AppointMan.Domain.Events;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Common.Services
{
    /// <summary>
    /// Kalici kayit sonrasi domain event'lerini MediatR uzerinden yayinlar.
    /// </summary>
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Dispatcher'i MediatR yayinciyla olusturur.
        /// </summary>
        /// <param name="publisher">Domain event'lerini tasayacak MediatR yayincisi.</param>
        public DomainEventDispatcher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Her domain event'i ayni iptal tokeniyle sirayla yayinlar.
        /// </summary>
        /// <param name="domainEvents">Yayinlanacak event'ler.</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        public async Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                // MediatR 13 Publish yalnizca INotification kabul eder; IDomainEvent
                // bu arayuzu uygulamaz (Domain katmani MediatR'a bagimli degildir).
                // Event'ler adaptor uzerinden yayinlanir, aksi halde
                // "notification does not implement INotification" hatasi alinir.
                await _publisher.Publish(new DomainEventNotification(domainEvent), cancellationToken);
            }
        }
    }
}
