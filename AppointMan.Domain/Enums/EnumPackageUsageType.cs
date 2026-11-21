namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Paket hak hareketinin turu. Bir seansin omru:
    /// Reservation (randevu olusturuldugunda ayrilir) ->
    /// Consumption (randevu tamamlaninca tuketilir) veya
    /// Release (randevu iptal edilince havuza geri doner).
    /// </summary>
    public enum EnumPackageUsageType
    {
        /// <summary>Randevu icin hak ayrildi (aktif rezervasyon).</summary>
        Reservation = 0,

        /// <summary>Randevu gerceklesti, hak tuketildi.</summary>
        Consumption = 1,

        /// <summary>Ayrilan hak randevu iptalinin ardindan serbest birakildi.</summary>
        Release = 2,

        /// <summary>Iade islemi (ilk surumde kullanilmaz, dokuman Modul 5).</summary>
        Refund = 3
    }
}
