namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Toplu işlemin yaşam döngüsü durumu (doküman Modül 10).
    /// Geçişler yalnızca ileri yönde ve mantıklı adımlarla yapılır;
    /// kural dışı geçişler DomainException fırlatır.
    /// </summary>
    public enum EnumBulkOperationStatus
    {
        /// <summary>Kayıt oluşturuldu; henüz doğrulanmadı.</summary>
        Pending = 0,

        /// <summary>Etkilenecek kayıtlar doğrulanuyor (onizleme aşaması).</summary>
        Validating = 1,

        /// <summary>Doğrulama tamam; yönetici onayı bekleniyor.</summary>
        AwaitingApproval = 2,

        /// <summary>İşlem su anda çalışıyor.</summary>
        Running = 3,

        /// <summary>İşlem kısmen tamamlandı; bazı kayıtlar başarısız.</summary>
        PartiallyCompleted = 4,

        /// <summary>Tüm kayıtlar başarıyla işlendi.</summary>
        Completed = 5,

        /// <summary>İşlem hatayla sonlandı.</summary>
        Failed = 6,

        /// <summary>İşlem başlamadan yönetici tarafından iptal edildi.</summary>
        Cancelled = 7
    }
}
