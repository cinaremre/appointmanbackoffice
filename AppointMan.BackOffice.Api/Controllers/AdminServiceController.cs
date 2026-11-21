using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Features.Admin.Services.Commands.CancelBulkServiceOperation;
using AppointMan.Application.Features.Admin.Services.Commands.ExecuteBulkServiceOperation;
using AppointMan.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin;
using AppointMan.Application.Features.Admin.Services.Queries.GetAdminServices;
using AppointMan.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation;
using AppointMan.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Hizmet katalogu ve toplu islemler (dokuman Modul 10).
    /// Toplu islem akisi: once POST bulk-operations ile onizleme/kayit,
    /// sonra execute ile yurutme; baslamayan isler cancel ile iptal edilir.
    /// Not: ASP.NET Core sinif seviyesi route'lari birlesmedigi icin
    /// tam prefix burada tekrar yazilir.
    /// </summary>
    [Authorize]
    [Route("api/v1/admin/services")]
    public sealed class AdminServiceController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminServiceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Hizmet katalogunu sayfali listeler (max 200, default 50).</summary>
        [HttpGet]
        [Authorize(Policy = AdminPermissions.ServiceRead)]
        public async Task<IActionResult> GetServices(
            [FromQuery] Guid? businessId,
            [FromQuery] Guid? serviceCategoryId,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(
                new GetAdminServicesQuery
                {
                    BusinessId = businessId,
                    ServiceCategoryId = serviceCategoryId,
                    Search = search,
                    IsActive = isActive,
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);

            if (!result.IsSuccess)
                return result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());

            var value = result.Value!;
            return OkList(
                value.Items,
                value.Page,
                value.PageSize,
                value.TotalCount);
        }

        /// <summary>Platform admin'i hizmet ad/fiyat/sure bilgisi gunceller.</summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = AdminPermissions.ServiceUpdate)]
        public async Task<IActionResult> UpdateService(
            Guid id,
            [FromBody] UpdateServiceByAdminCommand command,
            CancellationToken cancellationToken)
        {
            command.ServiceId = id;

            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>
        /// Toplu islem kaydi olusturur. dryRun=true (varsayilan) onizleme:
        /// etkilenecek kayit sayisi ve gecersiz ID'ler doner, yurutulemez.
        /// dryRun=false gercek islem kaydi: execute ile yurutulur.
        /// </summary>
        [HttpPost("bulk-operations")]
        [Authorize(Policy = AdminPermissions.ServiceBulkUpdate)]
        public async Task<IActionResult> PreviewBulkOperation(
            [FromBody] PreviewBulkServiceOperationQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>
        /// Validating/AwaitingApproval durumundaki islemi yurutur.
        /// Ilk surumde en fazla 2000 kayit; asimi Conflict.
        /// </summary>
        [HttpPost("bulk-operations/{id:guid}/execute")]
        [Authorize(Policy = AdminPermissions.ServiceBulkUpdate)]
        public async Task<IActionResult> ExecuteBulkOperation(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new ExecuteBulkServiceOperationCommand { BulkOperationId = id },
                cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>Baslamamis toplu islemi iptal eder; Running+ durumlar Conflict.</summary>
        [HttpPost("bulk-operations/{id:guid}/cancel")]
        [Authorize(Policy = AdminPermissions.ServiceBulkUpdate)]
        public async Task<IActionResult> CancelBulkOperation(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CancelBulkServiceOperationCommand { BulkOperationId = id },
                cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }
    }
}
