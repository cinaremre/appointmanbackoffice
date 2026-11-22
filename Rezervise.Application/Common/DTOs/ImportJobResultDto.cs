using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Veri aktarim isinin ozeti ve satir bazli sonuclari.
    /// </summary>
    public class ImportJobResultDto
    {
        public Guid ImportJobId { get; set; }
        public Guid BusinessId { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public EnumImportStatus Status { get; set; }
        public int TotalRows { get; set; }
        public int ImportedRows { get; set; }
        public int FailedRows { get; set; }

        /// <summary>Mukerrer nedeniyle atlanan satir sayisi (job uzerinde sayac tutulmaz).</summary>
        public int SkippedRows { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<ImportRowResultDto> Rows { get; set; } = new();
    }

    /// <summary>Tek bir aktarim satirinin sonucu.</summary>
    public class ImportRowResultDto
    {
        public int RowNumber { get; set; }
        public string? SourceRecordKey { get; set; }
        public EnumImportRowStatus Status { get; set; }
        public string? ErrorDetails { get; set; }
    }
}
