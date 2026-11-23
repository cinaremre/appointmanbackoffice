using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations
{
    /// <summary>
    /// Başarısız operasyonlar raporu: Failed aktarım işleri (tam liste) ve
    /// Failed satır örnekleri (ilk 50).
    /// </summary>
    public class AdminFailedOperationsDto
    {
        /// <summary>Failed durumundaki tüm aktarım işleri.</summary>
        public List<AdminJobDto> FailedJobs { get; set; } = new();

        /// <summary>Başarısız satır örnekleri; ilk 50 ile sınırlıdır.</summary>
        public List<AdminFailedRowDto> FailedRows { get; set; } = new();
    }

    /// <summary>Başarısız bir aktarım satırının örnek kaydı.</summary>
    public class AdminFailedRowDto
    {
        public Guid ImportJobId { get; set; }

        /// <summary>CSV içindeki 1 tabanlı satır numarası.</summary>
        public int RowNumber { get; set; }

        /// <summary>Normalize edilmiş telefon; mükerrer anahtar.</summary>
        public string? SourceRecordKey { get; set; }

        /// <summary>Hatanın kısa açıklaması.</summary>
        public string? ErrorDetails { get; set; }
    }
}
