using System;

namespace AppointMan.Domain.Exceptions
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
        /// <param name="message">Kullaniciya gosterilecek Turkce mesaj.</param>
        /// <param name="innerException">Asil veritabani istisnasi (opsiyonel).</param>
        public ConcurrencyConflictException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
