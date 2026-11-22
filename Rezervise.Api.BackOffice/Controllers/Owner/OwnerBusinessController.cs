using Rezervise.Application.Features.Owner.Businesses.Commands.UpdateMyBusiness;
using Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinessDetail;
using Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinesses;
using Rezervise.Application.Features.Owner.Dashboard.Queries.GetMyBusinessDashboard;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// Isletme sahibinin kendi isletme bilgileri ve dashboard'u.
    /// Route seviyesinde BusinessOwner rolune sinirlidir; isletme
    /// kapsami (sahiplik) handler'larda dogrulanir.
    ///
    /// Route notu: base sinifin "api/v1/admin" oneki burada bilincli
    /// olarak "api/v1/owner" ile degistirilir.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerBusinessController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerBusinessController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Sahibin isletme listesi; baska kullanilarin kayitlari donmez.</summary>
        [HttpGet("businesses")]
        public async Task<IActionResult> GetMyBusinesses(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyBusinessesQuery
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Sahibin isletme detayi + calisan/hizmet/randevu sayilari.</summary>
        [HttpGet("businesses/{businessId:guid}")]
        public async Task<IActionResult> GetMyBusinessDetail(Guid businessId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetMyBusinessDetailQuery { BusinessId = businessId },
                cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>
        /// Isletme temel bilgilerini guncellestirir. Gonderilen alanlar
        /// guncellenir; denetim kaydi yazilir.
        /// </summary>
        [HttpPatch("businesses/{businessId:guid}")]
        public async Task<IActionResult> UpdateMyBusiness(
            Guid businessId,
            [FromBody] UpdateMyBusinessRequest? body,
            CancellationToken cancellationToken)
        {
            var command = new UpdateMyBusinessCommand
            {
                BusinessId = businessId,
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

        /// <summary>
        /// Isletmenin gunluk dashboard kartlari + ayni gunun satis/kasa
        /// ozeti. Date verilmezse bugun (UTC) raporlanir.
        /// </summary>
        [HttpGet("businesses/{businessId:guid}/dashboard")]
        public async Task<IActionResult> GetMyBusinessDashboard(
            Guid businessId,
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyBusinessDashboardQuery
            {
                BusinessId = businessId,
                Date = date
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>PATCH govdesi: yalnizca gonderilen alanlar guncellenir.</summary>
        public sealed class UpdateMyBusinessRequest
        {
            public string? Name { get; init; }

            public string? Description { get; init; }

            /// <summary>Opsiyonel islem gerekcesi; denetim kaydina yazilir.</summary>
            public string? Reason { get; init; }
        }
    }
}
