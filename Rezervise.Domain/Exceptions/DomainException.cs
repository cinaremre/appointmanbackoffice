using System;

namespace Rezervise.Domain.Exceptions
{
    /// <summary>
    /// Is kurali ihlali. Lokalize metin tasimaz; anahtar tasir ve metne cevirme
    /// Api sinirinda (GlobalExceptionMiddleware) yapilir. Boylece Domain katmani
    /// paketsiz ve lokalizasyon altyapisindan bagimsiz kalir.
    /// </summary>
    public class DomainException : Exception
    {
        /// <summary>
        /// Cozumlenecek lokalizasyon anahtari (orn. "Appointment.DateInPast").
        /// Anahtar katalogda yoksa yedek metin veya anahtarin kendisi doner.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Yalnizca lokalizasyon anahtari ile istisna olusturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        public DomainException(string key)
            : base(key)
        {
            Key = key;
        }

        /// <summary>
        /// Anahtar ve yedek metin ile istisna olusturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        /// <param name="message">Anahtar cozulemezse kullanilacak yedek metin.</param>
        public DomainException(string key, string message)
            : base(message)
        {
            Key = key;
        }

        /// <summary>
        /// Anahtar ve ic istisna ile istisna olusturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        /// <param name="innerException">Asil istisna.</param>
        public DomainException(string key, Exception? innerException)
            : base(key, innerException)
        {
            Key = key;
        }

        /// <summary>
        /// Anahtar, yedek metin ve ic istisna ile istisna olusturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        /// <param name="message">Anahtar cozulemezse kullanilacak yedek metin.</param>
        /// <param name="innerException">Asil istisna.</param>
        public DomainException(string key, string message, Exception? innerException)
            : base(message, innerException)
        {
            Key = key;
        }
    }
}
