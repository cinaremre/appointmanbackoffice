using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Yonetici denetim kayitlari (dokuman Modul 21). Route,
    /// BackOfficeControllerBase uzerindeki "api/v1/admin" oneki ile birlesir:
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
        /// Denetim kayitlari: aktor, isletme, varlik tipi ve tarih filtreleriyle
        /// sayfali sonuc; CreatedAtUtc tersten sirali.
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
        /// Hata kodunu HTTP durum koduna esler; izin hatasi 403, kimlik hatasi
        /// 401, bulunamadi 404 olarak doner.
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
