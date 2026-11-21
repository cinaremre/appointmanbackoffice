using AppointMan.Application.Features.Admin.Operations.Queries.GetAdminJobs;

namespace AppointMan.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations
{
    /// <summary>
    /// Basarisiz operasyonlar raporu: Failed aktarim isleri (tam liste) ve
    /// Failed satir ornekleri (ilk 50).
    /// </summary>
    public class AdminFailedOperationsDto
    {
        /// <summary>Failed durumundaki tum aktarim isleri.</summary>
        public List<AdminJobDto> FailedJobs { get; set; } = new();

        /// <summary>Basarisiz satir ornekleri; ilk 50 ile sinirlidir.</summary>
        public List<AdminFailedRowDto> FailedRows { get; set; } = new();
    }

    /// <summary>Basarisiz bir aktarim satirinin ornek kaydi.</summary>
    public class AdminFailedRowDto
    {
        public Guid ImportJobId { get; set; }

        /// <summary>CSV icindeki 1 tabanli satir numarasi.</summary>
        public int RowNumber { get; set; }

        /// <summary>Normalize edilmis telefon; mukerrer anahtar.</summary>
        public string? SourceRecordKey { get; set; }

        /// <summary>Hatanin kisa aciklamasi.</summary>
        public string? ErrorDetails { get; set; }
    }
}
