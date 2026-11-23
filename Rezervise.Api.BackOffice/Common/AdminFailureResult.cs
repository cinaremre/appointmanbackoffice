using Rezervise.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Common
{
    /// <summary>
    /// Handler Result hatalarını HTTP cevaplarına çevirir. Api katmanı
    /// hata kodunu bilir; mesaj içeriği Application katmanından gelir.
    /// </summary>
    public static class AdminFailureResult
    {
        /// <summary>
        /// Error.Code'a göre status kodu seçip standart hata gövdesi üretir.
        /// Bilinmeyen kodlar 400 BadRequest olarak döner.
        /// </summary>
        public static IActionResult FromError(Error error)
        {
            var status = error.Code switch
            {
                "NOT_FOUND" => StatusCodes.Status404NotFound,
                "CONFLICT" => StatusCodes.Status409Conflict,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
                "INTERNAL_ERROR" => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status400BadRequest
            };

            var body = new AdminErrorResponse
            {
                Error = new AdminErrorBody
                {
                    Code = error.Code,
                    Message = error.Message,
                    Detail = error.Detail
                },
                Meta = new AdminMeta()
            };

            return new ObjectResult(body) { StatusCode = status };
        }

        /// <summary>Handler sonucu başarısızsa hata cevabı, değilse null.</summary>
        public static IActionResult? FailureOrNull<T>(Result<T> result)
            => result.IsFailure ? FromError(result.Error!) : null;

        /// <summary>Hata gövdesi: tekil "error" nesnesi + meta.</summary>
        private sealed class AdminErrorResponse
        {
            public AdminErrorBody? Error { get; init; }

            public AdminMeta? Meta { get; init; }
        }

        private sealed class AdminErrorBody
        {
            public string Code { get; init; } = string.Empty;

            public string Message { get; init; } = string.Empty;

            public string? Detail { get; init; }
        }
    }
}
