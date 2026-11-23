using System.Text.Json;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.BackOffice.Api.Middleware
{
    /// <summary>
    /// Backoffice istekleri için denetim bağlamı ve korelasyon kimliği.
    /// RFC 9457 uyumlu ProblemDetails üretir; iç hata detayları istemciye
    /// sızmaz (doküman 7.3).
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
            // Denetim bağlamı: correlation + IP handler'lara akar.
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
                    "Backoffice işlenmemiş hata. Path={Path} CorrelationId={CorrelationId}",
                    context.Request.Path,
                    correlationId);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type = "https://api.rezervise.com/problems/internal-error",
                    title = "İşlem tamamlanamadı.",
                    status = 500,
                    detail = "İstek işlenirken beklenmeyen bir hata oluştu. Destek için correlationId'yi iletin.",
                    traceId = correlationId
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }
    }

    /// <summary>
    /// HttpContext'ten denetim bilgisi okuyan bağlam erişimcisi.
    /// Program.cs tarafından IAdminAuditContext olarak kaydedilir.
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
