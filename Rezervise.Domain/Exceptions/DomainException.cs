using System;

namespace Rezervise.Domain.Exceptions
{
    /// <summary>
    /// İs kuralı ihlali. Lokalize metin taşımaz; anahtar taşır ve metne çevirme
    /// Api sınırında (GlobalExceptionMiddleware) yapılır. Böylece Domain katmanı
    /// paketsiz ve lokalizasyon altyapısından bağımsız kalır.
    /// </summary>
    public class DomainException : Exception
    {
        /// <summary>
        /// Çözümlenecek lokalizasyon anahtarı (orn. "Appointment.DateInPast").
        /// Anahtar katalogda yoksa yedek metin veya anahtarın kendisi döner.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Yalnızca lokalizasyon anahtarı ile istisna oluşturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        public DomainException(string key)
            : base(key)
        {
            Key = key;
        }

        /// <summary>
        /// Anahtar ve yedek metin ile istisna oluşturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        /// <param name="message">Anahtar çözülemezse kullanılacak yedek metin.</param>
        public DomainException(string key, string message)
            : base(message)
        {
            Key = key;
        }

        /// <summary>
        /// Anahtar ve iç istisna ile istisna oluşturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        /// <param name="innerException">Asıl istisna.</param>
        public DomainException(string key, Exception? innerException)
            : base(key, innerException)
        {
            Key = key;
        }

        /// <summary>
        /// Anahtar, yedek metin ve iç istisna ile istisna oluşturur.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        /// <param name="message">Anahtar çözülemezse kullanılacak yedek metin.</param>
        /// <param name="innerException">Asıl istisna.</param>
        public DomainException(string key, string message, Exception? innerException)
            : base(message, innerException)
        {
            Key = key;
        }
    }
}
