using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Veri aktarim isindeki tek bir satirin islem sonucu.
    /// SourceRecordKey, telefonun PhoneNumber value object ile
    /// normalize edilmis degeridir; deterministik mukerrer kontrolu saglar.
    /// Bu surumde satir bazinda musteri kaydi olusturulmaz (register akisina baglanacak).
    /// </summary>
    public class ImportRowResult : BaseEntity
    {
        public Guid ImportJobId { get; private set; }

        /// <summary>CSV icindeki 1 tabanli satir numarasi.</summary>
        public int RowNumber { get; private set; }

        /// <summary>Normalize edilmis telefon (deterministik eslestirme anahtari).</summary>
        public string? SourceRecordKey { get; private set; }

        public EnumImportRowStatus Status { get; private set; }

        /// <summary>Failed durumunda hatanin kisa aciklamasi.</summary>
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
                throw new ArgumentException("Aktarim isi ID bos olamaz.", nameof(importJobId));

            if (rowNumber < 1)
                throw new DomainException("ImportRow.RowNumberInvalid", "Satir numarasi 1'den kucuk olamaz.");

            if (errorDetails != null && errorDetails.Length > 500)
                throw new DomainException("ImportRow.ErrorTooLong", "Hata detayi en fazla 500 karakter olabilir.");

            ImportJobId = importJobId;
            RowNumber = rowNumber;
            Status = status;
            SourceRecordKey = string.IsNullOrWhiteSpace(sourceRecordKey) ? null : sourceRecordKey;
            ErrorDetails = string.IsNullOrWhiteSpace(errorDetails) ? null : errorDetails;
        }
    }
}
