namespace AppointMan.Application.Common.Results
{
    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string? Detail { get; set; }

        public Error(string code, string message, string? detail = null)
        {
            Code = code;
            Message = message;
            Detail = detail;
        }

        public static Error NotFound(string entity, string id)
            => new Error("NOT_FOUND", $"{entity} bulunamadi.", $"Id: {id}");

        public static Error NotFoundById(string entity, Guid id)
            => new Error("NOT_FOUND", $"{entity} bulunamadi.", $"Id: {id}");

        public static Error Validation(string message, string? detail = null)
            => new Error("VALIDATION_ERROR", message, detail);

        public static Error Conflict(string message, string? detail = null)
            => new Error("CONFLICT", message, detail);

        public static Error Unauthorized(string message = "Yetkisiz erisim.")
            => new Error("UNAUTHORIZED", message);

        public static Error Forbidden(string message = "Bu islem icin yetkiniz yok.")
            => new Error("FORBIDDEN", message);

        public static Error BusinessRule(string message, string? detail = null)
            => new Error("BUSINESS_RULE_VIOLATION", message, detail);

        public static Error Internal(string message, string? detail = null)
            => new Error("INTERNAL_ERROR", message, detail);
    }
}