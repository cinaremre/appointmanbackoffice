using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Application.Features.Admin.Common
{
    /// <summary>
    /// Yönetici işlemlerinin denetim kaydını tek biçimde yazar
    /// (doküman Modül 21). Rol adı sabit "Admin"dir: backoffice
    /// policy'leri zaten rolleri çözmüştür, denetim kaydı yalnızca
    /// kimin ne yaptığını taşır. Yazım hatası is akışını bozmaz.
    /// </summary>
    public static class AdminAuditRecorder
    {
        /// <summary>Denetim kaydındaki sabit rol adı.</summary>
        public const string ActorRole = "Admin";

        /// <summary>
        /// Kritik yönetici işlemini denetim kaydına yazar.
        /// Kimlik doğrulanmamışsa veya yazım hata verirse kayıt atlanır.
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
                // Denetim kaydı yazımı is akışını bozmamalı (doküman Modül 21).
            }
        }
    }
}
