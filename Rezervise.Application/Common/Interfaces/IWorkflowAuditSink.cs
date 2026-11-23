using System;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Domain event yan etkilerinin izini yapılandırılmış log/Diagnostics
    /// kanalına yazar. Uygulama katmanı belirli bir log kütüphanesine
    /// bağımlı olmadığı için soyutlama üzerinden erişilir.
    /// </summary>
    public interface IWorkflowAuditSink
    {
        /// <summary>
        /// Tek bir iş akışı olayını kaydeder.
        /// </summary>
        /// <param name="eventType">Olayın CLR tipi.</param>
        /// <param name="eventId">Olay kimliği (dedüpe anahtarı).</param>
        /// <param name="aggregateId">İlgili toplulaştırıcı kimliği.</param>
        /// <param name="details">Özet bilgi.</param>
        void Record(string eventType, Guid eventId, Guid? aggregateId, string? details);
    }
}
