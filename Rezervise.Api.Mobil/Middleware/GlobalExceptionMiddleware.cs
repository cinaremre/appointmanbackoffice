using Rezervise.Application.Common.Exceptions;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Rezervise.Api.Middleware
{
    /// <summary>
    /// Tüm istisnaları tek noktada HTTP yanıtına çevirir ve lokalizasyon
    /// anahtarlarını aktif kültürde çözer. İstemciye istisna tipi veya
    /// stack trace sızdırılmaz; <c>Detail</c> yalnızca geliştirme ortamında dolar.
    /// Yakalanan her istisna yapılandırılmış alanlarla loğlanır; stack trace
    /// GELF üzerinden Graylog'a iletilir.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        /// <summary>
        /// Hata log şeması. Placeholder'lar GELF alanlarına dönüşür
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
        /// Middleware'i bağımlılıklarıyla oluşturur.
        /// </summary>
        /// <param name="next">Sonraki middleware.</param>
        /// <param name="logger">Hata kaydı.</param>
        /// <param name="localizer">Anahtar cozen lokalizasyon servisi.</param>
        /// <param name="environment">Detail alanının doldurulacağı ortam bilgisi.</param>
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
        /// İsteği çalıştırır ve oluşan istisnaları lokalize edilmiş yanıta çevirir.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
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
                // Veritabanı tekillik kısıtı ihlali: 500 yerine 409 Conflict.
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.Conflict, ex);
                await HandleDomainExceptionAsync(context, HttpStatusCode.Conflict, "duplicate_key", ex);
            }
            catch (ConcurrencyConflictException ex)
            {
                // Es zamanlı güncelleme çakışması: istemci tekrar denemeli.
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.Conflict, ex);
                await HandleDomainExceptionAsync(context, HttpStatusCode.Conflict, "concurrency_conflict", ex);
            }
            catch (ForbiddenAccessException ex)
            {
                // Kaynak sahipliği ihlali: 403 Forbidden.
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
                // Bulunamayan kaynak: 404 NotFound (varlık sızıntısını önlemek için 403 yerine tercih edilir).
                LogException(context, LogLevel.Warning, (int)HttpStatusCode.NotFound, ex);
                await HandleSimpleAsync(context, HttpStatusCode.NotFound, "not_found", "Error.NotFound");
            }
            catch (DomainException ex)
            {
                // İs kuralı ihlali: istemci hatası (400).
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
        /// İstisnayı yapılandırılmış alanlarla loglar. Loglama hatası yanıt üretimini engellemez.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <param name="level">Kullanılacak log seviyesi (4xx için Warning, 5xx için Error).</param>
        /// <param name="statusCode">İstemciye dönülecek HTTP durum kodu.</param>
        /// <param name="exception">Yakalanan istisna; stack trace loğlanır.</param>
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
                // Loglama best effort: hata yanıtını asla bozmaz.
            }
        }

        /// <summary>
        /// Correlation id'yi korelasyon middleware'inin bıraktığı değerden okur.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <returns>İstek için kullanılan correlation id.</returns>
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
        /// Çağırıcı kimliğini token claim'lerinden okur.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <returns>Kullanıcı kimliği; anonim isteklerde boş metin.</returns>
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
        /// İstek/yanıt loglama middleware'inin bıraktığı maskeli gövdeyi okur.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <returns>Maskelenmiş istek gövdesi; yoksa boş metin.</returns>
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
        /// Anahtar taşıyan domain istisnalarını lokalize edilmiş yanıta çevirir.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <param name="statusCode">Dönülecek HTTP durum kodu.</param>
        /// <param name="code">İstemciye dönülecek hata kodu.</param>
        /// <param name="exception">Anahtar taşıyan istisna.</param>
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
        /// Sabit anahtarlı basit hata yanıtı üretir.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <param name="statusCode">Dönülecek HTTP durum kodu.</param>
        /// <param name="code">İstemciye dönülecek hata kodu.</param>
        /// <param name="key">Lokalizasyon anahtarı.</param>
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
        /// Validasyon hatalarını alan bazında lokalize edilmiş sözlüğe çevirir.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <param name="exception">FluentValidation istisnası.</param>
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
        /// Validatör mesajını çözer. Mesaj zaten lokalize edilmiş bir cümleyse
        /// öldüğü gibi bırakılır; aksi halde anahtar olarak kataloga bakılır.
        /// </summary>
        /// <param name="message">Validatör'un ürettiği mesaj.</param>
        /// <returns>İstemciye dönen mesaj.</returns>
        private string ResolveValidationMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return _localizer.Get("Error.Unexpected");

            // Validatorlar çoğu zaman zaten lokalize edilmiş cümle üretir. Cümleyi
            // anahtar gibi tekrar çözmek katalogda bulunamayıp "Unexpected" mesajına
            // düşürüyordu (DateOfBirth hatasının kök nedeni).
            var looksLikeKey = !message.Any(char.IsWhiteSpace) && message.Contains('.');

            return looksLikeKey ? ResolveMessage(message, message) : message;
        }

        /// <summary>
        /// Beklenmeyen hatalar için genel hata yanıtı üretir.
        /// </summary>
        /// <param name="context">Geçerli HTTP bağlamı.</param>
        /// <param name="exception">Yakalanan istisna.</param>
        private Task HandleUnexpectedAsync(HttpContext context, Exception exception)
        {
            // İstisna detayı yalnızca geliştirme ortamında istemciye döner.
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
        /// Anahtarı aktif kültürde çözer; anahtar katalogda yoksa yedek metne,
        /// o da yoksa genel hata mesajına düşer.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı (boş olabilir).</param>
        /// <param name="fallbackMessage">Anahtar çözülemezse kullanılacak metin.</param>
        /// <returns>İstemciye gösterilecek metin.</returns>
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
