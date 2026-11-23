using System;

namespace Rezervise.Domain.Exceptions
{
    /// <summary>
    /// Es zamanlı güncelleme çakışması durumunda fırlatılır.
    /// Kaynaklar: rowversion uyuşmazlığı (DbUpdateConcurrencyException)
    /// veya SQL Server deadlock (hata 1205).
    /// Kullanıcı işlemi tekrar denemelidir.
    /// </summary>
    public class ConcurrencyConflictException : DomainException
    {
        /// <summary>
        /// Güncelleme çakışması oluşturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı (orn. "Concurrency.RowVersionMismatch").</param>
        /// <param name="innerException">Asıl veritabanı istisnası (opsiyonel).</param>
        /// <param name="message">Anahtar çözülemezse kullanılacak yedek metin (opsiyonel).</param>
        public ConcurrencyConflictException(
            string key,
            Exception? innerException = null,
            string? message = null)
            : base(key, message ?? key, innerException)
        {
        }
    }
}
