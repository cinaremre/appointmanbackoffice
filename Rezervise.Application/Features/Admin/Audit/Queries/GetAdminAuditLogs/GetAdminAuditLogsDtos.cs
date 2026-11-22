namespace Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs
{
    /// <summary>Tek bir yonetici denetim kaydinin okunmus hali.</summary>
    public class AdminAuditLogDto
    {
        public Guid Id { get; set; }
        public Guid ActorUserId { get; set; }

        /// <summary>Islem anindaki yonetici rol adi.</summary>
        public string ActorRole { get; set; } = string.Empty;

        /// <summary>Ilgili isletme; platform genelinde islemlerde bos.</summary>
        public Guid? BusinessId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        /// <summary>Onceki degerlerin kisa betimlemesi (JSON metin).</summary>
        public string? OldValues { get; set; }

        /// <summary>Yeni degerlerin kisa betimlemesi (JSON metin).</summary>
        public string? NewValues { get; set; }

        /// <summary>Yoneticinin islem gerekcesi.</summary>
        public string? Reason { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }

        /// <summary>Kaydin olusturulma ani (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Sayfali denetim kaydi listesi sonucu.</summary>
    public class AdminAuditLogListDto
    {
        public IReadOnlyList<AdminAuditLogDto> Items { get; set; } = Array.Empty<AdminAuditLogDto>();

        /// <summary>Kirpanma sonrasi uygulanan sayfa numarasi.</summary>
        public int Page { get; set; }

        /// <summary>Kirpanma sonrasi uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam kayit sayisi.</summary>
        public long TotalCount { get; set; }
    }
}
