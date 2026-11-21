namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Toplu islemin yasam dongusu durumu (dokuman Modul 10).
    /// Gecisler yalnizca ileri yonde ve mantikli adimlarla yapilir;
    /// kural disi gecisler DomainException firlatir.
    /// </summary>
    public enum EnumBulkOperationStatus
    {
        /// <summary>Kayit olusturuldu; henuz dogrulanmadi.</summary>
        Pending = 0,

        /// <summary>Etkilenecek kayitlar dogrulanuyor (onizleme asamasi).</summary>
        Validating = 1,

        /// <summary>Dogrulama tamam; yonetici onayi bekleniyor.</summary>
        AwaitingApproval = 2,

        /// <summary>Islem su anda calisiyor.</summary>
        Running = 3,

        /// <summary>Islem kismen tamamlandi; bazi kayitlar basarisiz.</summary>
        PartiallyCompleted = 4,

        /// <summary>Tum kayitlar basariyla islendi.</summary>
        Completed = 5,

        /// <summary>Islem hatayla sonlandi.</summary>
        Failed = 6,

        /// <summary>Islem baslamadan yonetici tarafindan iptal edildi.</summary>
        Cancelled = 7
    }
}
