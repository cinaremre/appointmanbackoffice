namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Müşteri paketinin durumu. Kalan hak hareketlerden hesaplandığı için
    /// "Exhausted" (tükendi) durumu hareket toplamlarından çıkarılır;
    /// burada yalnızca ömür ve iptal bilgisi tutulur.
    /// </summary>
    public enum EnumCustomerPackageStatus
    {
        /// <summary>Paket satın alındı ve hala kullanılabilir.</summary>
        Active = 0,

        /// <summary>Geçerlilik süresi doldu.</summary>
        Expired = 1,

        /// <summary>Tüm haklar tükendi (hesaplamadan türetilir, kalıcı kayıt).</summary>
        Exhausted = 2,

        /// <summary>Paket iptal edildi / iade edildi.</summary>
        Cancelled = 3
    }
}
