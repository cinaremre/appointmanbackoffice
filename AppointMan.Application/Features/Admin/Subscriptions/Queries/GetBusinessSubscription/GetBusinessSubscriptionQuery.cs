using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessSubscription
{
    /// <summary>
    /// Isletmenin gecerli aboneligini plan adi ve plan ozellik listesiyle
    /// getirir (dokuman Modul 13). Aktif abonelik yoksa basariyla bos
    /// (Data null) doner; bu bir hata degil.
    /// </summary>
    public class GetBusinessSubscriptionQuery : IRequest<Result<AdminBusinessSubscriptionDto?>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Isletme aboneligi ve plan ozelliklerinin admin gorunumu.</summary>
    public class AdminBusinessSubscriptionDto
    {
        public Guid SubscriptionId { get; set; }
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }

        /// <summary>UTC bitis ani; null = belirsiz sure.</summary>
        public DateTime? EndAt { get; set; }

        public EnumSubscriptionStatus Status { get; set; }

        /// <summary>Plana dahil ozellikler.</summary>
        public IReadOnlyList<AdminSubscriptionPlanFeatureDto> PlanFeatures { get; set; }
            = Array.Empty<AdminSubscriptionPlanFeatureDto>();
    }

    /// <summary>Plan ozellik satirinin admin gorunumu.</summary>
    public class AdminSubscriptionPlanFeatureDto
    {
        public string FeatureCode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }

        /// <summary>Donemsel kota; null = sinirsiz.</summary>
        public int? IncludedQuota { get; set; }
    }
}
