using System.Security.Claims;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Notifications.Commands.RetryAdminNotification;
using Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats;
using Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotifications;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Bildirim yönetimi (doküman Modül 14). Route'lar
    /// BackOfficeControllerBase üzerindeki "api/v1/admin" öneki ile birleşir:
    /// GET  api/v1/admin/notifications
    /// GET  api/v1/admin/notifications/stats
    /// POST api/v1/admin/notifications/{id}/retry
    /// </summary>
    public sealed class AdminNotificationController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminNotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Bildirim listesi: kullanıcı, tip ve gönderim zamanı filtreleriyle
        /// sayfalı sonuç döndürür.
        /// </summary>
        [HttpGet("notifications")]
        [Authorize(Policy = AdminPermissions.NotificationRead)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] Guid? userId,
            [FromQuery] EnumNotificationType? type,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAdminNotificationsQuery
            {
                UserId = userId,
                Type = type,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            });

            if (result.IsFailure)
                return FromError(result);

            return OkList(result.Value.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Son 30 günün bildirim istatistikleri: toplam, okunmuş, okunmamış
        /// ve tip bazında kırılım.
        /// </summary>
        [HttpGet("notifications/stats")]
        [Authorize(Policy = AdminPermissions.NotificationRead)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _mediator.Send(new GetAdminNotificationStatsQuery());

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Bildirim yeniden gönderim talebi (kritik işlem; denetim kaydı yazılır).
        /// İlk sürümde asıl gönderim sağlayıcı entegrasyonuna kadar yalnızca
        /// kabul ve denetim kaydı üretir.
        /// </summary>
        [HttpPost("notifications/{id:guid}/retry")]
        [Authorize(Policy = AdminPermissions.NotificationRetry)]
        public async Task<IActionResult> Retry(Guid id, [FromQuery] string? reason)
        {
            // Aktör kimliği JWT claim'lerinden çözülür; istemciye güvenilmez.
            var actorIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var actorRole = User.Claims
                .FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)
                ?.Value ?? string.Empty;

            var result = await _mediator.Send(new RetryAdminNotificationCommand
            {
                NotificationId = id,
                ActorUserId = Guid.TryParse(actorIdValue, out var actorId) ? actorId : Guid.Empty,
                ActorRole = actorRole,
                Reason = reason
            });

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Hata kodunu HTTP durum koduna eşler; izin hatası 403, kimlik hatası
        /// 401, bulunamadı 404 olarak döner.
        /// </summary>
        private IActionResult FromError<T>(Result<T> result)
        {
            return result.Error?.Code switch
            {
                "NOT_FOUND" => NotFound(result),
                "UNAUTHORIZED" => Unauthorized(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }
    }
}
