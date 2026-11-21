namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Backoffice'te toplu uygulanabilecek hizmet islemi turleri
    /// (dokuman Modul 10). ChangeServiceCategory ilk surumde entity'deki
    /// SetServiceCategory metodu uzerinden desteklenir.
    /// </summary>
    public enum EnumBulkOperationType
    {
        /// <summary>Hizmetleri pasife al.</summary>
        DeactivateServices = 0,

        /// <summary>Hizmetleri aktife getir.</summary>
        ActivateServices = 1,

        /// <summary>Hizmetlerin kategorisini degistir.</summary>
        ChangeServiceCategory = 2
    }
}
