using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Subscriptions.Commands.AssignSubscriptionByAdmin;
using Rezervise.Application.Features.Admin.Subscriptions.Commands.CreateSubscriptionPlanByAdmin;
using Rezervise.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin;
using Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminFeatureDefinitions;
using Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminSubscriptionPlans;
using Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureOverrides;
using Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureUsage;
using Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessSubscription;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Abonelik ve özellik yönetimi (doküman Modül 13).
    /// Yazma uçları mevcut domain komutlarını (CreatePlan/AssignSubscription/
    /// GrantFeatureOverride) IMediator ile sarmalayan admin handler'lara
    /// bağlanır; bu controller yalnızca HTTP eşlemesi yapar.
    ///
    /// Route notu: controller birden fazla kaynak grubunu (subscription-plans,
    /// features, businesses/...) taşııdığından şablonlar action seviyesinde
    /// verilir; sınıf seviyesinde taban "api/v1/admin" sabitlenir.
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

        /// <summary>Tüm abonelik planlarını plan basına özellik listesiyle döndürür.</summary>
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
        /// Yeni abonelik planı oluşturur. Domain kuralları mevcut
        /// CreatePlanCommand üzerinde yürütülür; işlem denetimi yazılır.
        /// </summary>
        /// <returns>Oluşturulan planın ID'si</returns>
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
        /// İşletmenin geçerli aboneliğini plan adı ve özellik listesiyle döndürür.
        /// Aktif abonelik yoksa Data null olan başarı cevabı döner.
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
        /// İşletmeye abonelik planı atar; varsa eski aktif abonelik Expired yapılır.
        /// </summary>
        /// <returns>Oluşturulan aboneliğin ID'si</returns>
        [HttpPost("businesses/{businessId:guid}/subscription/change")]
        [Authorize(Policy = AdminPermissions.SubscriptionUpdate)]
        public async Task<IActionResult> ChangeBusinessSubscription(
            Guid businessId,
            [FromBody] AssignSubscriptionByAdminCommand command,
            CancellationToken cancellationToken)
        {
            // İşletme kimliği route'tan gelir; gövdedeki değerin yerine geçer.
            command.BusinessId = businessId;

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>
        /// Platformda tanımlı tüm özellikleri listeler; plan ve override
        /// uçlarındaki özellik seçimi bu listeden yapılır.
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

        /// <summary>İşletmeye verilmiş özellik override kararları listeler.</summary>
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
        /// İşletmeye özellik bazlı geçici erişim kararı (override) verir.
        /// </summary>
        /// <returns>Oluşturulan override kaydının ID'si</returns>
        [HttpPost("businesses/{businessId:guid}/features/feature-overrides")]
        [Authorize(Policy = AdminPermissions.FeatureOverride)]
        public async Task<IActionResult> GrantFeatureOverride(
            Guid businessId,
            [FromBody] GrantFeatureOverrideByAdminCommand command,
            CancellationToken cancellationToken)
        {
            // İşletme kimliği route'tan gelir; gövdedeki değerin yerine geçer.
            command.BusinessId = businessId;

            var result = await _mediator.Send(command, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>İşletmenin son uç dönemdeki özellik kullanım sayıları.</summary>
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
