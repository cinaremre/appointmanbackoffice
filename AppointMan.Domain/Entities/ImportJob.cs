using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Veri aktarim isi (musteri CSV vb.).
    /// Durum akisi: Uploaded -> Mapping -> Processing -> Completed/Failed.
    /// CsvContent entity uzerinde saklanir ki Execute adimi satirlari
    /// yeniden isleyebilsin (ilk surum basitlestirmesi).
    /// </summary>
    public class ImportJob : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Kaynak sistem etiketi (orn. "google-contacts", "excel").</summary>
        public string SourceSystem { get; private set; }
        public string FileName { get; private set; }

        public EnumImportStatus Status { get; private set; }

        /// <summary>Kolon eslesmesi JSON metni (orn. {"0":"FullName","1":"Phone"}).</summary>
        public string ColumnMapping { get; private set; }

        /// <summary>Ham CSV icerigi; Execute adiminda tekrar parse edilir.</summary>
        public string CsvContent { get; private set; }

        public int TotalRows { get; private set; }
        public int ImportedRows { get; private set; }
        public int FailedRows { get; private set; }

        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        public Guid CreatedBy { get; private set; }

        private ImportJob() { }

        public ImportJob(
            Guid businessId,
            string sourceSystem,
            string fileName,
            string csvContent,
            int totalRows,
            Guid createdBy)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (createdBy == Guid.Empty)
                throw new ArgumentException("Olusturan kullanici ID bos olamaz.", nameof(createdBy));

            if (string.IsNullOrWhiteSpace(sourceSystem))
                throw new DomainException("ImportJob.SourceSystemRequired", "Kaynak sistem bos olamaz.");

            if (sourceSystem.Length > 100)
                throw new DomainException("ImportJob.SourceSystemTooLong", "Kaynak sistem en fazla 100 karakter olabilir.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new DomainException("ImportJob.FileNameRequired", "Dosya adi bos olamaz.");

            if (fileName.Length > 260)
                throw new DomainException("ImportJob.FileNameTooLong", "Dosya adi en fazla 260 karakter olabilir.");

            if (string.IsNullOrWhiteSpace(csvContent))
                throw new DomainException("ImportJob.CsvContentRequired", "CSV icerigi bos olamaz.");

            if (totalRows < 0)
                throw new DomainException("ImportJob.TotalRowsNegative", "Satir sayisi negatif olamaz.");

            BusinessId = businessId;
            SourceSystem = sourceSystem.Trim();
            FileName = fileName.Trim();
            CsvContent = csvContent;
            TotalRows = totalRows;
            CreatedBy = createdBy;
            ColumnMapping = string.Empty;
            ImportedRows = 0;
            FailedRows = 0;
            Status = EnumImportStatus.Uploaded;
        }

        /// <summary>Isi kolon eslesmesi bekler duruma alir.</summary>
        public void MarkAwaitingMapping()
        {
            if (Status != EnumImportStatus.Uploaded)
                return;

            Status = EnumImportStatus.Mapping;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Kolon eslesmesini kaydeder.</summary>
        public void SetColumnMapping(string columnMapping)
        {
            if (string.IsNullOrWhiteSpace(columnMapping))
                throw new DomainException("ImportJob.MappingRequired", "Kolon eslesmesi bos olamaz.");

            if (columnMapping.Length > 4000)
                throw new DomainException("ImportJob.MappingTooLong", "Kolon eslesmesi en fazla 4000 karakter olabilir.");

            ColumnMapping = columnMapping;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Islemeye baslar: Processing durumuna gecer ve baslangic zamani tusar.</summary>
        public void StartProcessing()
        {
            if (Status == EnumImportStatus.Completed)
                throw new DomainException("ImportJob.AlreadyCompleted", "Bu aktarim isi zaten tamamlanmis.");

            Status = EnumImportStatus.Processing;
            StartedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Isi tamamlanmis olarak isaretler ve sayaclari yazar.</summary>
        public void Complete(int importedRows, int failedRows)
        {
            if (importedRows < 0 || failedRows < 0)
                throw new DomainException("ImportJob.CountsNegative", "Satir sayaclari negatif olamaz.");

            Status = EnumImportStatus.Completed;
            ImportedRows = importedRows;
            FailedRows = failedRows;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Isi hata ile sonlandirir.</summary>
        public void MarkFailed()
        {
            Status = EnumImportStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
