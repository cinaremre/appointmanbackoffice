namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Veri aktarim isinin durumunu temsil eder.
    /// </summary>
    public enum EnumImportStatus
    {
        /// <summary>Dosya yuklendi, satir sayisi hesaplandi.</summary>
        Uploaded = 0,

        /// <summary>Kolon eslesmesi (mapping) bekleniyor.</summary>
        Mapping = 1,

        /// <summary>Eslesme onaylandi, islem onaylandi.</summary>
        Approved = 2,

        /// <summary>Islem suruyor.</summary>
        Processing = 3,

        /// <summary>Islem sorunsuz tamamlandi.</summary>
        Completed = 4,

        /// <summary>Islem hata ile sonlandi.</summary>
        Failed = 5
    }
}
