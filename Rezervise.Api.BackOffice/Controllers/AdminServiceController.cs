using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Services.Commands.CancelBulkServiceOperation;
using Rezervise.Application.Features.Admin.Services.Commands.ExecuteBulkServiceOperation;
using Rezervise.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin;
using Rezervise.Application.Features.Admin.Services.Queries.GetAdminServices;
using Rezervise.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Hizmet kataloğu ve toplu işlemler (doküman Modül 10).
    /// Toplu işlem akışı: önce POST bulk-operations ile onizleme/kayıt,
    /// sonra execute ile yürütme; başlamayan işler cancel ile iptal edilir.
    /// Not: ASP.NET Core sınıf seviyesi route'lari birleşmediği için
    /// tam prefix burada tekrar yazılır.
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

        /// <summary>Hizmet kataloğunu sayfalı listeler (max 200, default 50).</summary>
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

        /// <summary>Platform admin'i hizmet ad/fiyat/süre bilgisi günceller.</summary>
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
        /// Toplu işlem kaydı oluşturur. dryRun=true (varsayılan) onizleme:
        /// etkilenecek kayıt sayısı ve geçersiz ID'ler döner, yürütülemez.
        /// dryRun=false gerçek işlem kaydı: execute ile yürütülür.
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
        /// Validating/AwaitingApproval durumundaki işlemi yürütür.
        /// İlk sürümde en fazla 2000 kayıt; asımı Conflict.
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

        /// <summary>Başlamamış toplu işlemi iptal eder; Running+ durumlar Conflict.</summary>
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
