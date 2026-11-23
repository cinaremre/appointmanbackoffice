using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Backoffice kaynaklı toplu işlem kaydı (doküman Modül 10).
    /// Append-only ilke: istek payload'i oluşturma sonrası değiştirilemez;
    /// yalnızca durum, sayaç ve hata raporu güncellenir.
    /// </summary>
    public class BulkOperation : BaseEntity
    {
        public Guid? BusinessId { get; private set; }
        public EnumBulkOperationType OperationType { get; private set; }
        public string RequestedPayload { get; private set; }
        public bool DryRun { get; private set; }
        public EnumBulkOperationStatus Status { get; private set; }
        public Guid RequestedBy { get; private set; }
        public int TotalRecords { get; private set; }
        public int SuccessCount { get; private set; }
        public int FailedCount { get; private set; }

        /// <summary>İşlenen kayıtların hata özeti; 4000 karakterle sınırlı.</summary>
        public string? ErrorReport { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? StartedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        private BulkOperation() { } // EF Core

        public BulkOperation(
            Guid id,
            EnumBulkOperationType operationType,
            string requestedPayload,
            bool dryRun,
            Guid requestedBy,
            int totalRecords,
            Guid? businessId = null)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(requestedPayload))
                throw new DomainException("Toplu işlem payload'i boş olamaz.");

            if (requestedBy == Guid.Empty)
                throw new DomainException("Toplu işlem istekçisi boş olamaz.");

            if (totalRecords < 0)
                throw new DomainException("Toplu işlem kayıt sayısı negatif olamaz.");

            OperationType = operationType;
            RequestedPayload = requestedPayload;
            DryRun = dryRun;
            RequestedBy = requestedBy;
            TotalRecords = totalRecords;
            BusinessId = businessId;
            SuccessCount = 0;
            FailedCount = 0;
            Status = EnumBulkOperationStatus.Pending;
            CreatedAtUtc = DateTime.UtcNow;
        }

        /// <summary>Pending -> Validating: kayıtların doğrulaması başladı.</summary>
        public void MarkValidating()
        {
            EnsureStatus(EnumBulkOperationStatus.Pending, "Doğrulama yalnızca Pending durumundan başlatılabilir.");
            Status = EnumBulkOperationStatus.Validating;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Validating -> AwaitingApproval: doğrulama tamam, onay bekleniyor.</summary>
        public void MarkAwaitingApproval()
        {
            EnsureStatus(EnumBulkOperationStatus.Validating, "Onay bekleme yalnızca Validating durumundan başlatılabilir.");
            Status = EnumBulkOperationStatus.AwaitingApproval;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Validating/AwaitingApproval -> Running: işlem yürütülmeye başladı.
        /// Pending kayıt doğrulanmadan yürütülemez; Completed kayıt tekrar çalışmaz.
        /// </summary>
        public void Start()
        {
            if (Status != EnumBulkOperationStatus.Validating
                && Status != EnumBulkOperationStatus.AwaitingApproval)
                throw new DomainException(
                    "Toplu işlem yalnızca Validating veya AwaitingApproval durumundan başlatılabilir.",
                    $"Mevcut durum: {Status}");

            Status = EnumBulkOperationStatus.Running;
            StartedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Running -> Completed/PartiallyCompleted: işlem sonlandı.
        /// Başarısız kayıt yoksa Completed, varsa PartiallyCompleted olur.
        /// </summary>
        public void Complete(int successCount, int failedCount)
        {
            EnsureStatus(EnumBulkOperationStatus.Running, "Tamamlama yalnızca Running durumundan yapılabilir.");

            if (successCount < 0 || failedCount < 0)
                throw new DomainException("Başarı/başarısız sayıları negatif olamaz.");

            SuccessCount = successCount;
            FailedCount = failedCount;
            Status = failedCount == 0
                ? EnumBulkOperationStatus.Completed
                : EnumBulkOperationStatus.PartiallyCompleted;
            CompletedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Running -> Failed: işlem işlenemeyen bir hatayla sonlandı.</summary>
        public void Fail(string report)
        {
            EnsureStatus(EnumBulkOperationStatus.Running, "Hata sonucu yalnızca Running durumundan yazılabilir.");

            Status = EnumBulkOperationStatus.Failed;
            ErrorReport = TruncateReport(report);
            CompletedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Pending/Validating/AwaitingApproval -> Cancelled: işlem başlamadan iptal edildi.
        /// Running ve sonraki durumlarda iptale izin verilmez.
        /// </summary>
        public void Cancel()
        {
            if (Status != EnumBulkOperationStatus.Pending
                && Status != EnumBulkOperationStatus.Validating
                && Status != EnumBulkOperationStatus.AwaitingApproval)
                throw new DomainException(
                    "Toplu işlem yalnızca Pending, Validating veya AwaitingApproval durumunda iptal edilebilir.",
                    $"Mevcut durum: {Status}");

            Status = EnumBulkOperationStatus.Cancelled;
            CompletedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Hata raporunu alan sınırına kırpar (kalan kısım sayacı ile takip edilir).</summary>
        private static string? TruncateReport(string? report)
        {
            if (string.IsNullOrEmpty(report))
                return report;

            return report.Length <= 4000 ? report : report[..4000];
        }

        private void EnsureStatus(EnumBulkOperationStatus expected, string message)
        {
            if (Status != expected)
                throw new DomainException(message, $"Mevcut durum: {Status}, beklenen: {expected}");
        }
    }
}
