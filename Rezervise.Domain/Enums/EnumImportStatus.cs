namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Veri aktarım işinin durumunu temsil eder.
    /// </summary>
    public enum EnumImportStatus
    {
        /// <summary>Dosya yüklendi, satır sayısı hesaplandı.</summary>
        Uploaded = 0,

        /// <summary>Kolon eşleşmesi (mapping) bekleniyor.</summary>
        Mapping = 1,

        /// <summary>Eşleşme onaylandı, işlem onaylandı.</summary>
        Approved = 2,

        /// <summary>İşlem sürüyor.</summary>
        Processing = 3,

        /// <summary>İşlem sorunsuz tamamlandı.</summary>
        Completed = 4,

        /// <summary>İşlem hata ile sonlandı.</summary>
        Failed = 5
    }
}
