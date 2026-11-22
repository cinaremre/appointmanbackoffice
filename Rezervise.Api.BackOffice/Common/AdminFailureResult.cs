using Rezervise.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Common
{
    /// <summary>
    /// Handler Result hatalarini HTTP cevaplarina cevirir. Api katmani
    /// hata kodunu bilir; mesaj icerigi Application katmanindan gelir.
    /// </summary>
    public static class AdminFailureResult
    {
        /// <summary>
        /// Error.Code'a gore status kodu secip standart hata govdesi uretir.
        /// Bilinmeyen kodlar 400 BadRequest olarak doner.
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

        /// <summary>Handler sonucu basarisizsa hata cevabi, degilse null.</summary>
        public static IActionResult? FailureOrNull<T>(Result<T> result)
            => result.IsFailure ? FromError(result.Error!) : null;

        /// <summary>Hata govdesi: tekil "error" nesnesi + meta.</summary>
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
