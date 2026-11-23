namespace Rezervise.Domain.Common
{
    /// <summary>
    /// Bekleme listesi is akışının zaman tabanlı kuralları.
    /// <see cref="WorkflowPolicy"/> gibi domain katmanında tek kaynak olarak
    /// tutulur; handler'lar ve arka plan servisleri aynı değeri kullanır.
    /// </summary>
    public static class WaitlistPolicy
    {
        /// <summary>
        /// Slot teklifinin (offer) geçerli kalma süresi. Müşteri bu süre içinde
        /// yanıt vermezse teklif otomatik olarak expire edilir.
        /// Doküman Modül 10: 10 dakika.
        /// </summary>
        public static readonly TimeSpan OfferLifetime = TimeSpan.FromMinutes(10);
    }
}
