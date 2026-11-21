using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Features.Admin.Reports.Queries.GetFeatureAdoption;
using AppointMan.Application.Features.Admin.Reports.Queries.GetPlatformOverview;
using AppointMan.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue;
using AppointMan.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Platform raporlama endpoint'leri (dokuman Modul 18). Tum raporlar
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

        /// <summary>Platform geneli ozet: isletme/randevu sayilari ve oranlar.</summary>
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

        /// <summary>Abonelik geliri: aktif sayi, plan dagilimi, MRR.</summary>
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

        /// <summary>Ozellik benimseme: kod bazinda acik isletme sayilari.</summary>
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
