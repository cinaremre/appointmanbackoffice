namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Backoffice'te toplu uygulanabilecek hizmet işlemi türleri
    /// (doküman Modül 10). ChangeServiceCategory ilk sürümde entity'deki
    /// SetServiceCategory metodu üzerinden desteklenir.
    /// </summary>
    public enum EnumBulkOperationType
    {
        /// <summary>Hizmetleri pasife al.</summary>
        DeactivateServices = 0,

        /// <summary>Hizmetleri aktife getir.</summary>
        ActivateServices = 1,

        /// <summary>Hizmetlerin kategorisini değiştir.</summary>
        ChangeServiceCategory = 2
    }
}
