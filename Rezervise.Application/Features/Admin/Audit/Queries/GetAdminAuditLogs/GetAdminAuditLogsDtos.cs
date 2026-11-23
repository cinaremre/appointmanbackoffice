namespace Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs
{
    /// <summary>Tek bir yönetici denetim kaydının okunmuş hali.</summary>
    public class AdminAuditLogDto
    {
        public Guid Id { get; set; }
        public Guid ActorUserId { get; set; }

        /// <summary>İşlem anındaki yönetici rol adı.</summary>
        public string ActorRole { get; set; } = string.Empty;

        /// <summary>İlgili işletme; platform genelinde işlemlerde boş.</summary>
        public Guid? BusinessId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        /// <summary>Önceki değerlerin kısa betimlemesi (JSON metin).</summary>
        public string? OldValues { get; set; }

        /// <summary>Yeni değerlerin kısa betimlemesi (JSON metin).</summary>
        public string? NewValues { get; set; }

        /// <summary>Yöneticinin işlem gerekçesi.</summary>
        public string? Reason { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }

        /// <summary>Kaydın oluşturulma anı (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Sayfalı denetim kaydı listesi sonucu.</summary>
    public class AdminAuditLogListDto
    {
        public IReadOnlyList<AdminAuditLogDto> Items { get; set; } = Array.Empty<AdminAuditLogDto>();

        /// <summary>Kırpanma sonrası uygulanan sayfa numarası.</summary>
        public int Page { get; set; }

        /// <summary>Kırpanma sonrası uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam kayıt sayısı.</summary>
        public long TotalCount { get; set; }
    }
}
