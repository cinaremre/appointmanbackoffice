using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Application.Features.Admin.Operations.Queries.GetSystemHealth;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Operasyon izleme (doküman Modül 20). Route'lar BackOfficeControllerBase
    /// üzerindeki "api/v1/admin" öneki ile birleşir:
    /// GET api/v1/admin/operations/jobs
    /// GET api/v1/admin/operations/jobs/{id}
    /// GET api/v1/admin/operations/failed
    /// GET api/v1/admin/operations/health
    /// </summary>
    public sealed class AdminOperationsController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminOperationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Aktarım isi listesi: işletme ve durum filtreleriyle sayfalı sonuç.
        /// </summary>
        [HttpGet("operations/jobs")]
        [Authorize(Policy = AdminPermissions.OperationsRead)]
        public async Task<IActionResult> GetJobs(
            [FromQuery] Guid? businessId,
            [FromQuery] EnumImportStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAdminJobsQuery
            {
                BusinessId = businessId,
                Status = status,
                Page = page,
                PageSize = pageSize
            });

            if (result.IsFailure)
                return FromError(result);

            return OkList(result.Value.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Aktarım isi detayı: is özeti ve satır sonuçlarının ilk 200'u.
        /// </summary>
        [HttpGet("operations/jobs/{id:guid}")]
        [Authorize(Policy = AdminPermissions.OperationsRead)]
        public async Task<IActionResult> GetJobDetail(Guid id)
        {
            var result = await _mediator.Send(new GetAdminJobDetailQuery
            {
                ImportJobId = id
            });

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Başarısız operasyonlar: Failed işler ve örnek niteliğinde başarısız
        /// satır kayıtları.
        /// </summary>
        [HttpGet("operations/failed")]
        [Authorize(Policy = AdminPermissions.OperationsRead)]
        public async Task<IActionResult> GetFailedOperations()
        {
            var result = await _mediator.Send(new GetAdminFailedOperationsQuery());

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Sistem sağlık raporu: uygulamanın ayakta öldüğü bilgisi ve aktarım
        /// işlerinin durum sayıları.
        /// </summary>
        [HttpGet("operations/health")]
        [Authorize(Policy = AdminPermissions.OperationsRead)]
        public async Task<IActionResult> GetHealth()
        {
            var result = await _mediator.Send(new GetSystemHealthQuery());

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
