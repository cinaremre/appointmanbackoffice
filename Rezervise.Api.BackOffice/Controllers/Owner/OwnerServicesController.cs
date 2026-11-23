using Rezervise.Application.Features.Owner.Services.Commands.UpdateMyService;
using Rezervise.Application.Features.Owner.Services.Queries.GetMyServices;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// İşletme sahibinin hizmet yönetimi. Route seviyesinde BusinessOwner
    /// rolüne sınırlıdır; işletme kapsamı handler'larda doğrulanır.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerServicesController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerServicesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Sahibin hizmet listesi; ad araması ve durum filtresiyle.</summary>
        [HttpGet("businesses/{businessId:guid}/services")]
        public async Task<IActionResult> GetMyServices(
            Guid businessId,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyServicesQuery
            {
                BusinessId = businessId,
                Search = search,
                IsActive = isActive,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Hizmet bilgilerini güncelleştirir. Gönderilen alanlar
        /// güncellenir; denetim kaydı yazılır.
        /// </summary>
        [HttpPatch("businesses/{businessId:guid}/services/{serviceId:guid}")]
        public async Task<IActionResult> UpdateMyService(
            Guid businessId,
            Guid serviceId,
            [FromBody] UpdateMyServiceRequest? body,
            CancellationToken cancellationToken)
        {
            var command = new UpdateMyServiceCommand
            {
                BusinessId = businessId,
                ServiceId = serviceId,
                Name = body?.Name,
                Price = body?.Price,
                Currency = body?.Currency,
                DurationMinutes = body?.DurationMinutes,
                Reason = body?.Reason
            };

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>PATCH gövdesi: yalnızca gönderilen alanlar güncellenir.</summary>
        public sealed class UpdateMyServiceRequest
        {
            public string? Name { get; init; }

            /// <summary>Yeni fiyat; gönderildiyse güncellenir.</summary>
            public decimal? Price { get; init; }

            /// <summary>Fiyat para birimi; boş ise mevcut birim korunur.</summary>
            public string? Currency { get; init; }

            /// <summary>Yeni süre (dakika).</summary>
            public int? DurationMinutes { get; init; }

            /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
            public string? Reason { get; init; }
        }
    }
}
