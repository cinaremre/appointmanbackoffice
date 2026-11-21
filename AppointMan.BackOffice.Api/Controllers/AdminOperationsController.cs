using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations;
using AppointMan.Application.Features.Admin.Operations.Queries.GetAdminJobDetail;
using AppointMan.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using AppointMan.Application.Features.Admin.Operations.Queries.GetSystemHealth;
using AppointMan.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Operasyon izleme (dokuman Modul 20). Route'lar BackOfficeControllerBase
    /// uzerindeki "api/v1/admin" oneki ile birlesir:
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
        /// Aktarim isi listesi: isletme ve durum filtreleriyle sayfali sonuc.
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
        /// Aktarim isi detayi: is ozeti ve satir sonuclarinin ilk 200'u.
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
        /// Basarisiz operasyonlar: Failed isler ve ornek niteliginde basarisiz
        /// satir kayitlari.
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
        /// Sistem saglik raporu: uygulamanin ayakta oldugu bilgisi ve aktarim
        /// islerinin durum sayilari.
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
