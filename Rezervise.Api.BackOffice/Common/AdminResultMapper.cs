using Rezervise.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Common
{
    /// <summary>
    /// Result hatalarını RFC 9457 uyumlu ProblemDetails cevaplarına çevirir
    /// (doküman 7.3). Handler'lar istisna fırlatmak yerine Result döndüğü
    /// için eşleme controller sınırında yapılır.
    /// </summary>
    public static class AdminResultMapper
    {
        /// <summary>
        /// Başarısız Result'i HTTP hata cevabına çevirir.
        /// Başarılı Result'larda çağırılmaz.
        /// </summary>
        public static ObjectResult ToProblem<T>(this Result<T> result, string correlationId)
        {
            var error = result.Error ?? new Error("INTERNAL_ERROR", "Bilinmeyen hata.");

            return new ObjectResult(new
            {
                type = $"https://api.rezervise.com/problems/{error.Code.ToLowerInvariant()}",
                title = error.Message,
                status = StatusCodeFor(error.Code),
                detail = error.Detail,
                traceId = correlationId,
                errors = new Dictionary<string, string[]> { [error.Code] = new[] { error.Message } }
            })
            {
                StatusCode = StatusCodeFor(error.Code),
                ContentTypes = { "application/problem+json" }
            };
        }

        private static int StatusCodeFor(string code) => code switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "BUSINESS_RULE_VIOLATION" => StatusCodes.Status422UnprocessableEntity,
            "INTERNAL_ERROR" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
