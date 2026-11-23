using Rezervise.Application.Features.Owner.Finance.Queries.GetMyCashSessions;
using Rezervise.Application.Features.Owner.Finance.Queries.GetMyPayments;
using Rezervise.Application.Features.Owner.Finance.Queries.GetMySales;
using Rezervise.BackOffice.Api.Common;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// İşletme sahibinin finans okuma uçları. Tüm listeler salt okunurdur:
    /// tutar düzeltmeleri (iade, ters kayıt) domain komutlarıyla yapılır,
    /// bu controller'da veri asla değiştirilmez.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerFinanceController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerFinanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Satış listesi; tarih aralığı filtresiyle. Filtre kapsamının
        /// GrandTotal toplamı yanıtta tek nesne olarak döner (OkList
        /// ekleme alan taşımadığından liste + toplam birlikte).
        /// </summary>
        [HttpGet("businesses/{businessId:guid}/finance/sales")]
        public async Task<IActionResult> GetMySales(
            Guid businessId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMySalesQuery
            {
                BusinessId = businessId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Ödeme listesi; iadeler IsRefund bayrağıyla ayrı listelenebilir.</summary>
        [HttpGet("businesses/{businessId:guid}/finance/payments")]
        public async Task<IActionResult> GetMyPayments(
            Guid businessId,
            [FromQuery] bool? isRefund,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyPaymentsQuery
            {
                BusinessId = businessId,
                IsRefund = isRefund,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Kasa oturumu listesi; beklenen/sayılan nakit ve fark raposuyla.</summary>
        [HttpGet("businesses/{businessId:guid}/finance/cash-sessions")]
        public async Task<IActionResult> GetMyCashSessions(
            Guid businessId,
            [FromQuery] EnumCashSessionStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyCashSessionsQuery
            {
                BusinessId = businessId,
                Status = status,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }
    }
}
