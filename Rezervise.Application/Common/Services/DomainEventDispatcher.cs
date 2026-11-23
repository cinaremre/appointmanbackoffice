using Rezervise.Domain.Events;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Common.Services
{
    /// <summary>
    /// Kalıcı kayıt sonrası domain event'lerini MediatR üzerinden yayınlar.
    /// </summary>
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Dispatcher'i MediatR yayıncıyla oluşturur.
        /// </summary>
        /// <param name="publisher">Domain event'lerini tasayacak MediatR yayıncısı.</param>
        public DomainEventDispatcher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Her domain event'i aynı iptal tokeniyle sırayla yayınlar.
        /// </summary>
        /// <param name="domainEvents">Yayınlanacak event'ler.</param>
        /// <param name="cancellationToken">İptal tokeni.</param>
        public async Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                // MediatR 13 Publish yalnızca INotification kabul eder; IDomainEvent
                // bu arayüzü uygulamaz (Domain katmanı MediatR'a bağımlı değildir).
                // Event'ler adaptor üzerinden yayınlanır, aksi halde
                // "notification does not implement INotification" hatası alınır.
                await _publisher.Publish(new DomainEventNotification(domainEvent), cancellationToken);
            }
        }
    }
}
