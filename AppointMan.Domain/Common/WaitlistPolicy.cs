namespace AppointMan.Domain.Common
{
    /// <summary>
    /// Bekleme listesi is akisinin zaman tabanli kurallari.
    /// <see cref="WorkflowPolicy"/> gibi domain katmaninda tek kaynak olarak
    /// tutulur; handler'lar ve arka plan servisleri ayni degeri kullanir.
    /// </summary>
    public static class WaitlistPolicy
    {
        /// <summary>
        /// Slot teklifinin (offer) gecerli kalma suresi. Musteri bu sure icinde
        /// yanit vermezse teklif otomatik olarak expire edilir.
        /// Dokuman Modul 10: 10 dakika.
        /// </summary>
        public static readonly TimeSpan OfferLifetime = TimeSpan.FromMinutes(10);
    }
}
