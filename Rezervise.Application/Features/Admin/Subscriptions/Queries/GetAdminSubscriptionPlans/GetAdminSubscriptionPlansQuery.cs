using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminSubscriptionPlans
{
    /// <summary>
    /// Tüm abonelik planlarını, plan basına dahil özellik listesiyle
    /// getirir (doküman Modül 13). Plan-özellik eşleşmesi PlanFeature
    /// kayıtları ile FeatureDefinition üzerinden bellek içinde kurulur.
    /// </summary>
    public class GetAdminSubscriptionPlansQuery : IRequest<Result<IReadOnlyList<AdminSubscriptionPlanDto>>>
    {
    }

    /// <summary>Plan ve özellik setinin admin görünümü.</summary>
    public class AdminSubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Domain.Enums.EnumBillingPeriod BillingPeriod { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Plana dahil özellikler; kod bazında eşleşmiş halde.</summary>
        public IReadOnlyList<AdminPlanFeatureDto> Features { get; set; } = Array.Empty<AdminPlanFeatureDto>();
    }

    /// <summary>Plana dahil tek özellik satırı.</summary>
    public class AdminPlanFeatureDto
    {
        /// <summary>Özellik tanım kodu (orn. "WHATSAPP_REMINDER").</summary>
        public string FeatureCode { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        /// <summary>Dönemsel kota; null = sınırsız.</summary>
        public int? IncludedQuota { get; set; }
    }
}
