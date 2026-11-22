namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Musteri paketinin durumu. Kalan hak hareketlerden hesaplandigi icin
    /// "Exhausted" (tukendi) durumu hareket toplamlarindan cikarilir;
    /// burada yalnizca omur ve iptal bilgisi tutulur.
    /// </summary>
    public enum EnumCustomerPackageStatus
    {
        /// <summary>Paket satin alindi ve hala kullanilabilir.</summary>
        Active = 0,

        /// <summary>Geçerlilik suresi doldu.</summary>
        Expired = 1,

        /// <summary>Tum haklar tukendi (hesaplamadan turetilir, kalici kayit).</summary>
        Exhausted = 2,

        /// <summary>Paket iptal edildi / iade edildi.</summary>
        Cancelled = 3
    }
}
