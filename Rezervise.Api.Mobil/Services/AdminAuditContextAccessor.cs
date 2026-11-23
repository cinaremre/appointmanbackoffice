using System.Security.Claims;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Api.Services
{
    /// <summary>
    /// Yönetici denetim bağlamı (IAdminAuditContext) müşteri API'si uyarlaması.
    /// Tek solution'da MediatR tüm Application assembly'sını taradığı için
    /// Admin/Owner handler'larının DI bağımlılıkları bu hoşt'ta da karşılanır.
    /// Correlation ID istek başlığından, IP bağlantıdan çözülür.
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
