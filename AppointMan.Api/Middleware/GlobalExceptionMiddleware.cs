using AppointMan.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace AppointMan.Api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validasyon hatası");
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (DuplicateKeyException ex)
            {
                // Veritabani tekillik kisiti ihlali: 500 yerine 409 Conflict.
                _logger.LogWarning(ex, "Tekillik ihlali: {IndexName}", ex.IndexName);
                await HandleDomainConflictAsync(context, "duplicate_key", ex.Message);
            }
            catch (ConcurrencyConflictException ex)
            {
                // Es zamanli guncelleme cakismasi: istemci tekrar denemeli.
                _logger.LogWarning(ex, "Es zamanli guncelleme cakismasi");
                await HandleDomainConflictAsync(context, "concurrency_conflict", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen hata");
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Yaris kosulu kaynakli hatalari 409 Conflict olarak dondurur.
        /// </summary>
        /// <param name="context">Gecerli HTTP baglami.</param>
        /// <param name="code">Istemciye donulecek hata kodu.</param>
        /// <param name="message">Kullaniciya gosterilecek Turkce mesaj.</param>
        private static Task HandleDomainConflictAsync(HttpContext context, string code, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Code = code,
                Message = message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var errors = exception.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Validasyon hatası",
                Errors = errors
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                Detail = exception.Message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}