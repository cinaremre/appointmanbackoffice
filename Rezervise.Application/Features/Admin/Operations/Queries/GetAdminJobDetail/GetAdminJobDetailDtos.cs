using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail
{
    /// <summary>Aktarım isi detayı: is özeti ve satır sonuçları.</summary>
    public class AdminJobDetailDto
    {
        /// <summary>İs özeti; alan seti AdminJobDto ile aynıdır.</summary>
        public AdminJobDto Job { get; set; } = new();

        /// <summary>Satır sonuçlarının ilk 200'u; satır numarasına göre sıralı.</summary>
        public List<AdminJobRowDto> Rows { get; set; } = new();
    }

    /// <summary>Tek bir aktarım satırının sonucu.</summary>
    public class AdminJobRowDto
    {
        public Guid Id { get; set; }

        /// <summary>CSV içindeki 1 tabanlı satır numarası.</summary>
        public int RowNumber { get; set; }

        /// <summary>Normalize edilmiş telefon (deterministik mükerrer anahtar).</summary>
        public string? SourceRecordKey { get; set; }
        public EnumImportRowStatus Status { get; set; }

        /// <summary>Failed durumunda hatanın kısa açıklaması.</summary>
        public string? ErrorDetails { get; set; }
    }
}
