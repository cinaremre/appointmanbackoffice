using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Yönetici denetim kayıtları (doküman Modül 21). Route,
    /// BackOfficeControllerBase üzerindeki "api/v1/admin" öneki ile birleşir:
    /// GET api/v1/admin/audit-logs
    /// </summary>
    public sealed class AdminAuditController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminAuditController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Denetim kayıtları: aktör, işletme, varlık tipi ve tarih filtreleriyle
        /// sayfalı sonuç; CreatedAtUtc tersten sıralı.
        /// </summary>
        [HttpGet("audit-logs")]
        [Authorize(Policy = AdminPermissions.AuditRead)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] Guid? actorUserId,
            [FromQuery] Guid? businessId,
            [FromQuery] string? entityType,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAdminAuditLogsQuery
            {
                ActorUserId = actorUserId,
                BusinessId = businessId,
                EntityType = entityType,
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
