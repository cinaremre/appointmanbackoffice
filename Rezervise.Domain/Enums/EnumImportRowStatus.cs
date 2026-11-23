namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Veri aktarım işindeki tek bir satırın işlem sonucu.
    /// </summary>
    public enum EnumImportRowStatus
    {
        /// <summary>Henüz işlenmedi.</summary>
        Pending = 0,

        /// <summary>Satır başarıyla aktarıldı.</summary>
        Imported = 1,

        /// <summary>Aynı SourceRecordKey zaten aktarılmış; atlandı.</summary>
        SkippedDuplicate = 2,

        /// <summary>Satır işlenirken hata oluştu.</summary>
        Failed = 3
    }
}
