using AppointMan.Application.Common.Results;
using AppointMan.BackOffice.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Common
{
    /// <summary>
    /// Handler hatalarini RFC 9457 ProblemDetails'e cevirir (dokuman 7.3).
    /// Status kodu Error.Code'dan turetilir; ic hata ayrintilari istemciye
    /// sizmaz. Controller'lar bu mapper ile tek bicimde hata doner.
    /// </summary>
    internal static class AdminErrorMapper
    {
        /// <summary>Hata sonucunu uygun status kodlu ProblemDetails'e cevirir.</summary>
        /// <param name="error">Handler'dan donen hata; null ise genel ic hata uretilir.</param>
        /// <returns>ProblemDetails iceren IActionResult.</returns>
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
                Title = error?.Message ?? "Islem tamamlanamadi.",
                Status = statusCode,
                // Ic hata ayrintisi yalnizca sunucu loglarinda kalir.
                Detail = statusCode < 500 ? error?.Message : "Istek islenirken beklenmeyen bir hata olustu."
            };

            if (error is not null)
                problem.Extensions["code"] = error.Code;

            return new ObjectResult(problem)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        /// <summary>Problem tip kisalmasini uretir.</summary>
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
