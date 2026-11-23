using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Platform yöneticilerinin kritik işlemlerinin denetim kaydı
    /// (backoffice dokümanı Modül 21). Append-only: güncelleme ve silme
    /// metotları bilinçli olarak yoktur. Şifre, token veya ödeme kartı
    /// bilgisi bu kayıtta asla tutulmaz.
    /// </summary>
    public class AdminAuditLog : BaseEntity
    {
        public Guid ActorUserId { get; private set; }

        /// <summary>İşlem anındaki yönetici rolü adı (SuperAdmin, PlatformAdmin vb.).</summary>
        public string ActorRole { get; private set; }

        /// <summary>İlgili işletme; platform genelinde işlemlerde boş bırakılır.</summary>
        public Guid? BusinessId { get; private set; }

        public string EntityType { get; private set; }
        public string EntityId { get; private set; }
        public string Action { get; private set; }

        /// <summary>Önceki değerlerin kısa betimlemesi (JSON metin).</summary>
        public string? OldValues { get; private set; }

        /// <summary>Yeni değerlerin kısa betimlemesi (JSON metin).</summary>
        public string? NewValues { get; private set; }

        /// <summary>Yöneticinin işlem gerekçesi; kritik işlemlerde zorunludur.</summary>
        public string? Reason { get; private set; }

        public string? CorrelationId { get; private set; }
        public string? IpAddress { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        private AdminAuditLog() { } // EF Core

        public AdminAuditLog(
            Guid actorUserId,
            string actorRole,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            DateTime createdAtUtc,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            string? correlationId = null,
            string? ipAddress = null)
        {
            if (actorUserId == Guid.Empty)
                throw new ArgumentException("Yönetici ID boş olamaz.", nameof(actorUserId));

            if (string.IsNullOrWhiteSpace(actorRole))
                throw new DomainException("AdminAuditLog.ActorRoleRequired");

            if (string.IsNullOrWhiteSpace(entityType))
                throw new DomainException("AdminAuditLog.EntityTypeRequired");

            if (string.IsNullOrWhiteSpace(entityId))
                throw new DomainException("AdminAuditLog.EntityIdRequired");

            if (string.IsNullOrWhiteSpace(action))
                throw new DomainException("AdminAuditLog.ActionRequired");

            if (oldValues != null && oldValues.Length > 2000)
                throw new DomainException("AdminAuditLog.ValuesTooLong");

            if (newValues != null && newValues.Length > 2000)
                throw new DomainException("AdminAuditLog.ValuesTooLong");

            if (reason != null && reason.Length > 1000)
                throw new DomainException("AdminAuditLog.ReasonTooLong");

            ActorUserId = actorUserId;
            ActorRole = actorRole.Trim();
            BusinessId = businessId;
            EntityType = entityType.Trim();
            EntityId = entityId.Trim();
            Action = action.Trim();
            OldValues = oldValues;
            NewValues = newValues;
            Reason = reason;
            CorrelationId = correlationId;
            IpAddress = ipAddress;
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        }
    }
}
