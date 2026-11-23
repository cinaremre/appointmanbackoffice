using Rezervise.Application.Common.Results;
using Rezervise.BackOffice.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Common
{
    /// <summary>
    /// Handler hatalarını RFC 9457 ProblemDetails'e çevirir (doküman 7.3).
    /// Status kodu Error.Code'dan türetilir; iç hata ayrıntıları istemciye
    /// sızmaz. Controller'lar bu mapper ile tek biçimde hata döner.
    /// </summary>
    internal static class AdminErrorMapper
    {
        /// <summary>Hata sonucunu uygun status kodlu ProblemDetails'e çevirir.</summary>
        /// <param name="error">Handler'dan dönen hata; null ise genel iç hata üretilir.</param>
        /// <returns>ProblemDetails içeren IActionResult.</returns>
        public static ObjectResult ToProblem(Error? error)
        {
            var statusCode = error switch
            {
                null => StatusCodes.Status500InternalServerError,
                _ when error.Code == "NOT_FOUND" => StatusCodes.Status404NotFound,
                _ when error.Code == "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
                _ when error.Code == "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
                _ when error.Code == "FORBIDDEN" => StatusCodes.Status403Forbidden,
                _ when error.Code == "CONFLICT" || error.Code == "BUSINESS_RULE_VIOLATED"
                    => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            var problem = new ProblemDetails
            {
                Type = $"https://api.rezervise.com/problems/{MapProblemType(error, statusCode)}",
                Title = error?.Message ?? "İşlem tamamlanamadı.",
                Status = statusCode,
                // İç hata ayrıntısı yalnızca sunucu loglarında kalır.
                Detail = statusCode < 500 ? error?.Message : "İstek işlenirken beklenmeyen bir hata oluştu."
            };

            if (error is not null)
                problem.Extensions["code"] = error.Code;

            return new ObjectResult(problem)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        /// <summary>Problem tip kısalmasını üretir.</summary>
        private static string MapProblemType(Error? error, int statusCode)
            => error?.Code switch
            {
                "NOT_FOUND" => "not-found",
                "VALIDATION_ERROR" => "validation",
                "UNAUTHORIZED" => "unauthorized",
                "FORBIDDEN" => "forbidden",
                "CONFLICT" or "BUSINESS_RULE_VIOLATED" => "conflict",
                _ => statusCode >= 500 ? "internal-error" : "error"
            };
    }
}
