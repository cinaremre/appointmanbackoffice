using System;

namespace Rezervise.Domain.Exceptions
{
    /// <summary>
    /// Veritabanı tekillik kısıtı ihlal edildiğinde fırlatılır.
    /// SaveChangesAsync tarafından SQL Server hata 2601/2627 için üretilir.
    /// Böylece es zamanlı iki istekten ikincisi 500 yerine anlamlı bir
    /// çakışma sonucu alır.
    /// </summary>
    public class DuplicateKeyException : DomainException
    {
        /// <summary>İhlal edilen varlık (tablo) adı.</summary>
        public string EntityName { get; }

        /// <summary>İhlal edilen index veya kısıt adı (bilinmiyorsa null).</summary>
        public string? IndexName { get; }

        /// <summary>
        /// Tekillik ihlali oluşturur.
        /// </summary>
        /// <param name="entityName">İhlal edilen varlık adı.</param>
        /// <param name="key">Lokalizasyon anahtarı (orn. "Concurrency.SlotTaken").</param>
        /// <param name="indexName">İhlal edilen index adı (opsiyonel).</param>
        /// <param name="innerException">Asıl veritabanı istisnası (opsiyonel).</param>
        /// <param name="message">Anahtar çözülemezse kullanılacak yedek metin (opsiyonel).</param>
        public DuplicateKeyException(
            string entityName,
            string key,
            string? indexName = null,
            Exception? innerException = null,
            string? message = null)
            : base(key, message ?? key, innerException)
        {
            EntityName = entityName;
            IndexName = indexName;
        }
    }
}
