namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Paket hak hareketinin durumu. Yalnızca "Active" hareketler
    /// kalan hak hesabına girer; iptal edilen hareketler serbest bırakılır.
    /// </summary>
    public enum EnumPackageUsageStatus
    {
        /// <summary>Hareket geçerli; kalan hak hesabında yer alır.</summary>
        Active = 0,

        /// <summary>Hareket iptal edildi; hak yeniden kullanılabilir.</summary>
        Cancelled = 1
    }
}
