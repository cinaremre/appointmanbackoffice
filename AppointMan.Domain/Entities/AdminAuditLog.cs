using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Platform yoneticilerinin kritik islemlerinin denetim kaydi
    /// (backoffice dokumani Modul 21). Append-only: guncelleme ve silme
    /// metotlari bilincli olarak yoktur. Sifre, token veya odeme karti
    /// bilgisi bu kayitta asla tutulmaz.
    /// </summary>
    public class AdminAuditLog : BaseEntity
    {
        public Guid ActorUserId { get; private set; }

        /// <summary>Islem anindaki yonetici rolu adi (SuperAdmin, PlatformAdmin vb.).</summary>
        public string ActorRole { get; private set; }

        /// <summary>Ilgili isletme; platform genelinde islemlerde bos birakilir.</summary>
        public Guid? BusinessId { get; private set; }

        public string EntityType { get; private set; }
        public string EntityId { get; private set; }
        public string Action { get; private set; }

        /// <summary>Onceki degerlerin kisa betimlemesi (JSON metin).</summary>
        public string? OldValues { get; private set; }

        /// <summary>Yeni degerlerin kisa betimlemesi (JSON metin).</summary>
        public string? NewValues { get; private set; }

        /// <summary>Yoneticinin islem gerekcesi; kritik islemlerde zorunludur.</summary>
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
                throw new ArgumentException("Yonetici ID bos olamaz.", nameof(actorUserId));

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
