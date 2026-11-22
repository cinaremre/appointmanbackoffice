using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Businesses.Commands.ActivateBusinessByAdmin;
using Rezervise.Application.Features.Admin.Businesses.Commands.SuspendBusinessByAdmin;
using Rezervise.Application.Features.Admin.Businesses.Commands.UpdateBusinessByAdmin;
using Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessAuditTrail;
using Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessDetail;
using Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinesses;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Platform yonetimi isletme endpoint'leri (dokuman Modul 8).
    /// Yetki policy'leriyle korunur; is kurallari handler'lardadir.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/admin/businesses")]
    public class AdminBusinessController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminBusinessController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Isletme listesi; durum, ad aramasi ve sehir filtreleri.</summary>
        [HttpGet]
        [Authorize(Policy = AdminPermissions.BusinessRead)]
        public async Task<IActionResult> GetList([FromQuery] GetBusinessesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            var value = result.Value!;

            return OkList(value.Items, value.Page, value.PageSize, value.TotalCount);
        }

        /// <summary>Isletme detayi + calisan/hizmet/randevu ozetleri.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AdminPermissions.BusinessRead)]
        public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBusinessDetailQuery { BusinessId = id }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Isletme temel bilgilerini guncellestirir; denetim kaydi yazilir.</summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = AdminPermissions.BusinessUpdate)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBusinessByAdminRequest body, CancellationToken cancellationToken)
        {
            var command = new UpdateBusinessByAdminCommand
            {
                BusinessId = id,
                Name = body?.Name,
                Description = body?.Description,
                Reason = body?.Reason
            };

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Isletmeyi askiya alir; Reason govdede zorunludur.</summary>
        [HttpPost("{id:guid}/suspend")]
        [Authorize(Policy = AdminPermissions.BusinessSuspend)]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendBusinessByAdminRequest? body, CancellationToken cancellationToken)
        {
            // Gerekce bos gonderilirse handler dogrulama hatasi donerur ve
            // buradan 400 BadRequest ile yansitilir.
            var command = new SuspendBusinessByAdminCommand
            {
                BusinessId = id,
                Reason = body?.Reason
            };

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Askidaki isletmeyi yeniden aktif eder; denetim kaydi yazilir.</summary>
        [HttpPost("{id:guid}/activate")]
        [Authorize(Policy = AdminPermissions.BusinessSuspend)]
        public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateBusinessByAdminRequest? body, CancellationToken cancellationToken)
        {
            var command = new ActivateBusinessByAdminCommand
            {
                BusinessId = id,
                Reason = body?.Reason
            };

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Isletme uzerindeki yonetici islemlerinin denetim kaydi.</summary>
        [HttpGet("{id:guid}/audit")]
        [Authorize(Policy = AdminPermissions.AuditRead)]
        public async Task<IActionResult> GetAuditTrail(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var query = new GetBusinessAuditTrailQuery
            {
                BusinessId = id,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            var value = result.Value!;

            return OkList(value.Items, value.Page, value.PageSize, value.TotalCount);
        }

        /// <summary>PATCH govdesi: yalnizca gonderilen alanlar guncellenir.</summary>
        public sealed class UpdateBusinessByAdminRequest
        {
            public string? Name { get; init; }

            public string? Description { get; init; }

            /// <summary>Opsiyonel islem gerekcesi; denetim kaydina yazilir.</summary>
            public string? Reason { get; init; }
        }

        /// <summary>Suspend govdesi: Reason zorunludur (handler dogrular).</summary>
        public sealed class SuspendBusinessByAdminRequest
        {
            public string? Reason { get; init; }
        }

        /// <summary>Activate govdesi: Reason opsiyoneldir.</summary>
        public sealed class ActivateBusinessByAdminRequest
        {
            public string? Reason { get; init; }
        }
    }
}
