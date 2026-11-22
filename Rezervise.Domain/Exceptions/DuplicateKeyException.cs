using System;

namespace Rezervise.Domain.Exceptions
{
    /// <summary>
    /// Veritabani tekillik kisiti ihlal edildiginde firlatilir.
    /// SaveChangesAsync tarafindan SQL Server hata 2601/2627 icin uretilir.
    /// Boylece es zamanli iki istekten ikincisi 500 yerine anlamli bir
    /// cakisma sonucu alir.
    /// </summary>
    public class DuplicateKeyException : DomainException
    {
        /// <summary>Ihlal edilen varlik (tablo) adi.</summary>
        public string EntityName { get; }

        /// <summary>Ihlal edilen index veya kisit adi (bilinmiyorsa null).</summary>
        public string? IndexName { get; }

        /// <summary>
        /// Tekillik ihlali olusturur.
        /// </summary>
        /// <param name="entityName">Ihlal edilen varlik adi.</param>
        /// <param name="key">Lokalizasyon anahtari (orn. "Concurrency.SlotTaken").</param>
        /// <param name="indexName">Ihlal edilen index adi (opsiyonel).</param>
        /// <param name="innerException">Asil veritabani istisnasi (opsiyonel).</param>
        /// <param name="message">Anahtar cozulemezse kullanilacak yedek metin (opsiyonel).</param>
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
