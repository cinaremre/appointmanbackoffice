using System;
using System.Collections.Generic;
using Rezervise.Domain.Events;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Idempotency-Key ile gelen tekrar isteklerin eşleşmesini tutar.
    /// Aynı anahtarla gelen ikinci istek yeni randevu üretmez, ilk sonucu döner.
    /// Ağ tekrarı veya çift tıklama senaryolarında mükerrer kayıt oluşmasını engeller.
    /// </summary>
    public class IdempotencyRecord : IAggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>Birincil anahtar.</summary>
        public int Id { get; private set; }

        /// <summary>İstemciden gelen tekil istek anahtarı.</summary>
        public string Key { get; private set; }

        /// <summary>İsteği yapan kullanıcı.</summary>
        public Guid UserId { get; private set; }

        /// <summary>İlk istekte oluşturulan randevu.</summary>
        public Guid AppointmentId { get; private set; }

        /// <summary>Kaydın oluşturulma zamanı (UTC).</summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>Domain event listesi (bu varlıkta kullanılmaz).</summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        private IdempotencyRecord() { } // EF Core

        /// <summary>
        /// Yeni bir idempotency kaydı oluşturur.
        /// </summary>
        /// <param name="key">İstemci anahtarı.</param>
        /// <param name="userId">İsteği yapan kullanıcı.</param>
        /// <param name="appointmentId">Oluşturulan randevu.</param>
        /// <param name="now">Kayıt zamanı (UTC).</param>
        /// <exception cref="ArgumentException">Anahtar boş ise.</exception>
        public IdempotencyRecord(string key, Guid userId, Guid appointmentId, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Idempotency anahtarı boş olamaz.", nameof(key));

            Key = key.Trim();
            UserId = userId;
            AppointmentId = appointmentId;
            CreatedAt = now;
        }

        public void ClearDomainEvents() => _domainEvents.Clear();

        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    }
}
