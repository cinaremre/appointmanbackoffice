using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Veri aktarım isi (müşteri CSV vb.).
    /// Durum akışı: Uploaded -> Mapping -> Processing -> Completed/Failed.
    /// CsvContent entity üzerinde saklanır ki Execute adımı satırları
    /// yeniden işleyebilsin (ilk sürüm basitleştirmesi).
    /// </summary>
    public class ImportJob : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Kaynak sistem etiketi (orn. "google-contacts", "excel").</summary>
        public string SourceSystem { get; private set; }
        public string FileName { get; private set; }

        public EnumImportStatus Status { get; private set; }

        /// <summary>Kolon eşleşmesi JSON metni (orn. {"0":"FullName","1":"Phone"}).</summary>
        public string ColumnMapping { get; private set; }

        /// <summary>Ham CSV içeriği; Execute adımında tekrar parse edilir.</summary>
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (createdBy == Guid.Empty)
                throw new ArgumentException("Oluşturan kullanıcı ID boş olamaz.", nameof(createdBy));

            if (string.IsNullOrWhiteSpace(sourceSystem))
                throw new DomainException("ImportJob.SourceSystemRequired", "Kaynak sistem boş olamaz.");

            if (sourceSystem.Length > 100)
                throw new DomainException("ImportJob.SourceSystemTooLong", "Kaynak sistem en fazla 100 karakter olabilir.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new DomainException("ImportJob.FileNameRequired", "Dosya adı boş olamaz.");

            if (fileName.Length > 260)
                throw new DomainException("ImportJob.FileNameTooLong", "Dosya adı en fazla 260 karakter olabilir.");

            if (string.IsNullOrWhiteSpace(csvContent))
                throw new DomainException("ImportJob.CsvContentRequired", "CSV içeriği boş olamaz.");

            if (totalRows < 0)
                throw new DomainException("ImportJob.TotalRowsNegative", "Satır sayısı negatif olamaz.");

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

        /// <summary>İsi kolon eşleşmesi bekler duruma alır.</summary>
        public void MarkAwaitingMapping()
        {
            if (Status != EnumImportStatus.Uploaded)
                return;

            Status = EnumImportStatus.Mapping;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Kolon eşleşmesini kaydeder.</summary>
        public void SetColumnMapping(string columnMapping)
        {
            if (string.IsNullOrWhiteSpace(columnMapping))
                throw new DomainException("ImportJob.MappingRequired", "Kolon eşleşmesi boş olamaz.");

            if (columnMapping.Length > 4000)
                throw new DomainException("ImportJob.MappingTooLong", "Kolon eşleşmesi en fazla 4000 karakter olabilir.");

            ColumnMapping = columnMapping;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>İşlemeye başlar: Processing durumuna geçer ve başlangıç zamanı tuşar.</summary>
        public void StartProcessing()
        {
            if (Status == EnumImportStatus.Completed)
                throw new DomainException("ImportJob.AlreadyCompleted", "Bu aktarım isi zaten tamamlanmış.");

            Status = EnumImportStatus.Processing;
            StartedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>İsi tamamlanmış olarak işaretler ve sayaçları yazar.</summary>
        public void Complete(int importedRows, int failedRows)
        {
            if (importedRows < 0 || failedRows < 0)
                throw new DomainException("ImportJob.CountsNegative", "Satır sayaçları negatif olamaz.");

            Status = EnumImportStatus.Completed;
            ImportedRows = importedRows;
            FailedRows = failedRows;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>İsi hata ile sonlandırır.</summary>
        public void MarkFailed()
        {
            Status = EnumImportStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
