using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs
{
    /// <summary>
    /// Veri aktarım işinin yönetici görünümü. İlk sürümde ham CSV içeriği ve
    /// kolon eşleşmesi gibi ağır alanlar bilinçli olarak taşınmaz.
    /// </summary>
    public class AdminJobDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
        public EnumImportStatus Status { get; set; }
        public int TotalRows { get; set; }
        public int ImportedRows { get; set; }
        public int FailedRows { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>Sayfalı aktarım isi listesi sonucu.</summary>
    public class AdminJobListDto
    {
        public IReadOnlyList<AdminJobDto> Items { get; set; } = Array.Empty<AdminJobDto>();

        /// <summary>Kırpanma sonrası uygulanan sayfa numarası.</summary>
        public int Page { get; set; }

        /// <summary>Kırpanma sonrası uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam is sayısı.</summary>
        public long TotalCount { get; set; }
    }
}
