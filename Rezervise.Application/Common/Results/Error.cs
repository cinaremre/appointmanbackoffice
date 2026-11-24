namespace Rezervise.Application.Common.Results
{
    /// <summary>
    /// İşlem sonucu hatası. <see cref="Key"/> doluysa Api katmanı bu anahtarı
    /// aktif kültürde çözer; anahtar hiçbir katalogda yoksa <see cref="Message"/>
    /// değeri kullanılır. Böylece geçiş sürecinde mevcut çağrılar bozulmaz.
    /// </summary>
    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string? Detail { get; set; }

        /// <summary>Çözümlenecek lokalizasyon anahtarı (orn. "Error.NotFound").</summary>
        public string? Key { get; set; }

        /// <summary>Anahtar çözülürken kullanılacak biçimlendirme argümanları.</summary>
        public object[]? KeyArgs { get; set; }

        /// <summary>
        /// Bekleme süresi dolmadan yapılan isteklerde (HTTP 429) istemcinin
        /// geri sayım gösterebilmesi için kalan saniye. Diğer hatalarda null.
        /// </summary>
        public int? RetryAfterSeconds { get; set; }

        public Error(string code, string message, string? detail = null)
        {
            Code = code;
            Message = message;
            Detail = detail;
        }

        /// <summary>
        /// Aynı kod, detay ve lokalizasyon anahtarını koruyarak mesajı
        /// değiştirilmiş yeni bir hata üretir.
        /// </summary>
        /// <param name="message">Yeni mesaj metni.</param>
        /// <returns>Mesajı değiştirilmiş yeni <see cref="Error"/> örneği.</returns>
        public Error WithMessage(string message)
        {
            return new Error(Code, message, Detail)
            {
                Key = Key,
                KeyArgs = KeyArgs,
                RetryAfterSeconds = RetryAfterSeconds
            };
        }

        public static Error NotFound(string entity, string id)
            => new Error("NOT_FOUND", $"{entity} bulunamadı.", $"Id: {id}")
            {
                Key = "Error.NotFound",
                KeyArgs = new object[] { entity }
            };

        public static Error NotFoundById(string entity, Guid id)
            => NotFound(entity, id.ToString());

        public static Error Validation(string message, string? detail = null)
            => new Error("VALIDATION_ERROR", message, detail) { Key = message };

        public static Error Conflict(string message, string? detail = null)
            => new Error("CONFLICT", message, detail) { Key = message };

        public static Error Unauthorized(string? message = null)
            => new Error("UNAUTHORIZED", message ?? "Yetkisiz erişim.")
            {
                Key = message ?? "Error.Unauthorized"
            };

        public static Error Forbidden(string? message = null)
            => new Error("FORBIDDEN", message ?? "Bu işlem için yetkiniz yok.")
            {
                Key = message ?? "Error.Forbidden"
            };

        public static Error BusinessRule(string message, string? detail = null)
            => new Error("BUSINESS_RULE_VIOLATION", message, detail) { Key = message };

        /// <summary>
        /// Bekleme süresi (cooldown) dolmadan yapılan istek. <paramref name="keyArgs"/>
        /// lokalizasyon metnindeki {0} yer tutucusunu doldurur (kalan saniye).
        /// </summary>
        public static Error TooManyRequests(string message, int retryAfterSeconds, params object[] keyArgs)
            => new Error("TOO_MANY_REQUESTS", message)
            {
                Key = message,
                KeyArgs = keyArgs.Length > 0 ? keyArgs : null,
                RetryAfterSeconds = retryAfterSeconds
            };

        public static Error Internal(string message, string? detail = null)
            => new Error("INTERNAL_ERROR", message, detail) { Key = message };
    }
}
