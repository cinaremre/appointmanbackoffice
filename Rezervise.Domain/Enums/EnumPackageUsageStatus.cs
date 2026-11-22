namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Paket hak hareketinin durumu. Yalnizca "Active" hareketler
    /// kalan hak hesabina girer; iptal edilen hareketler serbest birakilir.
    /// </summary>
    public enum EnumPackageUsageStatus
    {
        /// <summary>Hareket gecerli; kalan hak hesabinda yer alir.</summary>
        Active = 0,

        /// <summary>Hareket iptal edildi; hak yeniden kullanilabilir.</summary>
        Cancelled = 1
    }
}
