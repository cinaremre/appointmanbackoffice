using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessSubscription
{
    /// <summary>
    /// İşletmenin geçerli aboneliğini plan adı ve plan özellik listesiyle
    /// getirir (doküman Modül 13). Aktif abonelik yoksa başarıyla boş
    /// (Data null) döner; bu bir hata değil.
    /// </summary>
    public class GetBusinessSubscriptionQuery : IRequest<Result<AdminBusinessSubscriptionDto?>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>İşletme aboneliği ve plan özelliklerinin admin görünümü.</summary>
    public class AdminBusinessSubscriptionDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }

        /// <summary>UTC bitiş anı; null = belirsiz süre.</summary>
        public DateTime? EndAt { get; set; }

        public EnumSubscriptionStatus Status { get; set; }

        /// <summary>Plana dahil özellikler.</summary>
        public IReadOnlyList<AdminSubscriptionPlanFeatureDto> PlanFeatures { get; set; }
            = Array.Empty<AdminSubscriptionPlanFeatureDto>();
    }

    /// <summary>Plan özellik satırının admin görünümü.</summary>
    public class AdminSubscriptionPlanFeatureDto
    {
        public string FeatureCode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }

        /// <summary>Dönemsel kota; null = sınırsız.</summary>
        public int? IncludedQuota { get; set; }
    }
}
