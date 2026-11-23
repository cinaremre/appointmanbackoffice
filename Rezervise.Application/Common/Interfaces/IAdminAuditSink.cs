namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Platform yöneticilerinin kritik işlemlerini kalıcı denetim kaydına yazan
    /// soyutlama (doküman Modül 21). Kayıtlar append-only'dir; normal kullanıcılar
    /// değiştiremez. İçerisinde şifre, token veya ödeme kartı bilgisi TUTULMAZ.
    /// </summary>
    public interface IAdminAuditSink
    {
        /// <summary>
        /// Tek bir yönetici işlemini denetim kaydına yazar.
        /// Yazım hatası is akışını bozmamalı; kayıt atlanabilir.
        /// </summary>
        /// <param name="actorUserId">İşlemi yapan yöneticinin User ID'si.</param>
        /// <param name="actorRole">İşlem anındaki yönetici rolü adı.</param>
        /// <param name="businessId">İlgili işletme; platform genelinde null.</param>
        /// <param name="entityType">İşlem yapılan varlık tipi ("Business" gibi).</param>
        /// <param name="entityId">İşlem yapılan varlığın kimliği.</param>
        /// <param name="action">İşlem adı ("Suspend" gibi).</param>
        /// <param name="oldValues">Önceki değerlerin kısa betimlemesi (JSON metin).</param>
        /// <param name="newValues">Yeni değerlerin kısa betimlemesi (JSON metin).</param>
        /// <param name="reason">Yöneticinin gerekçesi; kritik işlemlerde zorunlu.</param>
        /// <param name="correlationId">İstek korelasyon kimliği.</param>
        /// <param name="ipAddress">İsteği yapan istemci adresi.</param>
        /// <returns>Bekleyen kayıt yazımını tamamlayan görev.</returns>
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
