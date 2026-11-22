using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminSubscriptionPlans
{
    /// <summary>
    /// Tum abonelik planlarini, plan basina dahil ozellik listesiyle
    /// getirir (dokuman Modul 13). Plan-ozellik eslesmesi PlanFeature
    /// kayitlari ile FeatureDefinition uzerinden bellek icinde kurulur.
    /// </summary>
    public class GetAdminSubscriptionPlansQuery : IRequest<Result<IReadOnlyList<AdminSubscriptionPlanDto>>>
    {
    }

    /// <summary>Plan ve ozellik setinin admin gorunumu.</summary>
    public class AdminSubscriptionPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Domain.Enums.EnumBillingPeriod BillingPeriod { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Plana dahil ozellikler; kod bazinda eslesmis halde.</summary>
        public IReadOnlyList<AdminPlanFeatureDto> Features { get; set; } = Array.Empty<AdminPlanFeatureDto>();
    }

    /// <summary>Plana dahil tek ozellik satiri.</summary>
    public class AdminPlanFeatureDto
    {
        /// <summary>Ozellik tanim kodu (orn. "WHATSAPP_REMINDER").</summary>
        public string FeatureCode { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        /// <summary>Donemsel kota; null = sinirsiz.</summary>
        public int? IncludedQuota { get; set; }
    }
}
