namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Paket hak hareketinin turu. Bir seansın ömrü:
    /// Reservation (randevu oluşturulduğunda ayrılır) ->
    /// Consumption (randevu tamamlanınca tüketilir) veya
    /// Release (randevu iptal edilince havuza geri döner).
    /// </summary>
    public enum EnumPackageUsageType
    {
        /// <summary>Randevu için hak ayrıldı (aktif rezervasyon).</summary>
        Reservation = 0,

        /// <summary>Randevu gerçekleşti, hak tüketildi.</summary>
        Consumption = 1,

        /// <summary>Ayrılan hak randevu iptalinin ardından serbest bırakıldı.</summary>
        Release = 2,

        /// <summary>İade işlemi (ilk sürümde kullanılmaz, doküman Modül 5).</summary>
        Refund = 3
    }
}
