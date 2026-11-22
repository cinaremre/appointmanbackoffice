using System.Text.Json;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.BackOffice.Api.Middleware
{
    /// <summary>
    /// Backoffice istekleri icin denetim baglami ve korelasyon kimligi.
    /// RFC 9457 uyumlu ProblemDetails uretir; ic hata detaylari istemciye
    /// sizmaz (dokuman 7.3).
    /// </summary>
    public sealed class BackOfficeExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BackOfficeExceptionMiddleware> _logger;

        public BackOfficeExceptionMiddleware(RequestDelegate next, ILogger<BackOfficeExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Denetim baglami: correlation + IP handler'lara akar.
            var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var value)
                ? value.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items[AdminAuditContextAccessor.CorrelationItemKey] = correlationId;
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Backoffice islenmemis hata. Path={Path} CorrelationId={CorrelationId}",
                    context.Request.Path,
                    correlationId);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type = "https://api.rezervise.com/problems/internal-error",
                    title = "Islem tamamlanamadi.",
                    status = 500,
                    detail = "Istek islenirken beklenmeyen bir hata olustu. Destek icin correlationId'yi iletin.",
                    traceId = correlationId
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }
    }

    /// <summary>
    /// HttpContext'ten denetim bilgisi okuyan baglam erisimcisi.
    /// Program.cs tarafindan IAdminAuditContext olarak kaydedilir.
    /// </summary>
    public sealed class AdminAuditContextAccessor : IAdminAuditContext
    {
        internal const string CorrelationItemKey = "BackOffice:CorrelationId";

        private readonly IHttpContextAccessor _accessor;

        public AdminAuditContextAccessor(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public string CorrelationId
            => _accessor.HttpContext?.Items.TryGetValue(CorrelationItemKey, out var value) == true
                ? value?.ToString() ?? string.Empty
                : string.Empty;

        public string IpAddress
            => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}
