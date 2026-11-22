using System;

namespace Rezervise.Domain.Exceptions
{
    /// <summary>
    /// Es zamanli guncelleme cakismasi durumunda firlatilir.
    /// Kaynaklar: rowversion uyusmazligi (DbUpdateConcurrencyException)
    /// veya SQL Server deadlock (hata 1205).
    /// Kullanici islemi tekrar denemelidir.
    /// </summary>
    public class ConcurrencyConflictException : DomainException
    {
        /// <summary>
        /// Guncelleme cakismasi olusturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari (orn. "Concurrency.RowVersionMismatch").</param>
        /// <param name="innerException">Asil veritabani istisnasi (opsiyonel).</param>
        /// <param name="message">Anahtar cozulemezse kullanilacak yedek metin (opsiyonel).</param>
        public ConcurrencyConflictException(
            string key,
            Exception? innerException = null,
            string? message = null)
            : base(key, message ?? key, innerException)
        {
        }
    }
}
