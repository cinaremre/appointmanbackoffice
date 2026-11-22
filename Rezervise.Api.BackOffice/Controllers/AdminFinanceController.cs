using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminCashSessions;
using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminCommissions;
using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminExpenses;
using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminPayments;
using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminSales;
using Rezervise.BackOffice.Api.Common;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Finans okuma uclari (dokuman Modul 16). Tum listeler salt okunurdur:
    /// tutar duzeltmeleri (iade, ters kayit, prim ters cevirme) domain
    /// komutlariyla yapilir, bu controller'da veri asla degistirilmez.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/admin/finance")]
    public class AdminFinanceController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminFinanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Satis listesi; isletme ve tarih araligi filtreleriyle.
        /// Filtre kapsaminin GrandTotal toplami yanitta tek nesne olarak
        /// doner (OkList ekleme alan tasimadigindan liste + toplam birlikte).
        /// </summary>
        [HttpGet("sales")]
        [Authorize(Policy = AdminPermissions.FinanceRead)]
        public async Task<IActionResult> GetSales(
            [FromQuery] Guid? businessId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminSalesQuery
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

        /// <summary>Odeme listesi; iadeler IsRefund bayragiyla ayri listelenebilir.</summary>
        [HttpGet("payments")]
        [Authorize(Policy = AdminPermissions.FinanceRead)]
        public async Task<IActionResult> GetPayments(
            [FromQuery] Guid? businessId,
            [FromQuery] EnumPaymentMethod? paymentMethod,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] bool? isRefund,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminPaymentsQuery
            {
                BusinessId = businessId,
                PaymentMethod = paymentMethod,
                FromDate = fromDate,
                ToDate = toDate,
                IsRefund = isRefund,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Kasa oturumu listesi; beklenen/sayilan nakit ve fark raposuyla.</summary>
        [HttpGet("cash-sessions")]
        [Authorize(Policy = AdminPermissions.FinanceRead)]
        public async Task<IActionResult> GetCashSessions(
            [FromQuery] Guid? businessId,
            [FromQuery] EnumCashSessionStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminCashSessionsQuery
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

        /// <summary>Gider listesi; isletme ve tarih araligi filtreleriyle.</summary>
        [HttpGet("expenses")]
        [Authorize(Policy = AdminPermissions.FinanceRead)]
        public async Task<IActionResult> GetExpenses(
            [FromQuery] Guid? businessId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminExpensesQuery
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

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Prim tahakkuk listesi; isletme, calisan ve durum filtreleriyle.</summary>
        [HttpGet("commissions")]
        [Authorize(Policy = AdminPermissions.FinanceRead)]
        public async Task<IActionResult> GetCommissions(
            [FromQuery] Guid? businessId,
            [FromQuery] Guid? employeeId,
            [FromQuery] EnumCommissionAccrualStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminCommissionsQuery
            {
                BusinessId = businessId,
                EmployeeId = employeeId,
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
