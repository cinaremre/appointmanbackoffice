using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Veri aktarım işindeki tek bir satırın işlem sonucu.
    /// SourceRecordKey, telefonun PhoneNumber value object ile
    /// normalize edilmiş değeridir; deterministik mükerrer kontrolü sağlar.
    /// Bu sürümde satır bazında müşteri kaydı oluşturulmaz (register akışına bağlanacak).
    /// </summary>
    public class ImportRowResult : BaseEntity
    {
        public Guid ImportJobId { get; private set; }

        /// <summary>CSV içindeki 1 tabanlı satır numarası.</summary>
        public int RowNumber { get; private set; }

        /// <summary>Normalize edilmiş telefon (deterministik eşleştirme anahtarı).</summary>
        public string? SourceRecordKey { get; private set; }

        public EnumImportRowStatus Status { get; private set; }

        /// <summary>Failed durumunda hatanın kısa açıklaması.</summary>
        public string? ErrorDetails { get; private set; }

        private ImportRowResult() { }

        public ImportRowResult(
            Guid importJobId,
            int rowNumber,
            EnumImportRowStatus status,
            string? sourceRecordKey = null,
            string? errorDetails = null)
        {
            if (importJobId == Guid.Empty)
                throw new ArgumentException("Aktarım isi ID boş olamaz.", nameof(importJobId));

            if (rowNumber < 1)
                throw new DomainException("ImportRow.RowNumberInvalid", "Satır numarası 1'den küçük olamaz.");

            if (errorDetails != null && errorDetails.Length > 500)
                throw new DomainException("ImportRow.ErrorTooLong", "Hata detayı en fazla 500 karakter olabilir.");

            ImportJobId = importJobId;
            RowNumber = rowNumber;
            Status = status;
            SourceRecordKey = string.IsNullOrWhiteSpace(sourceRecordKey) ? null : sourceRecordKey;
            ErrorDetails = string.IsNullOrWhiteSpace(errorDetails) ? null : errorDetails;
        }
    }
}
