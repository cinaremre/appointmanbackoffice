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
    /// İşletme sahibinin kendi işletme bilgileri ve dashboard'u.
    /// Route seviyesinde BusinessOwner rolüne sınırlıdır; işletme
    /// kapsamı (sahiplik) handler'larda doğrulanır.
    ///
    /// Route notu: base sınıfın "api/v1/admin" öneki burada bilinçli
    /// olarak "api/v1/owner" ile değiştirilir.
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

        /// <summary>Sahibin işletme listesi; başka kullanıların kayıtları dönmez.</summary>
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

        /// <summary>Sahibin işletme detayı + çalışan/hizmet/randevu sayıları.</summary>
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
        /// İşletme temel bilgilerini güncelleştirir. Gönderilen alanlar
        /// güncellenir; denetim kaydı yazılır.
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
        /// İşletmenin günlük dashboard kartları + aynı günün satış/kasa
        /// özeti. Date verilmezse bugün (UTC) raporlanır.
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

        /// <summary>PATCH gövdesi: yalnızca gönderilen alanlar güncellenir.</summary>
        public sealed class UpdateMyBusinessRequest
        {
            public string? Name { get; init; }

            public string? Description { get; init; }

            /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
            public string? Reason { get; init; }
        }
    }
}
