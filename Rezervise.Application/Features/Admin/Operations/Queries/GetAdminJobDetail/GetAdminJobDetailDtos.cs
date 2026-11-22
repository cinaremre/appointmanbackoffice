using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail
{
    /// <summary>Aktarim isi detayi: is ozeti ve satir sonuclari.</summary>
    public class AdminJobDetailDto
    {
        /// <summary>Is ozeti; alan seti AdminJobDto ile aynidir.</summary>
        public AdminJobDto Job { get; set; } = new();

        /// <summary>Satir sonuclarinin ilk 200'u; satir numarasina gore sirali.</summary>
        public List<AdminJobRowDto> Rows { get; set; } = new();
    }

    /// <summary>Tek bir aktarim satirinin sonucu.</summary>
    public class AdminJobRowDto
    {
        public Guid Id { get; set; }

        /// <summary>CSV icindeki 1 tabanli satir numarasi.</summary>
        public int RowNumber { get; set; }

        /// <summary>Normalize edilmis telefon (deterministik mukerrer anahtar).</summary>
        public string? SourceRecordKey { get; set; }
        public EnumImportRowStatus Status { get; set; }

        /// <summary>Failed durumunda hatanin kisa aciklamasi.</summary>
        public string? ErrorDetails { get; set; }
    }
}
