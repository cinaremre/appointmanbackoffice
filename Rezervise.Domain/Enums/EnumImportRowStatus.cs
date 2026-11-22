namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Veri aktarim isindeki tek bir satirin islem sonucu.
    /// </summary>
    public enum EnumImportRowStatus
    {
        /// <summary>Henuz islenmedi.</summary>
        Pending = 0,

        /// <summary>Satir basariyla aktarildi.</summary>
        Imported = 1,

        /// <summary>Ayni SourceRecordKey zaten aktarilmis; atlandi.</summary>
        SkippedDuplicate = 2,

        /// <summary>Satir islenirken hata olustu.</summary>
        Failed = 3
    }
}
