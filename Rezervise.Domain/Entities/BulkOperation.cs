using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Backoffice kaynakli toplu islem kaydi (dokuman Modul 10).
    /// Append-only ilke: istek payload'i olusturma sonrasi degistirilemez;
    /// yalnizca durum, sayac ve hata raporu guncellenir.
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

        /// <summary>Islenen kayitlarin hata ozeti; 4000 karakterle sinirli.</summary>
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
                throw new DomainException("Toplu islem payload'i bos olamaz.");

            if (requestedBy == Guid.Empty)
                throw new DomainException("Toplu islem istekcisi bos olamaz.");

            if (totalRecords < 0)
                throw new DomainException("Toplu islem kayit sayisi negatif olamaz.");

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

        /// <summary>Pending -> Validating: kayitlarin dogrulamasi basladi.</summary>
        public void MarkValidating()
        {
            EnsureStatus(EnumBulkOperationStatus.Pending, "Dogrulama yalnizca Pending durumundan baslatilabilir.");
            Status = EnumBulkOperationStatus.Validating;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Validating -> AwaitingApproval: dogrulama tamam, onay bekleniyor.</summary>
        public void MarkAwaitingApproval()
        {
            EnsureStatus(EnumBulkOperationStatus.Validating, "Onay bekleme yalnizca Validating durumundan baslatilabilir.");
            Status = EnumBulkOperationStatus.AwaitingApproval;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Validating/AwaitingApproval -> Running: islem yurutulmeye basladi.
        /// Pending kayit dogrulanmadan yurutulemez; Completed kayit tekrar calismaz.
        /// </summary>
        public void Start()
        {
            if (Status != EnumBulkOperationStatus.Validating
                && Status != EnumBulkOperationStatus.AwaitingApproval)
                throw new DomainException(
                    "Toplu islem yalnizca Validating veya AwaitingApproval durumundan baslatilabilir.",
                    $"Mevcut durum: {Status}");

            Status = EnumBulkOperationStatus.Running;
            StartedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Running -> Completed/PartiallyCompleted: islem sonlandi.
        /// Basarisiz kayit yoksa Completed, varsa PartiallyCompleted olur.
        /// </summary>
        public void Complete(int successCount, int failedCount)
        {
            EnsureStatus(EnumBulkOperationStatus.Running, "Tamamlama yalnizca Running durumundan yapilabilir.");

            if (successCount < 0 || failedCount < 0)
                throw new DomainException("Basari/basarisiz sayilari negatif olamaz.");

            SuccessCount = successCount;
            FailedCount = failedCount;
            Status = failedCount == 0
                ? EnumBulkOperationStatus.Completed
                : EnumBulkOperationStatus.PartiallyCompleted;
            CompletedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Running -> Failed: islem islenemeyen bir hatayla sonlandi.</summary>
        public void Fail(string report)
        {
            EnsureStatus(EnumBulkOperationStatus.Running, "Hata sonucu yalnizca Running durumundan yazilabilir.");

            Status = EnumBulkOperationStatus.Failed;
            ErrorReport = TruncateReport(report);
            CompletedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Pending/Validating/AwaitingApproval -> Cancelled: islem baslamadan iptal edildi.
        /// Running ve sonraki durumlarda iptale izin verilmez.
        /// </summary>
        public void Cancel()
        {
            if (Status != EnumBulkOperationStatus.Pending
                && Status != EnumBulkOperationStatus.Validating
                && Status != EnumBulkOperationStatus.AwaitingApproval)
                throw new DomainException(
                    "Toplu islem yalnizca Pending, Validating veya AwaitingApproval durumunda iptal edilebilir.",
                    $"Mevcut durum: {Status}");

            Status = EnumBulkOperationStatus.Cancelled;
            CompletedAtUtc = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Hata raporunu alan sinirina kirpar (kalan kisim sayaci ile takip edilir).</summary>
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
