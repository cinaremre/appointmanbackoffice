using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Operations.Queries.GetAdminJobs
{
    /// <summary>
    /// Veri aktarim isinin yonetici gorunumu. Ilk surumde ham CSV icerigi ve
    /// kolon eslesmesi gibi agir alanlar bilincli olarak tasinmaz.
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

    /// <summary>Sayfali aktarim isi listesi sonucu.</summary>
    public class AdminJobListDto
    {
        public IReadOnlyList<AdminJobDto> Items { get; set; } = Array.Empty<AdminJobDto>();

        /// <summary>Kirpanma sonrasi uygulanan sayfa numarasi.</summary>
        public int Page { get; set; }

        /// <summary>Kirpanma sonrasi uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam is sayisi.</summary>
        public long TotalCount { get; set; }
    }
}
