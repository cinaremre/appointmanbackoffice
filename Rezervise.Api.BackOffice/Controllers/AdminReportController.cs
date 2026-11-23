using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Reports.Queries.GetFeatureAdoption;
using Rezervise.Application.Features.Admin.Reports.Queries.GetPlatformOverview;
using Rezervise.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Platform raporlama endpoint'leri (doküman Modül 18). Tüm raporlar
    /// ReportRead permission'i ile korunur.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/admin/reports")]
    public class AdminReportController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminReportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Platform geneli özet: işletme/randevu sayıları ve oranlar.</summary>
        [HttpGet("platform-overview")]
        [Authorize(Policy = AdminPermissions.ReportRead)]
        public async Task<IActionResult> GetPlatformOverview(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPlatformOverviewQuery(), cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Abonelik geliri: aktif sayı, plan dağılımı, MRR.</summary>
        [HttpGet("subscription-revenue")]
        [Authorize(Policy = AdminPermissions.ReportRead)]
        public async Task<IActionResult> GetSubscriptionRevenue(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSubscriptionRevenueQuery(), cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Özellik benimseme: kod bazında açık işletme sayıları.</summary>
        [HttpGet("feature-adoption")]
        [Authorize(Policy = AdminPermissions.ReportRead)]
        public async Task<IActionResult> GetFeatureAdoption(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetFeatureAdoptionQuery(), cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }
    }
}
