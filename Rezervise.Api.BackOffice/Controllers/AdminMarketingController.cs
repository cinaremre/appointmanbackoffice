using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance;
using Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns;
using Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots;
using Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts;
using Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Pazarlama ve gelir geri kazanımı raporları (doküman Modül 15). Route'lar
    /// BackOfficeControllerBase üzerindeki "api/v1/admin" öneki ile birleşir:
    /// GET api/v1/admin/marketing/campaigns
    /// GET api/v1/admin/marketing/campaigns/{id}/performance
    /// GET api/v1/admin/waitlist/overview
    /// GET api/v1/admin/reengagement/attempts
    /// GET api/v1/admin/waitlist/recovered-slots
    /// </summary>
    public sealed class AdminMarketingController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminMarketingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Kampanya listesi: işletme, kanal ve aktiflik filtreleriyle sayfalı
        /// sonuç; her kampanya toplam harcama ve atıf sayısı taşır.
        /// </summary>
        [HttpGet("marketing/campaigns")]
        [Authorize(Policy = AdminPermissions.MarketingRead)]
        public async Task<IActionResult> GetCampaigns(
            [FromQuery] Guid? businessId,
            [FromQuery] EnumCampaignChannel? channel,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAdminCampaignsQuery
            {
                BusinessId = businessId,
                Channel = channel,
                IsActive = isActive,
                Page = page,
                PageSize = pageSize
            });

            if (result.IsFailure)
                return FromError(result);

            return OkList(result.Value.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Kampanya performans özeti: toplam harcama, atfedilen randevu ve
        /// tamamlanan randevu sayıları.
        /// </summary>
        [HttpGet("marketing/campaigns/{id:guid}/performance")]
        [Authorize(Policy = AdminPermissions.MarketingRead)]
        public async Task<IActionResult> GetCampaignPerformance(Guid id)
        {
            var result = await _mediator.Send(new GetAdminCampaignPerformanceQuery
            {
                CampaignId = id
            });

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Bekleme listesi genel bakışı: aktif kayıt, teklif durumları ve
        /// kurtarılan slot sayıları.
        /// </summary>
        [HttpGet("waitlist/overview")]
        [Authorize(Policy = AdminPermissions.MarketingRead)]
        public async Task<IActionResult> GetWaitlistOverview([FromQuery] Guid? businessId)
        {
            var result = await _mediator.Send(new GetAdminWaitlistOverviewQuery
            {
                BusinessId = businessId
            });

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Geri çağırma denemeleri: işletme, durum ve tarih filtreleriyle liste.
        /// </summary>
        [HttpGet("reengagement/attempts")]
        [Authorize(Policy = AdminPermissions.MarketingRead)]
        public async Task<IActionResult> GetReengagementAttempts(
            [FromQuery] Guid? businessId,
            [FromQuery] EnumReengagementStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var result = await _mediator.Send(new GetAdminReengagementAttemptsQuery
            {
                BusinessId = businessId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            });

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value.Items);
        }

        /// <summary>
        /// Kurtarılan kapasite raporu: iptallerden açığa çıkan slotlar ve
        /// toplam Rebooked özet sayısı.
        /// </summary>
        [HttpGet("waitlist/recovered-slots")]
        [Authorize(Policy = AdminPermissions.MarketingRead)]
        public async Task<IActionResult> GetRecoveredSlots(
            [FromQuery] Guid? businessId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var result = await _mediator.Send(new GetAdminRecoveredSlotsQuery
            {
                BusinessId = businessId,
                FromDate = fromDate,
                ToDate = toDate
            });

            if (result.IsFailure)
                return FromError(result);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Hata kodunu HTTP durum koduna eşler; izin hatası 403, kimlik hatası
        /// 401, bulunamadı 404 olarak döner.
        /// </summary>
        private IActionResult FromError<T>(Result<T> result)
        {
            return result.Error?.Code switch
            {
                "NOT_FOUND" => NotFound(result),
                "UNAUTHORIZED" => Unauthorized(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }
    }
}
