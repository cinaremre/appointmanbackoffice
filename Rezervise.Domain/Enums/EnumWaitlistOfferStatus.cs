namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Bekleme listesi slot teklifinin durumu (doküman Modül 10).
    /// </summary>
    public enum EnumWaitlistOfferStatus
    {
        /// <summary>Teklif gönderildi, yanıt bekleniyor.</summary>
        Pending = 0,

        /// <summary>Müşteri teklifi kabul etti.</summary>
        Accepted = 1,

        /// <summary>Müşteri teklifi reddetti.</summary>
        Declined = 2,

        /// <summary>Yanıt süresi doldu.</summary>
        Expired = 3,

        /// <summary>Aynı slot için başka bir teklif kabul edildiği için geçersizleşti.</summary>
        Superseded = 4
    }
}
