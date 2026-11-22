using System;
using System.Collections.Generic;
using Rezervise.Domain.Events;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Idempotency-Key ile gelen tekrar isteklerin eslesmesini tutar.
    /// Ayni anahtarla gelen ikinci istek yeni randevu uretmez, ilk sonucu doner.
    /// Ag tekrari veya cift tiklama senaryolarinda mukerrer kayit olusmasini engeller.
    /// </summary>
    public class IdempotencyRecord : IAggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>Birincil anahtar.</summary>
        public int Id { get; private set; }

        /// <summary>Istemciden gelen tekil istek anahtari.</summary>
        public string Key { get; private set; }

        /// <summary>Istegi yapan kullanici.</summary>
        public Guid UserId { get; private set; }

        /// <summary>Ilk istekte olusturulan randevu.</summary>
        public Guid AppointmentId { get; private set; }

        /// <summary>Kaydin olusturulma zamani (UTC).</summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>Domain event listesi (bu varlikta kullanilmaz).</summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        private IdempotencyRecord() { } // EF Core

        /// <summary>
        /// Yeni bir idempotency kaydi olusturur.
        /// </summary>
        /// <param name="key">Istemci anahtari.</param>
        /// <param name="userId">Istegi yapan kullanici.</param>
        /// <param name="appointmentId">Olusturulan randevu.</param>
        /// <param name="now">Kayit zamani (UTC).</param>
        /// <exception cref="ArgumentException">Anahtar bos ise.</exception>
        public IdempotencyRecord(string key, Guid userId, Guid appointmentId, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Idempotency anahtari bos olamaz.", nameof(key));

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
