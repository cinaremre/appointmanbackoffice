namespace AppointMan.Application.Common.Interfaces
{
    /// <summary>
    /// Platform yoneticilerinin kritik islemlerini kalici denetim kaydina yazan
    /// soyutlama (dokuman Modul 21). Kayitlar append-only'dir; normal kullanicilar
    /// degistiremez. Icerisinde sifre, token veya odeme karti bilgisi TUTULMAZ.
    /// </summary>
    public interface IAdminAuditSink
    {
        /// <summary>
        /// Tek bir yonetici islemini denetim kaydina yazar.
        /// Yazim hatasi is akisini bozmamali; kayit atlanabilir.
        /// </summary>
        /// <param name="actorUserId">Islemi yapan yoneticinin User ID'si.</param>
        /// <param name="actorRole">Islem anindaki yonetici rolu adi.</param>
        /// <param name="businessId">Ilgili isletme; platform genelinde null.</param>
        /// <param name="entityType">Islem yapilan varlik tipi ("Business" gibi).</param>
        /// <param name="entityId">Islem yapilan varligin kimligi.</param>
        /// <param name="action">Islem adi ("Suspend" gibi).</param>
        /// <param name="oldValues">Onceki degerlerin kisa betimlemesi (JSON metin).</param>
        /// <param name="newValues">Yeni degerlerin kisa betimlemesi (JSON metin).</param>
        /// <param name="reason">Yoneticinin gerekcesi; kritik islemlerde zorunlu.</param>
        /// <param name="correlationId">Istek korelasyon kimligi.</param>
        /// <param name="ipAddress">Istegi yapan istemci adresi.</param>
        /// <returns>Bekleyen kayit yazimini tamamlayan görev.</returns>
        Task RecordAsync(
            Guid actorUserId,
            string actorRole,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            string? correlationId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);
    }
}
