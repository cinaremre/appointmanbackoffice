using Rezervise.Application.Common.Exceptions;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Rezervise.Api.Middleware
{
    /// <summary>
    /// Tum istisnalari tek noktada HTTP yanitina cevirir ve lokalizasyon
    /// anahtarlarini aktif kulturde cozer. Istemciye istisna tipi veya
    /// stack trace sizdirilmaz; <c>Detail</c> yalnizca gelistirme ortaminda dolar.
    /// Yakalanan her istisna yapilandirilmis alanlarla loglanir; stack trace
    /// GELF uzerinden Graylog'a iletilir.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        /// <summary>
        /// Hata log semasi. Placeholder'lar GELF alanlarina donusur
        /// (<c>_correlationId</c>, <c>_requestBody</c>, ...).
        /// </summary>
        private const string LogTemplate =
            "HTTP {HttpMethod} {Url} failed with {StatusCode} | " +
            "correlationId={CorrelationId} userId={UserId} query={Query} " +
            "requestBody={RequestBody} error={ErrorMessage}";

        private const string UserIdClaimType = "userId";

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
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.BadRequest, ex);
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (DuplicateKeyException ex)
            {
                // Veritabani tekillik kisiti ihlali: 500 yerine 409 Conflict.
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.Conflict, ex);
                await HandleDomainExceptionAsync(context, HttpStatusCode.Conflict, "duplicate_key", ex);
            }
            catch (ConcurrencyConflictException ex)
            {
                // Es zamanli guncelleme cakismasi: istemci tekrar denemeli.
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.Conflict, ex);
                await HandleDomainExceptionAsync(context, HttpStatusCode.Conflict, "concurrency_conflict", ex);
            }
            catch (ForbiddenAccessException ex)
            {
                // Kaynak sahipligi ihlali: 403 Forbidden.
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.Forbidden, ex);
                await HandleSimpleAsync(context, HttpStatusCode.Forbidden, "forbidden", "Error.Forbidden");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.Unauthorized, ex);
                await HandleSimpleAsync(context, HttpStatusCode.Unauthorized, "unauthorized", "Error.Unauthorized");
            }
            catch (KeyNotFoundException ex)
            {
                // Bulunamayan kaynak: 404 NotFound (varlik sizintisini onlemek icin 403 yerine tercih edilir).
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.NotFound, ex);
                await HandleSimpleAsync(context, HttpStatusCode.NotFound, "not_found", "Error.NotFound");
            }
            catch (DomainException ex)
            {
                // Is kurali ihlali: istemci hatasi (400).
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.BadRequest, ex);
                await HandleDomainExceptionAsync(context, HttpStatusCode.BadRequest, "business_rule", ex);
            }
            catch (Exception ex)
            {
                // Beklenmeyen hata: 500 ve Error seviyesinde log.
                LogException(context, LogLevel.Error, (int)HttpStatusCode.InternalServerError, ex);
                await HandleUnexpectedAsync(context, ex);
            }
        }

        /// <summary>
        /// Istisnayi yapilandirilmis alanlarla loglar. Loglama hatasi yanit uretimini engellemez.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <param name="level">Kullanilacak log seviyesi (4xx icin Warning, 5xx icin Error).</param>
        /// <param name="statusCode">Istemciye donulecek HTTP durum kodu.</param>
        /// <param name="exception">Yakalanan istisna; stack trace loglanir.</param>
        private void LogException(HttpContext context, LogLevel level, int statusCode, Exception exception)
        {
            try
            {
                _logger.Log(
                    level,
                    exception,
                    LogTemplate,
                    context.Request.Method,
                    context.Request.Path.Value ?? "/",
                    statusCode,
                    ResolveCorrelationId(context),
                    ResolveUserId(context),
                    context.Request.QueryString.Value ?? string.Empty,
                    ResolveRequestBody(context),
                    exception.Message);
            }
            catch
            {
                // Loglama best effort: hata yanitini asla bozmaz.
            }
        }

        /// <summary>
        /// Correlation id'yi korelasyon middleware'inin biraktigi degerden okur.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <returns>Istek icin kullanilan correlation id.</returns>
        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value)
                && value is string correlationId
                && !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            return context.TraceIdentifier;
        }

        /// <summary>
        /// Cagirici kimligini token claim'lerinden okur.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <returns>Kullanici kimligi; anonim isteklerde bos metin.</returns>
        private static string ResolveUserId(HttpContext context)
        {
            var principal = context.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return string.Empty;

            return principal.FindFirst(UserIdClaimType)?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Istek/yanit loglama middleware'inin biraktigi maskeli govdeyi okur.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <returns>Maskelenmis istek govdesi; yoksa bos metin.</returns>
        private static string ResolveRequestBody(HttpContext context)
        {
            if (context.Items.TryGetValue(RequestResponseLoggingMiddleware.RequestBodyItemKey, out var value)
                && value is string body)
            {
                return body;
            }

            return string.Empty;
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
                Message = message,
                CorrelationId = ResolveCorrelationId(context)
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
                Message = _localizer.Get(key),
                CorrelationId = ResolveCorrelationId(context)
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
                    g => g.Select(e => ResolveValidationMessage(e.ErrorMessage)).ToArray()
                );

            return WriteAsync(context, HttpStatusCode.BadRequest, new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Code = "validation_error",
                Message = _localizer.Get("Error.Validation"),
                Errors = errors,
                CorrelationId = ResolveCorrelationId(context)
            });
        }

        /// <summary>
        /// Validator mesajini cozer. Mesaj zaten lokalize edilmis bir cumleyse
        /// oldugu gibi birakilir; aksi halde anahtar olarak kataloga bakilir.
        /// </summary>
        /// <param name="message">Validator'un urettigi mesaj.</param>
        /// <returns>Istemciye donen mesaj.</returns>
        private string ResolveValidationMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return _localizer.Get("Error.Unexpected");

            // Validatorlar cogu zaman zaten lokalize edilmis cumle uretir. Cumleyi
            // anahtar gibi tekrar cozmek katalogda bulunamayip "Unexpected" mesajina
            // dusuruyordu (DateOfBirth hatasinin kok nedeni).
            var looksLikeKey = !message.Any(char.IsWhiteSpace) && message.Contains('.');

            return looksLikeKey ? ResolveMessage(message, message) : message;
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
                Detail = detail,
                CorrelationId = ResolveCorrelationId(context)
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
