namespace Rezervise.Application.Common.Results
{
    /// <summary>
    /// Islem sonucu hatasi. <see cref="Key"/> doluysa Api katmani bu anahtari
    /// aktif kulturde cozer; anahtar hicbir katalogda yoksa <see cref="Message"/>
    /// degeri kullanilir. Boylece gecis surecinde mevcut cagrilar bozulmaz.
    /// </summary>
    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string? Detail { get; set; }

        /// <summary>Cozumlenecek lokalizasyon anahtari (orn. "Error.NotFound").</summary>
        public string? Key { get; set; }

        /// <summary>Anahtar cozulurken kullanilacak bicimlendirme argumanlari.</summary>
        public object[]? KeyArgs { get; set; }

        public Error(string code, string message, string? detail = null)
        {
            Code = code;
            Message = message;
            Detail = detail;
        }

        /// <summary>
        /// Ayni kod, detay ve lokalizasyon anahtarini koruyarak mesaji
        /// degistirilmis yeni bir hata uretir.
        /// </summary>
        /// <param name="message">Yeni mesaj metni.</param>
        /// <returns>Mesaji degistirilmis yeni <see cref="Error"/> ornegi.</returns>
        public Error WithMessage(string message)
        {
            return new Error(Code, message, Detail)
            {
                Key = Key,
                KeyArgs = KeyArgs
            };
        }

        public static Error NotFound(string entity, string id)
            => new Error("NOT_FOUND", $"{entity} bulunamadi.", $"Id: {id}")
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
            => new Error("UNAUTHORIZED", message ?? "Yetkisiz erisim.")
            {
                Key = message ?? "Error.Unauthorized"
            };

        public static Error Forbidden(string? message = null)
            => new Error("FORBIDDEN", message ?? "Bu islem icin yetkiniz yok.")
            {
                Key = message ?? "Error.Forbidden"
            };

        public static Error BusinessRule(string message, string? detail = null)
            => new Error("BUSINESS_RULE_VIOLATION", message, detail) { Key = message };

        public static Error Internal(string message, string? detail = null)
            => new Error("INTERNAL_ERROR", message, detail) { Key = message };
    }
}
