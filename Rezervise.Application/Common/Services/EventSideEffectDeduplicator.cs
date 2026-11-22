using System.Collections.Concurrent;

namespace Rezervise.Application.Common.Services
{
    /// <summary>
    /// Aynı domain event'in birden fazla yan etki üretmesini engeller.
    /// Event'ler benzersiz <see cref="Domain.Events.IDomainEvent.Id"/> taşır;
    /// yeniden dispatch edilen olaylar burada elelenir.
    /// </summary>
    public sealed class EventSideEffectDeduplicator
    {
        private const int MaxTrackedEvents = 10_000;
        private readonly ConcurrentDictionary<Guid, byte> _seen = new();

        /// <summary>
        /// Seed gibi toplu veri yükleme sırasında yan etkileri geçici olarak kapatır.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Event daha önce görülmediyse kaydeder ve true döndürür.
        /// </summary>
        public bool TryRegister(Guid eventId)
        {
            if (eventId == Guid.Empty)
                return false;

            if (_seen.ContainsKey(eventId))
                return false;

            // Uzun süre çalışan süreçte bellek sızıntısını önle.
            if (_seen.Count >= MaxTrackedEvents)
                _seen.Clear();

            return _seen.TryAdd(eventId, 0);
        }
    }
}
