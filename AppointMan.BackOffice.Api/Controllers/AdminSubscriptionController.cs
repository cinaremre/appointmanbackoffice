using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Features.Admin.Subscriptions.Commands.AssignSubscriptionByAdmin;
using AppointMan.Application.Features.Admin.Subscriptions.Commands.CreateSubscriptionPlanByAdmin;
using AppointMan.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetAdminFeatureDefinitions;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetAdminSubscriptionPlans;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureOverrides;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureUsage;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessSubscription;
using AppointMan.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Abonelik ve ozellik yonetimi (dokuman Modul 13).
    /// Yazma uclari mevcut domain komutlarini (CreatePlan/AssignSubscription/
    /// GrantFeatureOverride) IMediator ile sarmalayan admin handler'lara
    /// baglanir; bu controller yalnizca HTTP eslemesi yapar.
    ///
    /// Route notu: controller birden fazla kaynak grubunu (subscription-plans,
    /// features, businesses/...) tasiidigindan sablonlar action seviyesinde
    /// verilir; sinif seviyesinde taban "api/v1/admin" sabitlenir.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/admin")]
    public class AdminSubscriptionController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminSubscriptionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Tum abonelik planlarini plan basina ozellik listesiyle dondurur.</summary>
        [HttpGet("subscription-plans")]
        [Authorize(Policy = AdminPermissions.SubscriptionRead)]
        public async Task<IActionResult> GetSubscriptionPlans(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminSubscriptionPlansQuery(), cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>
        /// Yeni abonelik plani olusturur. Domain kurallari mevcut
        /// CreatePlanCommand uzerinde yurutulur; islem denetimi yazilir.
        /// </summary>
        /// <returns>Olusturulan planin ID'si</returns>
        [HttpPost("subscription-plans")]
        [Authorize(Policy = AdminPermissions.SubscriptionUpdate)]
        public async Task<IActionResult> CreateSubscriptionPlan(
            [FromBody] CreateSubscriptionPlanByAdminCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>
        /// Isletmenin gecerli aboneligini plan adi ve ozellik listesiyle dondurur.
        /// Aktif abonelik yoksa Data null olan basari cevabi doner.
        /// </summary>
        [HttpGet("businesses/{businessId:guid}/subscription")]
        [Authorize(Policy = AdminPermissions.SubscriptionRead)]
        public async Task<IActionResult> GetBusinessSubscription(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetBusinessSubscriptionQuery { BusinessId = businessId },
                cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Isletmeye abonelik plani atar; varsa eski aktif abonelik Expired yapilir.
        /// </summary>
        /// <returns>Olusturulan aboneligin ID'si</returns>
        [HttpPost("businesses/{businessId:guid}/subscription/change")]
        [Authorize(Policy = AdminPermissions.SubscriptionUpdate)]
        public async Task<IActionResult> ChangeBusinessSubscription(
            Guid businessId,
            [FromBody] AssignSubscriptionByAdminCommand command,
            CancellationToken cancellationToken)
        {
            // Isletme kimligi route'tan gelir; govdedeki degerin yerine gecer.
            command.BusinessId = businessId;

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>
        /// Platformda tanimli tum ozellikleri listeler; plan ve override
        /// uclarindaki ozellik secimi bu listeden yapilir.
        /// </summary>
        [HttpGet("features")]
        [Authorize(Policy = AdminPermissions.FeatureRead)]
        public async Task<IActionResult> GetFeatureDefinitions(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminFeatureDefinitionsQuery(), cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Isletmeye verilmis ozellik override kararlari listeler.</summary>
        [HttpGet("businesses/{businessId:guid}/features")]
        [Authorize(Policy = AdminPermissions.FeatureRead)]
        public async Task<IActionResult> GetBusinessFeatureOverrides(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetBusinessFeatureOverridesQuery { BusinessId = businessId },
                cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>
        /// Isletmeye ozellik bazli gecici erisim karari (override) verir.
        /// </summary>
        /// <returns>Olusturulan override kaydinin ID'si</returns>
        [HttpPost("businesses/{businessId:guid}/features/feature-overrides")]
        [Authorize(Policy = AdminPermissions.FeatureOverride)]
        public async Task<IActionResult> GrantFeatureOverride(
            Guid businessId,
            [FromBody] GrantFeatureOverrideByAdminCommand command,
            CancellationToken cancellationToken)
        {
            // Isletme kimligi route'tan gelir; govdedeki degerin yerine gecer.
            command.BusinessId = businessId;

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Isletmenin son uc donemdeki ozellik kullanim sayilari.</summary>
        [HttpGet("businesses/{businessId:guid}/usage")]
        [Authorize(Policy = AdminPermissions.FeatureRead)]
        public async Task<IActionResult> GetBusinessFeatureUsage(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetBusinessFeatureUsageQuery { BusinessId = businessId },
                cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }
    }
}
