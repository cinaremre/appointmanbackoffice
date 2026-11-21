using System.Security.Claims;
using AppointMan.Application.Common.Interfaces;

namespace AppointMan.Api.Services
{
    /// <summary>
    /// Yonetici denetim baglami (IAdminAuditContext) musteri API'si uyarlamasi.
    /// Tek solution'da MediatR tum Application assembly'sini taradigi icin
    /// Admin/Owner handler'larinin DI bagimliliklari bu host'ta da karsilanir.
    /// Correlation ID istek basligindan, IP baglantidan cozulur.
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
