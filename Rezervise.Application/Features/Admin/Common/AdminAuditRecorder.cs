using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Application.Features.Admin.Common
{
    /// <summary>
    /// Yonetici islemlerinin denetim kaydini tek bicimde yazar
    /// (dokuman Modul 21). Rol adi sabit "Admin"dir: backoffice
    /// policy'leri zaten rolleri cozmustur, denetim kaydi yalnizca
    /// kimin ne yaptigini tasir. Yazim hatasi is akisini bozmaz.
    /// </summary>
    public static class AdminAuditRecorder
    {
        /// <summary>Denetim kaydindaki sabit rol adi.</summary>
        public const string ActorRole = "Admin";

        /// <summary>
        /// Kritik yonetici islemini denetim kaydina yazar.
        /// Kimlik dogrulanmamissa veya yazim hata verirse kayit atlanir.
        /// </summary>
        public static async Task RecordAsync(
            IAdminAuditSink sink,
            IAdminAuditContext context,
            Guid? actorUserId,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            if (actorUserId is null || string.IsNullOrWhiteSpace(entityId))
                return;

            try
            {
                await sink.RecordAsync(
                    actorUserId.Value,
                    ActorRole,
                    businessId,
                    entityType,
                    entityId,
                    action,
                    oldValues,
                    newValues,
                    reason,
                    context.CorrelationId,
                    context.IpAddress,
                    cancellationToken);
            }
            catch
            {
                // Denetim kaydi yazimi is akisini bozmamali (dokuman Modul 21).
            }
        }
    }
}
