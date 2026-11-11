using AppointMan.Application.Common.Interfaces;
using AppointMan.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace AppointMan.Api.Middleware
{
    /// <summary>
    /// Tum istisnalari tek noktada HTTP yanitina cevirir ve lokalizasyon
    /// anahtarlarini aktif kulturde cozer. Istemciye istisna tipi veya
    /// stack trace sizdirilmaz; <c>Detail</c> yalnizca gelistirme ortaminda dolar.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly ILocalizationService _localizer;
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Middleware'i bagimliliklariyla olusturur.
        /// </summary>
        /// <param name="next">Sonraki middleware.</param>
        /// <param name="logger">Hata kaydi.</param>
        /// <param name="localizer">Anahtar cozen lokalizasyon servisi.</param>
        /// <param name="environment">Detail alaninin doldurulacagi ortam bilgisi.</param>
        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            ILocalizationService localizer,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _localizer = localizer;
            _environment = environment;
        }

        /// <summary>
        /// Istegi calistirir ve olusan istisnalari lokalize edilmis yanita cevirir.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validasyon hatasi");
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (DuplicateKeyException ex)
            {
                // Veritabani tekillik kisiti ihlali: 500 yerine 409 Conflict.
                _logger.LogWarning(ex, "Tekillik ihlali. Key: {Key}, Index: {IndexName}", ex.Key, ex.IndexName);
                await HandleDomainExceptionAsync(context, HttpStatusCode.Conflict, "duplicate_key", ex);
            }
            catch (ConcurrencyConflictException ex)
            {
                // Es zamanli guncelleme cakismasi: istemci tekrar denemeli.
                _logger.LogWarning(ex, "Es zamanli guncelleme cakismasi. Key: {Key}", ex.Key);
                await HandleDomainExceptionAsync(context, HttpStatusCode.Conflict, "concurrency_conflict", ex);
            }
            catch (DomainException ex)
            {
                // Is kurali ihlali: istemci hatasi (400).
                _logger.LogWarning(ex, "Is kurali ihlali. Key: {Key}", ex.Key);
                await HandleDomainExceptionAsync(context, HttpStatusCode.BadRequest, "business_rule", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Yetkisiz erisim");
                await HandleSimpleAsync(context, HttpStatusCode.Unauthorized, "unauthorized", "Error.Unauthorized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen hata");
                await HandleUnexpectedAsync(context, ex);
            }
        }

        /// <summary>
        /// Anahtar tasiyan domain istisnalarini lokalize edilmis yanita cevirir.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <param name="statusCode">Donulecek HTTP durum kodu.</param>
        /// <param name="code">Istemciye donulecek hata kodu.</param>
        /// <param name="exception">Anahtar tasiyan istisna.</param>
        private Task HandleDomainExceptionAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string code,
            DomainException exception)
        {
            var message = ResolveMessage(exception.Key, exception.Message);

            return WriteAsync(context, statusCode, new
            {
                StatusCode = (int)statusCode,
                Code = code,
                Message = message
            });
        }

        /// <summary>
        /// Sabit anahtarli basit hata yaniti uretir.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <param name="statusCode">Donulecek HTTP durum kodu.</param>
        /// <param name="code">Istemciye donulecek hata kodu.</param>
        /// <param name="key">Lokalizasyon anahtari.</param>
        private Task HandleSimpleAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string code,
            string key)
        {
            return WriteAsync(context, statusCode, new
            {
                StatusCode = (int)statusCode,
                Code = code,
                Message = _localizer.Get(key)
            });
        }

        /// <summary>
        /// Validasyon hatalarini alan bazinda lokalize edilmis sozluge cevirir.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <param name="exception">FluentValidation istisnasi.</param>
        private Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => ResolveMessage(e.ErrorMessage, e.ErrorMessage)).ToArray()
                );

            return WriteAsync(context, HttpStatusCode.BadRequest, new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Code = "validation_error",
                Message = _localizer.Get("Error.Validation"),
                Errors = errors
            });
        }

        /// <summary>
        /// Beklenmeyen hatalar icin genel hata yaniti uretir.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <param name="exception">Yakalanan istisna.</param>
        private Task HandleUnexpectedAsync(HttpContext context, Exception exception)
        {
            // Istisna detayi yalnizca gelistirme ortaminda istemciye doner.
            var detail = _environment.IsDevelopment() ? exception.Message : null;

            return WriteAsync(context, HttpStatusCode.InternalServerError, new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Code = "unexpected_error",
                Message = _localizer.Get("Error.Unexpected"),
                Detail = detail
            });
        }

        /// <summary>
        /// Anahtari aktif kulturde cozer; anahtar katalogda yoksa yedek metne,
        /// o da yoksa genel hata mesajina duser.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari (bos olabilir).</param>
        /// <param name="fallbackMessage">Anahtar cozulemezse kullanilacak metin.</param>
        /// <returns>Istemciye gosterilecek metin.</returns>
        private string ResolveMessage(string? key, string? fallbackMessage)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                var localized = _localizer.Get(key);

                if (!string.Equals(localized, key, StringComparison.Ordinal))
                    return localized;
            }

            if (!string.IsNullOrWhiteSpace(fallbackMessage)
                && !string.Equals(fallbackMessage, key, StringComparison.Ordinal))
            {
                return fallbackMessage;
            }

            return _localizer.Get("Error.Unexpected");
        }

        private static Task WriteAsync(HttpContext context, HttpStatusCode statusCode, object payload)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
